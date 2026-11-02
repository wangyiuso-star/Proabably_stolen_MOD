using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace DeepSpaceQoL
{
    /// <summary>
    /// Appraisal assistant: tells the player the correct value for value-estimate
    /// appraisals and the true authenticity for the genuine/counterfeit flow.
    /// </summary>
    public sealed class AppraisalAssistantMod : IMod
    {
        internal const string ModId = "deepspace.appraisal_assistant";
        internal const string ModVersion = "1.0.0";
        internal const string AssemblyName = "AppraisalAssistant";

        internal static ConfigEntry<bool> ShowTrueValue;
        internal static ConfigEntry<bool> AutoFill;
        internal static ConfigEntry<bool> ForceEstimate;

        public void Init(ModManifest manifest)
        {
            Log.Info("Init " + manifest.ID + " v" + manifest.ModVersion);
        }

        public void OnEnable()
        {
            ModConfig config = ModHelper.GetConfig(ModId);
            ShowTrueValue = config.Define(
                "show_true_value",
                true,
                "appraisal",
                delegate { return "显示真实价值"; },
                delegate { return "在鉴定物品提示中加入真值与精确区间。"; });
            AutoFill = config.Define(
                "auto_fill",
                true,
                "appraisal",
                delegate { return "自动填入鉴定价格"; },
                delegate { return "打开鉴定面板时，自动把正确价值填入输入框。"; });
            ForceEstimate = config.Define(
                "force_correct_estimate",
                true,
                "appraisal",
                delegate { return "提交时使用正确价格"; },
                delegate { return "按下估算时，即使输入框被改过，也以正确价值提交。"; });

            Log.Info("enabled v" + ModVersion);
        }

        public void OnDisable()
        {
        }
    }

    internal static class Log
    {
        private const string Prefix = "[AppraisalAssistant] ";

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

        private sealed class ValueBox
        {
            public int Value;
        }

        private static readonly ConditionalWeakTable<GameItem, ValueBox> ValueCache =
            new ConditionalWeakTable<GameItem, ValueBox>();

        public static void Forget(GameItem item)
        {
            if (item != null)
            {
                ValueCache.Remove(item);
            }
        }

        public static bool TryGetCorrectValue(GameItem item, out int value)
        {
            value = 0;
            if (item == null)
            {
                return false;
            }
            ValueBox cached;
            if (ValueCache.TryGetValue(item, out cached))
            {
                value = cached.Value;
                return value > 0;
            }
            try
            {
                if (!item.IsTag("APPRAISAL_ITEM"))
                {
                    ValueCache.Add(item, new ValueBox());
                    return false;
                }
                if (AppraisalItemRegistry.Find(item) == null)
                {
                    ValueCache.Add(item, new ValueBox());
                    return false;
                }
                value = AppraisalHelper.GetCorrectBaseValue(item);
                bool ok = value > 0;
                ValueCache.Add(item, new ValueBox { Value = ok ? value : 0 });
                return ok;
            }
            catch (Exception e)
            {
                Log.Warn("Could not compute true value: " + e.Message);
                return false;
            }
        }
    }

    internal static class AppraisalTag
    {
        // GameItem.ModifyTag -> ValidateShapeState -> OverlayHandler.UpdateInventoryStateUI.
        // These call sites are event-driven, but avoid a needless rebuild when the
        // value is already correct.
        public static bool NeedsWrite(GameItem item, int value)
        {
            if (item == null)
            {
                return false;
            }
            TagState state = item.GetTagReadonly("APPRAISAL_DEFAULT_VALUE");
            return !state.IsEnabled() || state.GetInt() != value;
        }

        public static void SetDefaultValue(GameItem item, int value)
        {
            if (!NeedsWrite(item, value))
            {
                return;
            }
            // APPRAISAL_DEFAULT_VALUE is a pure number tag: it does not change
            // shape, features or inventory slots. Skip ValidateShapeState (which
            // rebuilds the inventory UI) and sync the cached modified tag by hand.
            item.ModifyTag("APPRAISAL_DEFAULT_VALUE", delegate(TagState state)
            {
                state.Enable().SetInt(value);
            }, true);
            TagState modified = item.modifiedState.GetTag("APPRAISAL_DEFAULT_VALUE");
            modified.Enable().SetInt(value);
        }
    }

    [HarmonyPatch(typeof(AppraisalHelper), nameof(AppraisalHelper.CreateAppraisalTooltip))]
    internal static class AppraisalTooltipPatch
    {
        private static void Postfix(RichTextBuilder builder, GameItem item)
        {
            if (builder == null || item == null || !AppraisalSession.IsCurrent(item))
            {
                return;
            }
            if (AppraisalAssistantMod.ShowTrueValue != null && !AppraisalAssistantMod.ShowTrueValue.Value)
            {
                return;
            }
            if (!AppraisalSession.HasValue)
            {
                return;
            }
            int value = AppraisalSession.Value;
            int low = Mathf.CeilToInt(value * 0.95f);
            int high = Mathf.FloorToInt(value * 1.05f);
            builder.AddLine(
                "真值：" + value.ToString(CultureInfo.InvariantCulture),
                true,
                RenderHandler.ColorPalette.LightGreen);
            builder.AddLine(
                "精确区间：" + low.ToString(CultureInfo.InvariantCulture) + " - " + high.ToString(CultureInfo.InvariantCulture),
                true,
                RenderHandler.ColorPalette.LightGreen);
        }
    }

    [HarmonyPatch(typeof(InspectionUIManager), nameof(InspectionUIManager.OnInspectItem))]
    internal static class AppraisalInspectResetPatch
    {
        private static void Postfix(InspectionUIManager __instance, GameItem gameItem)
        {
            if (__instance == null || gameItem == null)
            {
                return;
            }
            // Inspecting an item is the point where its features/condition may
            // become relevant; drop both caches for this item.
            Truth.Forget(gameItem);
            Log.Forget(gameItem);

            // One scan per magnifier use. Tooltip rebuilding then only formats
            // this session, even if the item is dragged or hovered again.
            TruthResult truth;
            bool hasTruth = Truth.TryGet(gameItem, out truth);
            int value;
            bool hasValue = Log.TryGetCorrectValue(gameItem, out value);
            AppraisalSession.Item = gameItem;
            AppraisalSession.HasValue = hasValue;
            AppraisalSession.Value = value;
            AppraisalSession.HasTruth = hasTruth;
            AppraisalSession.Truth = truth;

            if (!gameItem.IsTag("APPRAISAL_ITEM"))
            {
                return;
            }
            // 同一顾客的第二件鉴定品可能沿用上一个输入框状态；
            // 每件物品开始检查时都重置，确保各自使用自己的价值。
            __instance.isEditingAppraisalField = false;
            if (AppraisalAssistantMod.AutoFill != null && !AppraisalAssistantMod.AutoFill.Value)
            {
                return;
            }
            if (!hasValue)
            {
                return;
            }
            string text = value.ToString(CultureInfo.InvariantCulture);
            if (__instance.apprisalServiceInputField != null && __instance.apprisalServiceInputField.text != text)
            {
                __instance.apprisalServiceInputField.text = text;
            }
            AppraisalTag.SetDefaultValue(gameItem, value);
        }
    }

    [HarmonyPatch(typeof(InspectionUIManager), nameof(InspectionUIManager.OnAppraisalServiceEstimated))]
    internal static class AppraisalForceEstimatePatch
    {
        private static void Prefix(InspectionUIManager __instance)
        {
            if (__instance == null || __instance.currentInspectingItem == null)
            {
                return;
            }
            if (AppraisalAssistantMod.ForceEstimate != null && !AppraisalAssistantMod.ForceEstimate.Value)
            {
                return;
            }
            int value;
            if (!Log.TryGetCorrectValue(__instance.currentInspectingItem, out value))
            {
                return;
            }
            string text = value.ToString(CultureInfo.InvariantCulture);
            if (__instance.apprisalServiceInputField != null && __instance.apprisalServiceInputField.text != text)
            {
                __instance.apprisalServiceInputField.text = text;
            }
            AppraisalTag.SetDefaultValue(__instance.currentInspectingItem, value);
        }
    }

    [HarmonyPatch(typeof(InspectionUIManager), nameof(InspectionUIManager.OpenCigaretteConfrontPanel))]
    internal static class CigaretteTitlePatch
    {
        private static void Postfix(InspectionUIManager __instance, GameItem gameItem)
        {
            ConfrontTitle.Append(__instance, gameItem);
        }
    }

    [HarmonyPatch(typeof(InspectionUIManager), nameof(InspectionUIManager.OpenStampConfrontPanel))]
    internal static class StampTitlePatch
    {
        private static void Postfix(InspectionUIManager __instance, GameItem gameItem)
        {
            ConfrontTitle.Append(__instance, gameItem);
        }
    }

    [HarmonyPatch(typeof(InspectionUIManager), nameof(InspectionUIManager.OpenInjectorConfrontPanel))]
    internal static class InjectorTitlePatch
    {
        private static void Postfix(InspectionUIManager __instance, GameItem gameItem)
        {
            ConfrontTitle.Append(__instance, gameItem);
        }
    }

    internal static class ConfrontTitle
    {
        public static void Append(InspectionUIManager ui, GameItem item)
        {
            if (ui == null || ui.confrontTitleTMP == null || item == null)
            {
                return;
            }
            TruthResult truth;
            if (!Truth.TryGet(item, out truth))
            {
                return;
            }
            string tag;
            switch (truth.Kind)
            {
                case TruthKind.Genuine:
                    tag = "  <color=#39A78E>[真品]</color>";
                    break;
                case TruthKind.Counterfeit:
                    tag = "  <color=#FF5555>[假货]</color>";
                    break;
                case TruthKind.Defective:
                    tag = "  <color=#FFAA00>[有缺陷]</color>";
                    break;
                case TruthKind.Replica:
                    tag = "  <color=#FFAA00>[复制品]</color>";
                    break;
                default:
                    return;
            }
            ui.confrontTitleTMP.text += tag;
        }
    }

    [HarmonyPatch(typeof(InspectableHelper), nameof(InspectableHelper.CreateInspectableTooltip))]
    internal static class AuthenticityTooltipPatch
    {
        private static void Postfix(RichTextBuilder builder, GameItem item)
        {
            if (builder == null || item == null || !AppraisalSession.IsCurrent(item))
            {
                return;
            }
            if (!AppraisalSession.HasTruth)
            {
                return;
            }
            TruthResult truth = AppraisalSession.Truth;
            RenderHandler.ColorPalette color = truth.Kind == TruthKind.Genuine
                ? RenderHandler.ColorPalette.LightGreen
                : (truth.Kind == TruthKind.Counterfeit ? RenderHandler.ColorPalette.Red : RenderHandler.ColorPalette.Orange);
            builder.AddLine("真伪判定：" + truth.Label, true, color);
            if (truth.Reason != null)
            {
                builder.AddLine("原因：" + truth.Reason, true, color);
            }
        }
    }

    internal enum TruthKind
    {
        None,
        Genuine,
        Counterfeit,
        Defective,
        Replica
    }

    internal struct TruthResult
    {
        public TruthKind Kind;
        public string Label;
        public string Reason;
    }

    internal static class AppraisalSession
    {
        public static GameItem Item;
        public static bool HasValue;
        public static int Value;
        public static bool HasTruth;
        public static TruthResult Truth;

        public static bool IsCurrent(GameItem item)
        {
            if (item == null || Item != item)
            {
                return false;
            }
            InspectionUIManager ui = InspectionUIManager.Instance;
            return ui != null && ui.currentInspectingItem == item;
        }
    }

    internal static class Truth
    {
        private sealed class TruthBox
        {
            public bool HasValue;
            public TruthResult Value;
        }

        private static readonly ConditionalWeakTable<GameItem, TruthBox> ResultCache =
            new ConditionalWeakTable<GameItem, TruthBox>();

        public static void Forget(GameItem item)
        {
            if (item != null)
            {
                ResultCache.Remove(item);
            }
        }

        public static bool TryGet(GameItem item, out TruthResult result)
        {
            result = default(TruthResult);
            if (item == null)
            {
                return false;
            }
            try
            {
                // Ribwich can change when opened (reason includes "已开封"), so it
                // stays uncached. Wine is a single cheap tag check.
                if (item.IsTag("RIBWICH_TAG"))
                {
                    bool fake = RibwichHelper.IsCounterfeit(item);
                    result.Kind = fake ? TruthKind.Counterfeit : TruthKind.Genuine;
                    result.Label = fake ? "假货" : "真品";
                    result.Reason = fake ? RibwichReason(item) : null;
                    return true;
                }
                if (item.IsTag("WINE_COUNTERFEIT_TAG"))
                {
                    result.Kind = TruthKind.Counterfeit;
                    result.Label = "假货";
                    result.Reason = "带有假酒标记";
                    return true;
                }

                TruthBox cached;
                if (ResultCache.TryGetValue(item, out cached))
                {
                    if (cached.HasValue)
                    {
                        result = cached.Value;
                    }
                    return cached.HasValue;
                }

                TruthResult computed;
                bool ok = Compute(item, out computed);
                ResultCache.Add(item, new TruthBox { HasValue = ok, Value = computed });
                result = computed;
                return ok;
            }
            catch (Exception e)
            {
                Log.Warn("Authenticity check failed: " + e.Message);
            }
            return false;
        }

        private static bool Compute(GameItem item, out TruthResult result)
        {
            result = default(TruthResult);
            ItemFeature feature = item.FindItemFeatureByCategory("CATEGORY_GENUINE_CIGARETTE");
            if (feature != null && feature.realCondition != null)
            {
                return FromCondition(feature, item, out result);
            }
            feature = item.FindItemFeatureByCategory("CATEGORY_GENUINE_STAMP");
            if (feature != null && feature.realCondition != null)
            {
                return FromCondition(feature, item, out result);
            }
            feature = item.FindItemFeatureByCategory("CATEGORY_GENUINE_INJECTOR");
            if (feature != null && feature.realCondition != null)
            {
                return FromCondition(feature, item, out result);
            }
            feature = item.FindItemFeatureByCategory("CATEGORY_REPLICA");
            if (feature != null && feature.realCondition != null)
            {
                return FromCondition(feature, item, out result);
            }
            return false;
        }

        private static bool FromCondition(ItemFeature feature, GameItem item, out TruthResult result)
        {
            string id = feature.realCondition.identifier ?? "";
            result = new TruthResult();
            switch (id)
            {
                case "genuineCigarette":
                case "genuineStamp":
                case "genuineInjector":
                case "non_replica":
                    result.Kind = TruthKind.Genuine;
                    result.Label = "真品";
                    return true;
                case "counterfeitCigarette":
                case "counterfeitStamp":
                case "counterfeitInjector":
                    result.Kind = TruthKind.Counterfeit;
                    result.Label = "假货";
                    result.Reason = CounterfeitReason(item);
                    return true;
                case "expiredInjector":
                    result.Kind = TruthKind.Defective;
                    result.Label = "有缺陷";
                    result.Reason = "已过期" + ReasonSuffix(item);
                    return true;
                case "unusableInjector":
                    result.Kind = TruthKind.Defective;
                    result.Label = "有缺陷";
                    result.Reason = "无法使用" + ReasonSuffix(item);
                    return true;
                case "replica":
                    result.Kind = TruthKind.Replica;
                    result.Label = "复制品";
                    result.Reason = "复制品，非原版";
                    return true;
                default:
                    return false;
            }
        }

        private static string CounterfeitReason(GameItem item)
        {
            string issues = CollectIssues(item);
            return string.IsNullOrEmpty(issues) ? "与正品特征不符" : issues;
        }

        private static string ReasonSuffix(GameItem item)
        {
            string issues = CollectIssues(item);
            return string.IsNullOrEmpty(issues) ? "" : "（" + issues + "）";
        }

        private static string RibwichReason(GameItem item)
        {
            List<string> reasons = new List<string>();
            try
            {
                int month = item.GetTagReadonly("RIBWICH_PRODUCTION_MONTH_TAG").GetInt();
                int year = item.GetTagReadonly("RIBWICH_PRODUCTION_YEAR_TAG").GetInt();
                int facility = item.GetTagReadonly("RIBWICH_FACILITY_NUMBER_TAG").GetInt();
                int logo = item.GetTagReadonly("RIBWICH_DAVRE_LOGO_TAG").GetInt();
                if (!RibwichHelper.IsValidProductionDate(month, year)) reasons.Add("生产日期异常");
                if (!RibwichHelper.IsValidFacilityNumber(facility)) reasons.Add("设施编号异常");
                if (logo != RibwichHelper.RIBWICH_DAVRE_LOGO_CORRECT) reasons.Add("商标异常");
                if (RibwichHelper.IsOpened(item)) reasons.Add("已开封");
            }
            catch (Exception)
            {
            }
            if (reasons.Count == 0) reasons.Add("包装与正品不符");
            return string.Join("、", reasons.ToArray());
        }

        private static string CollectIssues(GameItem item)
        {
            List<string> issues = new List<string>();
            if (item.IsTag("CIGARETTE_LETTERING_ISSUE")) issues.Add("字母印刷问题");
            if (item.IsTag("CIGARETTE_COLOR_ISSUE")) issues.Add("颜色问题");
            if (item.IsTag("CIGARETTE_SEAL_ISSUE")) issues.Add("防伪标问题");
            if (item.IsTag("CIGARETTE_LOGO_ISSUE")) issues.Add("商标问题");
            if (item.IsTag("CIGARETTE_BOX_ISSUE")) issues.Add("包装盒问题");
            if (item.IsTag("STAMP_LETTERING_ISSUE")) issues.Add("字母印刷问题");
            if (item.IsTag("STAMP_LOGO_ISSUE")) issues.Add("商标问题");
            if (item.IsTag("STAMP_SEAL_ISSUE")) issues.Add("防伪标问题");
            if (item.IsTag("STAMP_AMOUNT_ISSUE")) issues.Add("面额问题");
            if (item.IsTag("STAMP_CHIP_ISSUE")) issues.Add("芯片问题");
            if (item.IsTag("ISSUE_INDICATOR")) issues.Add("指示灯问题");
            if (item.IsTag("ISSUE_LABEL")) issues.Add("标签问题");
            if (item.IsTag("ISSUE_SERIAL")) issues.Add("序列号问题");
            if (item.IsTag("ISSUE_BODY")) issues.Add("瓶体问题");
            if (item.IsTag("ISSUE_COLOR")) issues.Add("颜色问题");
            return issues.Count == 0 ? null : string.Join("、", issues.ToArray());
        }
    }

}
