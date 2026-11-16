# 製卡與銷贓（CardCraftAndFence）規格

> §3.1–3.11 全文仍未補齊。本文件目前包含：P1/P2 已定案事實、
> §3.3 箱體定案表、§3.3 loot 表、貼圖決策與 errata、§3.4b 的 T3 卡／核定價，
> §3.5 的 P3 實作（空白卡晶片、專業密序器、批量模組），以及 P4 的
> §3.8（退休製卡匠）、§3.10(2)（小偷分級箱）、§3.11（Wild Network）。

## §3.3 箱子 — 定案表（P2 已實作，T3 未做）

| 家族 | T1 id | T1 容量 | T1 價 | T2 id | T2 容量 | T2 價 | sprite |
|---|---|---|---:|---|---:|---:|---|
| SEC | `sec_box`（原版） | 7×5 | 200 | `cf_sec_box_t2` | 7×7 | 225 | `sec_box` |
| MED | `med_box`（原版） | 7×5 | 200 | `cf_med_box_t2` | **8×8** | 225 | `med_box` |
| ENG | `eng_box`（原版） | 7×5 | 200 | `cf_eng_box_t2` | 7×7 | 225 | `eng_box` |
| SER | `service_box`（原版） | 7×5 | 175 | `cf_ser_box_t2` | 7×7 | 225 | `service_box` |
| CMD | `cf_cmd_box_t1`（新） | **7×7** | 200 | `cf_cmd_box_t2` | **9×8** | 350 | `sec_box` |

- **容量校正**：MED T2 用 8×8、CMD T1 用 7×7、CMD T2 用 9×8。原因是
  10／11 rolls + rare 在原本 7×7／7×5 會放不下（med 實測 -13.36%、
  CMD T1 -12.79%）；SEC／ENG／SER 的 7×7 實測都 fit，維持不變。
  容量只影響「放不放得下」，不改 EV／價與件數。
- T1 SEC／MED／ENG／SER 沿用原版 id，不重新註冊、不加 `CF_CRATE_*`，
  走原版 `LockHelper.TryOpenLock`；原版價格就是 200／200／200／175。
- **§3.3 T1 價格 errata**：spec 表寫 T1 一般箱 150，與「T1 沿用原版 id」衝突；
  以代碼為準，T1 實際是 SEC／MED／ENG = 200、SER = 175，不能改。
  T1 CMD 是新物品，用 200；T2 才用 225／350。
- 新箱加 `CF_CRATE_FAMILY`、`CF_CRATE_TIER`；`LOCK_ID` 由
  `LockHelper.InitLockedContainerItem` 設定，`LockUpContainer` 上鎖。
- T3 箱未做；T3 容量 9×7、價格 300／500 仍待 P3 以後。

## §3.3 Loot — 原版基準、校準與實作

### 原版基準（runtime `[CF-EV]`，200 samples/box）

`CfEvProbe`（已移除）當時對原版箱各生成 200 個，統計直接子物品 `unitValue` 平均；
`CloseContentWindow(true)` 後 `Destroy()`，不存檔。

| 原版箱 | mean | 原版價 | EV/價 |
|---|---:|---:|---:|
| `sec_box` | 698.91 | 200 | 3.49 |
| `med_box` | 706.95 | 200 | 3.53 |
| `eng_box` | 604.42 | 200 | 3.02 |
| `service_box` | 458.11 | 175 | 2.62 |

> 上一版提案的 MED T1 0.96 是**靜態提案表**的數字，不是 runtime；實測原版
> `med_box` 是 3.53。T1 SEC／MED／ENG／SER 箱子是原版物品、內容由
> `Fill*` 生成，`RegisterLootTable` 不會呼叫；因此 T1 表已刪除，只保留這張
> runtime 基準。T1 CMD（`cf_cmd_box_t1`）是新物品，下表生效。

### 校準後生效表（T2 五箱 + `cf_cmd_box_t1`）

軟目標：T2 不得低於同家族 T1，且至少高 5%。原本的「+0.2」降為軟目標，
不再為了 SEC 的 0.026 margin 抬權重；SEC 目前 +6.5% 已達標。
rolls 只用來對齊開箱件數；rare 保底維持 1，稀有池保留 T2 卡／核。
件數 = rolls + 1（rare 保底）。

| 箱 | rolls | 件數 | 原版密度參考 |
|---|---:|---:|---:|
| SEC T2 | 10 | 11 | 11.56 |
| MED T2 | 10 | 11 | 10.57 |
| ENG T2 | 5 | 6 | 5.13 |
| SER T2 | 6 | 7 | 6.57 |
| CMD T1 | 8 | 9 | — |
| CMD T2 | 10 | 11 | — |

**SEC T2（common EV 58.35 / rare EV 252.50）**

| common itemId | value | weight |
|---|---:|---:|
| `heavy_pistol_ammo` | 30 | 20 |
| `heavy_pistol_ammo_p` | 36 | 15 |
| `bandage_item` | 8 | 15 |
| `flashbang_grenade` | 85 | 15 |
| `smoke_grenade` | 60 | 15 |
| `stun_baton` | 165 | 10 |
| `combat_knife` | 75 | 10 |

| rare itemId | value | weight |
|---|---:|---:|
| `cf_sec_keycard_t2` | 150 | 30 |
| `cf_crypto_module_sec_t2` | 500 | 20 |
| `glock_receiver` | 200 | 15 |
| `stun_gun` | 250 | 15 |
| `tazer` | 100 | 10 |
| `current_converter` | 300 | 10 |

**MED T2（common EV 59.70 / rare EV 272.90）**

| common itemId | value | weight |
|---|---:|---:|
| `bandage_item` | 8 | 5 |
| `hemostatic_bandage_item` | 16 | 5 |
| `blood_bag` | 33 | 5 |
| `salve` | 80 | 22 |
| `rubbing_alcohol` | 35 | 10 |
| `common_medical` | 40 | 15 |
| `common_chemical` | 50 | 10 |
| `pink_injector` | 50 | 15 |
| `surgery_tool` | 200 | 8 |
| `galaxy_blend` | 25 | 5 |

| rare itemId | value | weight |
|---|---:|---:|
| `cf_med_keycard_t2` | 110 | 25 |
| `cf_crypto_module_med_t2` | 400 | 40 |
| `black_injector` | 350 | 15 |
| `surgery_tool` | 200 | 10 |
| `blue_blood_bag` | 250 | 5 |
| `bandage_item` | 8 | 5 |

**ENG T2（common EV 89.34 / rare EV 307.00）**

| common itemId | value | weight |
|---|---:|---:|
| `common_electronic` | 33 | 15 |
| `nuts_metal` | 14 | 8 |
| `flux_agent` | 4 | 8 |
| `screwdriver` | 25 | 8 |
| `wire_cutter` | 40 | 8 |
| `crowbar` | 75 | 10 |
| `welder` | 80 | 10 |
| `turbo_booster` | 35 | 5 |
| `module_extractor` | 100 | 10 |
| `current_converter` | 300 | 5 |
| `system_capped_neural_core` | 250 | 10 |
| `module_extractor_advanced` | 350 | 3 |

| rare itemId | value | weight |
|---|---:|---:|
| `cf_eng_keycard_t2` | 110 | 20 |
| `cf_crypto_module_eng_t2` | 400 | 40 |
| `module_extractor_advanced` | 350 | 10 |
| `turbo_booster_adv` | 300 | 10 |
| `advanced_flux_agent` | 300 | 10 |
| `current_converter` | 300 | 10 |

**SER T2（common EV 80.95 / rare EV 198.90）**

| common itemId | value | weight |
|---|---:|---:|
| `processed_cheese` | 20 | 8 |
| `box_cutter` | 28 | 10 |
| `kitchen_knife` | 45 | 15 |
| `hatchet` | 55 | 12 |
| `caffeine_pill` | 10 | 12 |
| `kotton_seed` | 20 | 5 |
| `hydroreed_seed` | 20 | 5 |
| `module_bay_expansion_kit` | 150 | 26 |
| `current_converter` | 300 | 7 |

| rare itemId | value | weight |
|---|---:|---:|
| `cf_ser_keycard_t2` | 60 | 20 |
| `cf_crypto_module_ser_t2` | 300 | 40 |
| `current_converter` | 300 | 15 |
| `module_bay_expansion_kit` | 150 | 10 |
| `hatchet` | 55 | 10 |
| `box_cutter` | 28 | 5 |

**CMD T1（`cf_cmd_box_t1`，common EV 55.50 / rare EV 238.50）**

| common itemId | value | weight |
|---|---:|---:|
| `heavy_pistol_ammo` | 30 | 25 |
| `heavy_pistol_ammo_p` | 36 | 15 |
| `flashbang_grenade` | 85 | 10 |
| `smoke_grenade` | 60 | 10 |
| `bandage_item` | 8 | 10 |
| `common_electronic` | 33 | 10 |
| `stun_baton` | 165 | 10 |
| `combat_knife` | 75 | 10 |

| rare itemId | value | weight |
|---|---:|---:|
| `cmd_keycard` | 125 | 30 |
| `crypto_module_cmd` | 350 | 20 |
| `stun_gun` | 250 | 15 |
| `glock_receiver` | 200 | 10 |
| `smg` | 280 | 10 |
| `shotgun` | 310 | 5 |
| `current_converter` | 300 | 10 |

**CMD T2（common EV 104.39 / rare EV 374.00）**

| common itemId | value | weight |
|---|---:|---:|
| `heavy_pistol_ammo` | 30 | 22 |
| `heavy_pistol_ammo_p` | 36 | 12 |
| `flashbang_grenade` | 85 | 10 |
| `smoke_grenade` | 60 | 10 |
| `bandage_item` | 8 | 4 |
| `common_electronic` | 33 | 5 |
| `stun_baton` | 165 | 8 |
| `combat_knife` | 75 | 8 |
| `stun_gun` | 250 | 9 |
| `smg` | 280 | 5 |
| `shotgun` | 310 | 3 |
| `current_converter` | 300 | 4 |

| rare itemId | value | weight |
|---|---:|---:|
| `cf_cmd_keycard_t2` | 200 | 20 |
| `cf_crypto_module_cmd_t2` | 600 | 35 |
| `stun_gun` | 250 | 10 |
| `smg` | 280 | 10 |
| `shotgun` | 310 | 10 |
| `current_converter` | 300 | 10 |
| `glock_receiver` | 200 | 5 |

### 校準後 EV

`EV = rolls × EV_common + 1 × EV_rare`；所有表權重合計 100。

| 箱 | 價 | common EV | rare EV | rolls | 件數 | EV | EV/價 | 目標帶 |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| SEC T2 | 225 | 58.35 | 252.50 | 10 | 11 | 836.00 | **3.716** | 3.6–3.9 |
| MED T2 | 225 | 59.70 | 272.90 | 10 | 11 | 869.90 | **3.866** | 3.6–3.9 |
| ENG T2 | 225 | 89.34 | 307.00 | 5 | 6 | 753.70 | **3.350** | 3.2–3.5 |
| SER T2 | 225 | 80.95 | 198.90 | 6 | 7 | 684.60 | **3.043** | 2.9–3.2 |
| CMD T1 | 200 | 55.50 | 238.50 | 8 | 9 | 682.50 | **3.413** | 3.2–3.5 |
| CMD T2 | 350 | 104.39 | 374.00 | 10 | 11 | 1417.90 | **4.051** | 4.0–4.5 |

軟目標檢查（T2 ≥ 同家族 T1 且至少 +5%）：

- SEC T2 3.716 vs T1 3.49 = **+6.5%**（達標；不再抬權重）
- MED T2 3.866 vs T1 3.53 = **+9.5%**
- ENG T2 3.350 vs T1 3.02 = **+10.9%**
- SER T2 3.043 vs T1 2.62 = **+16.1%**
- CMD T2 4.051 vs CMD T1 3.413 = **+18.7%**

以上為模型 EV；實測見下（`CfLootProbe` 已移除，clean build 不含 probe）。

### 模型 vs 實測（最終，probe 已移除）

`CfLootProbe` 對六種 cf 箱各生成 200 個，統計直接子物品 `unitValue` 平均；
`CloseContentWindow(true)` 後 `Destroy()`，不存檔。

| 箱 | model EV | measured mean（最終輪） | error | 判定 |
|---|---:|---:|---:|---|
| SEC T2 | 836.00 | 828.50 | -0.90% | PASS |
| MED T2 | 869.90 | 876.97 | +0.81% | PASS |
| ENG T2 | 753.70 | 754.60 | +0.12% | PASS |
| SER T2 | 684.60 | 716.22 | +4.62% | PASS |
| CMD T1 | 682.50 | 674.13 | -1.23% | PASS |
| CMD T2 | 1417.90 | 1313.39 | -7.37% | 接受例外（使用者裁定「差一點沒事」） |

前幾輪的 fit 問題：MED 7×7 → 8×8、CMD T1 7×5 → 7×7、CMD T2 8×8 → 9×8；
CMD T2 另有 rolls 11→10、common EV 104.39。最終五箱在 ±5% 內；CMD T2
-7.37% 超過 5%，但 9×8 已是最後一次調整，依使用者裁定接受，不再加容量或改價。

## 箱子貼圖決策（asset 字串掃描，2026-11-16）

從 `Probably Stolen_Data/resources.assets` 掃到的 `Items/items_container` 箱類
sprite：

- 有 locked／unlocked 成對：`sec_box`／`sec_box_u`、`med_box`／`med_box_u`、
  `eng_box`／`eng_box_u`、`service_box`／`service_box_u`、
  `evidence_box`／`evidence_box_u`。
- 沒有 `_u` 的箱類：`eleanor_box`、`expedition_box`、`cardboard_box`、
  `toolbox_new`、`toolbox_old`。
- **沒有 `cmd_box`／`cmd_box_u`**。

決策：CMD 箱（`cf_cmd_box_t1`、`cf_cmd_box_t2`）使用
`Items/items_container` + `sec_box`；`LockHelper.UnlockContainer` 會把
`SUPPLY_CRATE` 的 sprite 換成 `sec_box_u`，兩張圖都存在，不會 fallback 成
`unknown`。代價是 CMD 箱圖示與 SEC 箱相同；好處是 locked／unlocked 都有圖。
T1 SEC／MED／ENG／SER 用原版箱，不動貼圖。

> 這是 asset 字串掃描，不是 `Resources.LoadAll<Sprite>` 的 runtime 結果；
> 實機第一次進遊戲時仍應以 `Resources.LoadAll` 印出的清單為準。

## P2 測試備註

- **前置**：`Player.log` 需有 `[DevTools] Developer tools enabled`，否則 F9 的
  debug spawner 不會開；測試卡要先檢查這行。
- `GraphUtils.TryAcceptAll` 的回傳是**剩餘量**（IL：`amount - accepted`），
  不是接受量。測試 log 應寫 `remaining=0 accepted=1/1`，不要寫
  `add accepted=0`。
- `ModHook.OnGameLoadedLate` 只在 `PlayerStore.LoadGame()` 觸發；新遊戲走
  `NewGameData.HandleInitialItem()` → `OnNewGameStarted`。P2 測試卡只寫
  「載入存檔」，不寫「或新遊戲」。
- 開鎖 prefix 的規則：cf 箱有 `CF_CRATE_TIER` 才接管；CMD 卡可開任何家族，
  但 `cardTier >= crateTier`；其他卡需 `cardFamily == crateFamily` 且
  `cardTier >= crateTier`。可開 → `audioClickClick` + `key.Destroy()` +
  `UnlockContainer`；不可開 → `audioNegativeTwoBeep`，不消耗卡。
- 非 cf 家族的 `ACCESS_CARD`（例如 `sci_keycard`／`sup_keycard`／
  `blank_keycard`）打 cf 箱時 **一律拒絕**，不 fallback 到原版
  `LOCK_ID_COMMAND` 通殺規則。
- `[CF-LOCK]` log 會帶 `key=… keyLockId=… box=… card=…/… crate=…/… canOpen=…`。

## §3.4 卡／核心價格與工作量 — Errata

- **SER T1 核心工作量 = 75（不是 100）**。原版 `crypto_module_ser` 由
  `ModuleDirectory.CrypoModuleSer()` 以
  `MachineProgressHelper.InitProgressSourceItem(item, 75)` 建立
  （`ModuleDirectory.cs:430-445`）。T1 是原版物品，沿用不可改；T2 才是 150。
- 其餘 T1 核心工作量：CMD 150、SEC 125、MED 100、ENG 100；T2：CMD 250、SEC 200、
  MED 150、ENG 150、SER 150。
- 價格表不變（順序 CMD／SEC／MED／ENG／SER）：
  - T1 卡：125／100／75／75／40；T1 核：350／300／250／250／200。
  - T2 卡：200／150／110／110／60；T2 核：600／500／400／400／300。

## §3.4b T3 卡與核（P4 新增，定價／工作量）

P1–P3 只建了 T1／T2。P4 的退休製卡匠庫存要求「1 張 T3 卡 + 1 個 T3 核」，
所以本輪補 5 家族各一張 T3 卡、一個 T3 核。定價規則（沿用 T2 → T3 的
+50% 曲線，四捨五入到整數）：

| 家族 | T3 卡 id | T3 卡價 | T3 核 id | T3 核價 | T3 核工作量 |
|---|---|---:|---|---:|---:|
| CMD | `cf_cmd_keycard_t3` | 300 | `cf_crypto_module_cmd_t3` | 900 | 350 |
| SEC | `cf_sec_keycard_t3` | 225 | `cf_crypto_module_sec_t3` | 750 | 300 |
| MED | `cf_med_keycard_t3` | 165 | `cf_crypto_module_med_t3` | 600 | 250 |
| ENG | `cf_eng_keycard_t3` | 165 | `cf_crypto_module_eng_t3` | 600 | 250 |
| SER | `cf_ser_keycard_t3` | 90 | `cf_crypto_module_ser_t3` | 450 | 250 |

- 卡價 = T2 卡價 ×1.5；核價 = T2 核價 ×1.5；核工作量 = T2 工作量 +100。
- T3 沿用同家族 T2 的 sprite，`CF_CARD_FAMILY` / `CF_CARD_TIER=3`；
  開鎖規則 `cardTier >= crateTier` 自然成立。
- **T3 箱仍未做**（9×7、300／500），所以 T3 卡目前沒有可開的 T3 鎖；
  它的用途是退休製卡匠的高價庫存、密碼解序器產物與交易品。
- T3 核的 `DESEQUENCER_TARGET_ID_TAG` 指向同家族 T3 卡，專業解序器可直接做。

## §3.5 P3 — 空白卡晶片、專業密碼解序器、批量模組（0.3.0 已實作）

> 本節只寫「已寫進代碼、且已用離線驗證核對過」的事實。實機驗收（測試卡 1–7）
> 尚未跑，見 `CARD_CRAFT_STATUS.md`。

### §3.5.1 3D 印表機晶片 `cf_printer_chip_blank_keycard`

- 照 `ModuleDirectory.CreateMeleeChip` / `CreateCncChip` 既有模式建立，**沒有新增 patch**：
  - `Items/items_tool` + `printer_chip2`；`EnableTag("PRINTER_CHIP_TAG")`。
  - `MachineProgressHelper.SetDualAdditionalRequiredItem(chip, ("common_electronic",1), ("printer_plastic",1))`。
  - `MachineProgressHelper.InitProgressSourceItem(chip, 40, "blank_keycard", useDefaultTooltip: true, targetItemCount: 1)`。
  - `MachineProgressHelper.AddRequiredMachineTag(chip, "CNC Module")`（CNC 模組由
    原版 `PrinterModuleCNC` 在插模組時 `EnableTag("CNC Module")` 到機器上）。
  - `AudioHelper.InitSmallModule(chip)`。
- 晶片不消耗：`MachinePrinter.FinishPrinting` 只 `UnlockItem`，不 destroy。
- 面板顯示工作量 40／額外材料／需要 CNC 模組，全部由 `MachineProgressHelper`
  的既有 tooltip 產生。
- `unitValue = 250`、sprite `printer_chip2`：**規格未指定，為本輪暫定值**，
  只影響圖示與售價，不影響機制。

### §3.5.2 專業密碼解序器 `cf_pro_desequencer`

- **視窗外觀照 3D 印表機**（`MachinePrinter.CreateMachineInventoryWindow` 的同一組
  panel、同一順序）：電源／模組／輸入（9×6）／芯片槽／輸出（9×5）／便條。
  與印表機唯一的外觀差異是 **芯片槽 2 格**（`new GameSlotInventory(2, 1)`；
  vanilla 解序器與印表機都是 1 格）。此為使用者看圖後指定。
- 機制仍照 `MachineDesequencer.Desequencer()`：`ItemDirectory.CreateEmptyItem` +
  `MachineHelper.CreateMachineryWindow` + `MachineHelper.SetupBatterySlot`
  + `MachineryHelper.InitMachinery(machine, 20)`
  + `MachineProgressHelper.InitProgressTypeMachine(machine, 50, useDefaultTooltip: false)`。
- **輸入／輸出不是 vanilla 的單格 `GameSlotInventory`，而是 `GameGridInventory`**：
  `blank_keycard` 不可堆疊（`GameItem.MaxNumAdd` 在 `maxSlotAmountItemFunc` 為 null
  時上限 = 1），單格輸入永遠只有 1 張，驗收 #4「2 張空白卡 → 2 張卡」不可能成立。
  尺寸用印表機的 9×6／9×5，於是輸入盤同時變成一個「排隊槽」：一次開工仍只取
  `min(batch, 空白卡數)` 張。此差異已由使用者在 P3 開工時裁定（選項 A）。
- 過濾：input 只吃 owned 且 `identifier == blank_keycard`；output 只吃 owned、
  非 `blank_keycard` 的 `ACCESS_CARD`；chip slot 照 vanilla（owned、非 MACHINE、
  有 `DESEQUENCER_TARGET_ID_TAG`）。
- `useDefaultTooltip: false` 也是照 vanilla：進度由
  `MachineDesequencer.CreateMachineDesequencerTooltip`（以 `DESEQUENCER_STATE_TAG`
  判斷）顯示，開預設 tooltip 會重複一條進度。
- 每輪流程（`onCycleEndSlotItemFunc`，每晚一次，不輪詢、不記時間）：
  1. 沒電 → 直接 return（進度、輸入卡、產出全部保留）。
  2. `STATE_READY` 且有晶片／有空白卡／輸出還有位 → 抽電、`OnMachineActioned`、
     把晶片的目標 id 寫進 machine 的 `DESEQUENCER_TARGET_ID_TAG`、
     `StartProgressTypeMachine(machine, chip)`、`ContinueProgressTypeMachine`。
  3. 開工時 `plan = min(batch, 空白卡張數)`，把其中 `plan` 張標 `CF_CARD_WIP_TAG`、
     `GeneralHelper.LockItem`、改名「已修改的通行卡」＋描述改成進度。
     **不改貼圖**：vanilla 會換成 `blank_card_wip`，但那是在 1×1 slot 裡做的，
     該切片的真實 footprint 無法離線核實，改 shape 是唯一可能讓 grid 失效的
     動作，所以保留原卡面。
  4. `STATE_DESEQUENCING` → 先看輸出還有沒有位（沒有就不抽電、不加進度、不產出），
     有就抽電＋Continue；`IsProgressTypeMachineFinished` 才結算。
  5. 結算：先把 wip 張數的成品**實際放進輸出格**；全部放得下才摧毀 wip 卡並
     `FinishProgressTypeMachine` + 回 `STATE_READY`；放不下就撤回剛放的全部成品、
     保留進度、輸入卡不動，下一晚重試。「做能做的數量」只用於輸入不足，
     不用於輸出不足（部分批次會白吃輸入卡）。
- 核心不消耗（同 vanilla）：目標 id 在開工時已存在 machine tag，中途換／拔核心
  不影響當前批次。
- 進度與狀態全部在 machine 的 tag（`MACHINE_STATE_TAG`、`MACHINE_PROGRESS_CURRENT_TAG`、
  `MACHINE_PROGRESS_TARGET_TAG`、`CURRENT_PROCESSING_SPEED_TAG`、`DESEQUENCER_STATE_TAG`、
  `DESEQUQUENCER_PROGRESS_TAG`、`DESEQUENCER_TARGET_ID_TAG`）與 wip 卡上的
  `CF_CARD_WIP_TAG`，存檔自動帶走；mod 不另存進度。
- `unitValue = 800`、貼圖沿用 `desequencer`／插電 `desequencer_r`：**規格未指定，
  為本輪暫定值**。

### §3.5.3 批量模組 `cf_batch_card_module`

- `Items/items_module` + `long_module2`（1×4）、`SetGameItemType` 由
  `ModuleHelper.InitModuleItem` 設成 `MODULE`。
- `ModuleHelper.InitModuleItem(module, "MODULE_TYPE_UNIVERSAL", -50, -50, 0,
  isShipModule: true, "每輪制卡產出 +1")`：速度／耗電完全交給框架，
  `ApplyBasicModuleEffect` 會算成「速度 -50%、耗電 +50%」，mod 不乘任何係數。
- `MODULE_TYPE_UNIVERSAL` 讓同一顆模組同時能插 3D 印表機（收 UNIVERSAL／PRINTER）
  與專業解序器（收 UNIVERSAL／ALARM）。
- 效果標籤 `CF_CARD_BATCH_MODULE_TAG`。`batch = 1 + (模組欄裡有這個 tag ? 1 : 0)`，
  **上限 2，第二顆不再增加產出**；但兩顆的 -50/-50 統計仍會被
  `ModuleHelper` 疊加（vanilla 模組行為），所以插第二顆只有懲罰、沒有收益。

### §3.5.4 印表機側批量（P3 唯一的 patch）

`CfPrinterBatchPatch` = prefix on `MachinePrinter.FinishPrinting`，只在
`chipInv.childItem.identifier == cf_printer_chip_blank_keycard` 時做事：

- `batch == 1`（沒有批量模組）→ 直接放行，完全走 vanilla。
- `batch == 2`：
  - `sets` = 輸入盤（panel index 2）上、依晶片 `REQUIRED_ITEM_ID_*`／
    `REQUIRED_ITEM_COUNT_*` 能湊出的完整套數。
  - `extra = min(batch, sets + 1) - 1`；`+1` 是 vanilla 在開工時已經吃掉的那一套，
    所以 `2 電子 + 2 耗材` → `extra = 1`，總共消耗 2 套、產出 2 張。
  - 先把 `extra` 張卡真的放進輸出格，再探測「vanilla 即將放的那一張」還有沒有位
    （先放再探測才看得到剛放進去的卡）。
  - 任一放不下 → 撤掉剛放的卡、回傳 `false` 中止 `FinishPrinting`：進度保留、
    不額外消耗、不產出；下一晚重試（重試那晚仍會抽電，這是沿用 vanilla 的
    「先抽電再判定完成」順序）。
  - 放得下 → 每多 1 張呼叫一次 `MachineProgressHelper.ConsumeRequiredAdditionalItems`。
    **每次都要重讀 `tray.childItems`**：舊快照裡還留著上一次已摧毀的物件，
    而 `Destroy()` 第二次是 no-op，會出現「說消耗 2 套、其實只掉 1 套」。
- 工作量不變：progress target 永遠是晶片的 40，batch 不乘 target、不乘 speed。

### §3.5.5 實機驗證與未核實

- **2026-11-16 實機通過（`Player.log`）**：印表機批量 `2 電子 + 2 耗材 → 2 張空白卡`
  （`[CF-PRINT] batch=2 extra=1 setsBefore=1 setsAfter=0` ×2）；解序器 `plan=1` 與
  `plan=2` 都跑過，T1 `cmd_keycard` 與 T2 `cf_cmd_keycard_t2` 都實際產出
  （`[CF-DESQ] finished … produced=N consumed=N`）。逐行對照見
  `CARD_CRAFT_STATUS.md`「P3 實機驗證」。
- 仍未由 log 直接證明：驗收 #5（拔電／輸出滿）、#6（存檔重載）、#7（第二顆批量
  模組不疊加）。#5／#6 使用者口頭確認可用；#7 目前只有程式硬上限（`count > 0 ? 2 : 1`）。
- `blank_card_wip` 的真實格子大小（因此 §3.5.2 第 3 點不改貼圖）。
- `long_module2` 是否真的是 1×4（由 sprite 切片決定，未在 runtime 量過）。
- 專業解序器與晶片在存檔中的實際序列化結果（tag 應自動帶走，未逐欄驗）。


### §3.5.6 Errata — 輸出探測必須用「實際產出的卡」

- 0.3.0 初版 `Start`/`Continue` 用 `CardBatch.OutputHasRoom(outputGrid, "blank_keycard")`
  判斷輸出還有沒有位。但輸出格本來就拒絕 `blank_keycard`（`mayInventoryAddItemFunc`），
  `GraphUtils.CanAccept` 因此**永遠 false** → 機台每晚都走「輸出滿」分支直接 return，
  看起來就是「完全沒實裝」。
- 修正：探測改成**實際會產出的卡 id** —— `Start` 用晶片上的
  `DESEQUENCER_TARGET_ID_TAG`，`Continue` 用 machine 上記下的同一個 id。
  印表機側不受影響（輸出盤接受任何 owned 物品，且實機 log 已有
  `[CF-PRINT] batch=2 extra=1 setsBefore=1 setsAfter=0` 證明它會動）。
- 回歸檢查進 `.scratch/p3check`：`Start`/`Continue` 必須呼叫 `OutputHasRoom`，
  且方法內不得出現 `"blank_keycard"` 字面值（防止有人把探測改回空白卡）。
- 同時補診斷 log（同一原因只印一次）：
  `[CF-DESQ] waiting: no charged battery / no crypto module in the chip slot /
  no blank keycard in the input grid / output full`；開工印
  `[CF-DESQ] start plan=… target=… work=…/… speed=… power=…`；每晚印
  `[CF-DESQ] night progress=cur/target speed=…`；結算印
  `[CF-DESQ] finished target=… produced=… consumed=…`。
- **修正已在實機驗證**（2026-11-16）：同一份 `Player.log` 有 4 筆
  `[CF-DESQ] start/night/finished`，含 `plan=1` 與 `plan=2`、T1 與 T2 目標。

## §3.8 退休製卡匠（P4）

> 本節只寫「已寫進代碼、且已用離線驗證核對過」的定案。實機（製卡匠到訪、
> 買賣流程）仍需人類開遊戲驗，見 `CARD_CRAFT_STATUS.md`。

### §3.8.1 出現規則

- 解鎖條件：Wild Network 升級 `CF_FENCE_CONTACT`。
- 到訪日：`StoreStation.Instance.dayCounter % 7 == 6`（每週第 6 天），
  在 `ModHook.OnGenerateCustomerVeryLate` 掛 handler 後 `manager.AddClient(...)`。
  `dayCounter == 0` 不生成。
- **首次解鎖補訪**：`CF_FENCE_CONTACT` 買下後第一次
  `GenerateClient`（通常就是當天，不限星期）補一次到訪，避免玩家買完還要等
  最多 6 天。用 `ModHelper` 的存檔內 modData key `maker_last_day` 記錄最後到訪日；
  `maker_last_day < 0` 代表從未到訪。
- 一天最多一位：`maker_last_day == dayCounter` 時不再加。
- 每週到訪不消耗原版 `clientTracker`／cooldown，避免和其他客戶互相擋。

### §3.8.2 客戶欄位

| 欄位 | 值 | 說明 |
|---|---|---|
| `identifier` | `cf_retired_cardmaker` | 供 log／排除重複 |
| `displayName` | `退休制卡匠` | 在地化 key `name_retired_cardmaker`，缺 key 用中文 fallback |
| 立繪 | `SpriteDict.GetRandomLowerLevelSpriteName()` | 下層市民 |
| `clientFaction` | `FACTION_LOWER_LEVEL` | 與原版退休匠一致 |
| `clientIntent` | `SELL` | 客戶賣、玩家買 |
| `nonShootable` | `true` | 同原版退休匠 |
| `clientItemFeatureBuying` | `premiumBuy15` | 同原版退休匠 |
| `acceptRetailMarkup` | `false` | 固定售價，不套零售加成 |
| 對話 | `dialog_retired_cardmaker_1`（缺 key 用中文 fallback）+ `AddBasicDialog()` / `CompleteClientCreation()` | 與原版退休匠相同流程 |

### §3.8.3 庫存（售價 = 物品 `unitValue`，工匠不加價、不標 stolen）

每次到訪重新生成，不保存庫存。下列「解鎖升級」全部是 Wild Network §3.11 的 id。

| 解鎖升級 | 物品 id | 數量 | 單價 |
|---|---|---|---:|
| `CF_FENCE_CONTACT` | `printer` | 1 | 350 |
| `CF_FENCE_CONTACT` | `printer_module_cnc` | 1 | 400 |
| `CF_FENCE_CONTACT` | `cf_pro_desequencer` | 1 | 800 |
| `CF_FENCE_BATCH` | `cf_batch_card_module` | 1 | 250 |
| `CF_FENCE_SUPPLY` | `cf_printer_chip_blank_keycard` | 1 | 250 |
| `CF_FENCE_SUPPLY` | `common_electronic` | 5 | 33 |
| `CF_FENCE_SUPPLY` | `printer_plastic` | 5 | 12 |
| `CF_FENCE_T1` | 5 家族 T1 卡（`cmd/sec/med/eng/ser_keycard`） | 各 1 | 125／100／75／75／40 |
| `CF_FENCE_T1` | 5 家族 T1 核（`crypto_module_cmd/sec/med/eng/ser`） | 各 1 | 350／300／250／250／200 |
| `CF_FENCE_T2` | 隨機 3 個**不同家族**的 T2 卡 | 3 | 200／150／110／110／60 |
| `CF_FENCE_T2` | 隨機 3 個**不同家族**的 T2 核 | 3 | 600／500／400／400／300 |
| `CF_FENCE_T3` | 1 個隨機家族的 T3 卡 | 1 | 300／225／165／165／90 |
| `CF_FENCE_T3` | 同家族的 T3 核 | 1 | 900／750／600／600／450 |

- **不賣箱子**（任何 T1／T2／T3 箱都不在工匠庫存）。
- T2／T3 的隨機抽選用遊戲 `RNG`；T3 卡與核同家族，方便直接拿去解序。
- 全部 `isOwend: false`：玩家要付 `unitValue` 買；箱子以外的物品沒有額外 markup。
- 電力／彈藥等消耗品只給電子與列印耗材各 5，其他材料不賣。

### §3.8.4 診斷 log

- `[CF-MAKER] visit day=… gate=weekly|catchup stock=tools,supply,t1,t2,t3`
- `[CF-MAKER] skip: already visited today day=…`
- `[CF-MAKER] skip: CF_FENCE_CONTACT not unlocked`

## §3.10 黑市與小偷（P4 只做第 2 項）

> 第 2 項「小偷賣分級箱」本輪實作；其餘黑市內容（紙本、索耶）不做。

### §3.10.1 觸發

- 解鎖條件：Wild Network 升級 `CF_FENCE_THIEF`。
- 掛在 `StoreClientList.CreateThief` 的 postfix：走完原版 thief 對話的
  `endAction` 後，**最多再給 1 個 CF 分級箱**（原版 thief 的雜物／原版箱照舊）。
- 每次 thief 到訪只給 1 箱；不因 `GetBMRepStockMult()` 增加數量。
- 同時把 thief 的 `acceptRetailMarkup` 設為 `false`。

### §3.10.2 箱等級（黑市聲望 → 等級）

`bm = StoreReputation.GetBMReputation()`（原版範圍 −200～200）。

| 條件 | 箱等級 |
|---|---|
| `bm >= 60` | T2 |
| `bm >= 30` 且已解鎖 `CF_FENCE_THIEF_T2` | T2 |
| 其餘 | T1 |

- `CF_FENCE_THIEF_T2` 把門檻從 60 降到 30，不改箱的內容與價格。
- **T3 箱不做**：目前最高只到 T2（T3 箱仍是 9×7、300／500 的未來項）。

### §3.10.3 家族權重（合計 100）

| 家族 | 權重 | T1 id／價 | T2 id／價 |
|---|---:|---|---|
| SEC | 30 | `sec_box`／200 | `cf_sec_box_t2`／225 |
| MED | 25 | `med_box`／200 | `cf_med_box_t2`／225 |
| ENG | 20 | `eng_box`／200 | `cf_eng_box_t2`／225 |
| SER | 15 | `service_box`／175 | `cf_ser_box_t2`／225 |
| CMD | 10 | `cf_cmd_box_t1`／200 | `cf_cmd_box_t2`／350 |

- 用累積權重 + `RNG.GetRandomInt(0, 99)` 抽家族，再依上表取該家族對應等級的 id。
- **卡／核等級 ≤ 箱等級**：T2 箱的 loot 表只放 T2 卡／核（§3.3 表），
  T1 CMD 只放 T1（`cmd_keycard`／`crypto_module_cmd`）；T1 SEC／MED／ENG／SER
  是原版箱、用原版 `Fill*`，不含 CF 卡。故規則由表的內容保證，不需要 runtime 過濾。
- 箱子生成路徑：
  - **T1 SEC／MED／ENG／SER**：`ContainerItemDirectory` 建的是**空箱**，
    必須走 `PreBuiltItemHelper.LootCrateSecurity/Medical/Engineering/Service()`，
    由原版 `crateFillers` 填內容再上鎖。
  - **T1 CMD**（`cf_cmd_box_t1`）與 **T2 五箱**：`DirectoryMaster.Item` →
    §3.3 的 postfix 自動填 loot + 上鎖。
  - 用錯路徑會賣出空箱，所以 `CfThiefBoxes.NewBox` 必須照這張表分流。

### §3.10.4 販售標記

- 非 CMD 家族：`isStolen: true`（小偷貨，會被 `StolenHelper` 標 stolen；
  若該存檔尚未解鎖 stolen 系統則只當一般貨）。
- CMD 家族：`isStolen: false`（固定價、不標 stolen，沿用 §3.10 的 CMD 例外）。
- 售價 = 箱子的 `unitValue`（T1 200／175、T2 225／350），不加零售 markup。

### §3.10.5 診斷 log

- `[CF-THIEF] box family=… tier=… id=… bm=… stolen=… added=…`

## §3.11 Wild Network「CardFence」分頁（P4）

### §3.11.1 分頁寬度先量 tabBar

`WildNetworkStyle` 的實測常數（不是猜的）：

- 視窗寬 `WINDOW_W = 1664`；分頁條位置 `TABS_X = 536`、寬 `TABS_W = 1060`。
- 單一分頁 `TAB_W = 256`、`TAB_GAP = 12`；`WildNetworkUI.BuildTabs` 的步進是
  `num += 268f`（= 256 + 12），所以 n 個分頁實際佔 `n * 268 - 12`。

現有分頁（原版）：`""`（Basic）與 `"Advanced"` 共 2 個。

| 分頁數 | 佔用寬度 | 對 1060 |
|---:|---:|---|
| 2（現況） | 524 | 剩 536 |
| 3（＋CardFence） | **792** | **剩 268，夠** |
| 4（＋第 4 個分頁） | 1060 | 剛好貼齊、無餘量 |

結論：**CardFence 用第 3 個分頁，`tab = "CardFence"`，不需要塞進 Advanced。**
但**不准再加第 4 個分頁**（例：P5 索耶若想要獨立分頁，會剛好 1060、沒有邊距）；
P5 的索耶升級要嘛放進 `CardFence`，要嘛放進 `Advanced`，不得新增分頁。
若未來真的需要第 4 個分頁，必須先重做 `tabBar` 寬度（改 prefab），不是改這段算術。

### §3.11.2 標籤與註冊

- `OnEnable` 呼叫 `ModHelper.SetNetworkTabLabel("CardFence", () => "制卡")`；
  `GetTabLabel` 會先找 `UI` 表的 `network_ui_tab_CardFence`，沒有才用這個 provider。
- 8 個升級全部用 `ModHelper.AddNetworkUpgrade(...)`，`tab: "CardFence"`，
  `subtitle` 只有 `Cardmaker` 與 `Thief` 兩種（分成兩張卡）。
- title／description 用 `Func<string>` provider 直接回中文（本 MOD 沒有自己的
  在地化檔）；`GetLocalizedTitle` 會先找 `NetworkUpgrade` 表的 `network_<id>_title`
  key，找不到才用 provider，所以之後補在地化不用改代碼。

### §3.11.3 8 個升級（價格定案）

| id | 價格 | 前置 | subtitle | 效果 |
|---|---:|---|---|---|
| `CF_FENCE_CONTACT` | 300 | — | Cardmaker | 解鎖退休製卡匠到訪；賣 3D 印表機、CNC 模組、專業密碼解序器 |
| `CF_FENCE_BATCH` | 800 | `CF_FENCE_CONTACT` | Cardmaker | 工匠加賣批量制卡模組 `cf_batch_card_module` |
| `CF_FENCE_SUPPLY` | 500 | `CF_FENCE_CONTACT` | Cardmaker | 工匠加賣空白卡晶片、電子 ×5、列印耗材 ×5 |
| `CF_FENCE_T1` | 1200 | `CF_FENCE_CONTACT` | Cardmaker | 工匠加賣 5 家族 T1 卡與 T1 核 |
| `CF_FENCE_T2` | 3000 | `CF_FENCE_T1` | Cardmaker | 工匠加賣隨機 3 張 T2 卡 + 3 個 T2 核 |
| `CF_FENCE_T3` | 9000 | `CF_FENCE_T2` | Cardmaker | 工匠加賣 1 張 T3 卡 + 同家族 1 個 T3 核 |
| `CF_FENCE_THIEF` | 1500 | `CF_FENCE_CONTACT` | Thief | 小偷每次到訪加賣 1 個 CF 分級箱（§3.10） |
| `CF_FENCE_THIEF_T2` | 4500 | `CF_FENCE_THIEF` | Thief | 箱等級門檻由黑市聲望 60 降到 30 |

- 8 個升級都是**取得管道**的開關，不直接給物品；玩家仍要付 `unitValue` 向工匠／小偷買。
- `state = 1`（未買）、不可重複、無 cooldown；`recommended` 不設，避免佔用
  recommended 卡位（該卡位是 10 欄、1118 寬，會把 CardFence 的分頁排版吃滿）。
- 圖示 `iconKey` 不指定：`WildNetworkUI.ResolveIcon` 會 fallback 到 `BUY_REFERAL`，
  不會出現空圖；P5 有專屬 icon 再補。

### §3.11.4 CF_SAWYER_* 隱藏

- Postfix `NetworkUpgrade.IsHiddenForRun(string id)`：`id` 以 `CF_SAWYER_` 開頭
  一律回 `true`。
- 本輪**不註冊**任何 `CF_SAWYER_*` 升級（P5 才做），只先把隱藏規則掛好；
  否則 P5 註冊後會直接出現在 CardFence 分頁裡。
- 注意原版 `IsHiddenForRun` 另有 `Advanced` 分頁要在 `IsPropertyPaid` 後才顯示的規則；
  postfix 只覆寫 `CF_SAWYER_*`，不動其他 id。

### §3.11.5 診斷 log

- `[CF-NET] registered 8 CardFence upgrades tab=CardFence width=792/1060`
- `[CF-NET] hide CF_SAWYER_* via IsHiddenForRun postfix`

## P4 未核實（截至本輪）

- 工匠、thief 的實際 UI／對話流程（需人類開遊戲）。
- T3 卡／核沒有對應的 T3 鎖，實際只能交易與收藏。
- 工匠庫存 40+ 件是否會把 18×4 的前台塞滿（每次到訪重新生成，理論上放得下）。