# CardCraftAndFence 開發狀態

更新時間：2026-11-17（0.7.2：P5 容量與填充修訂——SEC T3／CMD T3／CMD T2）

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
  T3 卡／核與 **5 個 T3 箱**（9×8／CMD 10×8、300／500）。
  **CMD 箱貼圖**改為執行期染色：`sec_box`／`sec_box_u` → 深藍 `cmd_box`／
  `cmd_box_u`，註冊進 `LoadedMod.Sprites`，零美術檔。
  **0.5.2 修啟動 NRE**：`ModHelper.AddNetworkUpgrade` 會碰
  `PlayerStore.Instance`，主選單階段這個 getter 會 new PlayerStore 並在 ctor NRE；
  4 個升級改在 `OnGameLoadedLate`／`OnNewGameStarted` 註冊，OnEnable 只設分頁標籤。
  PRO 工具包另加 `energy_credit`（100 電）。
  **0.5.3 製卡匠補給**：PRO 後每次到訪都上架製卡週邊，避免自訂晶片遺失後
  無法再生產。**0.5.8 櫃台滿**：箱子／貨物先 `CanAccept` 探測，放不下就
  `Destroy()`＋`skipped` log，**不進後台、不進玩家背包**。
  **0.5.7 核心／地板**：T2/T3 箱移除全部核心；common 改成
  T2 `value>=60`、T3 `value>=120` 的同家族主題池，rare 只放高階卡＋最高價
  裝備。**0.6.1 多台機器**：卡匠固定賣 printer×1，chip／CNC 只在
  `CountOwned(chip/cnc) < CountOwned(printer)` 時各補 1；材料 2+2、
  核心 2（最高等級、`day%5` 連續 2 家族）；不賣金屬模組。
  7–9 件、上限 11。博士固定只賣解序器＋批量模組；T1 CMD rare 也移除
  `crypto_module_cmd`。**0.6.2 博士住所／小偷順序**：
  `OnPlaceInventorInventoryItemEarly` hook 在住所與特殊庫存都補兩件；
  `CreateInventor`／`CreateInventorStorage` postfix 先印
  `[CF-INVENTOR] client=… pro=…`；小偷改成先放 CF 箱、再跑原版庫存。
  實機驗證（到訪、買賣、分頁、T3 fit、染色外觀、啟動、滿台 skip、
  住所庫存）尚未跑。
- **P5：0.7.0 完成並已實機跑起來；0.7.1 修三個實機 bug；0.7.2 修容量／填充。**
  索耶（`OnHandlingNightlyServicesEarly` hook，在 `EndNight` 的
  `HandleJanitorial` 之前）：垃圾桶同家族鑰匙卡按 SEC 3／MED 4／ENG 4／
  SER 4／CMD 3 換已填 loot＋已上鎖的物資箱；0.7.1 起同家族卡**高→低**排序、
  箱子取組內最低等級（3 T3 + 1 T1 → T3、2 T1 + 3 T2 → T2）；未解鎖批量
  每晚最多 1 箱、**優先最高等級**。`CF_SAWYER_CHIP` 開芯片（3 同家族同級→
  下一級，T3 不升，每晚每家族 1 次）；`CF_SAWYER_PERMANENT` 解鎖後接管原版
  三個 crate barter（同時清 `promotionalBarter` 與
  `activePromotionalBarterIds`），並發一本 `cf_sawyer_card_catalog` 換卡目錄
  紙本。0.7.1 紙本改用 `CfPaperSprites` 執行期產圖（紙底＋X／放大鏡／左右
  箭頭）註冊進 `LoadedMod.PaperSprites`，修掉藍方塊。0.7.2 用實際
  `itemShape` 面積重算容量（SEC T2 10×8、CMD T1 10×8、CMD T2 12×9、
  SEC T3 13×8、CMD T3 15×9），`LootTables.Fill` 改成全部 roll 完後按面積
  大→小放；`smg`／`shotgun`（7×3）只留在 rare，三張表權重重配後 EV 不變；
  overflow 每箱一行。Wild Network 由 4 個升級變 **7 個**。

## 目前 build

| 項目 | 值 |
|---|---|
| ModVersion | 0.7.2 |
| DLL | `dist/CardCraftAndFence/CardCraftAndFence.dll` |
| 大小 | 75,264 bytes |
| SHA256 | `CBB1EEFCD1DFA9FB6867CB184A29C794D79AF5548EE446E1299BBA12C483F19F` |
| Mods 安裝 | 本輪**未覆蓋** `.../Probably Stolen/Mods/CardCraftAndFence`（沙箱只寫工作區）；要實機請先複製 `dist/CardCraftAndFence` 過去，並完全退出遊戲再開 |
| dist / Mods hash | 尚未同步（Mods 仍是 0.6.2 的 58,880 bytes／`84FF14E6…`，P5 未覆蓋） |
| git | P1–P5 已提交：`9aab571`、`0f62dfc`、`7b1f161`（0.5.1）、`50204fb`（0.5.2）、`3bfd15c`（0.5.3）、`73351ab`（0.5.4）、`7f96242`（0.5.5）、`7d7363a`（0.5.6）、`e83c586`（0.5.7）、`dde90d7`（0.5.8）、`315df85`（0.5.9）、`8094136`（0.6.0）、`6a3387e`（0.6.1）、`75d227f`（0.6.2）、`66266cf`（0.7.0）、`81c2d08`（0.7.1）、`801a5b0`（0.7.2）；**未 push**；`dist/` 與 `backup/` 依 `.gitignore` 不進版控 |

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

- `tools/Build-Mods.ps1`：exit 0，三個 DLL（15,872／9,216／58,880 bytes）。
- `tools/Verify-Mods.ps1`：`RESULT: PASS`；CardCraft **8 個 patch**：
  `CfTryOpenLockPatch -> LockHelper.TryOpenLock`、
  `CfCrateTooltipPatch -> LockHelper.CreateLockerContainerTooltip`、
  `CfCrateFillPatch -> DirectoryMaster.Item`、
  `CfPrinterBatchPatch -> MachinePrinter.FinishPrinting`、
  `CfSawyerHidePatch -> NetworkUpgrade.IsHiddenForRun`、
  `CfThiefPatch -> StoreClientList.CreateThief`、
  `CfInventorPatch -> StoreClientList.CreateInventor`、
  `CfInventorStoragePatch -> StoreClientList.CreateInventorStorage`；無 banned API。
- `tools/Probe-Patches.ps1`：`patched 8 / 4 / 8`，`RESULT: PASS`。
- `tools/Probe-Logic.ps1`：CardCraft scan `refused=False capabilities=[] findings=0`，PASS。
- 動作指令註記：本機沒有 PowerShell 7，`pwsh` 不在 PATH，四條指令實際以
  `powershell -NoProfile -ExecutionPolicy Bypass -File tools\…` 執行（同一腳本、
  同一輸出）。Mono 的 `internal call` 警告是既有環境問題，四條指令仍全部 exit 0。
- `.scratch/p4bcheck`（scratch-only，`P4B CHECK: PASS`）：
  - 升級：4 個 id 與價格 1500／1200／3000／2200；舊 5 個 id 已不在 DLL。
  - T3 箱：五家族 `cf_*_box_t3`、價 300／300／300／300／500、
    0.5.7 rolls 4/4/4/4/7、容量 9×8（CMD 10×8）；T1 未動。
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
  - PRO kit 的 `energy_credit` 在 IL；
  - `AddMakerStock` 用 `UnlockedTiers`＋`CryptoModuleId`，`StockSummary` 有
    `fixed/materials/cores`；`AddTier2/3` 已移除。
  - `.scratch/p5loot/parse_code.py` 直接解析 10 張 T2/T3 表：無
    `cf_crypto_module_*`、T2 3.6–3.9、T3 3.9–4.5、T2 >= T1+0.2，全 PASS。
- `.scratch/upgradecheck`（scratch-only）：離線重現 0.5.1 的 NRE stack
  （`PlayerStore..ctor` → `PlayerStore.get_Instance` →
  `NetworkUpgrade.RegisterModUpgrade`），有無 prerequisites 都一樣，
  證明不是 prerequisites。

## P5 0.7.0 實作與離線驗收（分組／紙本／T2 fit 已被 0.7.1 取代）

**本機沒有 PowerShell 7／`pwsh` 不在 PATH，四條指令實際都用
`powershell -NoProfile -ExecutionPolicy Bypass -File tools\…` 執行**（同一腳本、
同一輸出）。四條全部 exit 0，Mono 的 `internal call` 警告是既有環境問題。

### 1) 建置

```text
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Build-Mods.ps1
Compiling AppraisalAssistant...
  -> ...\dist\AppraisalAssistant\AppraisalAssistant.dll (15872 bytes)
Compiling AugSpotter...
  -> ...\dist\AugSpotter\AugSpotter.dll (9216 bytes)
Compiling CardCraftAndFence...
  -> ...\dist\CardCraftAndFence\CardCraftAndFence.dll (69120 bytes)
Build OK: C:\Users\wangy\Desktop\Probably_Stolen_MOD\dist
exit 0
```

### 2) Verify-Mods

```text
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Verify-Mods.ps1
PASS CardCraftAndFence: ID deepspace.card_craft_and_fence
PASS CardCraftAndFence: capabilities empty
PASS CardCraftAndFence.dll: IMod implemented by DeepSpaceQoL.CardCraftAndFenceMod
PASS CardCraftAndFence.dll: no banned IL APIs
PASS CardCraftAndFence.dll: DeepSpaceQoL.CfTryOpenLockPatch -> LockHelper.TryOpenLock
PASS CardCraftAndFence.dll: DeepSpaceQoL.CfCrateTooltipPatch -> LockHelper.CreateLockerContainerTooltip
PASS CardCraftAndFence.dll: DeepSpaceQoL.CfCrateFillPatch -> DirectoryMaster.Item
PASS CardCraftAndFence.dll: DeepSpaceQoL.CfPrinterBatchPatch -> MachinePrinter.FinishPrinting
PASS CardCraftAndFence.dll: DeepSpaceQoL.CfSawyerHidePatch -> NetworkUpgrade.IsHiddenForRun
PASS CardCraftAndFence.dll: DeepSpaceQoL.CfThiefPatch -> StoreClientList.CreateThief
PASS CardCraftAndFence.dll: DeepSpaceQoL.CfInventorPatch -> StoreClientList.CreateInventor
PASS CardCraftAndFence.dll: DeepSpaceQoL.CfInventorStoragePatch -> StoreClientList.CreateInventorStorage
PASS CardCraftAndFence.dll: HarmonyPatch attributes checked (8)
RESULT: PASS
exit 0
```

### 3) Probe-Patches

```text
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Probe-Patches.ps1
（UnityEngine internal call / mono out of sync 警告，既有環境問題）
== ...\dist\CardCraftAndFence\CardCraftAndFence.dll
patched 8 method(s)
  DirectoryMaster.Item
  LockHelper.CreateLockerContainerTooltip
  LockHelper.TryOpenLock
  MachinePrinter.FinishPrinting
  NetworkUpgrade.IsHiddenForRun
  StoreClientList.CreateInventor
  StoreClientList.CreateInventorStorage
  StoreClientList.CreateThief
RESULT: PASS
exit 0
```

AppraisalAssistant `patched 8`、AugSpotter `patched 4` 也都在同一輪 PASS。

### 4) Probe-Logic

```text
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Probe-Logic.ps1
[Scan] CardCraftAndFence.dll refused=False capabilities=[] findings=0 error=null
PASS scan CardCraftAndFence.dll
RESULT: PASS
exit 0
```

### 5) Scratch 離線核對 `.scratch/p5sawyer/check.py`

```text
python .scratch\p5sawyer\check.py
P5 SAWYER CHECK
  source: src\CardCraftAndFence\CardCraftAndFence.cs
  dll   : dist\CardCraftAndFence\CardCraftAndFence.dll (69120 bytes)
  passed: 66  failed: 0

P5 SAWYER CHECK: PASS
exit 0
```

`check.py` 實際核對的項目：

- 原始碼：`CardRatio()` 的 SEC 3／MED 4／ENG 4／SER 4／CMD 3；三個
  `CF_SAWYER_*` id；`SawyerPermanentCost 400`／`SawyerBatchCost 700`／
  `SawyerChipCost 900`；`Tab = "CardFence"`；三個 `Add(...)` 的 subtitle 都是
  `索耶`；PERMANENT 無 prerequisites、BATCH／CHIP 都是
  `new[] { SawyerPermanent }`。
- 分組演算法（照原始碼邏輯重寫；**這是 0.7.0 的升冪版，0.7.1 已改成降冪**）：
  - 6 T1 SEC：cap → 1 箱、無 cap → 2 箱；
  - 3 T1 + 3 T2 → T1 + T2；1 T1 + 2 T2 → T1；4 T1 + 2 T2 → 2×T1；
  - 上述兩組把輸入順序反轉，結果相同；
  - MED 4 T1 → 1 箱、CMD 2 T1 → 0 箱。
- 原始碼／IL：`OnHandlingNightlyServicesEarly` 的 `+=`／`-=`、`trashcan`、
  `itemsBuffer.Add`、`.Destroy();`、三個 barter id 字串與
  `promotionalBarter.RemoveAll`／`activePromotionalBarterIds.RemoveAll`、
  `ModHelper.RegisterPaperBook(`、`ModHelper.SpawnPaperItem(`、
  `BookId`／`ItemId`；DLL 的 UTF-16LE user string heap 有
  `CF_SAWYER_*`、三個 barter id、`cf_sawyer_card_catalog*`，
  UTF-8 metadata 有 `OnHandlingNightlyServicesEarly`／`RegisterPaperBook`／
  `SpawnPaperItem`／`itemsBuffer`／`trashcan`。
- `[HarmonyPatch(typeof(...), nameof(...))]` 屬性數 = **8**，且沒有任何一個是
  Sawyer（**沒有新增 patch**）。
- 版本：`.cs` 與 manifest 都是 0.7.0；註冊 log 是 `N/7` 且只有 `ok == 7` 才印
  hide 行；紙本文案含 `CMD 3 张`、`T3 不再升`、`每晚最多换 1 个箱子`。

### 6) 本輪改了什麼（只 4 個檔）

- `src/CardCraftAndFence/CardCraftAndFence.cs`：新增 `CfSawyerExchange`
  （換卡／芯片／促銷 barter 移除）與 `CfSawyerPaper`（紙本目錄）；
  `CfNetwork` 由 4 個升級變 7 個（`N/7` log）、`OnGameLoadedLate`／
  `OnNewGameStarted` 補紙本註冊與促銷移除、PERMANENT `onUnlock` 包 try/catch；
  `OnEnable`／`OnDisable` 掛／卸 `OnHandlingNightlyServicesEarly`；
  `ModVersion = 0.7.0`。
- `src/CardCraftAndFence/manifest.xml`：0.7.0＋描述補索耶。
- `docs/CARD_CRAFT_SPEC.md`：新增 §3.9（含 §3.9.9 P5 未核實），§3.10.1／
  §3.11 更新成 7 個升級與 `N/7` log。
- `docs/CARD_CRAFT_STATUS.md`：本檔。

`OnEnable` 另外呼叫 `CfSawyerPaper.EnsureRegistered()`（純 static，只碰
`PaperBookRegistry`／`ModItemDirectory`）；`CfNetwork.EnsureRegistered` 與
`CfCmdBoxSprite.EnsureRegistered` 仍然不在 OnEnable，所以 0.5.2 的啟動 NRE
修正不受影響。

## P5 0.7.1 修訂（實機退回單：紙本 sprites／降冪分組／SEC T2 rare）

0.7.0 實機已跑起來（`registered 7/7`、`Harmony: 8`、無 dummy；換卡、上鎖、
T3 箱、存檔都正常），但退回三項，0.7.1 修如下。

### 1) 紙本右上角藍方塊

- 根因：遊戲 build 沒有 `Resources/PaperUI/PaperAssetCatalog`，
  `notebook_manual`／`paper_buttons_close1`／`paper_buttons_screen_0`／
  `paper_buttons_l_1`／`paper_buttons1` 五次 `No sprite found`；`PaperUI`
  對缺圖的 chrome 按鈕畫成 `0.3/0.5/0.8` 藍方塊。
- 修法：新增 `CfPaperSprites`，用 `Texture2D` 執行期產生 5 張 sprite——
  628×804 紙底（纖維＋顆粒＋邊緣壓暗）與 64×64 的 X／放大鏡／左／右箭頭，
  寫進 `LoadedMod.PaperSprites`（`PaperAssets.FromMod` 會查這個字典）。
  `OnModAssetsLoaded`、`OnGameLoadedLate`／`OnNewGameStarted` 的
  `CfSawyerPaper.EnsureRegistered()`、`Grant()` 前都會補；零美術檔。
- 沒有用「關掉 chrome 按鈕」的方案：`PaperUI` 沒有 Esc 關書，少了 close
  會把玩家卡在書裡。

### 2) 3 高 + 1 低 → 低級箱

- 根因：0.7.0 升冪排序後切組，`[T1,T2,T3,T3,T3]` 被切成 `[T1,T2,T3]`，
  取第一張（最低）→ T1；三張 T3 被拆散。
- 修法：同家族改成**降冪**排序，箱子等級取**組內最後一張（最低）**；
  所有組再按箱子等級高→低產出，所以未解鎖批量時 cap 先保留最高級箱。
- 新語意：3 T3 + 1 T1 → T3（T1 留下）；2 T1 + 3 T2 → T2（T1 留下）；
  1 T1 + 2 T2 → T1；3 T1 + 3 T2 → T1 + T2；4 T1 + 2 T2 → 2×T1。

### 3) SEC T2 rare 放不進（P4 遺留，P5 量產放大）

- 實機：`[CF-LOOT] spawn table=cf_sec_t2_rare item=stun_gun remaining=1`，
  箱子少一件 rare，EV 約 −24%。
- 修法：`LootTables.Fill` 改成 **rare 先放、common 後放**（rare 是保底且通常
  最大，先佔位）；SEC T2 容量由 7×7 放大到 8×8（MED T2 本來就是 8×8），
  件數／權重／EV 不變。另加 `[CF-LOOT] overflow … remaining=…` 警告，
  下次若再放不下會直接看到，而不是只有 `spawn` 的那一行。

### 4) 0.7.1 驗收（實際輸出）

**Build**
```text
Compiling AppraisalAssistant...
  -> ...\dist\AppraisalAssistant\AppraisalAssistant.dll (15872 bytes)
Compiling AugSpotter...
  -> ...\dist\AugSpotter\AugSpotter.dll (9216 bytes)
Compiling CardCraftAndFence...
  -> ...\dist\CardCraftAndFence\CardCraftAndFence.dll (74752 bytes)
Build OK: C:\Users\wangy\Desktop\Probably_Stolen_MOD\dist
EXIT=0
```

**Verify-Mods（CardCraft 段）**
```text
PASS CardCraftAndFence: ID deepspace.card_craft_and_fence
PASS CardCraftAndFence: capabilities empty
PASS CardCraftAndFence.dll: IMod implemented by DeepSpaceQoL.CardCraftAndFenceMod
PASS CardCraftAndFence.dll: no banned IL APIs
PASS CardCraftAndFence.dll: DeepSpaceQoL.CfTryOpenLockPatch -> LockHelper.TryOpenLock
PASS CardCraftAndFence.dll: DeepSpaceQoL.CfCrateTooltipPatch -> LockHelper.CreateLockerContainerTooltip
PASS CardCraftAndFence.dll: DeepSpaceQoL.CfCrateFillPatch -> DirectoryMaster.Item
PASS CardCraftAndFence.dll: DeepSpaceQoL.CfPrinterBatchPatch -> MachinePrinter.FinishPrinting
PASS CardCraftAndFence.dll: DeepSpaceQoL.CfSawyerHidePatch -> NetworkUpgrade.IsHiddenForRun
PASS CardCraftAndFence.dll: DeepSpaceQoL.CfThiefPatch -> StoreClientList.CreateThief
PASS CardCraftAndFence.dll: DeepSpaceQoL.CfInventorPatch -> StoreClientList.CreateInventor
PASS CardCraftAndFence.dll: DeepSpaceQoL.CfInventorStoragePatch -> StoreClientList.CreateInventorStorage
PASS CardCraftAndFence.dll: HarmonyPatch attributes checked (8)
RESULT: PASS
EXIT=0
```

**Probe-Patches**
```text
patched 8 method(s)   ← Appraisal
patched 4 method(s)   ← Aug
patched 8 method(s)   ← CardCraft
  DirectoryMaster.Item
  LockHelper.CreateLockerContainerTooltip
  LockHelper.TryOpenLock
  MachinePrinter.FinishPrinting
  NetworkUpgrade.IsHiddenForRun
  StoreClientList.CreateInventor
  StoreClientList.CreateInventorStorage
  StoreClientList.CreateThief
RESULT: PASS
EXIT=0
```
（Mono `internal call`／out-of-sync 警告是既有環境問題。）

**Probe-Logic**
```text
[Scan] CardCraftAndFence.dll refused=False capabilities=[] findings=0 error=null
PASS scan CardCraftAndFence.dll
RESULT: PASS
EXIT=0
```

**Scratch**
```text
python .scratch\p5sawyer\check.py
P5 SAWYER CHECK (0.7.1)
  source: src\CardCraftAndFence\CardCraftAndFence.cs
  dll   : dist\CardCraftAndFence\CardCraftAndFence.dll (74752 bytes)
  passed: 94  failed: 0

P5 SAWYER CHECK: PASS
EXIT=0
```
`check.py` 新增：降冪分組 14 個案例（含 3 T3+1 T1、2 T1+3 T2、cap 只留 T3、
順序反轉）、5 個 paper key／`PaperSprites`／`OnModAssetsLoaded`、rare-first
順序、SEC T2 8×8、overflow 警告、版本 0.7.1。

### 5) 0.7.1 待實機

- 開書：log 不再有上述五條 `No sprite found`；右上 X／放大鏡在紙面內；
  背景有紙紋；close 可關書。
- 分組：3 T3 + 1 T1 → 1 個 T3 箱（T1 留下）；2 T1 + 3 T2 → 1 個 T2 箱；
  未買 BATCH 時優先做最高級箱。
- SEC T2：開箱確認 7 件齊全，log 沒有 `[CF-LOOT] overflow`。

## P5 0.7.2 容量與填充修訂（T3／T2 掉件）

0.7.1 實機通過紙本、分組、SEC T2 rare、上鎖與 T3 鑰匙，但 SEC T3 仍掉件；
CMD T3／T2 高度可疑。這輪只動容量／填充，索耶邏輯不變。

### 1) 用實際 itemShape 重算

- 解析 66 個存檔、332 個 item id 的 `itemShape`（width／height／data），
  缺的 8 張 T2/T3 keycard 用同類 2×2（4 格）。
- 每張表用權重跑 10 萬次 rolls，取最大／p99 面積：

| 箱 | rolls | 舊容量 | 移除前最大 | 移除後最大 | 新容量 |
|---|---:|---|---:|---:|---|
| SEC T2 | 6 | 8×8=64 | 77 | 77 | **10×8=80** |
| CMD T1 | 8 | 7×7=49 | 65 | 65 | **10×8=80** |
| CMD T2 | 6 | 9×8=72 | 112 | 77 | **12×9=108** |
| SEC T3 | 4 | 9×8=72 | 85 | 57 | **13×8=104** |
| CMD T3 | 7 | 10×8=80 | 136 | 87 | **15×9=135** |

「移除前最大」是 smg／shotgun 還在 common 池時的面積；把它們移到 rare-only
後，common 最大只剩 4×3（10 格），SEC T3／CMD T2／CMD T3 的最大面積降到
57／77／87 格，新容量才有幾排餘量。

- MED／ENG／SER 的 T2/T3 最大面積都 ≤40（現有 8×8／7×7／9×8 足夠），不動。

### 2) 填充策略與 7×3

- `LootTables.Fill`：先 roll 出全部 id、建立 item，再按 `shape` 面積**大→小**
  放入（同面積再按最長邊）。grid 放置本身會試 4 種旋轉＋2 種翻轉。
- 只加容量仍不夠：8 個 7×3 的 smg／shotgun 在任何 8 高格子都排不下。
  所以把 `smg`／`shotgun` 從 **SEC T3／CMD T2／CMD T3 的 common 池移除**，
  只留在 rare。三張表權重重配如下，common EV 完全不變：
  - SEC T3：stun_baton 20／glock 19／stun_gun 31／current_converter 30
    → 238.50。
  - CMD T2：flashbang 11／smoke 6／stun_baton 12／combat_knife 10／
    glock 12／stun_gun 29／current_converter 8／tazer 12 → 172.75。
  - CMD T3：stun_baton 15／glock 5／stun_gun 44／current_converter 36
    → 252.75。
  - `.scratch/p5loot/parse_code.py` 重跑：三箱 model EV／價仍是
    4.068／3.701／4.117，全部 VALUE-FLOOR PASS。
- overflow 改成**每箱一行**：
  `[CF-LOOT] overflow box=… uid=… table=… expected=… actual=… dropped=…
  items=…; crate is short N`；不再只在同表第一次掉件時印。
- 離線 packing 模擬（`.scratch/p5sawyer/pack_test.py`，實際 shape、
  4 旋轉＋2 翻轉、first-fit）：
  - CMD T1 10×8、CMD T2 12×9、CMD T3 15×9、SEC T3 13×8：
    各 3000 次隨機 rolls 0 失敗；adversarial（全部最大 common＋最大 rare）
    也放得下（CMD T2 6×4×3＋7×3、CMD T3 7×4×3＋7×3）。
  - SEC T2 10×8：3000 次 0 失敗；adversarial 6×4×3＋7×3 仍排不下，
    但 glock 權重只有 5%，且 0.7.1 實機已通過，暫不動。

### 3) 0.7.2 驗收（實際輸出）

**Build**
```text
Compiling AppraisalAssistant...
  -> ...\dist\AppraisalAssistant\AppraisalAssistant.dll (15872 bytes)
Compiling AugSpotter...
  -> ...\dist\AugSpotter\AugSpotter.dll (9216 bytes)
Compiling CardCraftAndFence...
  -> ...\dist\CardCraftAndFence\CardCraftAndFence.dll (75264 bytes)
Build OK: C:\Users\wangy\Desktop\Probably_Stolen_MOD\dist
EXIT=0
```

**Verify-Mods（CardCraft 段）**
```text
PASS CardCraftAndFence: ID deepspace.card_craft_and_fence
PASS CardCraftAndFence: capabilities empty
PASS CardCraftAndFence.dll: IMod implemented by DeepSpaceQoL.CardCraftAndFenceMod
PASS CardCraftAndFence.dll: no banned IL APIs
PASS CardCraftAndFence.dll: DeepSpaceQoL.CfTryOpenLockPatch -> LockHelper.TryOpenLock
PASS CardCraftAndFence.dll: DeepSpaceQoL.CfCrateTooltipPatch -> LockHelper.CreateLockerContainerTooltip
PASS CardCraftAndFence.dll: DeepSpaceQoL.CfCrateFillPatch -> DirectoryMaster.Item
PASS CardCraftAndFence.dll: DeepSpaceQoL.CfPrinterBatchPatch -> MachinePrinter.FinishPrinting
PASS CardCraftAndFence.dll: DeepSpaceQoL.CfSawyerHidePatch -> NetworkUpgrade.IsHiddenForRun
PASS CardCraftAndFence.dll: DeepSpaceQoL.CfThiefPatch -> StoreClientList.CreateThief
PASS CardCraftAndFence.dll: DeepSpaceQoL.CfInventorPatch -> StoreClientList.CreateInventor
PASS CardCraftAndFence.dll: DeepSpaceQoL.CfInventorStoragePatch -> StoreClientList.CreateInventorStorage
PASS CardCraftAndFence.dll: HarmonyPatch attributes checked (8)
RESULT: PASS
EXIT=0
```

**Probe-Patches**
```text
patched 8 method(s)   ← Appraisal
patched 4 method(s)   ← Aug
patched 8 method(s)   ← CardCraft
  DirectoryMaster.Item
  LockHelper.CreateLockerContainerTooltip
  LockHelper.TryOpenLock
  MachinePrinter.FinishPrinting
  NetworkUpgrade.IsHiddenForRun
  StoreClientList.CreateInventor
  StoreClientList.CreateInventorStorage
  StoreClientList.CreateThief
RESULT: PASS
EXIT=0
```

**Probe-Logic**
```text
[Scan] CardCraftAndFence.dll refused=False capabilities=[] findings=0 error=null
PASS scan CardCraftAndFence.dll
RESULT: PASS
EXIT=0
```

**Scratch**
```text
python .scratch\p5sawyer\check.py
P5 SAWYER CHECK (0.7.2)
  dll : ... (75264 bytes)
  passed: 105  failed: 0
P5 SAWYER CHECK: PASS
EXIT=0

python .scratch\p5loot\parse_code.py
... CMD T2 ratio=3.701 ... SEC T3 ratio=4.068 ... CMD T3 ratio=4.117 ...
VALUE-FLOOR CHECK: PASS
EXIT=0
```

### 4) 0.7.2 待實機

- T3 SEC ×5、T3 CMD ×5、T2 CMD ×3：每箱件數 = rolls + 1（SEC T3 5、
  CMD T3 8、CMD T2 7），log 沒有 `[CF-LOOT] overflow`。
- 新容量在 UI 的格子大小／可讀性（CMD T3 15×9、CMD T2 12×9）未進遊戲看過。
- SEC T2 也順便開箱確認 7 件；若真的遇到 adversarial 6 glock＋shotgun，
  會出現 per-box overflow 行，屆時再放大或調 glock 權重。

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

## 0.5.6 MED T3 rare dummy 修正

實機：

```text
[CF-LOOT] spawn table=cf_med_t3_rare item=cf_crypto_module_med_t3
Could not find GameItem identifier 'cf_crypto_module_med_t3'
```

根因是 **id 順序不一致**，不是 `Add` 重複、也不是 `RequireTier2`：

- T2 核、T3 loot 表、spec 都是 `cf_crypto_module_<family>_t3`
  （例：`cf_crypto_module_med_t3`）。
- 0.5.4 的 `CryptoModuleId(family, 3)` 卻回 `cf_<family>_crypto_module_t3`
  （例：`cf_med_crypto_module_t3`），所以 `InitDirectory` 註冊的是錯的 key；
  loot 抽到 `cf_crypto_module_med_t3` 時查不到 → dummy。
- CMD 也有一樣的錯，只是貨架上那顆是用錯的 key 建的，看起來正常；
  CMD T3 箱的 rare 其實也會 dummy。
- 這不是 `Directory.Add` 回 false：錯的 key 是**另一個新 id**，Add 會成功；
  正確的 key 根本沒進字典。

修正：

- `CryptoModuleId(family, 3)` 改成 `cf_crypto_module_<family>_t3`，與 T2／loot／
  spec 一致。
- 舊的 `cf_<family>_crypto_module_t3` 保留成 **alias**，讓 0.5.4 存檔裡的舊物品
  不會變 unknown；0.5.6 之後不再產生舊 id。
- `BuildCryptoModule` 本來就只用 `RequireCardTier`，沒有 `RequireTier2`。
- 驗證用 `.scratch/idcheck`（離線逐一比對 factory 與 loot 字串，取代再加一次
  in-game InitDirectory dump）：
  - 30 個 cf_* id（5 家族 × T2/T3 × 卡核箱）全部唯一；
  - 5 家族的 T3 核／卡都在 `LootTables` 字串裡；
  - `MED core t3 = cf_crypto_module_med_t3`、`CMD core t3 = cf_crypto_module_cmd_t3`；
  - `BuildCryptoModule` 用 `RequireCardTier`、沒有 `RequireTier2`；`ID CHECK: PASS`。

## 0.5.7 價值地板／0.5.8 核心供貨

三件事一起做：

1. **箱子不再掉核心**：5 張 T2 + 5 張 T3 loot 表移除所有
   `crypto_module_*`／`cf_crypto_module_*`；0.5.8 也把 T1 CMD rare 的
   `crypto_module_cmd` 移除，現在 11 張 cf 表都不掉核心。
2. **價值地板**：
   - T2 common 只收 `unitValue >= 60`；T3 common 只收 `>= 120`；
     低價繃帶／止痛藥／廢金屬／種子全部移除。
   - rare 只放「高階鑰匙卡 + 該家族最高價裝備」，不再用核心撐 EV。
   - 候選清單、每張表的 value/weight、common/rare 平均、rolls 全部寫進
     spec §3.3。摘要：

   | 箱 | rolls | common EV | rare EV | model EV/價 |
   |---|---:|---:|---:|---:|
   | SEC T2 | 6 | 109.75 | 208.50 | 3.853 |
   | MED T2 | 4 | 152.50 | 235.00 | 3.756 |
   | ENG T2 | 3 | 190.75 | 255.00 | 3.677 |
   | SER T2 | 3 | 225.00 | 180.00 | 3.800 |
   | CMD T2 | 6 | 172.75 | 259.00 | 3.701 |
   | SEC T3 | 4 | 238.50 | 266.50 | 4.068 |
   | MED T3 | 4 | 249.50 | 239.50 | 4.125 |
   | ENG T3 | 4 | 250.00 | 281.25 | 4.271 |
   | SER T3 | 4 | 262.50 | 226.50 | 4.255 |
   | CMD T3 | 7 | 252.75 | 289.00 | 4.117 |

   T2 全部 3.6–3.9，且都 >= 同家族 T1 + 0.2（MED +0.226、CMD +0.288）；
   T3 全部 3.9–4.5。200 樣本離線模擬誤差 -3.12%～+0.86%。
3. **核心穩定供貨（0.6.1 多台）**：
   - 卡匠固定 printer×1；chip／CNC 只在
     `CountOwned(chip/cnc) < CountOwned(printer)` 時各補 1（含剛上架 printer）。
     材料電子×2／plastic×2，核心 2（最高等級、`day%5` 連續 2 家族）。
   - cap 11；無擴產 7 件、chip/CNC 都補 9 件；不會補滿 11。
   - 每週總價（設計參考，不是硬門檻）：

   | 等級 | 無擴產 7 件 | 擴產 9 件 | 參考帶 |
   |---|---:|---:|---:|
   | T1 | 890–1090 | 1540–1740 | 1000–1350 |
   | T2 | 1140–1540 | 1790–2190 | 1300–1900 |
   | T3 | 1490–2090 | 2140–2740 | 2000–2700 |

   不改核心 `unitValue`、不為壓價移除 chip／CNC。
   - 博士：PRO 後固定只賣 `cf_pro_desequencer` + `cf_batch_card_module`；
     不再抽 T2/T3 卡核。
   - 卡匠／博士都不再賣卡；卡只從箱子 rare／小偷箱來。
   - T1 CMD rare 也移除 `crypto_module_cmd`（0.5.8），與「箱子不掉核心」一致。

**0.5.6 的 T3 核 id 修正仍然有效**：`CryptoModuleId(...,3)` 現在是
`cf_crypto_module_<family>_t3`，舊錯序 id 保留 alias。`.scratch/idcheck`
改成「T3 卡在 loot、T3 核不在 loot」，`ID CHECK: PASS`。

## 0.6.2 博士住所／小偷順序

**博士住所**：

- `OnEnable` 訂閱 `ModHook.OnPlaceInventorInventoryItemEarly` →
  `CfInventorStock.OnInventorCounter`；`OnDisable` `-=`。
- `OnInventorCounter(List<GameItem> owned)`：PRO 未解鎖 return；否則
  `PlayerStore.Instance.TryAddDirectSellingItemToTable` 各放一件
  `cf_pro_desequencer`、`cf_batch_card_module`。
- 回 false 只印 `[CF-INVENTOR] counter full, skipped <id>`；不 Destroy、
  不丟後台、不進玩家背包。
- hook 在 `PlaceInventorInventory` 與 `PlaceInventorSpecialStock` 都會觸發，
  所以特殊庫存日也有兩件。
- `CfInventorPatch`（CreateInventor）與 `CfInventorStoragePatch`
  （CreateInventorStorage）postfix 一進來都印
  `[CF-INVENTOR] client=<identifier> pro=<bool>`。
  Storage patch **只 log 不 Attach**，住所庫存由 hook 負責，避免同一批加兩次。
- 原本 `CreateInventor` 的 Attach 保留（博士臨時來店裡）。

**小偷順序**：

- `CfThiefBoxes.Attach` 的 endAction 改成 **`Offer()` 先、原版 `original()` 後**。
- 6×4 箱需要 6 個連續空欄；原版先鋪會讓箱子幾乎永遠 `counter full, skipped`。
- 若原版商品被擠掉，這是**刻意取捨**，spec §3.10.1／§3.10.4 已註明。
- 兩個 try/catch 分開：Offer 失敗不會阻止原版庫存執行。

Patch 數：CardCraft 由 7 → **8**（新增 CreateInventorStorage）。

## P4 修訂實作要點（詳見 spec §3.8／§3.10／§3.11）

- **升級只剩 4 個**：`CF_FENCE_PRO` 1500（無前置）、`CF_FENCE_T2` 1200
  （前置 PRO）、`CF_FENCE_T3` 3000（前置 T2）、`CF_FENCE_THIEF_T2` 2200
  （無前置）。`CONTACT`／`SUPPLY`／`BATCH`／`T1`／`THIEF` 已刪。
- **PRO**：`onUnlock` 一次性發 printer、空白卡晶片、CNC、電子×2、耗材×2、
  `energy_credit` 電池×1，用 `GraphUtils.EmporiumTryAdd` 進 `backInvinvElement`；
  同時解鎖製卡匠到訪。
- **製卡匠**：掛 `OnGenerateCustomerVeryLate`；`dayCounter % 7 == 6` 每週一次，
  PRO 首次解鎖後補訪一次；`maker_last_day` 存在存檔 modData。
  **9 件（上限 11）**：晶片、CNC、電子×2、plastic×2，加上 3 顆核心
  （最高已解鎖等級；`day%5` 家族連續 3 個）；不賣 printer／metal、不賣箱／卡，
  同日不重複、不做翻頁。解序器與批量模組由博士固定賣。
- **博士**：`CfInventorPatch` postfix 包 `StoreClientList.CreateInventor` 最後一句
  對話的 `endAction`（先跑原版）；PRO 後**固定只賣** `cf_pro_desequencer` 與
  `cf_batch_card_module`，不再抽 T2／T3 卡核。
- **小偷**：postfix 包 `CreateThief` 的 `endAction`；**不加 T1、永不 CMD**。
  T3＝解鎖 `CF_FENCE_T3` 且聲望 ≥85；T2＝聲望 ≥60 或（≥30 且解鎖
  `CF_FENCE_THIEF_T2`）；兩者都不成立就不給 CF 箱。權重 SEC 32／MED 27／
  ENG 21／SER 20。全部 `isStolen: true`；不再改 `acceptRetailMarkup`。
- **櫃台滿 skip（0.5.8）**：前台原版 18×4、箱子 6×4，需要 6 個連續空欄；
  箱子／卡匠／博士貨物先 `GraphUtils.CanAccept(front, item)` 探測，放不下就
  `Destroy()`＋`[CF-THIEF]`／`[CF-STOCK] counter full, skipped <id>`，
  **不進後台、不進玩家背包**；該件本次不販售。
- **T3 箱**：`cf_*_box_t3`、0.7.2 起 SEC 13×8／CMD 15×9／其餘 9×8、
  300／500、0.5.7 rolls 4/4/4/4/7；`CF_CRATE_TIER=3`、同一組 `LOCK_ID_*`、
  T3 卡可開。Loot 用 T3 值地板池（`>=120`），rare 只放 T3 卡＋最高價裝備，
  **不掉核心**；model EV/價 3.9–4.5。
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
| 3 | 製卡匠買賣 | printer×1；chip／CNC 只在擁有數 < printer 數時各補 1；電子×2／plastic×2＋2 顆核心（最高等級、`day%5` 連續 2 家族）；7–9 件、上限 11；不賣 metal、不賣卡 |
| 4 | 遇博士 | `[CF-INVENTOR] client=… pro=…`；前台固定只有專業解序器與批量模組；住所 hook 另有 `[CF-INVENTOR] counter …` |
| 4b | 晚上博士住所 | 櫃台多 `cf_pro_desequencer`＋`cf_batch_card_module`；log 有 `client=…` 與 `counter …`（或 `counter full, skipped`） |
| 5 | 黑市聲望 <30 遇小偷 | 沒有 CF 箱（只有原版 thief 貨） |
| 6 | 聲望 ≥60 或（≥30＋`CF_FENCE_THIEF_T2`） | 小偷給 1 個 T2 箱（SEC/MED/ENG/SER） |
| 7 | 解鎖 `CF_FENCE_T3` + 聲望 ≥85 | 小偷給 1 個 T3 箱；用 T3 卡可開 |
| 8 | 打開 Wild Network | 第 3 個分頁「制卡」只有 4 個升級、無第 4 個分頁 |
| 9 | T3 箱 5–8 件 | 9×8／CMD 10×8 放得下（未實機量） |
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
- **滿台 skip 未實機**：`CanAccept` 後 `Destroy()`＋`skipped` 只在離線 IL
  驗過；實際 18×4 何時算滿、小偷先放 CF 箱後原版商品是否被擠掉，
  都要看 `Player.log` 的 `counter full, skipped …`。
- **博士住所 hook 未實機**：`OnPlaceInventorInventoryItemEarly` 是否在
  `PlaceInventorInventory`／`PlaceInventorSpecialStock` 都觸發、`owned` 數量、
  `TryAddDirectSellingItemToTable` 放不下時是否真的 skip，尚未看 log。
- **CreateInventorStorage patch 只 log**：確認 client identifier 用；住所庫存
  由 hook 加，避免 storage patch 再 Attach 造成重複。
- **CMD 箱染色未實機**：深藍 `0.24/0.35/0.62` 的觀感、6×4 輪廓、`SetNativeSize`
  大小、解鎖 `cmd_box_u`、存讀檔重貼，全部只有 IL／字串核對。
- **`ModLoader.Mods` 時序**：`OnEnable` 在 `Mods.Add` 之前，本輪用
  `OnModAssetsLoaded` 補註冊；它是否一定早於第一個 CMD 箱，還沒用 `Player.log` 驗。
  若來不及，該箱會先 fallback 成 `sec_box`（褐色），直到 `OnGameLoadedLate` 或
  下一箱才變深藍。
- **tabBar 實際寬度**：spec §3.11.1 的 1060 來自 `WildNetworkStyle.TABS_W` 常數，
  不是 prefab 或 runtime `RectTransform` 量測；如果 prefab 的 tabBar 不是 1060，
  第 3 個分頁的結論要重算。
- **T3／T2 fit**：0.7.2 已用實際 `itemShape` 重算容量並做 3000 次 packing
  模擬，但新尺寸（尤其 CMD T3 15×9）沒有 runtime 量過；若還是放不下，
  新的 per-box `[CF-LOOT] overflow …` 會直接指出。
- **T3「200 樣本」是離線模擬**：同權重、同 `Pick` 演算法，但不是 in-game
  `CfLootProbe`；實機 `[CF-EV]` 數字仍待人類補。
- **工匠 7–9 件（上限 11）**：printer 固定；chip／CNC 依數量差各 0–1；
  材料 2+2、核心 2。前台 18×4 理論上放得下，放不下則該件 `skipped`、
  本次不賣，不會掉進背包。
- **多台機器／核心輪換**：chip／CNC 的 `CountOwned` 用 `FindAllItem()`
  （owned only）＋剛上架 printer；`day%5` 取連續 2 家族、最高已解鎖等級。
  實際多台補件節奏、日期/等級行為未實機驗。
  **總價見 spec §3.8.4 表格**（T1 890–1090／1540–1740、T2 1140–1540／
  1790–2190、T3 1490–2090／2140–2740），參考帶不是硬門檻。
- **`premiumBuy15` 對 SELL 客戶的實際效果**：照原版退休匠加，但這個 flag 主要
  影響客戶「買」東西的價格；SELL 客戶是否真的用到，未在 runtime 核。
- **黑市聲望門檻 85／60／30**：是我方定案（spec §3.10.2），不是原版常數；
  實機要到對應聲望才能驗。
- **`CF_SAWYER_*` 隱藏**：0.7.0 起三個索耶升級已註冊，postfix 也在；但
  「交換所未解鎖就整條藏起來」仍未實機看過 Wild Network。
- **PRO 工具包進 backInvinvElement**：`EmporiumTryAdd` 在後台滿時會依序落到其他
  店內庫存；這個 fallback 沒有實機驗。
- **舊升級孤兒**：買過舊 `CF_FENCE_CONTACT/...` 的存檔，`networkUpgrade` 字典會
  留下未註冊的 id；不顯示、不影響新升級，但沒有存檔清理。
- P3 的 #5／#6／#7 仍未補 log 證據（沿用上一輪結論）。
- `unitValue`（晶片 250／解序器 800／T3 卡核新價）是定案值但未經遊戲平衡驗證。
- `long_module2` 是否真的是 1×4：由 sprite 切片決定，未在 runtime 量過。
- **P5 0.7.0 已實機通過一部分**：載入、`[CF-NET] registered 7/7`、
  `Harmony: 8`、無 `Init failed`、無 `cf_` dummy；`removed promotional
  barters`／`paper book granted` 有出現；`skip: janitorial off` 正常；夜間
  handler 有跑，T1／T2／T3 箱都建得出來、上鎖，T1 鑰匙開 T3 箱
  `canOpen=False`；存檔 T3 SEC 箱 5 件、`CF_CRATE_FILLED=true`。
- **P5 0.7.1 已實機通過**（紙本、分組、SEC T2 rare、上鎖、T3 鑰匙）：
  開書不再有 `No sprite found`／藍方塊；3 T3 + 1 T1 → T3、2 T1 + 3 T2 → T2；
  SEC T2 7 件齊全。
- **P5 0.7.2 待實機重測**（容量／填充）：
  - T3 SEC ×5、T3 CMD ×5、T2 CMD ×3：每箱件數 = rolls + 1
    （SEC T3 5、CMD T3 8、CMD T2 7），log 沒有 `[CF-LOOT] overflow`。
  - 新容量（CMD T3 15×9、CMD T2 12×9）在 UI 的格子大小／可讀性。
  - SEC T2 也順便開箱 7 件；adversarial 6 glock＋shotgun 仍可能排不下，
    會由 per-box overflow 行指出。
- **P5 其餘仍未實機**：
  - 芯片：3 個同家族同級→下一級、T3 不升、每晚每家族 1 次（3 T1＋3 T2 只做
    T1→T2），以及 0.5.4 舊 alias `cf_<slug>_crypto_module_t3` 的讀取。
  - 常駐接管：`promotionalBarter`／`activePromotionalBarterIds` 移除後，
    `ExchangeUIManager` 的促銷條目是否立刻消失、重開 UI 後會不會復活。
  - 紙本 `EnsureRegistered` 在 `ModItemDirectory` 未 live 時第一輪會靜默失敗，
    依賴 `OnGameLoadedLate`／`OnNewGameStarted`／`Grant` 前補註冊的時序。
  - `_removalLogged` 是 process 內 static：同一行程切換存檔時第二個存檔的移除
    不會再印 log（行為正確、只是少一條 log）。
  - 垃圾桶滿時原版 `exchange delivery, trashcan full` 會丟棄換卡產出。
  - P5 也把 CMD T1 箱納入換卡產出（`cf_cmd_box_t1`，200）；換卡是它第一個
    正常來源，未實機驗。
- **完整 §3.1–3.12 規格**：仍未補齊；目前只有 P1/P2 定案段、§3.3 loot 表、
  §3.4 errata、§3.4b T3 定價、§3.5 P3、§3.8／§3.10(2)／§3.11 P4、§3.12 貼圖，
  以及 P5 的 §3.9。
- **`Resources.LoadAll<Sprite>` runtime 清單**：目前只有 asset 字串掃描。
- **CMD T2 -7.37%**：已接受的例外，未改。
- **CMD 箱來源**：T1／T2／T3 在江洋大盜（P6）之前沒有正常來源，先留著。

## 下一步

1. 0.6.2（博士住所 hook／小偷順序）已提交（`75d227f`，本機 commit、未 push）。
2. **P5 0.7.0 已提交 `66266cf`；0.7.1 已提交 `81c2d08`；0.7.2（容量／
   填充）已提交 `801a5b0`、未 push**；工作區只剩本輪開始前就存在的
   `ACCEPTANCE_REVIEW.md`／`DEVELOPER_DIAGNOSTIC.md` 修改（非本輪）。
3. 人類實機重測 **0.7.2 容量三箱**（T3 SEC ×5、T3 CMD ×5、T2 CMD ×3，
   件數 = rolls+1、無 overflow、SEC T2 7 件），再跑 P4 修訂、CMD 貼圖與
   P5 其餘驗收卡，收集 `[CF-NET]`／`[CF-MAKER]`／`[CF-INVENTOR]`／
   `[CF-THIEF]`／`[CF-SPRITE]`／`[CF-SAWYER]`／`[CF-PAPER]`，並補 T3
   in-game `[CF-EV]`。
4. **江洋大盜（P6）**：補 CMD 箱的正常來源（P5 的換卡已先給 CMD T1 一個來源）。
5. 其餘黑市內容；補 §3.1–3.11 全文與 `Resources.LoadAll` 實機清單。

## 禁區

- 不要從 agent 啟動 Steam／遊戲；需要實機時出測試卡給人類。
- 不要改使用者存檔、Steam registry、Steam 進程。
- 不要動已完成 P1/P2 行為；不要改原版物品價格。
- 不要把 `.scratch/` 的參考源或 tool 輸出編譯進 MOD。
- 不要 push、不要改 `Assembly-CSharp.dll`。

## 主要檔案

- `src/CardCraftAndFence/CardCraftAndFence.cs`（P4 修訂新增 `CfCardStock`、
  `CfInventorStock`、`CfInventorPatch`；0.5.1 新增 `CfCmdBoxSprite`；
  **0.7.0（P5）新增 `CfSawyerExchange`（換卡／芯片／促銷 barter 移除）與
  `CfSawyerPaper`（換卡目錄紙本）；0.7.1 新增 `CfPaperSprites`（紙本 runtime
  貼圖）並改降冪分組；0.7.2 重算 T2/T3 容量（SEC T2 10×8、CMD T1 10×8、
  CMD T2 12×9、SEC T3 13×8、CMD T3 15×9）、`Fill` 改大件先放、smg/shotgun
  移出三張 common 池並重配權重、overflow 每箱一行**；`CfNetwork` 由 4 個
  升級變 7 個；`CfThiefBoxes` 只賣 T2/T3；`ItemFactory`／`LootTables` 擴到
  T3 箱）
- `src/CardCraftAndFence/manifest.xml`（0.7.2）
- `docs/CARD_CRAFT_SPEC.md`（§3.3 T3 箱／loot、§3.4b、§3.8、**§3.9 P5**、
  §3.10、§3.11、§3.12）
- `docs/CARD_CRAFT_STATUS.md`（本檔）
- `tools/Build-Mods.ps1`、`tools/Verify-Mods.ps1`、`tools/Probe-Patches.ps1`、
  `tools/Probe-Logic.ps1`（本輪未改）
- `.scratch/p3check/`、`.scratch/p4check/`、`.scratch/p4bcheck/`、
  `.scratch/p4t3loot/`、`.scratch/p5spritecheck/`、`.scratch/regcheck/`、
  `.scratch/upgradecheck/`、**`.scratch/p5sawyer/check.py`**（scratch-only
  離線核對，不進 MOD）
