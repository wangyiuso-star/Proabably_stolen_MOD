# CardCraftAndFence 開發狀態

更新時間：2026-11-16（0.5.2 啟動失敗修正：升級註冊移到讀檔／新遊戲；PRO 工具包加電池）

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
- **P4：完成（離線），已修訂。** 退休製卡匠客戶、博士（`CreateInventor`）販售、
  小偷只賣 T2／T3 分級箱、Wild Network **4 個** `CF_FENCE_*` 升級
  （`tab="CardFence"`）、`CF_SAWYER_*` 只在交換所未解鎖時隱藏。新增 5 家族
  T3 卡／核與 **5 個 T3 箱**（9×8／CMD 10×8、300／500、rolls 11/11/6/7/11）。
  **CMD 箱貼圖**改為執行期染色：`sec_box`／`sec_box_u` → 深藍 `cmd_box`／
  `cmd_box_u`，註冊進 `LoadedMod.Sprites`，零美術檔。
  **0.5.2 修啟動 NRE**：`ModHelper.AddNetworkUpgrade` 會碰
  `PlayerStore.Instance`，主選單階段這個 getter 會 new PlayerStore 並在 ctor NRE；
  4 個升級改在 `OnGameLoadedLate`／`OnNewGameStarted` 註冊，OnEnable 只設分頁標籤。
  PRO 工具包另加 `energy_credit`（100 電）。實機驗證（到訪、買賣、分頁、T3 fit、
  染色外觀、啟動）尚未跑。**P5（索耶）依指示未開。**

## 目前 build

| 項目 | 值 |
|---|---|
| ModVersion | 0.5.2 |
| DLL | `dist/CardCraftAndFence/CardCraftAndFence.dll` |
| 大小 | 59,904 bytes |
| SHA256 | `7BD2FDE8277337603603606015CC2199B1EC1F452B2DDAB5233C1A5A015FB557` |
| Mods 安裝 | 本輪**未覆蓋** `.../Probably Stolen/Mods/CardCraftAndFence`（沙箱只寫工作區）；要實機請先複製 `dist/CardCraftAndFence` 過去，並完全退出遊戲再開 |
| dist / Mods hash | 尚未同步（Mods 仍是 P3 的 `C8FFF6…`） |
| git | P1–P4 已提交：`9aab571`、`0f62dfc`、`7b1f161`（0.5.1 貼圖）、`50204fb`（0.5.2 啟動修正）；**未 push**；`dist/` 與 `backup/` 依 `.gitignore` 不進版控 |

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

## P4 修訂已驗收（本輪實際輸出，2026-11-16）

- `tools/Build-Mods.ps1`：exit 0，三個 DLL（15,872／9,216／59,904 bytes）。
- `tools/Verify-Mods.ps1`：`RESULT: PASS`；CardCraft **7 個 patch**：
  `CfTryOpenLockPatch -> LockHelper.TryOpenLock`、
  `CfCrateTooltipPatch -> LockHelper.CreateLockerContainerTooltip`、
  `CfCrateFillPatch -> DirectoryMaster.Item`、
  `CfPrinterBatchPatch -> MachinePrinter.FinishPrinting`、
  `CfSawyerHidePatch -> NetworkUpgrade.IsHiddenForRun`、
  `CfThiefPatch -> StoreClientList.CreateThief`、
  `CfInventorPatch -> StoreClientList.CreateInventor`；無 banned API。
- `tools/Probe-Patches.ps1`：`patched 8 / 4 / 7`，`RESULT: PASS`。
- `tools/Probe-Logic.ps1`：CardCraft scan `refused=False capabilities=[] findings=0`，PASS。
- 動作指令註記：本機沒有 PowerShell 7，`pwsh` 不在 PATH，四條指令實際以
  `powershell -NoProfile -ExecutionPolicy Bypass -File tools\…` 執行（同一腳本、
  同一輸出）。Mono 的 `internal call` 警告是既有環境問題，四條指令仍全部 exit 0。
- `.scratch/p4bcheck`（scratch-only，`P4B CHECK: PASS`）：
  - 升級：4 個 id 與價格 1500／1200／3000／2200；舊 5 個 id 已不在 DLL。
  - T3 箱：五家族 `cf_*_box_t3`、價 300／300／300／300／500、 rolls 11/11/6/7/11、
    容量 9×8（CMD 10×8）；T2／T1 舊值未動。
  - 小偷：families `SEC,MED,ENG,SER`、weights `32,27,21,20`；IL 沒有
    `_box_t1`／`sec_box`／`med_box`／`LootCrate*`／`acceptRetailMarkup`。
  - IL：`CF_FENCE_*` 4 個 id、PRO kit 的 5 個物品、`backInvinvElement`、
    `EmporiumTryAdd`、Sawyer 的 `isExchangeUnlocked`、博士的
    `cf_pro_desequencer`／`cf_batch_card_module` 都在。
- `.scratch/p4t3loot/verify_t3.py`（scratch-only，`T3 LOOT CHECK: PASS`）：
  直接從 `CardCraftAndFence.cs` 解析 T3 表，驗證 common 池 = T2 common 池、
  rare 池保留 T2 rare、權重合計 100；model EV 與 spec 逐箱相符（誤差 <0.02），
  200 樣本離線模擬見 spec §3.3。
- `.scratch/p5spritecheck`（scratch-only，`SPRITE CHECK: PASS`）：
  - `cmd_box`／`cmd_box_u` key、tint `0.24/0.35/0.62`、lum `0.299/0.587/0.114`；
  - `GetPixels`／`SetPixels`／`Sprite.Create`、`SetSpriteAndShapeFromMod`、
    `FindAllItem` 都在 IL；
  - `CfCmdBoxSprite` **沒有** `[HarmonyPatch]`；
  - `CfTryOpenLockPatch.Prefix` 的呼叫順序是
    `RemoveGameItemType` → `UnlockContainer` → `ApplyUnlocked`；
  - `ItemFactory.BuildContainer` 有 `ApplyLocked`；
  - `OnEnable` 訂閱、`OnDisable` 取消訂閱 `OnModAssetsLoaded`／`OnGameLoadedLate`。
- `.scratch/regcheck`（scratch-only，`REG CHECK: PASS`）：
  - OnEnable 只呼叫 `CfNetwork.EnsureTabLabel`，**沒有**呼叫
    `CfNetwork.EnsureRegistered` 或 `CfCmdBoxSprite.EnsureRegistered`；
  - OnEnable 訂閱 `OnGameLoadedLate`／`OnNewGameStarted`／`OnModAssetsLoaded`；
  - `EnsureRegistered` 有 4 次 `Add`、`Add` 有 exception handler，
    `[CF-NET] registered`／`register failed id=` 字串都在；
  - PRO kit 的 `energy_credit` 在 IL。
- `.scratch/upgradecheck`（scratch-only）：離線重現 0.5.1 的 NRE stack
  （`PlayerStore..ctor` → `PlayerStore.get_Instance` →
  `NetworkUpgrade.RegisterModUpgrade`），有無 prerequisites 都一樣，
  證明不是 prerequisites。

## 0.5.2 啟動 NRE 修正（實機回報）

實機 0.5.1 的 log：

```text
[CF-LOOT] registered 22 tables
Error: NullReferenceException ... at CardCraftAndFenceMod.OnEnable () [0x000a]
[ModLoader] Init failed: CardCraftAndFence v0.5.1
```

症狀是 CardFence 分頁只有一張 CARDMAKER 卡、3 格中 1 格有內容，THIEF 卡不存在；
PRO 可買、工具包有發，其餘三個升級沒有。**根因不是 prerequisites**：

- `ModHelper.AddNetworkUpgrade` → `NetworkUpgrade.RegisterModUpgrade` 會讀
  `PlayerStore.Instance`；這個 getter 在沒有存檔時會自己 `new PlayerStore()`。
- 主選單階段 `NewGameData.Instance` 還沒好，`PlayerStore` 建構子就 NRE。
- `RegisterModUpgrade` 是**先把 definition 加進 `modDefinitions`、再碰
  `PlayerStore.Instance`**，所以 PRO 先進去、然後才炸；T2／T3／THIEF_T2 沒跑到。
- `.scratch/upgradecheck` 已離線重現同一條 stack：
  `PlayerStore..ctor` → `PlayerStore.get_Instance` → `NetworkUpgrade.RegisterModUpgrade`
  → `ModHelper.AddNetworkUpgrade`；有無 prerequisites 都一樣炸。

修正：

- `OnEnable` 只呼叫 `CfNetwork.EnsureTabLabel()`（純 static label，不碰 PlayerStore）；
  不再呼叫 `CfNetwork.EnsureRegistered()`、也不呼叫 `CfCmdBoxSprite.EnsureRegistered()`。
- 4 個升級改在 `ModHook.OnGameLoadedLate`（讀檔）與 `ModHook.OnNewGameStarted`
  （新遊戲）註冊；每個 `Add` 獨立 `try/catch`，印
  `register start/ok/failed id=…`，最後一定印 `registered N/4`。
- 每次讀檔／新遊戲都重跑；`RegisterModUpgrade` 對同 id 是 remove+add，
  `PlayerStore.networkUpgrade` 只在缺 id 時補，不會清掉已買狀態。
- `.scratch/regcheck`（`REG CHECK: PASS`）確認 OnEnable 只呼叫 `EnsureTabLabel`、
  `EnsureRegistered` 有 4 次 `Add`、`Add` 有 exception handler、PRO kit 有
  `energy_credit`。
- **PRO 工具包加電池**：`energy_credit`（原版 `PowerHelper.InitPowerSourceItem(100,10)`）。

## P4 修訂實作要點（詳見 spec §3.8／§3.10／§3.11）

- **升級只剩 4 個**：`CF_FENCE_PRO` 1500（無前置）、`CF_FENCE_T2` 1200
  （前置 PRO）、`CF_FENCE_T3` 3000（前置 T2）、`CF_FENCE_THIEF_T2` 2200
  （無前置）。`CONTACT`／`SUPPLY`／`BATCH`／`T1`／`THIEF` 已刪。
- **PRO**：`onUnlock` 一次性發 printer、空白卡晶片、CNC、電子×2、耗材×2，
  用 `GraphUtils.EmporiumTryAdd` 進 `backInvinvElement`；同時解鎖製卡匠到訪。
- **製卡匠**：掛 `OnGenerateCustomerVeryLate`；`dayCounter % 7 == 6` 每週一次，
  PRO 首次解鎖後補訪一次；`maker_last_day` 存在存檔 modData。常備 T1 全卡／核；
  T2／T3 由升級加賣。不再賣機器、不賣箱子。
- **博士**：`CfInventorPatch` postfix 包 `StoreClientList.CreateInventor` 最後一句
  對話的 `endAction`（先跑原版）；PRO 後加賣 `cf_pro_desequencer` 與
  `cf_batch_card_module`，T2／T3 加賣與製卡匠相同的卡／核。
- **小偷**：postfix 包 `CreateThief` 的 `endAction`；**不加 T1、永不 CMD**。
  T3＝解鎖 `CF_FENCE_T3` 且聲望 ≥85；T2＝聲望 ≥60 或（≥30 且解鎖
  `CF_FENCE_THIEF_T2`）；兩者都不成立就不給 CF 箱。權重 SEC 32／MED 27／
  ENG 21／SER 20。全部 `isStolen: true`；不再改 `acceptRetailMarkup`。
- **T3 箱**：`cf_*_box_t3`、9×8（CMD 10×8）、300／500、rolls 11/11/6/7/11；
  `CF_CRATE_TIER=3`、同一組 `LOCK_ID_*`、T3 卡可開。Loot 以 T2 池為底，
  加 T3 卡／核與最高價稀有物；model EV 3.9–4.5。
- **Wild Network**：4 個升級全部 `tab="CardFence"`，
  `SetNetworkTabLabel("CardFence", () => "制卡")`。分頁寬度 792 ≤ 1060，
  不塞 Advanced、不加第 4 個分頁。
- **CF_SAWYER_**：`IsHiddenForRun` postfix 只在
  `exchangeManager.isExchangeUnlocked == false` 時把 `CF_SAWYER_*` 設為隱藏；
  本輪沒有註冊任何索耶升級（P5 才做）。

## P4 修訂驗收卡（人類實機用，尚未跑）

| # | 項目 | 預期 |
|---|---|---|
| 1 | 買 `CF_FENCE_PRO` | 後台多印表機／晶片／CNC／電子×2／耗材×2／`energy_credit` 電池×1（`[CF-NET] PRO kit granted …+battery`）；製卡匠下次 `GenerateClient` 到訪（`gate=catchup`） |
| 2 | 每週第 6 天 | 製卡匠再訪（`gate=weekly`）；同一天不重複 |
| 3 | 製卡匠買賣 | T1 全卡／核常備；T2 後加 3 卡＋3 核；T3 後加 1 卡＋1 核；價格 = `unitValue` |
| 4 | 遇博士 | `[CF-INVENTOR] stock …`；前台有專業解序器與批量模組；T2／T3 後加卡核 |
| 5 | 黑市聲望 <30 遇小偷 | 沒有 CF 箱（只有原版 thief 貨） |
| 6 | 聲望 ≥60 或（≥30＋`CF_FENCE_THIEF_T2`） | 小偷給 1 個 T2 箱（SEC/MED/ENG/SER） |
| 7 | 解鎖 `CF_FENCE_T3` + 聲望 ≥85 | 小偷給 1 個 T3 箱；用 T3 卡可開 |
| 8 | 打開 Wild Network | 第 3 個分頁「制卡」只有 4 個升級、無第 4 個分頁 |
| 9 | T3 箱 12 件 | 9×8／CMD 10×8 放得下（未實機量） |
| 10 | 生成 CMD T1/T2/T3 | 深藍箱、6×4、輪廓與治安箱一致；`[CF-SPRITE] registered …` |
| 11 | 用對應卡開 CMD 箱 | 換成深藍 `cmd_box_u`；無 `unknown`、無 `Mod sprite … not found` |
| 12 | 存檔→重載 | CMD 箱仍是深藍；應有 `[CF-SPRITE] reapplied … after load` |
| 13 | SEC／MED／ENG／SER 箱 | 外觀完全沒變 |

## CMD 箱貼圖＝執行期染色（0.5.1，詳見 spec §3.12）

- **零美術檔**：`CfCmdBoxSprite` 用 `RenderHandler.LoadFromAtlas("Items/items_container",
  "sec_box" / "sec_box_u")` 取原版圖，`GetPixels` 子區域後去色
  （`0.299/0.587/0.114`）再乘 `CmdBlue = (0.24, 0.35, 0.62)`，`Texture2D` RGBA32 +
  Point + Clamp，`Sprite.Create(..., 100f)`；96×64 → 6×4 格。
- **註冊**：塞進本 MOD `LoadedMod.Sprites["cmd_box"]` / `["cmd_box_u"]`。
  `ModLoader` 是 `OnEnable()` 之後才 `Mods.Add`，所以 OnEnable 第一次找不到自己；
  實作是 OnEnable 嘗試（靜默）＋`OnModAssetsLoaded` 正式註冊＋
  `BuildContainer`／`OnGameLoadedLate` 懶註冊。
- **建立**：`ItemFactory.BuildContainer` 只對 CMD 家族用
  `SetSpriteAndShapeFromMod(ModId,"cmd_box")`；失敗才 fallback `sec_box`（先把
  `useSpriteFromMod` 設回 false）。
- **解鎖**：既有 `CfTryOpenLockPatch` 先 `RemoveGameItemType("SUPPLY_CRATE")`、
  再 `UnlockContainer`、最後 `ApplyUnlocked`（`cmd_box_u`，失敗 fallback
  `sec_box_u`）。**沒有新增 Harmony patch**。
- **存讀檔**：`OnGameLoadedLate` 掃 `FindAllItem(false)`，對
  `spriteAtlasPath == ModId` 的 CMD 箱重跑貼圖，避免掉旗標變 unknown。
- **不動**：其他四家族箱子的 sprite、原版 `sec_box_u`、`LockHelper` 本體。

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

- **P4 修訂全部未實機**：PRO 工具包、製卡匠／博士到訪與買賣、小偷 T2/T3 箱、
  Wild Network 4 個升級、T3 箱開鎖，都只有離線驗證；`Player.log` 沒有任何
  `[CF-NET]`／`[CF-MAKER]`／`[CF-INVENTOR]`／`[CF-THIEF]`。
- **0.5.2 啟動修正未實機**：OnEnable 已不再碰 `PlayerStore.Instance`，但
  `OnGameLoadedLate`／`OnNewGameStarted` 註冊是否一定在第一次 Wild Network／
  製卡匠之前，還要看新的 `Player.log` 是否有
  `[CF-NET] registered 4/4`，且沒有 `Init failed`。
- **PRO 電池**：`energy_credit` 是原版 100 電／10 bandwidth；放進後台是否足夠
  印表機開機，未實機試。
- **CMD 箱染色未實機**：深藍 `0.24/0.35/0.62` 的觀感、6×4 輪廓、`SetNativeSize`
  大小、解鎖 `cmd_box_u`、存讀檔重貼，全部只有 IL／字串核對。
- **`ModLoader.Mods` 時序**：`OnEnable` 在 `Mods.Add` 之前，本輪用
  `OnModAssetsLoaded` 補註冊；它是否一定早於第一個 CMD 箱，還沒用 `Player.log` 驗。
  若來不及，該箱會先 fallback 成 `sec_box`（褐色），直到 `OnGameLoadedLate` 或
  下一箱才變深藍。
- **tabBar 實際寬度**：spec §3.11.1 的 1060 來自 `WildNetworkStyle.TABS_W` 常數，
  不是 prefab 或 runtime `RectTransform` 量測；如果 prefab 的 tabBar 不是 1060，
  第 3 個分頁的結論要重算。
- **T3 箱 12 件 fit**：9×8（CMD 10×8）只在紙上算過，沒有 runtime 量過；
  若放不下，`/CF-LOOT/` 的 `remaining` 會大於 0，箱子會少東西。
- **T3「200 樣本」是離線模擬**：同權重、同 `Pick` 演算法，但不是 in-game
  `CfLootProbe`；實機 `[CF-EV]` 數字仍待人類補。
- **工匠庫存份量**：T1 10 件 + T2 6 件 + T3 2 件 = 18 件（博士另有 2 件機器
  + 同樣的卡核）；前台 18×4 理論上放得下，但沒有實機確認會不會中途滿台。
- **`premiumBuy15` 對 SELL 客戶的實際效果**：照原版退休匠加，但這個 flag 主要
  影響客戶「買」東西的價格；SELL 客戶是否真的用到，未在 runtime 核。
- **黑市聲望門檻 85／60／30**：是我方定案（spec §3.10.2），不是原版常數；
  實機要到對應聲望才能驗。
- **`CF_SAWYER_*` 隱藏**：本輪沒有索耶升級，postfix 只用離線 IL 證明存在；
  P5 註冊後才能真正看到「交換所未解鎖就隱藏」的效果。
- **PRO 工具包進 backInvinvElement**：`EmporiumTryAdd` 在後台滿時會依序落到其他
  店內庫存；這個 fallback 沒有實機驗。
- **舊升級孤兒**：買過舊 `CF_FENCE_CONTACT/...` 的存檔，`networkUpgrade` 字典會
  留下未註冊的 id；不顯示、不影響新升級，但沒有存檔清理。
- P3 的 #5／#6／#7 仍未補 log 證據（沿用上一輪結論）。
- `unitValue`（晶片 250／解序器 800／T3 卡核新價）是定案值但未經遊戲平衡驗證。
- `long_module2` 是否真的是 1×4：由 sprite 切片決定，未在 runtime 量過。
- **完整 §3.1–3.12 規格**：仍未補齊；目前只有 P1/P2 定案段、§3.3 loot 表、
  §3.4 errata、§3.4b T3 定價、§3.5 P3、§3.8／§3.10(2)／§3.11 P4、§3.12 貼圖。
- **`Resources.LoadAll<Sprite>` runtime 清單**：目前只有 asset 字串掃描。
- **CMD T2 -7.37%**：已接受的例外，未改。
- **CMD 箱來源**：T1／T2／T3 在江洋大盜（P6）之前沒有正常來源，先留著。

## 下一步

1. 本輪 0.5.2（啟動 NRE 修正＋PRO 電池）已提交（`50204fb`，本機 commit、未 push）。
2. 人類實機跑 P4 修訂與 CMD 貼圖驗收卡，收集 `[CF-NET]`／`[CF-MAKER]`／
   `[CF-INVENTOR]`／`[CF-THIEF]`／`[CF-SPRITE]`，並補 T3 in-game `[CF-EV]`。
3. **索耶（P5）**：依指示本輪不開；`CF_SAWYER_*` 的隱藏規則已先掛好。
4. **江洋大盜（P6）**：補 CMD 箱的正常來源。
5. 紙本與其餘黑市內容；補 §3.1–3.11 全文與 `Resources.LoadAll` 實機清單。

## 禁區

- 不要從 agent 啟動 Steam／遊戲；需要實機時出測試卡給人類。
- 不要改使用者存檔、Steam registry、Steam 進程。
- 不要動已完成 P1/P2 行為；不要改原版物品價格。
- 不要把 `.scratch/` 的參考源或 tool 輸出編譯進 MOD。
- 不要 push、不要改 `Assembly-CSharp.dll`。

## 主要檔案

- `src/CardCraftAndFence/CardCraftAndFence.cs`（P4 修訂新增 `CfCardStock`、
  `CfInventorStock`、`CfInventorPatch`；0.5.1 新增 `CfCmdBoxSprite`；
  `CfNetwork` 縮成 4 個升級；`CfThiefBoxes` 只賣 T2/T3；
  `ItemFactory`／`LootTables` 擴到 T3 箱）
- `src/CardCraftAndFence/manifest.xml`（0.5.2）
- `docs/CARD_CRAFT_SPEC.md`（§3.3 T3 箱／loot、§3.4b、§3.8、§3.10、§3.11、§3.12）
- `docs/CARD_CRAFT_STATUS.md`（本檔）
- `tools/Build-Mods.ps1`、`tools/Verify-Mods.ps1`、`tools/Probe-Patches.ps1`、
  `tools/Probe-Logic.ps1`（本輪未改）
- `.scratch/p3check/`、`.scratch/p4check/`、`.scratch/p4bcheck/`、
  `.scratch/p4t3loot/`、`.scratch/p5spritecheck/`、`.scratch/regcheck/`、
  `.scratch/upgradecheck/`（scratch-only 離線核對，不進 MOD）
