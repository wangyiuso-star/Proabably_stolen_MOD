# CardCraftAndFence 開發狀態

更新時間：2026-11-16（P3 三件套完成，程式＋離線驗證＋**實機驗證**）

## 階段

- **P1 骨架：完成。** 自建 `CfItemDirectory`、5 家族 T1/T2 卡與核心 factory。
- **P2 箱體與開鎖：完成，實機驗證。** 6 個 T1/T2 箱、`TryOpenLock` prefix、
  tooltip、`[CF-LOCK]` log。
- **P2 loot：完成，實機驗證。** 六箱 × common/rare 12 張表、`DirectoryMaster.Item`
  postfix 填充、rolls → rare → `LockUpContainer`、存檔解碼跳過填充。
- **P3：完成，實機驗證。** 3D 印表機空白卡晶片
  `cf_printer_chip_blank_keycard`、專業密碼解序器 `cf_pro_desequencer`、
  批量模組 `cf_batch_card_module`，外加 1 個新 patch
  （`MachinePrinter.FinishPrinting` prefix）。實機 log 見「P3 實機驗證」一節。
- **P4：未開始。** 本輪依指示不開，由下一個對話接手。

## 目前 build

| 項目 | 值 |
|---|---|
| ModVersion | 0.3.0 |
| DLL | `dist/CardCraftAndFence/CardCraftAndFence.dll` |
| 大小 | 40,960 bytes |
| SHA256 | `C8FFF6084E3E56CFA491AD75650F9DE361F66390347EBD4050240AC50DE31E71` |
| Mods 安裝 | `.../Probably Stolen/Mods/CardCraftAndFence`（已覆蓋為本版；覆蓋時遊戲在跑，**需完全退出再開**才生效） |
| dist / Mods hash | 相同（皆為上列 SHA256） |
| git | P1–P3 已提交（本機 commit，**未 push**）；`dist/` 與 `backup/` 依 `.gitignore` 不進版控 |

## P3 實機驗證（2026-11-16，Player.log 逐行）

`...\LocalLow\Questing Goose Studio\Probably Stolen\Player.log`（20:04 那次 session，
載入的是上列 40,960 bytes／`C8FFF6…` 版本）：

```text
[CF-PRINT] batch=2 extra=1 setsBefore=1 setsAfter=0      x2   印表機：2 電子 + 2 耗材 → 2 張空白卡
[CF-DESQ] start plan=1 target=cmd_keycard work=150/104 speed=104 power=46
[CF-DESQ] night progress=208/150 speed=104
[CF-DESQ] finished target=cmd_keycard produced=1 consumed=1
[CF-DESQ] start plan=2 target=cmd_keycard work=150/79 speed=79 power=56
[CF-DESQ] night progress=158/150 speed=79
[CF-DESQ] finished target=cmd_keycard produced=2 consumed=2
[CF-DESQ] start plan=2 target=cf_cmd_keycard_t2 work=250/79 speed=79 power=56
[CF-DESQ] night progress=158/250 speed=79
[CF-DESQ] night progress=237/250 speed=79
[CF-DESQ] night progress=316/250 speed=79
[CF-DESQ] finished target=cf_cmd_keycard_t2 produced=2 consumed=2
```

對照驗收卡：

| # | 項目 | 結果 | 證據 |
|---|---|---|---|
| 1 | 印表機 + CNC + 空白卡晶片 → blank_keycard | 通過（使用者實測；空白卡確實產出並拿去解序） | `[CF-PRINT]` |
| 2 | 加批量模組：2 電子 + 2 耗材 → 2 張 | 通過 | `[CF-PRINT] batch=2 extra=1 setsBefore=1 setsAfter=0` ×2 |
| 3 | 解序器：核心 + blank_keycard + 電 → 對應卡 | 通過（T1 `cmd_keycard`、T2 `cf_cmd_keycard_t2` 都實際做出） | `[CF-DESQ] start/finished` |
| 4 | 批量：2 張空白卡 → 2 張；1 張 → 1 張 | 通過（`plan=2 produced=2`、`plan=1 produced=1`） | 同上 |
| 5 | 拔電／輸出滿 → 進度停住、插回可續 | 使用者實測「可以」；log 沒有斷電樣本 | — |
| 6 | 存檔 → 重載 → 進度與狀態一致 | 使用者實測「可以」；log 另證跨多晚持續推進（progress 遞增到 316/250） | — |
| 7 | 第二個批量模組不疊加 | 未直接測；程式是 `count > 0 ? 2 : 1` 的硬上限 | 程式碼 |

註：log 的 `speed=104/79`、`power=46/56` 是該機器模組欄裡**所有**模組經
`ModuleHelper` 疊加的結果（本機 base 是 speed 50／power 20，批量模組本身 -50/-50）；
mod 不自己乘係數，所以換插別的模組這些數字就會不同。

## 已驗收（本輪實際輸出）

- `tools/Build-Mods.ps1`：exit 0，三個 DLL（15,872／9,216／40,960 bytes）。
- `tools/Verify-Mods.ps1`：`RESULT: PASS`；CardCraft 4 個 patch：
  `CfTryOpenLockPatch -> LockHelper.TryOpenLock`、
  `CfCrateTooltipPatch -> LockHelper.CreateLockerContainerTooltip`、
  `CfCrateFillPatch -> DirectoryMaster.Item`、
  `CfPrinterBatchPatch -> MachinePrinter.FinishPrinting`；無 banned API。
- `tools/Probe-Patches.ps1`：`patched 8 / 4 / 4`，`RESULT: PASS`。
- `tools/Probe-Logic.ps1`：CardCraft scan `refused=False capabilities=[] findings=0`，PASS。
- 動作指令註記：本機沒有 PowerShell 7，`pwsh` 不在 PATH，四條指令實際以
  `powershell -NoProfile -ExecutionPolicy Bypass -File tools\…` 執行（同一腳本、
  同一輸出）。等價命令與輸出見 `docs/CARD_CRAFT_SPEC.md` §3.5 與本檔上列項目。
- `.scratch/p3check`（scratch-only，不進 MOD）：
  - `CardBatch.AvailableSets` 5 組：2/2→2、2/1→1、1/3→1、3/2→2、0/0→0 全 PASS。
  - IL 常數核對：晶片 40／`blank_keycard`／`common_electronic`／`printer_plastic`／
    `CNC Module`／`PRINTER_CHIP_TAG`；模組 `-50`／`MODULE_TYPE_UNIVERSAL`／
    `long_module2`／`CF_CARD_BATCH_MODULE_TAG`；解序器 50／20／9×6／9×5／2×1／
    `DESEQUENCER_STATE_TAG`／`STATE_READY`／`STATE_DESEQUENCING`；PASS。
  - 回歸檢查：`ProDesequencer.Start` / `Continue` 都有呼叫 `OutputHasRoom`，且
    方法內沒有 `"blank_keycard"` 字面值（防止輸出探測再拿空白卡去問）→ PASS。

## P3 實作要點（詳見 spec §3.5）

- 晶片：`PRINTER_CHIP_TAG`、目標 `blank_keycard`×1、工作量 40、額外材料
  `common_electronic`×1 + `printer_plastic`×1、`AddRequiredMachineTag("CNC Module")`；
  **沒有為晶片新增 patch**。
- 解序器：面板／tag／`InitProgressTypeMachine(50)`／`InitMachinery(20)`／
  `SetupBatterySlot` 照 vanilla；**視窗照 3D 印表機（電源／模組／輸入 9×6／芯片槽 2 格／輸出 9×5／便條）**，input/output 是 grid 而非單格（已由使用者裁定，
  否則驗收 #4 不可能成立，因為 `blank_keycard` 不可堆疊）；input 只收
  `blank_keycard`、output 只收非 blank 的 `ACCESS_CARD`、核心不消耗。
- 批量模組：`long_module2`、`MODULE_TYPE_UNIVERSAL`、`-50/-50/0`；本體不乘係數，
  速度／耗電由 `ModuleHelper` 算；`batch = 1 + (有模組 ? 1 : 0)`，上限 2。
- 批量只在印表機需要 patch：先把多出來的卡實際放進輸出格、再探測 vanilla 那一張
  的位置；放不下就全部撤回並中止 finish（保留進度、不額外消耗、不產出）。
- 進度／狀態全在 machine tag 與 wip 卡的 `CF_CARD_WIP_TAG`，mod 不另存進度。

## P3 修正記錄（0.3.0 內）

- **解序器不啟動（已修）**：`Start`/`Continue` 的「輸出還有沒有位」探測原本拿
  `blank_keycard` 去問輸出格，但輸出格刻意拒絕 blank 卡，`GraphUtils.CanAccept`
  永遠 false → 機台每晚都提早 return，看起來像沒實作。改成用**實際會產出的卡 id**
  探測（`DESEQUENCER_TARGET_ID_TAG`）。
- 加了 `[CF-DESQ]` 診斷 log：`waiting:`（沒電／沒晶片／沒空白卡／輸出滿）、
  `start plan=… target=… work=… speed=… power=…`、`night progress=cur/target`、
  `finished target=… produced=… consumed=…`。
- `.scratch/p3check` 新增回歸檢查：`Start`/`Continue` 必須呼叫 `OutputHasRoom`，
  且方法內不得出現 `"blank_keycard"` 字面值。
- 實機已證實可用的一條（舊版 log）：`[CF-PRINT] batch=2 extra=1 setsBefore=1 setsAfter=0`。

## 未決 / 未核實

- 驗收卡 #5（拔電／輸出滿）、#6（存檔重載）、#7（第二顆批量模組）
  使用者口頭實測「可以」，但 `Player.log` 沒有對應樣本：#5 沒有斷電期間的
  `[CF-DESQ] paused: output full` 或 `waiting: no charged battery`，
  #6 沒有跨 session 續跑的記錄，#7 沒有第二顆模組的 log。
- `unitValue`（晶片 250／解序器 800）與貼圖（`printer_chip2`／`desequencer`／
  `desequencer_r`／`long_module2`）是暫定值，規格未指定。
- 解序器卡片**不改** `blank_card_wip` 貼圖：該切片真實 footprint 無法離線核實，
  改 shape 是唯一可能弄壞 grid 格子的動作。
- `long_module2` 是否真的是 1×4：由 sprite 切片決定，未在 runtime 量過。
- P3 物品如何取得（除 F9 dev spawner 外）未設計；本輪沒有加入任何商店／掉落表。
- **T3 箱**：9×7、300／500，未做。
- **黑市、索耶、退休匠人、Wild Network、紙本**：未做。
- **完整 §3.1–3.11 規格**：仍未補齊；目前只有 P1/P2 定案段、§3.3 loot 表、
  §3.4 errata 與 §3.5 P3 實作。
- **`Resources.LoadAll<Sprite>` runtime 清單**：目前只有 asset 字串掃描。
- **箱子販售**：小偷 `isStolen`、CMD 固定價不標 stolen、`acceptRetailMarkup`
  都未接。
- **CMD T2 -7.37%**：已接受的例外，若 P4 要拉回 5% 內可再談容量／rolls。

## 下一步（P4，新對話）

1. 想補齊 #5/#6/#7 的 log 證據時，照「P3 實機驗證」一節的 `[CF-…]` 行對照即可。
2. T3 箱體、loot、開鎖（`CF_CRATE_TIER=3`，容量 9×7，價 300／500）。
3. 黑市、索耶、退休匠人、Wild Network、紙本。
4. 箱子販售（小偷 stolen、CMD 固定價）與 `Resources.LoadAll` 實機清單。
5. 補 §3.1–3.11 全文。

## 禁區

- 不要從 agent 啟動 Steam／遊戲；需要實機時出測試卡給人類。
- 不要改使用者存檔、Steam registry、Steam 進程。
- 不要動已完成 P1/P2 行為；不要改原版物品價格。
- 不要把 `.scratch/` 的參考源或 tool 輸出編譯進 MOD。
- 不要 push、不要改 `Assembly-CSharp.dll`。

## 主要檔案

- `src/CardCraftAndFence/CardCraftAndFence.cs`（1,739 行；P3 新增 `CardBatch`、
  `ProDesequencer`、`CfPrinterBatchPatch`）
- `src/CardCraftAndFence/manifest.xml`
- `docs/CARD_CRAFT_SPEC.md`（新增 §3.5）
- `tools/Build-Mods.ps1`、`tools/Verify-Mods.ps1`、`tools/Probe-Patches.ps1`、
  `tools/Probe-Logic.ps1`（本輪未改）
- `.scratch/p3check/Program.cs`（scratch-only 離線核對，不進 MOD）
