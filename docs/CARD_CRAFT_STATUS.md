# CardCraftAndFence 開發狀態

更新時間：2026-11-16（P4 完成：退休製卡匠、小偷分級箱、Wild Network「CardFence」分頁；程式＋離線驗證，**實機未跑**）

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
- **P4：完成（離線）。** 退休製卡匠客戶、小偷分級箱、Wild Network 8 個
  `CF_FENCE_*` 升級（`tab="CardFence"`）、`CF_SAWYER_*` 的 `IsHiddenForRun`
  隱藏規則；新增 5 家族 T3 卡／核。實機驗證（到訪、買賣、分頁）尚未跑。
  **P5（索耶）依指示未開。**

## 目前 build

| 項目 | 值 |
|---|---|
| ModVersion | 0.4.0 |
| DLL | `dist/CardCraftAndFence/CardCraftAndFence.dll` |
| 大小 | 51,200 bytes |
| SHA256 | `87CBDE783A3B23C3435FD788702C85F427DA91D6846A4795EDA908CFE168098B` |
| Mods 安裝 | 本輪**未覆蓋** `.../Probably Stolen/Mods/CardCraftAndFence`（沙箱只寫工作區）；要實機請先複製 `dist/CardCraftAndFence` 過去，並完全退出遊戲再開 |
| dist / Mods hash | 尚未同步（Mods 仍是 P3 的 `C8FFF6…`） |
| git | P1–P3 已提交（本機 commit，**未 push**）；P4 未提交；`dist/` 與 `backup/` 依 `.gitignore` 不進版控 |

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

## P4 已驗收（本輪實際輸出，2026-11-16）

- `tools/Build-Mods.ps1`：exit 0，三個 DLL（15,872／9,216／51,200 bytes）。
- `tools/Verify-Mods.ps1`：`RESULT: PASS`；CardCraft 6 個 patch：
  `CfTryOpenLockPatch -> LockHelper.TryOpenLock`、
  `CfCrateTooltipPatch -> LockHelper.CreateLockerContainerTooltip`、
  `CfCrateFillPatch -> DirectoryMaster.Item`、
  `CfPrinterBatchPatch -> MachinePrinter.FinishPrinting`、
  `CfSawyerHidePatch -> NetworkUpgrade.IsHiddenForRun`、
  `CfThiefPatch -> StoreClientList.CreateThief`；無 banned API。
- `tools/Probe-Patches.ps1`：`patched 8 / 4 / 6`，`RESULT: PASS`。
- `tools/Probe-Logic.ps1`：CardCraft scan `refused=False capabilities=[] findings=0`，PASS。
- 動作指令註記：本機沒有 PowerShell 7，`pwsh` 不在 PATH，四條指令實際以
  `powershell -NoProfile -ExecutionPolicy Bypass -File tools\…` 執行（同一腳本、
  同一輸出）。Mono 的 `internal call` 警告是既有環境問題，四條指令仍全部 exit 0。
- `.scratch/p4check`（scratch-only，不進 MOD，`P4 CHECK: PASS` 59 項）：
  - T3 純函式：`KeycardId`／`CryptoModuleId` 的 `_t3` id；卡價 300／225／165／165／90、
    核價 900／750／600／600／450、核工作量 350／300／250／250／250；T2 價格未動；
    `ContainerId(…,3)` 會丟 `ArgumentOutOfRangeException`（確認沒有 T3 箱）。
  - 升級：`tab="CardFence"`、8 個價格 300／800／500／1200／3000／9000／1500／4500。
  - 小偷權重 `30,25,20,15,10`（合計 100）。
  - 分頁算術直接讀遊戲常數：`TABS_W=1060`、`TAB_W=256`、`TAB_GAP=12`；
    3 個分頁 792 ≤ 1060、4 個分頁剛好 1060。
  - 工匠 literal 檢查：有 `printer`／`printer_module_cnc`／`cf_pro_desequencer`／
    `cf_batch_card_module`／`cf_printer_chip_blank_keycard`／`common_electronic`／
    `printer_plastic`／`premiumBuy15`／`maker_last_day`；整個類別沒有任何 `_box`
    字面值（不賣箱子）。
  - IL：`CF_SAWYER_`、`CardFence`、`制卡`、`CF_FENCE_CONTACT/THIEF/THIEF_T2` 都在。

## P4 實作要點（詳見 spec §3.8／§3.10／§3.11）

- **退休製卡匠**：掛 `ModHook.OnGenerateCustomerVeryLate`；`dayCounter % 7 == 6`
  每週一次，`CF_FENCE_CONTACT` 首次解鎖當天補訪一次；`maker_last_day` 存在
  `ModHelper` 的存檔 modData，重載不會重複到訪。客戶 `SELL`、`nonShootable`、
  `premiumBuy15`、`acceptRetailMarkup=false`。
- **工匠庫存**：工具（印表機／CNC／專業解序器）在 `CONTACT`；批量模組在
  `CF_FENCE_BATCH`；空白卡晶片＋電子×5＋耗材×5 在 `CF_FENCE_SUPPLY`；
  T1 全卡／核在 `CF_FENCE_T1`；隨機 3 T2 卡＋3 T2 核在 `CF_FENCE_T2`；
  1 T3 卡＋同家族 1 T3 核在 `CF_FENCE_T3`。全部 `isOwend:false`，售價 = `unitValue`。
- **T3 卡／核**：5 家族各新增一張卡一個核（§3.4b），沿用 T2 sprite，
  `CF_CARD_TIER=3`；T3 箱仍未做，所以 T3 卡目前沒有對應的鎖。
- **小偷分級箱**：postfix 包住 `StoreClientList.CreateThief` 最後一句對話的
  `endAction`；原版 thief 庫存照舊，之後最多加 1 個 CF 箱。箱等級：黑市聲望
  ≥60 → T2；解鎖 `CF_FENCE_THIEF_T2` 後 ≥30 → T2；否則 T1。家族權重
  30／25／20／15／10（SEC／MED／ENG／SER／CMD）。非 CMD 標 stolen；
  CMD 固定價不標 stolen；箱子售價 = 箱子的 `unitValue`。
  T1 SEC／MED／ENG／SER 必須走 `PreBuiltItemHelper.LootCrate*`（原版箱是空箱，
  只有這條路會填內容），T1 CMD 與 T2 才走 `DirectoryMaster.Item` + CF postfix。
- **Wild Network**：8 個 `CF_FENCE_*` 全部放 `tab="CardFence"`，
  `SetNetworkTabLabel("CardFence", () => "制卡")`。分頁寬度用遊戲常數量的：
  3 個分頁 792 ≤ 1060，所以**不塞 Advanced**；第 4 個分頁會剛好 1060、沒有餘量，
  本輪不新增第 4 個分頁。
- **CF_SAWYER_**：`NetworkUpgrade.IsHiddenForRun` postfix，前綴 `CF_SAWYER_` 一律
  回 `true`；本輪沒有註冊任何索耶升級（P5 才做）。

## P4 驗收卡（人類實機用，尚未跑）

| # | 項目 | 預期 |
|---|---|---|
| 1 | 買 `CF_FENCE_CONTACT` 後的下一位顧客 | 退休制卡匠到訪（`[CF-MAKER] visit … gate=catchup`），前台有印表機／CNC／解序器 |
| 2 | 每週第 6 天 | 製卡匠再訪（`gate=weekly`）；同一天不重複 |
| 3 | 工匠買賣 | 滑鼠點前台物品可看到價格 = `unitValue`；買下後進背包 |
| 4 | 買 T2／T3 門路後 | 工匠前台出現 3 張不同家族 T2 卡＋3 個 T2 核；T3 門路再加 1 卡 1 核 |
| 5 | 買 `CF_FENCE_THIEF` 後遇小偷 | `[CF-THIEF] box … added=True`；前台多 1 個上鎖的 CF 箱 |
| 6 | 黑市聲望 ≥60（或 ≥30＋`CF_FENCE_THIEF_T2`） | 小偷給 T2 箱；否則 T1 箱 |
| 7 | 打開 Wild Network | 出現第 3 個分頁「制卡」，8 個升級可買、無第 4 個分頁 |
| 8 | 小偷箱內容 | 卡／核等級 ≤ 箱等級；CMD T1 箱只出 T1 卡／核 |

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

- **P4 全部未實機**：製卡匠到訪／對話／買賣、小偷分級箱、Wild Network 分頁
  都只有離線驗證；`Player.log` 沒有任何 `[CF-MAKER]`／`[CF-THIEF]`／`[CF-NET]`。
- **tabBar 實際寬度**：spec §3.11.1 的 1060 來自 `WildNetworkStyle.TABS_W` 常數，
  不是 prefab 或 runtime `RectTransform` 量測；如果 prefab 的 tabBar 不是 1060，
  第 3 個分頁的結論要重算。
- **T3 卡／核沒有對應的 T3 鎖**：T3 箱（9×7、300／500）仍未做，所以 T3 卡目前
  只能交易／收藏，不能開任何箱子。
- **工匠庫存份量**：全部門路買齊後一次到訪約 40+ 件（含機器與 10 張 T1 卡／核），
  前台 18×4 理論上放得下，但沒有實機確認會不會中途滿台。
- **`premiumBuy15` 對 SELL 客戶的實際效果**：照原版退休匠加，但這個 flag 主要
  影響客戶「買」東西的價格；SELL 客戶是否真的用到，未在 runtime 核。
- **黑市聲望門檻 60／30**：是我方定案（spec §3.10.2），不是原版常數；實機要到
  聲望 30／60 才能驗。
- **`CF_SAWYER_*` 隱藏**：本輪沒有索耶升級，postfix 只用離線 IL 證明存在；
  P5 註冊後才能真正看到隱藏效果。
- P3 的 #5／#6／#7 仍未補 log 證據（沿用上一輪結論）。
- `unitValue`（晶片 250／解序器 800／T3 卡核新價）是定案值但未經遊戲平衡驗證。
- `long_module2` 是否真的是 1×4：由 sprite 切片決定，未在 runtime 量過。
- **完整 §3.1–3.11 規格**：仍未補齊；目前只有 P1/P2 定案段、§3.3 loot 表、
  §3.4 errata、§3.4b T3 定價、§3.5 P3、§3.8／§3.10(2)／§3.11 P4。
- **`Resources.LoadAll<Sprite>` runtime 清單**：目前只有 asset 字串掃描。
- **CMD T2 -7.37%**：已接受的例外，未改。

## 下一步（P5，新對話）

1. **索耶（P5）**：依指示本輪不開；`CF_SAWYER_*` 的隱藏規則已先掛好。
2. 把 P4 四條驗收卡交給人類實機跑，收集 `[CF-MAKER]`／`[CF-THIEF]`／`[CF-NET]`。
3. T3 箱體、loot、開鎖（`CF_CRATE_TIER=3`，容量 9×7，價 300／500）。
4. 紙本與其餘黑市內容。
5. 補 §3.1–3.11 全文與 `Resources.LoadAll` 實機清單。

## 禁區

- 不要從 agent 啟動 Steam／遊戲；需要實機時出測試卡給人類。
- 不要改使用者存檔、Steam registry、Steam 進程。
- 不要動已完成 P1/P2 行為；不要改原版物品價格。
- 不要把 `.scratch/` 的參考源或 tool 輸出編譯進 MOD。
- 不要 push、不要改 `Assembly-CSharp.dll`。

## 主要檔案

- `src/CardCraftAndFence/CardCraftAndFence.cs`（P4 新增 `CfNetwork`、
  `CfSawyerHidePatch`、`CfRetiredCardmaker`、`CfThiefBoxes`、`CfThiefPatch`，
  並把 `ItemFactory` 擴到 T3）
- `src/CardCraftAndFence/manifest.xml`（0.4.0）
- `docs/CARD_CRAFT_SPEC.md`（新增 §3.4b、§3.8、§3.10、§3.11）
- `docs/CARD_CRAFT_STATUS.md`（本檔）
- `tools/Build-Mods.ps1`、`tools/Verify-Mods.ps1`、`tools/Probe-Patches.ps1`、
  `tools/Probe-Logic.ps1`（本輪未改）
- `.scratch/p3check/Program.cs`、`.scratch/p4check/Program.cs`（scratch-only
  離線核對，不進 MOD）
