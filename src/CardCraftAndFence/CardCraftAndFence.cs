using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace DeepSpaceQoL
{
    /// <summary>
    /// CardCraftAndFence. P1: item directory + ten T2 items (five keycards, five
    /// crypto modules). P2: crates, tiered lock prefix, loot tables. P3: blank
    /// keycard printer chip, professional desequencer and the batch card module.
    /// P4: the retired cardmaker client, Dr. Jackson's card stock, the thief's
    /// graded-box offer, the four Wild Network "CardFence" upgrades that gate
    /// those channels, and the T3 crates.
    /// </summary>
    public sealed class CardCraftAndFenceMod : IMod
    {
        internal const string ModId = "deepspace.card_craft_and_fence";
        internal const string ModVersion = "0.5.0";

        public void Init(ModManifest manifest)
        {
            CfItemDirectory.EnsureCreated();
            Log.Info("Init " + manifest.ID + " v" + manifest.ModVersion);
        }

        public void OnEnable()
        {
            CfItemDirectory.EnsureCreated();
            LootTables.EnsureRegistered();
            CfNetwork.EnsureRegistered();
            // -= before += so a mod reload cannot subscribe the handler twice.
            ModHook.OnGenerateCustomerVeryLate -= CfRetiredCardmaker.OnGenerateCustomerVeryLate;
            ModHook.OnGenerateCustomerVeryLate += CfRetiredCardmaker.OnGenerateCustomerVeryLate;
            Log.Info("enabled v" + ModVersion);
        }

        public void OnDisable()
        {
            ModHook.OnGenerateCustomerVeryLate -= CfRetiredCardmaker.OnGenerateCustomerVeryLate;
            // Directory<GameItem> is a DontDestroyOnLoad singleton. Owned items may
            // still need its factories after the mod is disabled, so never destroy it.
            Log.Info("disabled; CfItemDirectory kept alive");
        }
    }

    // Tier gate for the new cf_ containers. Vanilla crates have no CF_CRATE_TIER
    // tag and keep the original LockHelper behaviour.
    [HarmonyPatch(typeof(LockHelper), nameof(LockHelper.TryOpenLock))]
    internal static class CfTryOpenLockPatch
    {
        private static bool Prefix(GameItem key, GameItem box, string keyLockId)
        {
            if (box == null || !box.IsTag("CF_CRATE_TIER"))
            {
                return true;
            }
            if (!LockHelper.IsCurrentlyLocked(box))
            {
                return false;
            }

            CardFamily cardFamily;
            int cardTier;
            if (!ItemFactory.TryGetCardInfo(key, out cardFamily, out cardTier))
            {
                // Any other access card must not fall back to vanilla: vanilla's
                // LOCK_ID_COMMAND rule would let sci/sup/blank cards open cf crates.
                if (key != null && key.IsGameItemType("ACCESS_CARD"))
                {
                    Log.Info("[CF-LOCK] key=" + key.identifier
                        + " keyLockId=" + keyLockId
                        + " box=" + box.identifier
                        + " card=<unknown> canOpen=False");
                    EmporiumAudioPlayer.Instance.audioNegativeTwoBeep.Play();
                    return false;
                }
                return true;
            }

            CardFamily crateFamily;
            if (!Enum.TryParse(box.GetTagReadonly("CF_CRATE_FAMILY").GetString(), out crateFamily))
            {
                return true;
            }
            int crateTier = box.GetTagReadonly("CF_CRATE_TIER").GetInt();
            bool canOpen = cardTier >= crateTier
                && (cardFamily == CardFamily.CMD || cardFamily == crateFamily);
            Log.Info("[CF-LOCK] key=" + key.identifier
                + " keyLockId=" + keyLockId
                + " box=" + box.identifier
                + " card=" + cardFamily + "/" + cardTier
                + " crate=" + crateFamily + "/" + crateTier
                + " canOpen=" + canOpen);
            if (canOpen)
            {
                EmporiumAudioPlayer.Instance.audioClickClick.Play();
                key.Destroy();
                LockHelper.UnlockContainer(box);
            }
            else
            {
                EmporiumAudioPlayer.Instance.audioNegativeTwoBeep.Play();
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(LockHelper), nameof(LockHelper.CreateLockerContainerTooltip))]
    internal static class CfCrateTooltipPatch
    {
        private static void Postfix(RichTextBuilder builder, GameItem item)
        {
            if (builder == null || item == null || !item.IsTag("CF_CRATE_TIER"))
            {
                return;
            }
            CardFamily family;
            if (!Enum.TryParse(item.GetTagReadonly("CF_CRATE_FAMILY").GetString(), out family))
            {
                return;
            }
            int tier = item.GetTagReadonly("CF_CRATE_TIER").GetInt();
            builder.AddLine(
                "需要：" + ItemFactory.CardName(family, tier) + " 或更高",
                true,
                RenderHandler.ColorPalette.LightCyan);
            builder.AddLine(
                "指挥卡可通用，但等级需 ≥ 箱子",
                true,
                RenderHandler.ColorPalette.LightCyan);
        }
    }

    // Fill after DirectoryMaster.Item has returned: at that point the crate is
    // owned and has its unique id. Filling inside BuildContainer happened before
    // SetItemOwned, so the crate rejected every child (remaining=1).
    [HarmonyPatch(typeof(DirectoryMaster), nameof(DirectoryMaster.Item), new Type[] { typeof(string), typeof(bool) })]
    internal static class CfCrateFillPatch
    {
        private static void Postfix(GameItem __result)
        {
            if (__result == null
                || !__result.IsTag("CF_CRATE_TIER")
                || __result.IsTag("CF_CRATE_FILLED")
                || SaveManager.IsDecoding)
            {
                return;
            }
            CardFamily family;
            if (!Enum.TryParse(__result.GetTagReadonly("CF_CRATE_FAMILY").GetString(), out family))
            {
                return;
            }
            int tier = __result.GetTagReadonly("CF_CRATE_TIER").GetInt();
            LootTables.Diag("postfix:" + family + tier,
                "[CF-LOOT] postfix fill " + family + " T" + tier
                + " owned=" + GeneralHelper.IsItemOwned(__result));
            LootTables.Fill(__result, family, tier);
            LockHelper.LockUpContainer(__result);
        }
    }

    public enum CardFamily
    {
        SEC,
        MED,
        ENG,
        SER,
        CMD
    }

    /// <summary>
    /// Self-owned ItemDirectory. It is created once from IMod.Init and kept alive
    /// across scenes; it does not rely on OnModItemDirectoryInit.
    /// </summary>
    public sealed class CfItemDirectory : ItemDirectory
    {
        private const string ProbeItemId = "cf_sec_keycard_t2";

        private static GameObject _root;

        internal static CfItemDirectory Instance;

        internal static void EnsureCreated()
        {
            if (_root != null)
            {
                return;
            }
            _root = new GameObject("CfItemDirectory");
            UnityEngine.Object.DontDestroyOnLoad(_root);
            Instance = _root.AddComponent<CfItemDirectory>();
            if (!DirectoryMaster.Has<GameItem>(ProbeItemId))
            {
                Log.Error("CfItemDirectory was created but " + ProbeItemId + " is not registered.");
            }
        }

        protected override void InitDirectory()
        {
            Instance = this;
            CardFamily[] families =
            {
                CardFamily.SEC,
                CardFamily.MED,
                CardFamily.ENG,
                CardFamily.SER,
                CardFamily.CMD
            };
            foreach (CardFamily family in families)
            {
                // Capture per iteration so every closure keeps its own family.
                CardFamily captured = family;
                string cardId = ItemFactory.KeycardId(captured, 2);
                string coreId = ItemFactory.CryptoModuleId(captured, 2);
                string boxId = ItemFactory.ContainerId(captured, 2);
                Add(cardId, () => ItemFactory.BuildKeycard(captured, 2));
                Add(coreId, () => ItemFactory.BuildCryptoModule(captured, 2));
                Add(boxId, () => ItemFactory.BuildContainer(captured, 2));
                // P4: T3 cards, cores and crates. The crates are 9x8 (CMD 10x8),
                // 300/500, and are only sold by the thief at high black-market rep.
                Add(ItemFactory.KeycardId(captured, 3),
                    () => ItemFactory.BuildKeycard(captured, 3));
                Add(ItemFactory.CryptoModuleId(captured, 3),
                    () => ItemFactory.BuildCryptoModule(captured, 3));
                Add(ItemFactory.ContainerId(captured, 3),
                    () => ItemFactory.BuildContainer(captured, 3));
            }
            // CMD has no vanilla T1 crate, so this one is new as well.
            Add(ItemFactory.ContainerId(CardFamily.CMD, 1),
                () => ItemFactory.BuildContainer(CardFamily.CMD, 1));

            // P3: blank-keycard printer chip, professional desequencer and the
            // batch card module. All three are plain items; only the printer
            // batch behaviour needs a patch (see CfPrinterBatchPatch).
            Add(ItemFactory.BlankKeycardChipId, () => ItemFactory.BuildBlankKeycardChip());
            Add(ItemFactory.ProDesequencerId, () => ItemFactory.BuildProDesequencer());
            Add(ItemFactory.BatchCardModuleId, () => ItemFactory.BuildBatchCardModule());
        }
    }

    /// <summary>
    /// Shared id/price/work lookup and item construction for P1. Tier 1 keeps the
    /// vanilla ids; tier 2 uses the CardCraftAndFence ids. Anything else throws.
    /// </summary>
    internal static class ItemFactory
    {
        internal const string DesequencerTargetTag = "DESEQUENCER_TARGET_ID_TAG";

        // P3 item ids. Keep them in one place: the patch classes and the
        // desequencer cycle compare against these strings.
        internal const string BlankKeycardChipId = "cf_printer_chip_blank_keycard";
        internal const string ProDesequencerId = "cf_pro_desequencer";
        internal const string BatchCardModuleId = "cf_batch_card_module";
        internal const string BatchModuleTag = "CF_CARD_BATCH_MODULE_TAG";
        internal const string WipTag = "CF_CARD_WIP_TAG";
        internal const string BlankCardId = "blank_keycard";

        public static string KeycardId(CardFamily family, int tier)
        {
            RequireCardTier(tier);
            if (tier == 3)
            {
                return "cf_" + Slug(family) + "_keycard_t3";
            }
            switch (family)
            {
                case CardFamily.SEC:
                    return tier == 1 ? "sec_keycard" : "cf_sec_keycard_t2";
                case CardFamily.MED:
                    return tier == 1 ? "med_keycard" : "cf_med_keycard_t2";
                case CardFamily.ENG:
                    return tier == 1 ? "eng_keycard" : "cf_eng_keycard_t2";
                case CardFamily.SER:
                    return tier == 1 ? "ser_keycard" : "cf_ser_keycard_t2";
                case CardFamily.CMD:
                    return tier == 1 ? "cmd_keycard" : "cf_cmd_keycard_t2";
                default:
                    throw new ArgumentOutOfRangeException(nameof(family), family, "Unknown card family.");
            }
        }

        public static string CryptoModuleId(CardFamily family, int tier)
        {
            RequireCardTier(tier);
            if (tier == 3)
            {
                return "cf_" + Slug(family) + "_crypto_module_t3";
            }
            switch (family)
            {
                case CardFamily.SEC:
                    return tier == 1 ? "crypto_module_sec" : "cf_crypto_module_sec_t2";
                case CardFamily.MED:
                    return tier == 1 ? "crypto_module_med" : "cf_crypto_module_med_t2";
                case CardFamily.ENG:
                    return tier == 1 ? "crypto_module_eng" : "cf_crypto_module_eng_t2";
                case CardFamily.SER:
                    return tier == 1 ? "crypto_module_ser" : "cf_crypto_module_ser_t2";
                case CardFamily.CMD:
                    return tier == 1 ? "crypto_module_cmd" : "cf_crypto_module_cmd_t2";
                default:
                    throw new ArgumentOutOfRangeException(nameof(family), family, "Unknown card family.");
            }
        }

        public static GameItem NewKeycard(CardFamily family, int tier)
        {
            return DirectoryMaster.Item(KeycardId(family, tier));
        }

        public static GameItem NewCryptoModule(CardFamily family, int tier)
        {
            return DirectoryMaster.Item(CryptoModuleId(family, tier));
        }

        public static int CardPrice(CardFamily family, int tier)
        {
            RequireCardTier(tier);
            switch (family)
            {
                case CardFamily.SEC:
                    return tier == 1 ? 100 : (tier == 2 ? 150 : 225);
                case CardFamily.MED:
                    return tier == 1 ? 75 : (tier == 2 ? 110 : 165);
                case CardFamily.ENG:
                    return tier == 1 ? 75 : (tier == 2 ? 110 : 165);
                case CardFamily.SER:
                    return tier == 1 ? 40 : (tier == 2 ? 60 : 90);
                case CardFamily.CMD:
                    return tier == 1 ? 125 : (tier == 2 ? 200 : 300);
                default:
                    throw new ArgumentOutOfRangeException(nameof(family), family, "Unknown card family.");
            }
        }

        public static int CorePrice(CardFamily family, int tier)
        {
            RequireCardTier(tier);
            switch (family)
            {
                case CardFamily.SEC:
                    return tier == 1 ? 300 : (tier == 2 ? 500 : 750);
                case CardFamily.MED:
                    return tier == 1 ? 250 : (tier == 2 ? 400 : 600);
                case CardFamily.ENG:
                    return tier == 1 ? 250 : (tier == 2 ? 400 : 600);
                case CardFamily.SER:
                    return tier == 1 ? 200 : (tier == 2 ? 300 : 450);
                case CardFamily.CMD:
                    return tier == 1 ? 350 : (tier == 2 ? 600 : 900);
                default:
                    throw new ArgumentOutOfRangeException(nameof(family), family, "Unknown card family.");
            }
        }

        public static int CoreWork(CardFamily family, int tier)
        {
            RequireCardTier(tier);
            switch (family)
            {
                case CardFamily.SEC:
                    return tier == 1 ? 125 : (tier == 2 ? 200 : 300);
                case CardFamily.MED:
                    return tier == 1 ? 100 : (tier == 2 ? 150 : 250);
                case CardFamily.ENG:
                    return tier == 1 ? 100 : (tier == 2 ? 150 : 250);
                case CardFamily.SER:
                    // Vanilla crypto_module_ser is 75 (ModuleDirectory.cs:441), not 100.
                    return tier == 1 ? 75 : (tier == 2 ? 150 : 250);
                case CardFamily.CMD:
                    return tier == 1 ? 150 : (tier == 2 ? 250 : 350);
                default:
                    throw new ArgumentOutOfRangeException(nameof(family), family, "Unknown card family.");
            }
        }

        public static string ContainerId(CardFamily family, int tier)
        {
            RequireTier(tier);
            if (tier >= 2)
            {
                return "cf_" + Slug(family) + "_box_t" + tier;
            }
            switch (family)
            {
                case CardFamily.SEC: return "sec_box";
                case CardFamily.MED: return "med_box";
                case CardFamily.ENG: return "eng_box";
                case CardFamily.SER: return "service_box";
                case CardFamily.CMD: return "cf_cmd_box_t1";
                default:
                    throw new ArgumentOutOfRangeException(nameof(family), family, "Unknown card family.");
            }
        }

        public static int ContainerPrice(CardFamily family, int tier)
        {
            RequireTier(tier);
            if (tier == 3)
            {
                return family == CardFamily.CMD ? 500 : 300;
            }
            if (tier == 2)
            {
                return family == CardFamily.CMD ? 350 : 225;
            }
            // T1 SEC/MED/ENG/SER are vanilla items; their prices are 200/200/200/175.
            switch (family)
            {
                case CardFamily.SEC:
                case CardFamily.MED:
                case CardFamily.ENG:
                    return 200;
                case CardFamily.SER:
                    return 175;
                case CardFamily.CMD:
                    return 200;
                default:
                    throw new ArgumentOutOfRangeException(nameof(family), family, "Unknown card family.");
            }
        }

        private static void ContainerSize(CardFamily family, int tier, out int width, out int height)
        {
            if (tier == 1)
            {
                width = 7;
                height = 7;
                return;
            }
            if (tier == 3)
            {
                // T3 boxes: 12 items per box (11 rolls + 1 rare), so the normal
                // families get 9x8 and CMD gets 10x8.
                width = family == CardFamily.CMD ? 10 : 9;
                height = 8;
                return;
            }
            if (family == CardFamily.MED)
            {
                width = 8;
                height = 8;
                return;
            }
            if (family == CardFamily.CMD)
            {
                width = 9;
                height = 8;
                return;
            }
            width = 7;
            height = 7;
        }

        public static GameItem BuildContainer(CardFamily family, int tier)
        {
            RequireTier(tier);
            if (tier == 1 && family != CardFamily.CMD)
            {
                return DirectoryMaster.Item(ContainerId(family, 1));
            }

            int width;
            int height;
            ContainerSize(family, tier, out width, out height);
            var (window, inventory) = DirectoryUtils.CreateInventoryWindow(width, height);
            GameItem item = ItemDirectory.CreateEmptyItem();
            item.SetContentWindow(window);
            item.SetSpriteAndShape("Items/items_container", ContainerSprite(family));
            item.name = LocHelper.GetLocalizedOrDefault(
                "Item",
                "item_cf_" + Slug(family) + "_box_t" + tier + "_name",
                TierName(tier) + Chinese(family) + "补给箱");
            item.shortDescription = LocHelper.GetLocalizedOrDefault(
                "Item",
                "item_cf_" + Slug(family) + "_box_t" + tier + "_desc",
                "锁定；需要" + CardName(family, tier) + "或更高。");
            item.unitValue = ContainerPrice(family, tier);
            LockHelper.InitLockedContainerItem(item, LockId(family), inventory);
            item.ModifyTag("CF_CRATE_FAMILY", delegate(TagState state)
            {
                state.Enable().SetString(Code(family));
            });
            item.ModifyTag("CF_CRATE_TIER", delegate(TagState state)
            {
                state.Enable().SetInt(tier);
            });
            // Loot and LockUpContainer happen in the DirectoryMaster.Item
            // postfix: after the item is owned, and before the crate is locked.
            return item;
        }

        internal static bool TryGetCardInfo(GameItem key, out CardFamily family, out int tier)
        {
            family = CardFamily.SEC;
            tier = 1;
            if (key == null)
            {
                return false;
            }
            if (key.IsTag("CF_CARD_FAMILY"))
            {
                if (!Enum.TryParse(key.GetTagReadonly("CF_CARD_FAMILY").GetString(), out family))
                {
                    return false;
                }
                tier = key.IsTag("CF_CARD_TIER") ? key.GetTagReadonly("CF_CARD_TIER").GetInt() : 1;
                if (tier < 1)
                {
                    tier = 1;
                }
                return true;
            }
            switch (key.identifier)
            {
                case "sec_keycard":
                    family = CardFamily.SEC;
                    return true;
                case "med_keycard":
                    family = CardFamily.MED;
                    return true;
                case "eng_keycard":
                    family = CardFamily.ENG;
                    return true;
                case "ser_keycard":
                    family = CardFamily.SER;
                    return true;
                case "cmd_keycard":
                    family = CardFamily.CMD;
                    return true;
                default:
                    return false;
            }
        }

        internal static string CardName(CardFamily family, int tier)
        {
            return TierName(tier) + Chinese(family) + "钥匙卡";
        }

        internal static string TierName(int tier)
        {
            if (tier == 1) return "初级";
            if (tier == 2) return "中级";
            return "高级";
        }

        public static GameItem BuildKeycard(CardFamily family, int tier)
        {
            RequireCardTier(tier);
            if (tier < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(tier), tier, "T1 cards are vanilla items.");
            }
            string code = Code(family);
            string slug = Slug(family);
            GameItem item = ItemDirectory.CreateEmptyItem();
            item.SetSpriteAndShape("Items/items_misc", CardSprite(family));
            item.name = LocHelper.GetLocalizedOrDefault(
                "Item",
                "item_cf_" + slug + "_keycard_t" + tier + "_name",
                KeycardNameFallback(family, tier));
            item.shortDescription = LocHelper.GetLocalizedOrDefault(
                "Item",
                "item_cf_" + slug + "_keycard_t" + tier + "_desc",
                KeycardDescFallback(family, tier));
            ContrabandHelper.InitContrabandItem(item, CardContraband(family));
            item.SetGameItemType("ACCESS_CARD");
            AudioHelper.InitPlasticCard(item);
            item.unitValue = CardPrice(family, tier);
            item.maySelectSlotItemFunc = (GameItem selected, GameInventory inventory, SlotMarker slot) => true;
            item.canSelectSlotItemFunc = (GameItem selected, GameInventory inventory, SlotMarker slot) => true;
            item.mayItemTargetItemFunc = (GameItem source, GameItem target) =>
                GeneralHelper.IsItemOwned(source) &&
                GeneralHelper.IsItemOwned(target) &&
                LockHelper.IsLockedContainer(target);
            item.canItemTargetItemFunc = (GameItem source, GameItem target) =>
                GeneralHelper.IsItemOwned(source) &&
                GeneralHelper.IsItemOwned(target) &&
                LockHelper.IsLockedContainer(target);
            string lockId = LockId(family);
            item.onItemTargetItemFunc = delegate(GameItem source, GameItem target)
            {
                LockHelper.TryOpenLock(source, target, lockId);
            };
            item.ModifyTag("CF_CARD_FAMILY", delegate(TagState state)
            {
                state.Enable().SetString(code);
            });
            item.ModifyTag("CF_CARD_TIER", delegate(TagState state)
            {
                state.Enable().SetInt(tier);
            });
            return item;
        }

        public static GameItem BuildCryptoModule(CardFamily family, int tier)
        {
            RequireCardTier(tier);
            if (tier < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(tier), tier, "T1 crypto modules are vanilla items.");
            }
            string slug = Slug(family);
            string cardId = KeycardId(family, tier);
            string cardName = LocHelper.GetLocalizedOrDefault(
                "Item",
                "item_cf_" + slug + "_keycard_t" + tier + "_name",
                KeycardNameFallback(family, tier));
            GameItem item = ItemDirectory.CreateEmptyItem();
            item.SetSpriteAndShape("Items/items_tool", CoreSprite(family));
            item.name = LocHelper.GetLocalizedOrDefault(
                "Item",
                "item_cf_" + slug + "_crypto_module_t" + tier + "_name",
                CryptoModuleNameFallback(family, tier));
            item.shortDescription = LocHelper.GetLocalizedOrDefault(
                "Item",
                "item_cf_" + slug + "_crypto_module_t" + tier + "_desc",
                "可在密码解序器中制作" + cardName + "。");
            item.unitValue = CorePrice(family, tier);
            ContrabandHelper.InitContrabandItem(item, CoreContraband(family));
            item.ModifyTag(DesequencerTargetTag, delegate(TagState state)
            {
                state.Enable().SetString(cardId);
            });
            MachineProgressHelper.InitProgressSourceItem(item, CoreWork(family, tier), cardId);
            InitChipSwap(item);
            AudioHelper.InitSmallModule(item);
            return item;
        }

        // P3. Same construction as vanilla ModuleDirectory.CreateMeleeChip /
        // CreateCncChip: a PRINTER_CHIP_TAG item that the 3D printer turns into
        // the target item, consuming additional items from the input tray. The
        // chip itself is never consumed (MachinePrinter.FinishPrinting only
        // unlocks it), so no patch is needed for this item.
        public static GameItem BuildBlankKeycardChip()
        {
            GameItem chip = ItemDirectory.CreateEmptyItem();
            chip.SetSpriteAndShape("Items/items_tool", "printer_chip2");
            chip.name = LocHelper.GetLocalizedOrDefault(
                "Item",
                "item_cf_printer_chip_blank_keycard_name",
                "3D 打印机芯片（空白钥匙卡 x{0}）",
                1);
            chip.shortDescription = LocHelper.GetLocalizedOrDefault(
                "Item",
                "item_cf_printer_chip_blank_keycard_desc",
                "在 3D 打印机中消耗 1 个电子件与 1 个打印耗材，制作 1 张空白钥匙卡。需要 CNC 模块。芯片不会消耗。");
            chip.unitValue = 250L;
            chip.EnableTag("PRINTER_CHIP_TAG");
            MachineProgressHelper.SetDualAdditionalRequiredItem(
                chip,
                ("common_electronic", 1),
                ("printer_plastic", 1));
            MachineProgressHelper.InitProgressSourceItem(
                chip, 40, BlankCardId, useDefaultTooltip: true, targetItemCount: 1);
            MachineProgressHelper.AddRequiredMachineTag(chip, "CNC Module");
            AudioHelper.InitSmallModule(chip);
            return chip;
        }

        // P3. Mirrors ModuleDirectory.PrinterModuleMeleeBatch: a printer module
        // that adds one extra output per cycle. It is MODULE_TYPE_UNIVERSAL so
        // the same item can also sit in the pro desequencer; the -50/-50 pair is
        // what the module framework turns into "speed -50%, power +50%".
        public static GameItem BuildBatchCardModule()
        {
            GameItem module = ItemDirectory.CreateEmptyItem();
            module.SetSpriteAndShape("Items/items_module", "long_module2");
            module.name = LocHelper.GetLocalizedOrDefault(
                "Item", "item_cf_batch_card_module_name", "批量制卡模块");
            module.shortDescription = LocHelper.GetLocalizedOrDefault(
                "Item",
                "item_cf_batch_card_module_desc",
                "装在 3D 打印机或密码解序器上，每轮多产出 1 张卡，同时多消耗 1 份输入。同一台机器只生效一次。");
            module.unitValue = 250L;
            module.EnableTag(BatchModuleTag);
            ModuleHelper.InitModuleItem(
                module,
                "MODULE_TYPE_UNIVERSAL",
                -50,
                -50,
                0,
                isShipModule: true,
                LocHelper.GetLocalizedOrDefault(
                    "Item", "module_effect_cf_batch_card", "每轮制卡产出 +{0}", 1));
            return module;
        }

        public static GameItem BuildProDesequencer()
        {
            return ProDesequencer.Build();
        }

        // One batch module is enough; a second one must not stack. Counting by
        // tag (not by id) also covers future variants, but the cap stays at 2.
        internal static int BatchSize(GameItem machine)
        {
            int count = 0;
            foreach (GameItem module in Modules(machine))
            {
                if (module != null && module.IsTag(BatchModuleTag))
                {
                    count++;
                }
            }
            return count > 0 ? 2 : 1;
        }

        private static List<GameItem> Modules(GameItem machine)
        {
            try
            {
                return MachineHelper.GetModules(machine);
            }
            catch (Exception e)
            {
                Log.Warn("[CF-BATCH] cannot read the module bay: " + e.GetType().Name + ": " + e.Message);
                return new List<GameItem>();
            }
        }

        private static void RequireTier(int tier)
        {
            if (tier != 1 && tier != 2 && tier != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(tier), tier, "CardCraftAndFence supports tiers 1, 2 and 3.");
            }
        }

        // Cards and cores exist at tier 1 (vanilla), 2 and 3 (P1/P4). Containers
        // stay on RequireTier: there is no T3 crate yet.
        private static void RequireCardTier(int tier)
        {
            if (tier != 1 && tier != 2 && tier != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(tier), tier, "CardCraftAndFence supports card/core tiers 1, 2 and 3.");
            }
        }

        private static string Code(CardFamily family)
        {
            switch (family)
            {
                case CardFamily.SEC: return "SEC";
                case CardFamily.MED: return "MED";
                case CardFamily.ENG: return "ENG";
                case CardFamily.SER: return "SER";
                case CardFamily.CMD: return "CMD";
                default: throw new ArgumentOutOfRangeException(nameof(family), family, "Unknown card family.");
            }
        }

        private static string Slug(CardFamily family)
        {
            return Code(family).ToLowerInvariant();
        }

        private static string Chinese(CardFamily family)
        {
            switch (family)
            {
                case CardFamily.SEC: return "治安";
                case CardFamily.MED: return "医疗";
                case CardFamily.ENG: return "工程";
                case CardFamily.SER: return "服务";
                case CardFamily.CMD: return "指挥";
                default: throw new ArgumentOutOfRangeException(nameof(family), family, "Unknown card family.");
            }
        }

        private static string KeycardNameFallback(CardFamily family, int tier)
        {
            return TierName(tier) + Chinese(family) + "钥匙卡";
        }

        private static string CryptoModuleNameFallback(CardFamily family, int tier)
        {
            return TierName(tier) + Chinese(family) + "加密模块";
        }

        private static string KeycardDescFallback(CardFamily family, int tier)
        {
            string tierName = TierName(tier);
            if (family == CardFamily.CMD)
            {
                return "可开启任何家族的" + tierName + "或更低等级锁。";
            }
            return "可开启" + tierName + Chinese(family) + "锁。指挥卡可通用于任何家族，但等级不能低于锁。";
        }

        private static string CardSprite(CardFamily family)
        {
            switch (family)
            {
                case CardFamily.SEC: return "sec_keycard";
                case CardFamily.MED: return "med_card";
                case CardFamily.ENG: return "eng_card";
                case CardFamily.SER: return "ser_card";
                case CardFamily.CMD: return "command_keycard";
                default: throw new ArgumentOutOfRangeException(nameof(family), family, "Unknown card family.");
            }
        }

        private static string CoreSprite(CardFamily family)
        {
            switch (family)
            {
                case CardFamily.SEC: return "sec_module";
                case CardFamily.MED: return "med_module";
                case CardFamily.ENG: return "eng_module";
                case CardFamily.SER: return "ser_module";
                case CardFamily.CMD: return "cmd_module";
                default: throw new ArgumentOutOfRangeException(nameof(family), family, "Unknown card family.");
            }
        }

        private static string ContainerSprite(CardFamily family)
        {
            switch (family)
            {
                case CardFamily.SEC: return "sec_box";
                case CardFamily.MED: return "med_box";
                case CardFamily.ENG: return "eng_box";
                case CardFamily.SER: return "service_box";
                case CardFamily.CMD: return "sec_box";
                default:
                    throw new ArgumentOutOfRangeException(nameof(family), family, "Unknown card family.");
            }
        }

        private static string LockId(CardFamily family)
        {
            switch (family)
            {
                case CardFamily.SEC: return "LOCK_ID_SEC";
                case CardFamily.MED: return "LOCK_ID_MED";
                case CardFamily.ENG: return "LOCK_ID_ENG";
                case CardFamily.SER: return "LOCK_ID_SER";
                case CardFamily.CMD: return "LOCK_ID_COMMAND";
                default: throw new ArgumentOutOfRangeException(nameof(family), family, "Unknown card family.");
            }
        }

        private static int CardContraband(CardFamily family)
        {
            switch (family)
            {
                case CardFamily.SEC: return 2;
                case CardFamily.MED: return 1;
                case CardFamily.ENG: return 1;
                case CardFamily.SER: return 1;
                case CardFamily.CMD: return 3;
                default: throw new ArgumentOutOfRangeException(nameof(family), family, "Unknown card family.");
            }
        }

        private static int CoreContraband(CardFamily family)
        {
            switch (family)
            {
                case CardFamily.SEC: return 3;
                case CardFamily.MED: return 2;
                case CardFamily.ENG: return 2;
                case CardFamily.SER: return 1;
                case CardFamily.CMD: return 4;
                default: throw new ArgumentOutOfRangeException(nameof(family), family, "Unknown card family.");
            }
        }

        // Copied from vanilla ModuleDirectory.CrypoModule*: lets a crypto module be
        // dragged onto another crypto module in a different inventory, swapping them.
        private static bool MayChipSwap(GameItem source, GameItem target)
        {
            if (source == null || target == null || source == target)
            {
                return false;
            }
            if (!source.IsTag(DesequencerTargetTag))
            {
                return false;
            }
            if (!target.IsTag(DesequencerTargetTag))
            {
                return false;
            }
            if (source.IsGameItemType("MACHINE") || target.IsGameItemType("MACHINE"))
            {
                return false;
            }
            if (target.MayStack(source))
            {
                return false;
            }
            if (source.parentInventory == null || target.parentInventory == null)
            {
                return false;
            }
            if (source.parentInventory == target.parentInventory)
            {
                return false;
            }
            return true;
        }

        private static void InitChipSwap(GameItem item)
        {
            item.canUseOutsideCombat = true;
            item.mayThisTargetItemFunc = (Func<GameItem, GameItem, bool>)Delegate.Combine(
                item.mayThisTargetItemFunc,
                new Func<GameItem, GameItem, bool>(MayChipSwap));
            item.canThisTargetItemFunc = (Func<GameItem, GameItem, bool>)Delegate.Combine(
                item.canThisTargetItemFunc,
                new Func<GameItem, GameItem, bool>(MayChipSwap));
            item.onThisTargetItemFunc = (Action<GameItem, GameItem>)Delegate.Combine(
                item.onThisTargetItemFunc,
                (Action<GameItem, GameItem>)delegate(GameItem source, GameItem target)
                {
                    if (MayChipSwap(source, target))
                    {
                        GraphUtils.SwapItemsInPlace(source, target);
                    }
                });
        }
    }

    /// <summary>
    /// Shared batch rules for the printer and the pro desequencer.
    ///
    /// Batch size is 1, or 2 when a CF_CARD_BATCH_MODULE_TAG module sits in the
    /// machine's module bay (a second one is ignored). Power and speed are left
    /// entirely to ModuleHelper: the batch module is a -50 performance / -50
    /// efficiency module, so the framework produces "half speed, 1.5x power" and
    /// this class never multiplies any coefficient itself.
    /// </summary>
    internal static class CardBatch
    {
        // Printer machinery panels, in MachinePrinter.CreateMachineInventoryWindow
        // order: 0 power, 1 module, 2 input, 3 chip, 4 output, 5 note.
        private const int PrinterInputPanel = 2;

        // Non-destructive "does one more of this item fit" probe. The probe item
        // is created and destroyed; nothing is placed in the target inventory.
        //
        // probeId MUST be an item the target inventory actually accepts: the probe
        // goes through the same mayInventoryAddItemFunc filter as a real insert. A
        // probe with the wrong id silently answers "no room" forever (the pro
        // desequencer's output rejects blank_keycard by design, so probing with a
        // blank card would dead-lock the machine).
        internal static bool OutputHasRoom(GameInventory output, string probeId)
        {
            if (output == null || string.IsNullOrEmpty(probeId))
            {
                return false;
            }
            GameItem probe = DirectoryMaster.Item(probeId);
            if (probe == null)
            {
                return false;
            }
            bool fits;
            try
            {
                fits = GraphUtils.CanAccept(output, probe);
            }
            finally
            {
                probe.Destroy();
            }
            return fits;
        }

        // How many complete sets of the chip's additional items are in the tray.
        // One set was already consumed by MachinePrinter before the print starts,
        // so the caller adds 1 for that set.
        internal static int AvailableSets(GameItem chip, List<GameItem> inventory)
        {
            if (chip == null || inventory == null || !chip.IsTag("PROGRESS_ITEM_USE_ADDITIONAL_ITEMS"))
            {
                return 0;
            }
            int types = chip.IsTag("REQUIRED_ITEM_TYPE_COUNT_TAG")
                ? chip.GetTagReadonly("REQUIRED_ITEM_TYPE_COUNT_TAG").GetInt()
                : 0;
            if (types <= 0)
            {
                return 0;
            }
            int sets = int.MaxValue;
            for (int i = 0; i < types; i++)
            {
                string idTag = "REQUIRED_ITEM_ID_" + i;
                string countTag = "REQUIRED_ITEM_COUNT_" + i;
                if (!chip.IsTag(idTag) || !chip.IsTag(countTag))
                {
                    continue;
                }
                string itemId = chip.GetTagReadonly(idTag).GetString();
                int need = chip.GetTagReadonly(countTag).GetInt();
                if (string.IsNullOrEmpty(itemId) || need <= 0)
                {
                    continue;
                }
                int have = 0;
                foreach (GameItem item in inventory)
                {
                    if (item != null && item.identifier == itemId)
                    {
                        have++;
                    }
                }
                sets = Math.Min(sets, have / need);
            }
            return sets == int.MaxValue ? 0 : sets;
        }

        // Prefix on MachinePrinter.FinishPrinting for the blank-keycard chip.
        // Returns false to abort the vanilla finish: progress is kept, nothing is
        // produced and nothing extra is consumed (the same "stay put" idea as the
        // printer's own power gate, applied to a full output tray).
        internal static bool OnPrinterFinish(GameItem machine, GameSlotInventory chipInv, GameGridInventory output)
        {
            try
            {
                if (machine == null || chipInv == null || output == null)
                {
                    return true;
                }
                GameItem chip = chipInv.childItem;
                if (chip == null || !ItemFactory.BlankKeycardChipId.Equals(chip.identifier))
                {
                    return true;
                }
                int batch = ItemFactory.BatchSize(machine);
                if (batch <= 1)
                {
                    return true;
                }
                GameInventory tray = MachineHelper.GetSlot(machine, PrinterInputPanel);
                if (tray == null)
                {
                    return true;
                }
                List<GameItem> trayItems = tray.childItems;
                int sets = AvailableSets(chip, trayItems);
                int extra = Math.Min(batch, sets + 1) - 1;
                if (extra <= 0)
                {
                    return true;
                }

                // Place the extra cards first, then probe one more slot for the
                // card vanilla is about to add. Probing before placing cannot see
                // the extra cards, so this order is what makes "output full"
                // detection exact instead of optimistic.
                List<GameItem> placed = new List<GameItem>();
                for (int i = 0; i < extra; i++)
                {
                    GameItem card = DirectoryMaster.Item(ItemFactory.BlankCardId);
                    if (card == null)
                    {
                        break;
                    }
                    if (GraphUtils.TryAcceptAll(output, card) > 0)
                    {
                        card.Destroy();
                        break;
                    }
                    placed.Add(card);
                }
                bool fits = placed.Count == extra && OutputHasRoom(output, ItemFactory.BlankCardId);
                if (!fits)
                {
                    foreach (GameItem card in placed)
                    {
                        card.Destroy();
                    }
                    Log.Info("[CF-PRINT] output full: batch deferred batch=" + batch
                        + " sets=" + sets + " placed=" + placed.Count);
                    return false;
                }

                // One extra set per extra card. ConsumeRequiredAdditionalItems
                // destroys exactly one set (REQUIRED_ITEM_ID_*/COUNT_*), so it is
                // reused instead of duplicating the item-matching rules. The
                // snapshot must be re-read every time: it still contains the items
                // the previous call just destroyed, and a second Destroy() is a
                // no-op, which would consume one set but report two.
                for (int i = 0; i < extra; i++)
                {
                    MachineProgressHelper.ConsumeRequiredAdditionalItems(chip, tray.childItems);
                }
                Log.Info("[CF-PRINT] batch=" + batch + " extra=" + extra
                    + " setsBefore=" + sets + " setsAfter=" + (sets - extra));
                return true;
            }
            catch (Exception e)
            {
                Log.Error("[CF-PRINT] batch hook failed: " + e.GetType().Name + ": " + e.Message);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(MachinePrinter), nameof(MachinePrinter.FinishPrinting))]
    internal static class CfPrinterBatchPatch
    {
        private static bool Prefix(GameItem item, GameSlotInventory chipInv, GameGridInventory itemInventoryRight)
        {
            return CardBatch.OnPrinterFinish(item, chipInv, itemInventoryRight);
        }
    }

    /// <summary>
    /// P3 professional desequencer (cf_pro_desequencer). Built like
    /// MachineDesequencer.Desequencer() — same panels, tags, InitProgressTypeMachine
    /// and InitMachinery calls — but the input and output are small grids instead
    /// of single slots so two blank cards can be loaded at once. Progress and
    /// state live on the machine's own tags (MACHINE_STATE_TAG,
    /// MACHINE_PROGRESS_* and DESEQUENCER_*), never in mod-side storage, so a save
    /// and reload keeps a half-finished card.
    ///
    /// Per night the game calls onCycleEndSlotItemFunc exactly once per item, so
    /// there is no Update() polling and no timestamp bookkeeping here.
    /// </summary>
    internal static class ProDesequencer
    {
        internal const string StateTag = "DESEQUENCER_STATE_TAG";
        internal const string ProgressTag = "DESEQUQUENCER_PROGRESS_TAG";
        internal const string StateWorking = "STATE_DESEQUENCING";
        internal const string StateReady = "STATE_READY";

        internal static GameItem Build()
        {
            // Same window as the 3D printer (MachinePrinter.CreateMachineInventoryWindow):
            // power, module bay, 9x6 input, chip, 9x5 output, note. The only
            // intended difference is the chip slot: two cells wide instead of one,
            // so a crypto module is not squeezed into a single cell.
            GameSlotInventory powerInv = new GameSlotInventory();
            GameGridInventory moduleInv = new GameGridInventory("10000010001000001110000010001000001", 7);
            GameGridInventory inputGrid = new GameGridInventory(9, 6);
            GameSlotInventory chipSlot = new GameSlotInventory(2, 1);
            GameGridInventory outputGrid = new GameGridInventory(9, 5);
            GameSlotInventory noteSlot = new GameSlotInventory();
            noteSlot.mayInventoryAddItemFunc = (GameItem item, GameInventory _) => item.identifier.Equals("postit");
            noteSlot.SetBackgroundFadeSprite("Items/items_tool", "postit");
            PixelWindow window = MachineHelper.CreateMachineryWindow(
                true,
                MachineHelper.PowerPanel(powerInv),
                MachineHelper.Panel(LocHelper.GetLocalizedMechanic("mech_desequencer_label_module"), moduleInv, "module"),
                MachineHelper.Panel(LocHelper.GetLocalizedMechanic("mech_desequencer_label_input"), inputGrid, "input"),
                MachineHelper.Panel(LocHelper.GetLocalizedMechanic("mech_desequencer_label_chip"), chipSlot, "chip"),
                MachineHelper.Panel(LocHelper.GetLocalizedMechanic("mech_desequencer_label_output"), outputGrid, "output"),
                MachineHelper.NotePanel(noteSlot));
            inputGrid.slotPriorityFunc = (GameItem _, GameInventory _, SlotMarker _) => 2;
            outputGrid.slotPriorityFunc = (GameItem _, GameInventory _, SlotMarker _) => 1;

            chipSlot.mayInventoryAddItemFunc = delegate(GameItem item, GameInventory inventory)
            {
                if (!GeneralHelper.IsItemOwned(item))
                {
                    return false;
                }
                return !item.IsGameItemType("MACHINE") && item.IsTag(ItemFactory.DesequencerTargetTag);
            };
            chipSlot.SetBackgroundFadeSprite("Items/items_tool", "sec_module");
            inputGrid.mayInventoryAddItemFunc = (GameItem item, GameInventory inventory) =>
                GeneralHelper.IsItemOwned(item) && ItemFactory.BlankCardId.Equals(item.identifier);
            outputGrid.mayInventoryAddItemFunc = delegate(GameItem item, GameInventory inventory)
            {
                if (!GeneralHelper.IsItemOwned(item))
                {
                    return false;
                }
                return !ItemFactory.BlankCardId.Equals(item.identifier)
                    && item.IsGameItemType("ACCESS_CARD");
            };
            chipSlot.onSlotAddItemFunc = (Action<GameItem, GameInventory, SlotMarker>)Delegate.Combine(
                chipSlot.onSlotAddItemFunc,
                (Action<GameItem, GameInventory, SlotMarker>)delegate
                {
                    AudioManager.Instance.Play3("module_insert");
                });
            chipSlot.onSlotRemoveItemFunc = (Action<GameItem, GameInventory, SlotMarker>)Delegate.Combine(
                chipSlot.onSlotRemoveItemFunc,
                (Action<GameItem, GameInventory, SlotMarker>)delegate
                {
                    AudioManager.Instance.Play3("module_remove");
                });

            GameItem machine = ItemDirectory.CreateEmptyItem()
                .SetContentWindow(window)
                .SetSpriteAndShape("Items/items_tool", "desequencer");
            machine.name = LocHelper.GetLocalizedOrDefault(
                "Item", "item_cf_pro_desequencer_name", "高级密码解序器");
            machine.flavorText = LocHelper.GetLocalizedOrDefault(
                "Item", "item_cf_pro_desequencer_flavor", "双槽进料：一张空白卡进去，一张写好的卡出来。");
            machine.shortDescription = LocHelper.GetLocalizedOrDefault(
                "Item",
                "item_cf_pro_desequencer_desc",
                "装入加密模块与空白钥匙卡，再插上电池。每晚解序一次，产出对应的钥匙卡；加密模块不会被消耗。");
            machine.unitValue = 800L;
            MachineHelper.SetupBatterySlot(powerInv, machine);
            machine.SetGameItemType("MACHINE");
            machine.EnableTag("CONTAINER_TAG");
            machine.EnableTag("manufacture");
            machine.EnableTag("STANDARD_MACHINE_TAG");
            ContrabandHelper.InitContrabandItem(machine, 2);
            machine.ModifyTag(StateTag, delegate(TagState state)
            {
                state.Enable().SetString(StateReady);
            });
            machine.ModifyTag(ProgressTag, delegate(TagState state)
            {
                state.Enable().SetInt(0);
            });
            MachineryHelper.InitMachinery(machine, 20);
            // Vanilla's desequencer hides the generic progress tooltip because
            // MachineDesequencer.CreateMachineDesequencerTooltip already prints
            // the "working / progress" lines for DESEQUENCER_STATE_TAG items.
            MachineProgressHelper.InitProgressTypeMachine(machine, 50, useDefaultTooltip: false);

            machine.onCycleEndSlotItemFunc = delegate(GameItem item, GameInventory parentInventory, SlotMarker slot)
            {
                // onCycleEndSlotItemFunc runs inside PlayerStore's nightly loop for
                // every item; an exception here would abort the remaining items of
                // the night, so the cycle never throws out into the game.
                try
                {
                    Cycle(item, powerInv, chipSlot, inputGrid, outputGrid, moduleInv);
                }
                catch (Exception e)
                {
                    Log.Error("[CF-DESQ] cycle failed: " + e.GetType().Name + ": " + e.Message);
                }
            };
            moduleInv.onSlotAddItemEndFunc = (Action<GameItem, GameInventory, SlotMarker>)Delegate.Combine(
                moduleInv.onSlotAddItemEndFunc,
                (Action<GameItem, GameInventory, SlotMarker>)delegate(GameItem addedItem, GameInventory parentInventory, SlotMarker slot)
                {
                    ModuleEffectHelper.OnAddedModuleToGrid(addedItem, machine, moduleInv.childItems);
                    if (addedItem.IsTag("MODULE_TAG"))
                    {
                        ModuleHelper.ComputeModuleEffect(moduleInv, addedItem, machine);
                        ModuleHelper.ApplyBasicModuleEffect(moduleInv, addedItem, machine);
                    }
                    MachineryHelper.UpdatePowerUsageProcessTypeMachine(machine);
                });
            moduleInv.onSlotRemoveItemEndFunc = (Action<GameItem, GameInventory, SlotMarker>)Delegate.Combine(
                moduleInv.onSlotRemoveItemEndFunc,
                (Action<GameItem, GameInventory, SlotMarker>)delegate(GameItem addedItem, GameInventory parentInventory, SlotMarker slot)
                {
                    ModuleEffectHelper.OnRemovedModuleFromGrid(addedItem, machine, moduleInv.childItems);
                    if (addedItem.IsTag("MODULE_TAG"))
                    {
                        ModuleHelper.ComputeModuleEffect(moduleInv, addedItem, machine);
                        ModuleHelper.ApplyBasicModuleEffect(moduleInv, addedItem, machine);
                    }
                    MachineryHelper.UpdatePowerUsageProcessTypeMachine(machine);
                });
            powerInv.onSlotAddItemEndFunc = delegate
            {
                bool charged = powerInv.childItem != null
                    && PowerHelper.CanDrawPowerSource(
                        powerInv.childItem,
                        MachineryHelper.GetMachinePowerUsage(machine));
                machine.SetSpriteAndShape("Items/items_tool", charged ? "desequencer_r" : "desequencer");
            };
            powerInv.onSlotRemoveItemEndFunc = delegate
            {
                machine.SetSpriteAndShape("Items/items_tool", "desequencer");
            };
            return machine;
        }

        private static void Cycle(
            GameItem machine,
            GameSlotInventory powerInv,
            GameSlotInventory chipSlot,
            GameGridInventory inputGrid,
            GameGridInventory outputGrid,
            GameGridInventory moduleInv)
        {
            if (machine == null || powerInv == null || inputGrid == null || outputGrid == null)
            {
                return;
            }
            int powerUsage = MachineryHelper.GetMachinePowerUsage(machine);
            if (powerInv.childItem == null
                || !PowerHelper.CanDrawPowerSource(powerInv.childItem, powerUsage))
            {
                // No power: keep progress, keep the input cards, draw nothing.
                Diag("nopower", "[CF-DESQ] waiting: no charged battery in the power slot");
                return;
            }
            string state = machine.GetTagReadonly(StateTag).GetString();
            if (state == StateWorking)
            {
                Continue(machine, powerInv, inputGrid, outputGrid, moduleInv, powerUsage);
                return;
            }
            if (state != StateReady)
            {
                return;
            }
            Start(machine, powerInv, chipSlot, inputGrid, outputGrid, moduleInv, powerUsage);
        }

        private static void Start(
            GameItem machine,
            GameSlotInventory powerInv,
            GameSlotInventory chipSlot,
            GameGridInventory inputGrid,
            GameGridInventory outputGrid,
            GameGridInventory moduleInv,
            int powerUsage)
        {
            GameItem chip = chipSlot.childItem;
            if (chip == null || !chip.IsTag(ItemFactory.DesequencerTargetTag))
            {
                Diag("ready-nochip", "[CF-DESQ] waiting: no crypto module in the chip slot");
                return;
            }
            string targetId = chip.GetTagReadonly(ItemFactory.DesequencerTargetTag).GetString();
            if (string.IsNullOrEmpty(targetId))
            {
                Diag("ready-notarget", "[CF-DESQ] waiting: chip has an empty target id");
                return;
            }
            int blanks = CountBlank(inputGrid);
            if (blanks <= 0)
            {
                Diag("ready-noblank", "[CF-DESQ] waiting: no blank keycard in the input grid");
                return;
            }
            int plan = Math.Min(ItemFactory.BatchSize(machine), blanks);
            if (plan <= 0)
            {
                return;
            }
            // Probe with the card that will actually be produced: the output grid
            // rejects blank_keycard, so probing with a blank card answers "no room"
            // forever and the machine would never start.
            if (!CardBatch.OutputHasRoom(outputGrid, targetId))
            {
                Diag("ready-outputfull", "[CF-DESQ] waiting: output full target=" + targetId);
                return;
            }
            PowerHelper.DrawPowerSource(powerInv.childItem, powerUsage);
            MachineHelper.OnMachineActioned(machine, moduleInv);
            machine.ModifyTag(ItemFactory.DesequencerTargetTag, delegate(TagState tag)
            {
                tag.Enable().SetString(targetId);
            });
            machine.ModifyTag(StateTag, delegate(TagState tag)
            {
                tag.Enable().SetString(StateWorking);
            });
            MachineProgressHelper.StartProgressTypeMachine(machine, chip);
            MachineProgressHelper.ContinueProgressTypeMachine(machine);
            MarkWip(machine, inputGrid, plan);
            UpdateProgress(machine, inputGrid);
            var (current, target) = MachineProgressHelper.GetProgress(machine);
            _diag.Clear();
            Log.Info("[CF-DESQ] start plan=" + plan + " target=" + targetId
                + " work=" + target + "/" + current + " speed="
                + machine.GetTagReadonly("CURRENT_PROCESSING_SPEED_TAG").GetInt()
                + " power=" + powerUsage);
            if (MachineProgressHelper.IsProgressTypeMachineFinished(machine))
            {
                Finish(machine, inputGrid, outputGrid);
            }
        }

        private static void Continue(
            GameItem machine,
            GameSlotInventory powerInv,
            GameGridInventory inputGrid,
            GameGridInventory outputGrid,
            GameGridInventory moduleInv,
            int powerUsage)
        {
            if (WipCards(inputGrid).Count == 0)
            {
                // Nothing being processed (e.g. an old save): fall back to ready
                // instead of spinning on a job whose input is gone.
                ResetToReady(machine);
                return;
            }
            string targetId = machine.GetTagReadonly(ItemFactory.DesequencerTargetTag).GetString();
            if (string.IsNullOrEmpty(targetId) || !CardBatch.OutputHasRoom(outputGrid, targetId))
            {
                // Output full: no power draw, no progress, no output.
                Diag("busy-outputfull", "[CF-DESQ] paused: output full target=" + targetId);
                return;
            }
            PowerHelper.DrawPowerSource(powerInv.childItem, powerUsage);
            MachineHelper.OnMachineActioned(machine, moduleInv);
            MachineProgressHelper.ContinueProgressTypeMachine(machine);
            UpdateProgress(machine, inputGrid);
            var (current, target) = MachineProgressHelper.GetProgress(machine);
            Log.Info("[CF-DESQ] night progress=" + current + "/" + target
                + " speed=" + machine.GetTagReadonly("CURRENT_PROCESSING_SPEED_TAG").GetInt());
            if (MachineProgressHelper.IsProgressTypeMachineFinished(machine))
            {
                Finish(machine, inputGrid, outputGrid);
            }
        }

        /// <summary>
        /// Produces one card per working input card. When the output cannot take
        /// all of them the already-placed cards are rolled back and the job stays
        /// on the machine (progress kept, inputs untouched) so it retries the next
        /// night — "do what fits" is only used for missing input, never for a full
        /// output, because a partial batch would silently eat input cards.
        /// </summary>
        private static void Finish(GameItem machine, GameGridInventory inputGrid, GameGridInventory outputGrid)
        {
            List<GameItem> wip = WipCards(inputGrid);
            if (wip.Count == 0)
            {
                ResetToReady(machine);
                return;
            }
            string targetId = machine.GetTagReadonly(ItemFactory.DesequencerTargetTag).GetString();
            if (string.IsNullOrEmpty(targetId))
            {
                ResetToReady(machine);
                return;
            }
            List<GameItem> placed = new List<GameItem>();
            foreach (GameItem ignored in wip)
            {
                GameItem card = DirectoryMaster.Item(targetId);
                if (card == null)
                {
                    break;
                }
                if (GraphUtils.TryAcceptAll(outputGrid, card) > 0)
                {
                    card.Destroy();
                    break;
                }
                placed.Add(card);
            }
            if (placed.Count < wip.Count)
            {
                foreach (GameItem card in placed)
                {
                    card.Destroy();
                }
                Log.Info("[CF-DESQ] output full: kept progress plan=" + wip.Count
                    + " output=" + outputGrid.childItems.Count);
                return;
            }
            foreach (GameItem card in wip)
            {
                // The card is LOCKED_TAG-locked while it is being worked on; clear
                // the removal guard first, exactly like vanilla FinishDesequencer.
                card.mayInventoryRemoveItemFunc = (GameItem _, GameInventory _) => true;
                card.Destroy();
            }
            MachineProgressHelper.FinishProgressTypeMachine(machine);
            ResetToReady(machine);
            Log.Info("[CF-DESQ] finished target=" + targetId + " produced=" + placed.Count
                + " consumed=" + wip.Count);
        }

        private static void MarkWip(GameItem machine, GameGridInventory inputGrid, int plan)
        {
            var (current, target) = MachineProgressHelper.GetProgress(machine);
            string desc = LocHelper.GetLocalizedMechanic("mech_desequencer_progress", current, target);
            string name = LocHelper.GetLocalizedItem("item_modified_access_card_name");
            int marked = 0;
            foreach (GameItem card in inputGrid.childItems)
            {
                if (marked >= plan)
                {
                    break;
                }
                if (card == null || !ItemFactory.BlankCardId.Equals(card.identifier)
                    || card.IsTag(ItemFactory.WipTag))
                {
                    continue;
                }
                card.EnableTag(ItemFactory.WipTag);
                GeneralHelper.LockItem(card);
                // Deliberately NOT swapping the art to blank_card_wip: vanilla
                // does that inside a 1x1 slot, and its real footprint cannot be
                // checked offline. Changing an item's shape inside a 5x1 grid is
                // the one step that could invalidate the slot, so the card keeps
                // its own sprite and only the name/description change.
                card.name = name;
                card.flavorText = "";
                card.shortDescription = desc;
                marked++;
            }
        }

        private static void UpdateProgress(GameItem machine, GameGridInventory inputGrid)
        {
            var (current, target) = MachineProgressHelper.GetProgress(machine);
            machine.ModifyTag(ProgressTag, delegate(TagState state)
            {
                state.Enable().SetInt(current);
            });
            string desc = LocHelper.GetLocalizedMechanic("mech_desequencer_progress", current, target);
            foreach (GameItem card in inputGrid.childItems)
            {
                if (card != null && card.IsTag(ItemFactory.WipTag))
                {
                    card.shortDescription = desc;
                }
            }
        }

        private static readonly HashSet<string> _diag = new HashSet<string>();

        // One log line per distinct blocked reason; the cycle runs every night and
        // a stuck machine would otherwise fill the log.
        private static void Diag(string key, string message)
        {
            if (_diag.Add(key))
            {
                Log.Info(message);
            }
        }

        private static void ResetToReady(GameItem machine)
        {
            machine.ModifyTag(StateTag, delegate(TagState state)
            {
                state.Enable().SetString(StateReady);
            });
            machine.ModifyTag(ProgressTag, delegate(TagState state)
            {
                state.Enable().SetInt(0);
            });
        }

        private static int CountBlank(GameGridInventory inputGrid)
        {
            int count = 0;
            foreach (GameItem card in inputGrid.childItems)
            {
                if (card != null
                    && ItemFactory.BlankCardId.Equals(card.identifier)
                    && !card.IsTag(ItemFactory.WipTag))
                {
                    count++;
                }
            }
            return count;
        }

        private static List<GameItem> WipCards(GameGridInventory inputGrid)
        {
            List<GameItem> cards = new List<GameItem>();
            foreach (GameItem card in inputGrid.childItems)
            {
                if (card != null && card.IsTag(ItemFactory.WipTag))
                {
                    cards.Add(card);
                }
            }
            return cards;
        }
    }

    /// <summary>
    /// P4 Wild Network "CardFence" tab. All four upgrades are acquisition gates,
    /// not stat buffs: each one only widens what the retired cardmaker or the
    /// thief is allowed to sell. The tab arithmetic is fixed by the prefab:
    /// TABS_W = 1060, TAB_W = 256, step = 268 (TAB_W + TAB_GAP). Three tabs use
    /// 792 px, so CardFence fits with 268 px to spare; a fourth tab would be
    /// exactly 1060 px with no margin, which is why this mod never adds one.
    /// </summary>
    internal static class CfNetwork
    {
        internal const string Tab = "CardFence";

        internal const string Pro = "CF_FENCE_PRO";
        internal const string Tier2 = "CF_FENCE_T2";
        internal const string Tier3 = "CF_FENCE_T3";
        internal const string ThiefTier2 = "CF_FENCE_THIEF_T2";

        internal const int ProCost = 1500;
        internal const int Tier2Cost = 1200;
        internal const int Tier3Cost = 3000;
        internal const int ThiefTier2Cost = 2200;

        private static bool _registered;

        internal static void EnsureRegistered()
        {
            // Must be re-applied on every enable: the label lives in a static
            // dictionary, not in the save, and the player can reload the mod.
            ModHelper.SetNetworkTabLabel(Tab, () => "制卡");
            if (_registered)
            {
                return;
            }
            _registered = true;

            Add(Pro, ProCost, "Cardmaker", "制卡门路",
                "引荐一位退休的制卡匠，并让杰克逊博士开始卖专业解序器与批量制卡模块。一次性附赠一套制卡工具：印表机、空白卡芯片、CNC 模块、电子件 ×2、耗材 ×2。",
                null,
                GrantProKit);
            Add(Tier2, Tier2Cost, "Cardmaker", "中级制卡门路",
                "制卡匠与杰克逊博士开始卖中级的五家族钥匙卡与加密模块。",
                new[] { Pro },
                null);
            Add(Tier3, Tier3Cost, "Cardmaker", "高级制卡门路",
                "制卡匠与杰克逊博士开始卖高级钥匙卡与同家族的加密模块。",
                new[] { Tier2 },
                null);
            Add(ThiefTier2, ThiefTier2Cost, "Thief", "高级箱源",
                "黑市声望 30 以上就能从小偷手里拿到中级箱（原本要 60）；解锁高级制卡门路后，声望 85 以上能拿到高级箱。",
                null,
                null);

            Log.Info("[CF-NET] registered 4 CardFence upgrades tab=CardFence width=792/1060");
            Log.Info("[CF-NET] hide CF_SAWYER_* until the exchange is unlocked");
        }

        internal static bool Unlocked(string id)
        {
            try
            {
                return ModHelper.IsNetworkUpgradeUnlocked(id);
            }
            catch (Exception e)
            {
                Log.Warn("[CF-NET] cannot read upgrade " + id + ": " + e.GetType().Name + ": " + e.Message);
                return false;
            }
        }

        // One-time starter kit for CF_FENCE_PRO. EmporiumTryAdd takes the back
        // counter first; if that is full it falls through to the other store
        // inventories instead of destroying the item. Items are owned because the
        // player is meant to use them, not to resell an unowned offer.
        internal static void GrantProKit()
        {
            try
            {
                if (PlayerStore.Instance == null || EmporiumEntry.Instance == null)
                {
                    return;
                }
                GameInventory back = EmporiumEntry.Instance.backInvinvElement;
                Grant(back, "printer");
                Grant(back, "cf_printer_chip_blank_keycard");
                Grant(back, "printer_module_cnc");
                Grant(back, "common_electronic");
                Grant(back, "common_electronic");
                Grant(back, "printer_plastic");
                Grant(back, "printer_plastic");
                Log.Info("[CF-NET] PRO kit granted printer+chip+cnc+2 electronic+2 plastic");
            }
            catch (Exception e)
            {
                Log.Error("[CF-NET] PRO kit failed: " + e.GetType().Name + ": " + e.Message);
            }
        }

        private static void Grant(GameInventory back, string id)
        {
            GameItem item = DirectoryMaster.Item(id);
            if (item == null)
            {
                Log.Warn("[CF-NET] PRO kit missing item " + id);
                return;
            }
            GeneralHelper.SetItemOwned(item, true);
            GraphUtils.EmporiumTryAdd(back, item);
        }

        private static void Add(
            string id,
            int cost,
            string subtitle,
            string title,
            string description,
            string[] prerequisites,
            Action onUnlock)
        {
            ModHelper.AddNetworkUpgrade(
                CardCraftAndFenceMod.ModId,
                id,
                cost,
                subtitle,
                () => title,
                () => description,
                onUnlock,
                Tab,
                null,
                prerequisites);
        }
    }

    // P5 boundary: Sawyer upgrades are registered later but must not show up in
    // the CardFence tab until the player has unlocked the exchange. Before the
    // exchange exists the whole Sawyer chain is dead content, so hiding it keeps
    // the tab honest. Once isExchangeUnlocked is true the vanilla body decides
    // (it still hides the whole Advanced tab until the property is paid).
    [HarmonyPatch(typeof(NetworkUpgrade), nameof(NetworkUpgrade.IsHiddenForRun))]
    internal static class CfSawyerHidePatch
    {
        private static void Postfix(string id, ref bool __result)
        {
            if (__result || id == null || !id.StartsWith("CF_SAWYER_", StringComparison.Ordinal))
            {
                return;
            }
            PlayerStore store = PlayerStore.Instance;
            if (store == null
                || store.exchangeManager == null
                || !store.exchangeManager.isExchangeUnlocked)
            {
                __result = true;
            }
        }
    }

    /// <summary>
    /// Shared card/core stock for the retired cardmaker and Dr. Jackson. T1 is the
    /// cardmaker's baseline; T2 adds three distinct families of cards and three of
    /// cores; T3 adds one matched card+core pair. Items stay unowned so the player
    /// pays unitValue at the counter.
    /// </summary>
    internal static class CfCardStock
    {
        internal static readonly CardFamily[] AllFamilies =
        {
            CardFamily.SEC,
            CardFamily.MED,
            CardFamily.ENG,
            CardFamily.SER,
            CardFamily.CMD
        };

        internal static void AddTier1()
        {
            foreach (CardFamily family in AllFamilies)
            {
                AddStock(ItemFactory.KeycardId(family, 1));
                AddStock(ItemFactory.CryptoModuleId(family, 1));
            }
        }

        internal static void AddTier2()
        {
            foreach (CardFamily family in PickFamilies(3))
            {
                AddStock(ItemFactory.KeycardId(family, 2));
            }
            foreach (CardFamily family in PickFamilies(3))
            {
                AddStock(ItemFactory.CryptoModuleId(family, 2));
            }
        }

        internal static void AddTier3()
        {
            CardFamily family = PickFamilies(1)[0];
            AddStock(ItemFactory.KeycardId(family, 3));
            AddStock(ItemFactory.CryptoModuleId(family, 3));
        }

        // Distinct random families: a Fisher-Yates shuffle over the five-family
        // pool driven by the game's RNG, not System.Random.
        internal static List<CardFamily> PickFamilies(int count)
        {
            List<CardFamily> pool = new List<CardFamily>(AllFamilies);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = RNG.GetRandomInt(0, i);
                CardFamily swap = pool[i];
                pool[i] = pool[j];
                pool[j] = swap;
            }
            if (count < pool.Count)
            {
                pool.RemoveRange(count, pool.Count - count);
            }
            return pool;
        }

        internal static void AddStock(string id)
        {
            try
            {
                if (!PlayerStore.Instance.TryAddDirectSellingItemToTable(id))
                {
                    Log.Warn("[CF-STOCK] counter full, skipped " + id);
                }
            }
            catch (Exception e)
            {
                Log.Warn("[CF-STOCK] cannot offer " + id + ": " + e.GetType().Name + ": " + e.Message);
            }
        }
    }

    /// <summary>
    /// P4 retired cardmaker. The client is a normal SELL client that puts its
    /// stock on the front counter in the dialogue's end action, exactly like the
    /// vanilla retired gunsmith/farmer. Visit bookkeeping uses the save-backed
    /// ModHelper modData key "maker_last_day" so a reload cannot double-visit and
    /// a first unlock gets an immediate catch-up visit.
    /// </summary>
    internal static class CfRetiredCardmaker
    {
        internal const string ClientId = "cf_retired_cardmaker";
        private const string LastDayKey = "maker_last_day";

        private static readonly HashSet<string> _diag = new HashSet<string>();

        internal static void OnGenerateCustomerVeryLate(StoreClientManager manager)
        {
            // This runs inside StoreClientManager.GenerateClient for every customer.
            // An exception would abort the rest of the generation pass, so it never
            // escapes into the game.
            try
            {
                TryAdd(manager);
            }
            catch (Exception e)
            {
                Log.Error("[CF-MAKER] visit hook failed: " + e.GetType().Name + ": " + e.Message);
            }
        }

        private static void TryAdd(StoreClientManager manager)
        {
            if (manager == null || StoreStation.Instance == null || PlayerStore.Instance == null)
            {
                // No run loaded (or the store singleton is between scenes): the
                // visit bookkeeping lives in PlayerStore modData, so adding a
                // client here would repeat on every generation pass.
                return;
            }
            if (!CfNetwork.Unlocked(CfNetwork.Pro))
            {
                Diag("locked", "[CF-MAKER] skip: CF_FENCE_PRO not unlocked");
                return;
            }
            int day = StoreStation.Instance.dayCounter;
            if (day <= 0)
            {
                return;
            }
            int last = ModHelper.GetModDataInt(CardCraftAndFenceMod.ModId, LastDayKey, -1);
            if (last == day)
            {
                Diag("today", "[CF-MAKER] skip: already visited today day=" + day);
                return;
            }
            bool weekly = day % 7 == 6;
            bool catchUp = last < 0;
            if (!weekly && !catchUp)
            {
                return;
            }
            manager.AddClient(BuildClient());
            ModHelper.SetModDataInt(CardCraftAndFenceMod.ModId, LastDayKey, day);
            Log.Info("[CF-MAKER] visit day=" + day
                + " gate=" + (weekly ? "weekly" : "catchup")
                + " stock=" + StockSummary());
        }

        private static StoreClient BuildClient()
        {
            StoreClient client = new StoreClient();
            client.identifier = ClientId;
            client.displayName = LocHelper.GetLocalizedOrDefault(
                "Client", "name_retired_cardmaker", "退休制卡匠");
            client.spriteName = SpriteDict.GetRandomLowerLevelSpriteName();
            client.clientFaction = "FACTION_LOWER_LEVEL";
            StoreClientFactionSetup.InitLowerLevelCitizen(client);
            client.clientIntent = StoreClient.ClientIntent.SELL;
            client.nonShootable = true;
            client.clientItemFeatureBuying.Add("premiumBuy15");
            client.acceptRetailMarkup = false;
            client.mainDialogue
                .SetText(client.displayName, LocHelper.GetLocalizedOrDefault(
                    "Dialogue",
                    "dialog_retired_cardmaker_1",
                    "老匠人把工具箱放在柜台上：「印表机、芯片、卡胚……你出得起价，我就有货。」"))
                .SetEndAction(delegate
                {
                    StockUp();
                });
            client.AddBasicDialog();
            client.CompleteClientCreation();
            ModHelper.SetClientSource(client, CardCraftAndFenceMod.ModId);
            return client;
        }

        // The cardmaker no longer sells machines: CF_FENCE_PRO hands over the
        // printer/chip/CNC kit once, and the inventor sells the desequencer and
        // batch module. What the cardmaker always carries is the T1 card/core set;
        // the T2/T3 upgrades widen that list.
        private static void StockUp()
        {
            CfCardStock.AddTier1();
            if (CfNetwork.Unlocked(CfNetwork.Tier2))
            {
                CfCardStock.AddTier2();
            }
            if (CfNetwork.Unlocked(CfNetwork.Tier3))
            {
                CfCardStock.AddTier3();
            }
        }

        private static string StockSummary()
        {
            List<string> parts = new List<string>();
            parts.Add("t1");
            if (CfNetwork.Unlocked(CfNetwork.Tier2))
            {
                parts.Add("t2");
            }
            if (CfNetwork.Unlocked(CfNetwork.Tier3))
            {
                parts.Add("t3");
            }
            return string.Join(",", parts);
        }

        private static void Diag(string key, string message)
        {
            if (_diag.Add(key))
            {
                Log.Info(message);
            }
        }
    }

    /// <summary>
    /// P4 thief graded box. The postfix wraps the last line of the vanilla thief
    /// dialogue: vanilla stock runs first, then at most one CF box is offered.
    /// T1 is never added (vanilla already sells T1) and CMD is never offered.
    /// T2 needs rep 60, or rep 30 with CF_FENCE_THIEF_T2; T3 needs CF_FENCE_T3
    /// and rep 85. Family weights are SEC 32 / MED 27 / ENG 21 / SER 20.
    /// </summary>
    internal static class CfThiefBoxes
    {
        private static readonly CardFamily[] Families =
        {
            CardFamily.SEC,
            CardFamily.MED,
            CardFamily.ENG,
            CardFamily.SER
        };

        // Weight order must match Families: SEC 32, MED 27, ENG 21, SER 20.
        private static readonly int[] Weights = { 32, 27, 21, 20 };

        private static readonly HashSet<string> _diag = new HashSet<string>();

        internal static void Attach(StoreClient client)
        {
            if (client == null)
            {
                return;
            }
            Dialogue last = client.mainDialogue;
            int guard = 0;
            while (last != null && last.nextDialogue != null && guard++ < 32)
            {
                last = last.nextDialogue;
            }
            if (last == null)
            {
                return;
            }
            Action original = last.endAction;
            last.endAction = delegate
            {
                try
                {
                    if (original != null)
                    {
                        original();
                    }
                    Offer();
                }
                catch (Exception e)
                {
                    Log.Error("[CF-THIEF] box offer failed: " + e.GetType().Name + ": " + e.Message);
                }
            };
        }

        private static void Offer()
        {
            int bm = StoreReputation.GetBMReputation();
            bool tier3 = CfNetwork.Unlocked(CfNetwork.Tier3) && bm >= 85;
            bool tier2 = bm >= 60 || (bm >= 30 && CfNetwork.Unlocked(CfNetwork.ThiefTier2));
            if (!tier3 && !tier2)
            {
                // T1 is never added: vanilla thief already sells T1 boxes.
                Diag("no-tier", "[CF-THIEF] skip: no T2/T3 box (bm=" + bm + ")");
                return;
            }
            int tier = tier3 ? 3 : 2;
            CardFamily family = PickFamily();
            string id = ItemFactory.ContainerId(family, tier);
            GameItem box = DirectoryMaster.Item(id);
            if (box == null)
            {
                Log.Warn("[CF-THIEF] no item for " + id);
                return;
            }
            // All families in the table are non-CMD, so every CF box is stolen.
            bool added = PlayerStore.Instance.TryAddDirectSellingItemToTable(
                box, isOwend: false, isStolen: true);
            if (!added)
            {
                box.Destroy();
            }
            Log.Info("[CF-THIEF] box family=" + family + " tier=" + tier
                + " id=" + id + " bm=" + bm + " added=" + added);
        }

        private static CardFamily PickFamily()
        {
            int roll = RNG.GetRandomInt(0, 99);
            int accumulated = 0;
            for (int i = 0; i < Families.Length; i++)
            {
                accumulated += Weights[i];
                if (roll < accumulated)
                {
                    return Families[i];
                }
            }
            return CardFamily.SEC;
        }

        private static void Diag(string key, string message)
        {
            if (_diag.Add(key))
            {
                Log.Info(message);
            }
        }
    }

    [HarmonyPatch(typeof(StoreClientList), nameof(StoreClientList.CreateThief))]
    internal static class CfThiefPatch
    {
        private static void Postfix(StoreClient __result)
        {
            CfThiefBoxes.Attach(__result);
        }
    }

    /// <summary>
    /// Dr. Jackson's shop is the second card-craft vendor. Once CF_FENCE_PRO is
    /// bought he also stocks the pro desequencer and the batch module; T2/T3 add
    /// the same card/core stock the cardmaker carries. Vanilla's dialogue
    /// endAction (if any) runs first.
    /// </summary>
    internal static class CfInventorStock
    {
        internal static void Attach(StoreClient client)
        {
            if (client == null || !CfNetwork.Unlocked(CfNetwork.Pro))
            {
                return;
            }
            Dialogue last = client.mainDialogue;
            int guard = 0;
            while (last != null && last.nextDialogue != null && guard++ < 32)
            {
                last = last.nextDialogue;
            }
            if (last == null)
            {
                return;
            }
            Action original = last.endAction;
            last.endAction = delegate
            {
                try
                {
                    if (original != null)
                    {
                        original();
                    }
                    CfCardStock.AddStock("cf_pro_desequencer");
                    CfCardStock.AddStock("cf_batch_card_module");
                    if (CfNetwork.Unlocked(CfNetwork.Tier2))
                    {
                        CfCardStock.AddTier2();
                    }
                    if (CfNetwork.Unlocked(CfNetwork.Tier3))
                    {
                        CfCardStock.AddTier3();
                    }
                    Log.Info("[CF-INVENTOR] stock desequencer+batch tier2="
                        + CfNetwork.Unlocked(CfNetwork.Tier2)
                        + " tier3=" + CfNetwork.Unlocked(CfNetwork.Tier3));
                }
                catch (Exception e)
                {
                    Log.Error("[CF-INVENTOR] stock failed: " + e.GetType().Name + ": " + e.Message);
                }
            };
        }
    }

    [HarmonyPatch(typeof(StoreClientList), nameof(StoreClientList.CreateInventor))]
    internal static class CfInventorPatch
    {
        private static void Postfix(StoreClient __result)
        {
            CfInventorStock.Attach(__result);
        }
    }

    // Six box loot definitions (common + rare per box). Registration happens once
    // in OnEnable; LootRegistry ignores duplicate Register calls, so we never
    // register a second time to "update" weights.
    internal static class LootTables
    {
        private static bool _registered;
        private static readonly HashSet<string> _diag = new HashSet<string>();

        internal static void Diag(string key, string message)
        {
            if (_diag.Add(key))
            {
                Log.Info(message);
            }
        }

        internal static void EnsureRegistered()
        {
            if (_registered)
            {
                return;
            }
            _registered = true;

            Register(CommonId(CardFamily.SEC, 2), new (string, float)[]
            {
                ("heavy_pistol_ammo", 20f),
                ("heavy_pistol_ammo_p", 15f),
                ("bandage_item", 15f),
                ("flashbang_grenade", 15f),
                ("smoke_grenade", 15f),
                ("stun_baton", 10f),
                ("combat_knife", 10f)
            });
            Register(RareId(CardFamily.SEC, 2), new (string, float)[]
            {
                ("cf_sec_keycard_t2", 30f),
                ("cf_crypto_module_sec_t2", 20f),
                ("glock_receiver", 15f),
                ("stun_gun", 15f),
                ("tazer", 10f),
                ("current_converter", 10f)
            });

            Register(CommonId(CardFamily.MED, 2), new (string, float)[]
            {
                ("bandage_item", 5f),
                ("hemostatic_bandage_item", 5f),
                ("blood_bag", 5f),
                ("salve", 22f),
                ("rubbing_alcohol", 10f),
                ("common_medical", 15f),
                ("common_chemical", 10f),
                ("pink_injector", 15f),
                ("surgery_tool", 8f),
                ("galaxy_blend", 5f)
            });
            Register(RareId(CardFamily.MED, 2), new (string, float)[]
            {
                ("cf_med_keycard_t2", 25f),
                ("cf_crypto_module_med_t2", 40f),
                ("black_injector", 15f),
                ("surgery_tool", 10f),
                ("blue_blood_bag", 5f),
                ("bandage_item", 5f)
            });

            Register(CommonId(CardFamily.ENG, 2), new (string, float)[]
            {
                ("common_electronic", 15f),
                ("nuts_metal", 8f),
                ("flux_agent", 8f),
                ("screwdriver", 8f),
                ("wire_cutter", 8f),
                ("crowbar", 10f),
                ("welder", 10f),
                ("turbo_booster", 5f),
                ("module_extractor", 10f),
                ("current_converter", 5f),
                ("system_capped_neural_core", 10f),
                ("module_extractor_advanced", 3f)
            });
            Register(RareId(CardFamily.ENG, 2), new (string, float)[]
            {
                ("cf_eng_keycard_t2", 20f),
                ("cf_crypto_module_eng_t2", 40f),
                ("module_extractor_advanced", 10f),
                ("turbo_booster_adv", 10f),
                ("advanced_flux_agent", 10f),
                ("current_converter", 10f)
            });

            Register(CommonId(CardFamily.SER, 2), new (string, float)[]
            {
                ("processed_cheese", 8f),
                ("box_cutter", 10f),
                ("kitchen_knife", 15f),
                ("hatchet", 12f),
                ("caffeine_pill", 12f),
                ("kotton_seed", 5f),
                ("hydroreed_seed", 5f),
                ("module_bay_expansion_kit", 26f),
                ("current_converter", 7f)
            });
            Register(RareId(CardFamily.SER, 2), new (string, float)[]
            {
                ("cf_ser_keycard_t2", 20f),
                ("cf_crypto_module_ser_t2", 40f),
                ("current_converter", 15f),
                ("module_bay_expansion_kit", 10f),
                ("hatchet", 10f),
                ("box_cutter", 5f)
            });

            Register(CommonId(CardFamily.CMD, 1), new (string, float)[]
            {
                ("heavy_pistol_ammo", 25f),
                ("heavy_pistol_ammo_p", 15f),
                ("flashbang_grenade", 10f),
                ("smoke_grenade", 10f),
                ("bandage_item", 10f),
                ("common_electronic", 10f),
                ("stun_baton", 10f),
                ("combat_knife", 10f)
            });
            Register(RareId(CardFamily.CMD, 1), new (string, float)[]
            {
                ("cmd_keycard", 30f),
                ("crypto_module_cmd", 20f),
                ("stun_gun", 15f),
                ("glock_receiver", 10f),
                ("smg", 10f),
                ("shotgun", 5f),
                ("current_converter", 10f)
            });

            Register(CommonId(CardFamily.CMD, 2), new (string, float)[]
            {
                ("heavy_pistol_ammo", 22f),
                ("heavy_pistol_ammo_p", 12f),
                ("flashbang_grenade", 10f),
                ("smoke_grenade", 10f),
                ("bandage_item", 4f),
                ("common_electronic", 5f),
                ("stun_baton", 8f),
                ("combat_knife", 8f),
                ("stun_gun", 9f),
                ("smg", 5f),
                ("shotgun", 3f),
                ("current_converter", 4f)
            });
            Register(RareId(CardFamily.CMD, 2), new (string, float)[]
            {
                ("cf_cmd_keycard_t2", 20f),
                ("cf_crypto_module_cmd_t2", 35f),
                ("stun_gun", 10f),
                ("smg", 10f),
                ("shotgun", 10f),
                ("current_converter", 10f),
                ("glock_receiver", 5f)
            });

            // T3 boxes. Base is the T2 common/rare pool plus the T3 card/core and
            // the highest-value rares. Model EV (price 300 / CMD 500):
            // SEC 4.332, MED 4.305, ENG 4.356, SER 4.165, CMD 4.150.
            Register(CommonId(CardFamily.SEC, 3), new (string, float)[]
            {
                ("heavy_pistol_ammo", 15f),
                ("heavy_pistol_ammo_p", 10f),
                ("bandage_item", 10f),
                ("flashbang_grenade", 15f),
                ("smoke_grenade", 15f),
                ("stun_baton", 20f),
                ("combat_knife", 15f)
            });
            Register(RareId(CardFamily.SEC, 3), new (string, float)[]
            {
                ("cf_sec_keycard_t3", 15f),
                ("cf_crypto_module_sec_t3", 45f),
                ("cf_sec_keycard_t2", 5f),
                ("cf_crypto_module_sec_t2", 5f),
                ("current_converter", 10f),
                ("stun_gun", 10f),
                ("glock_receiver", 5f),
                ("stun_baton", 3f),
                ("tazer", 2f)
            });

            Register(CommonId(CardFamily.MED, 3), new (string, float)[]
            {
                ("bandage_item", 4f),
                ("hemostatic_bandage_item", 4f),
                ("blood_bag", 4f),
                ("salve", 15f),
                ("rubbing_alcohol", 8f),
                ("common_medical", 10f),
                ("common_chemical", 10f),
                ("pink_injector", 15f),
                ("surgery_tool", 20f),
                ("galaxy_blend", 10f)
            });
            Register(RareId(CardFamily.MED, 3), new (string, float)[]
            {
                ("cf_med_keycard_t3", 10f),
                ("cf_crypto_module_med_t3", 60f),
                ("cf_med_keycard_t2", 4f),
                ("cf_crypto_module_med_t2", 4f),
                ("black_injector", 8f),
                ("blue_blood_bag", 5f),
                ("current_converter", 3f),
                ("surgery_tool", 4f),
                ("bandage_item", 2f)
            });

            Register(CommonId(CardFamily.ENG, 3), new (string, float)[]
            {
                ("common_electronic", 15f),
                ("nuts_metal", 2f),
                ("flux_agent", 2f),
                ("screwdriver", 5f),
                ("wire_cutter", 7f),
                ("crowbar", 8f),
                ("welder", 10f),
                ("turbo_booster", 5f),
                ("module_extractor", 10f),
                ("current_converter", 15f),
                ("system_capped_neural_core", 10f),
                ("module_extractor_advanced", 11f)
            });
            Register(RareId(CardFamily.ENG, 3), new (string, float)[]
            {
                ("cf_eng_keycard_t3", 15f),
                ("cf_crypto_module_eng_t3", 55f),
                ("cf_eng_keycard_t2", 5f),
                ("cf_crypto_module_eng_t2", 5f),
                ("module_extractor_advanced", 10f),
                ("turbo_booster_adv", 3f),
                ("advanced_flux_agent", 3f),
                ("current_converter", 4f)
            });

            Register(CommonId(CardFamily.SER, 3), new (string, float)[]
            {
                ("processed_cheese", 5f),
                ("box_cutter", 8f),
                ("kitchen_knife", 10f),
                ("hatchet", 10f),
                ("caffeine_pill", 5f),
                ("kotton_seed", 6f),
                ("hydroreed_seed", 5f),
                ("module_bay_expansion_kit", 25f),
                ("current_converter", 26f)
            });
            Register(RareId(CardFamily.SER, 3), new (string, float)[]
            {
                ("cf_ser_keycard_t3", 10f),
                ("cf_crypto_module_ser_t3", 55f),
                ("cf_ser_keycard_t2", 4f),
                ("cf_crypto_module_ser_t2", 10f),
                ("current_converter", 10f),
                ("module_bay_expansion_kit", 5f),
                ("hatchet", 5f),
                ("box_cutter", 1f)
            });

            Register(CommonId(CardFamily.CMD, 3), new (string, float)[]
            {
                ("heavy_pistol_ammo", 16f),
                ("heavy_pistol_ammo_p", 12f),
                ("flashbang_grenade", 10f),
                ("smoke_grenade", 10f),
                ("bandage_item", 3f),
                ("common_electronic", 3f),
                ("stun_baton", 8f),
                ("combat_knife", 7f),
                ("stun_gun", 10f),
                ("smg", 8f),
                ("shotgun", 9f),
                ("current_converter", 4f)
            });
            Register(RareId(CardFamily.CMD, 3), new (string, float)[]
            {
                ("cf_cmd_keycard_t3", 3f),
                ("cf_crypto_module_cmd_t3", 55f),
                ("cf_cmd_keycard_t2", 2f),
                ("glock_receiver", 2f),
                ("cf_crypto_module_cmd_t2", 5f),
                ("shotgun", 10f),
                ("smg", 10f),
                ("current_converter", 10f),
                ("stun_gun", 3f)
            });

            Diag("registered", "[CF-LOOT] registered 22 tables");
        }

        internal static void Fill(GameItem box, CardFamily family, int tier)
        {
            if (box == null || box.contentWindow == null || box.IsTag("CF_CRATE_FILLED"))
            {
                return;
            }
            box.EnableTag("CF_CRATE_FILLED");
            string common = CommonId(family, tier);
            string rare = RareId(family, tier);
            int rolls = Rolls(family, tier);
            Diag("fill:" + common, "[CF-LOOT] fill table=" + common + " rolls=" + rolls
                + " rare=" + rare);
            for (int i = 0; i < rolls; i++)
            {
                SpawnOne(box, common);
            }
            SpawnOne(box, rare);
        }

        internal static string CommonId(CardFamily family, int tier)
        {
            return "cf_" + family.ToString().ToLowerInvariant() + "_t" + tier + "_common";
        }

        internal static string RareId(CardFamily family, int tier)
        {
            return "cf_" + family.ToString().ToLowerInvariant() + "_t" + tier + "_rare";
        }

        internal static int Rolls(CardFamily family, int tier)
        {
            if (tier == 3)
            {
                switch (family)
                {
                    case CardFamily.SEC: return 11;
                    case CardFamily.MED: return 11;
                    case CardFamily.ENG: return 6;
                    case CardFamily.SER: return 7;
                    case CardFamily.CMD: return 11;
                    default: return 0;
                }
            }
            switch (family)
            {
                case CardFamily.SEC: return 10;
                case CardFamily.MED: return 10;
                case CardFamily.ENG: return 5;
                case CardFamily.SER: return 6;
                case CardFamily.CMD: return tier == 1 ? 8 : 10;
                default: return 0;
            }
        }

        private static void Register(string tableId, (string itemId, float weight)[] entries)
        {
            if (ModHelper.GetLootEntries(tableId) != null && ModHelper.GetLootEntries(tableId).Count > 0)
            {
                return;
            }
            ModHelper.RegisterLootTable(CardCraftAndFenceMod.ModId, tableId, entries);
        }

        private static void SpawnOne(GameItem box, string tableId)
        {
            string itemId = ModHelper.RollLootTable(tableId);
            if (string.IsNullOrEmpty(itemId))
            {
                Log.Warn("[CF-LOOT] no item for table " + tableId);
                return;
            }
            GameItem item = DirectoryMaster.Item(itemId);
            if (item == null)
            {
                return;
            }
            int remaining = GraphUtils.TryAcceptAll(box, item);
            Diag("spawn:" + tableId, "[CF-LOOT] spawn table=" + tableId
                + " item=" + itemId + " remaining=" + remaining);
            if (remaining > 0)
            {
                // The returned amount is the remainder; the accepted part is
                // already in the crate. Destroy only the leftover object.
                item.Destroy();
            }
        }
    }

    internal static class Log
    {
        private const string Prefix = "[CardCraftAndFence] ";

        public static void Info(string message)
        {
            Debug.Log(Prefix + message);
        }

        public static void Warn(string message)
        {
            Debug.LogWarning(Prefix + message);
        }

        public static void Error(string message)
        {
            Debug.LogError(Prefix + message);
        }
    }
}
