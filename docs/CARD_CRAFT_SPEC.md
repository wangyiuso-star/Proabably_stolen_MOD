# 製卡與銷贓（CardCraftAndFence）規格

> §3.1–3.11 全文仍未補齊。本文件目前包含：P1/P2 已定案事實、
> §3.3 箱體定案表、§3.3 loot 表、貼圖決策與 errata，以及 §3.5 的 P3 實作
> （空白卡晶片、專業密序器、批量模組）。

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