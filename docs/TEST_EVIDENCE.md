# 測試證據（2026-11-02）

本文件記錄實際跑過的命令與輸出，以及測試後如何還原使用者環境。

## 1. 環境

- Windows，PowerShell 5.1（沒有 `pwsh` 7）。
- 遊戲：`C:\Program Files (x86)\Steam\steamapps\common\Probably Stolen`
  - Unity 2021.3.45f2（Mono）
  - `PlayerStore.VERSION = VERSION 100-Rev6`
  - `Application.version` 日誌顯示 `0.46D`
- 遊戲本體 `Assembly-CSharp.dll`：4,612,872 bytes，SHA256 未記錄基準；以 IL 檢查確認
  `SteamManager.Awake` 仍含 `SteamAPI.RestartAppIfNecessary`（65 條 IL），即未被修改。
- 測試副本：`.scratch/testgame`（整個遊戲目錄的複本；只 patch 副本的
  `Assembly-CSharp.dll`，把 `SteamManager.Awake` 改成不初始化 Steam，只有 6 條 IL）。
  此副本不影響正式安裝。

## 2. 構建

```text
Compiling AppraisalAssistant...
  -> dist\AppraisalAssistant\AppraisalAssistant.dll (20480 bytes)
Compiling AugSpotter...
  -> dist\AugSpotter\AugSpotter.dll (11264 bytes)
Build OK
```

## 3. 靜態驗證（tools/Verify-Mods.ps1）

```text
PASS AppraisalAssistant: ID deepspace.appraisal_assistant
PASS AppraisalAssistant: capabilities empty
PASS AppraisalAssistant.dll: IMod implemented by DeepSpaceQoL.AppraisalAssistantMod
PASS AppraisalAssistant.dll: no banned IL APIs
PASS AppraisalAssistant.dll: AppraisalTooltipPatch -> AppraisalHelper.CreateAppraisalTooltip
PASS AppraisalAssistant.dll: AppraisalAutoFillPatch -> InspectionUIManager.UpdateAppraisalPanel
PASS AppraisalAssistant.dll: AppraisalForceEstimatePatch -> InspectionUIManager.OnAppraisalServiceEstimated
PASS AppraisalAssistant.dll: CigaretteTitlePatch -> InspectionUIManager.OpenCigaretteConfrontPanel
PASS AppraisalAssistant.dll: StampTitlePatch -> InspectionUIManager.OpenStampConfrontPanel
PASS AppraisalAssistant.dll: InjectorTitlePatch -> InspectionUIManager.OpenInjectorConfrontPanel
PASS AppraisalAssistant.dll: AuthenticityTooltipPatch -> InspectableHelper.CreateInspectableTooltip
PASS AppraisalAssistant.dll: HarmonyPatch attributes checked (7)
PASS AugSpotter: ID deepspace.aug_spotter
PASS AugSpotter: capabilities empty
PASS AugSpotter.dll: IMod implemented by DeepSpaceQoL.AugSpotterMod
PASS AugSpotter.dll: no banned IL APIs
PASS AugSpotter.dll: HarmonyPatch attributes checked (0)
RESULT: PASS
```

## 4. Harmony patch 套用（Mono 6.12，離線載入遊戲組件）

命令（實際使用 workspace 內的 Mono 6.12 與遊戲 `Managed` 的組件複本）：

```text
.tools\mono\Mono\bin\mono.exe .tools\harmonyprobe\HarmonyProbe.exe <Managed> dist\AppraisalAssistant\AppraisalAssistant.dll dist\AugSpotter\AugSpotter.dll
```

輸出：

```text
== dist\AppraisalAssistant\AppraisalAssistant.dll
patched 7 method(s)
  AppraisalHelper.CreateAppraisalTooltip
  InspectableHelper.CreateInspectableTooltip
  InspectionUIManager.OnAppraisalServiceEstimated
  InspectionUIManager.OpenCigaretteConfrontPanel
  InspectionUIManager.OpenInjectorConfrontPanel
  InspectionUIManager.OpenStampConfrontPanel
  InspectionUIManager.UpdateAppraisalPanel
== dist\AugSpotter\AugSpotter.dll
patched 0 method(s)
RESULT: PASS
```

（Mono 沒有 Unity 的 native ECall，所以載入時會印出 `cant resolve internal call ...` 警告；
這不影響 patch 套用。真正遊戲內由 Unity Mono 提供這些 internal call。）

## 5. 真假／Aug／估價邏輯（Mono，使用遊戲內真實類別）

命令：

```text
.tools\mono\Mono\bin\mono.exe .tools\logicprobe\LogicProbe.exe <Managed> dist\AppraisalAssistant\AppraisalAssistant.dll dist\AugSpotter\AugSpotter.dll
```

輸出：

```text
PASS aug=true
PASS aug=false
PASS aug=null
PASS fake_stamp kind=Counterfeit label=COUNTERFEIT
PASS real_stamp kind=Genuine label=GENUINE
PASS fake_injector kind=Counterfeit label=COUNTERFEIT
PASS expired_injector kind=Defective label=DEFECTIVE - EXPIRED
PASS unusable_injector kind=Defective label=DEFECTIVE - UNUSABLE
PASS replica kind=Replica label=REPLICA (NOT ORIGINAL)
PASS fake_wine kind=Counterfeit label=COUNTERFEIT
PASS unknown_item returns false
PASS appraisal_value hand=100
[Scan] AppraisalAssistant.dll refused=False capabilities=[] findings=0 error=null
PASS scan AppraisalAssistant.dll
[Scan] AugSpotter.dll refused=False capabilities=[] findings=0 error=null
PASS scan AugSpotter.dll
RESULT: PASS
```

說明：

- 真假測試使用 `GameItemElement` 的未初始化實例 + 真實 `ItemFeature` / `ItemCondition`，
  直接驅動 `DeepSpaceQoL.Truth.TryGet`，不是硬編碼預期字串。
- 估價測試建立帶 `HAND_TAG` 與 `APPRAISAL_ITEM` 的 `GameItemElement`，驅動
  `DeepSpaceQoL.Log.TryGetCorrectValue`，得到 `hand=100`，與 `AppHandHelper.GetCorrectValue`
  的基礎值一致。
- `[Scan]` 是**遊戲內建 `ModScanner.Scan`** 的實際執行結果，不是自製檢查器。
- 尚未覆蓋：`InspectionUIManager` 的 TMP 輸入框與 overlay 的實際 Unity 畫面行為；
  這需要真正執行遊戲（見第 7 節）。

## 6. 還原與最終安裝

測試期間曾把 MOD 複製到正式遊戲的 `Mods/`，並多次寫入 `SaveFile.es3` 的
`modTrust` / `modOrder`；中間因 PowerShell 5.1 的 `ConvertFrom-Json` 會吃掉 ES3 的
`__type`，一度使 MOD 全部無法載入。已修正並用正確格式重新寫入：

- `tools/Enable-Mods.ps1` 現在改由 Python `tools/update-mod-config.py` 更新，
  保留 `modOrder.__type` 與 `modTrust.__type`。
- 測試前乾淨備份仍在 `backup/SaveFile/SaveFile_20261102-171150.es3`
  （SHA256 `1FA1DCCA...B929D878`）。
- 目前 `SaveFile.es3` 是**已啟用兩個 MOD** 的狀態，`modOrder` / `modTrust`
  的 `__type` 正確；可用 `tools/Restore-SaveFile.ps1` 還原最近備份。
- 正式遊戲 `Mods/` 目前安裝並啟用 `AppraisalAssistant`、`AugSpotter`。
- 正式遊戲 `Assembly-CSharp.dll`：`RestartAppIfNecessary present: True`，
  `Awake instruction count: 65`；與測試副本的 `False / 6` 不同，證明正式 DLL 未被 patch。
- 正式遊戲 `Probably Stolen.exe`、`UnityPlayer.dll`、`save_*.es3` 未修改。

## 7. 遊戲內實測與最終清理

- 遊戲內已成功載入兩個 MOD：
  - `AppraisalAssistant`：8 個 Harmony patch。
  - `AugSpotter`：0 個 Harmony patch；overlay 用 `StoreCalendar.dayTMP` 複製中文
    字體／fallback／材質。
- 已截圖確認：
  - 改造體 overlay 顯示中文（早期只有數字 `4` 是字體 fallback 問題，已修正）。
  - 假貨 tooltip：`真伪判定：假货`、`原因：...`；沒有收購建議。
  - 對質標題只顯示 `[假货]`，原字級，不加原因。
  - 同一顧客多件鑑定品：加入 `AppraisalInspectResetPatch` 後每件各自使用自己的價值。
- 最終清理（使用者要求關掉多餘監聽／debug）：
  - 移除 `AppraisalRuntime` 自測協程、`RunSelfTest` 設定與 `RuntimeHost`。
  - 移除 `AugSpotter` 自測與相關設定。
  - 所有 `Player.log` 背景 watcher job 已結束，沒有執行中的監聽。
  - 最終 DLL：`AppraisalAssistant.dll` 14,848 bytes、`AugSpotter.dll` 8,192 bytes。
  - `tools/Verify-Mods.ps1` PASS；`HarmonyProbe` 8 patches；`LogicProbe` 全 PASS。
- 早期 76MB 的 `NullReferenceException` 日誌已移到 `backup/logs/`，原因是
  `AugOverlay` 太早讀 `PlayerStore`；已改成延遲 5 秒 + 例外保護 + 低頻探測。

## 8. 可重跑的工具

- `tools/Setup-Toolchain.ps1`：下載 Roslyn 4.8、複製 `DseCecil.dll`。
- `tools/Build-Mods.ps1`、`tools/Verify-Mods.ps1`。
- `tools/Probe-Patches.ps1`、`tools/Probe-Logic.ps1`：Harmony / 邏輯探針（需要 `.tools/mono`）。
- Mono 6.12 放在 `.tools/mono`，不在 git 內。
- `tools/Install-Mods.ps1` / `tools/Enable-Mods.ps1`：安裝與寫入信任紀錄；`Enable-Mods.ps1`
  會先備份 `SaveFile.es3`。
- `tools/Restore-SaveFile.ps1`：從 `backup/SaveFile/` 還原最近一次備份，會先備份目前檔案。
- `tools/Uninstall-Mods.ps1`：移除兩個 MOD 資料夾，會先備份到 `backup/Mods/`。

## 9. 切換顧客卡頓修正（2026-11-02）

> 第 3–7 節保留當時的歷史輸出；AugSpotter 在那些版本仍是 0 patch、每 0.5 秒輪詢。
> 本節（9.x）才是最終的事件驅動版本。

### 9.1 根因

`InspectionUIManager.Update()` 在估價面板開啟時每幀呼叫 `UpdateAppraisalPanel()`。
`AppraisalAutoFillPatch` 的 Postfix 原本每幀無條件呼叫：

```text
GameItem.ModifyTag("APPRAISAL_DEFAULT_VALUE", ...)
  -> GameItem.ValidateShapeState()
  -> OverlayHandler.UpdateInventoryStateUI()
```

`UpdateInventoryStateUI()` 會讓 visible windows 的 `stateChangeActionFunc` 重建 UI，
所以不是死循環，而是每幀重複做一次本可只做一次的背包 UI 重建。切換顧客／估價面板
仍 active 時最容易累積成掉幀。

### 9.2 修正

- 新增 `AppraisalTag.NeedsWrite()`：讀 `APPRAISAL_DEFAULT_VALUE` 的 enabled/int；
  只有未啟用或數值不同才允許寫入。
- `AppraisalAutoFillPatch`、`AppraisalInspectResetPatch`、
  `AppraisalForceEstimatePatch` 全部改走 `AppraisalTag.SetDefaultValue()`。
- 估價輸入框也只在文字不同時才赋值。

### 9.3 實際輸出

```text
Compiling AppraisalAssistant...
  -> dist\AppraisalAssistant\AppraisalAssistant.dll (15360 bytes)
Compiling AugSpotter...
  -> dist\AugSpotter\AugSpotter.dll (9216 bytes)
Build OK
```

`tools/Verify-Mods.ps1`：

```text
PASS AppraisalAssistant.dll: HarmonyPatch attributes checked (7)
PASS AugSpotter.dll: HarmonyPatch attributes checked (4)
RESULT: PASS
```

`tools/Probe-Logic.ps1` 新增：

```text
PASS appraisal_write_guard initial=true same=false changed=true
PASS appraisal_write_value=100
RESULT: PASS
```

`tools/Probe-Patches.ps1`：

```text
patched 7 method(s)
  AppraisalHelper.CreateAppraisalTooltip
  InspectableHelper.CreateInspectableTooltip
  InspectionUIManager.OnAppraisalServiceEstimated
  InspectionUIManager.OnInspectItem
  InspectionUIManager.OpenCigaretteConfrontPanel
  InspectionUIManager.OpenInjectorConfrontPanel
  InspectionUIManager.OpenStampConfrontPanel
patched 4 method(s)
  PlayerStore.DismissCurrentClient
  PlayerStore.Reset
  PlayerStore.SetupWaitingClient
  StoreUIManager.OnNextClientArrived
RESULT: PASS
```

（Mono 6.12 沒有 Unity native ECall，載入時仍會印 `cant resolve internal call` 警告；
這是離線 probe 的已知限制，不影響 patch 套用。）

安裝後雜湊：

```text
AppraisalAssistant match=True installed=1E5A9F4FB4DC4361B5137F92C4C67B4F87C74C53C71F9F3AD12EECF77B9EAF31
AugSpotter         match=True installed=1D655E0DC9B605A7A1DAFEFCCFFFA9D711CA1A567B49A61638D702AB067B340E
```

### 9.4 AugSpotter 改為事件驅動

- 移除 `AugOverlay.Update()` 內每 0.5 秒的 `PlayerStore.currentClientInstance` 讀取，
  以及 `_playerProbeAt`／`Time.realtimeSinceStartup`。
- 改為 4 個 Harmony Postfix：
  - `StoreUIManager.OnNextClientArrived`：正常顧客走到櫃檯。
  - `PlayerStore.SetupWaitingClient`：早晨／特殊顧客直接生成。
  - `PlayerStore.DismissCurrentClient`：顧客離開。
  - `PlayerStore.Reset`：讀檔／新遊戲重置。
- 首次建立 overlay 時仍讀一次目前顧客，覆蓋「載入存檔時顧客已在櫃檯」。
- 更新成功後 `AugOverlay.enabled = false`；之後只有事件到來才暫時啟用。
- 原始碼檢查：`0.5f` 只剩 `CanvasScaler.matchWidthOrHeight`，沒有顧客輪詢。

### 9.5 鑑定 auto-fill 也改為事件驅動

- 移除 `AppraisalAutoFillPatch`（原本 patch `InspectionUIManager.UpdateAppraisalPanel`，
  而該方法由遊戲的 `Update()` 每幀呼叫）。
- 自動填入改由真正的鑑定事件 `InspectionUIManager.OnInspectItem` 的 Postfix 負責：
  清除 `isEditingAppraisalField`、填入輸入框、設定 `APPRAISAL_DEFAULT_VALUE`。
- `auto_fill=false` 時，`AppraisalInspectResetPatch` 仍會清除編輯狀態，但跳過填值。
- `AppraisalTag.SetDefaultValue()` 使用 `ModifyTag(..., skipValidate: true)` 並手動同步
  `modifiedState`；`APPRAISAL_DEFAULT_VALUE` 是純數值 tag，不需要重建 shape／背包 UI。
- `AppraisalForceEstimatePatch` 保留，提交時仍以正確值覆蓋。
- `AppraisalAssistant` patch 數：8 → 7。
- 原始碼不再有 `UpdateAppraisalPanel` 的 Harmony patch。

### 9.6 Tooltip 每幀重建的快取

- `ItemTooltipHandler.OnEventUpdate()` 每幀重建 tooltip；拖動任何物品都會走
  `AppraisalHelper.CreateAppraisalTooltip` 與 `InspectableHelper.CreateInspectableTooltip`。
- `Log.TryGetCorrectValue()` 用 `ConditionalWeakTable<GameItem, ValueBox>` 快取正確值，
  包含負面結果（不是 `APPRAISAL_ITEM`、registry 找不到）。
- `Truth.TryGet()` 用 `ConditionalWeakTable<GameItem, TruthBox>` 快取真假結果，包含
  「沒有真假特徵」的負面結果；Ribwich 因開封狀態會影響 reason，不快取。
- `InspectionUIManager.OnInspectItem` 會清除該物品的兩個快取，避免物品後來才被標記。
- 效果：同一物品第一次 tooltip 之後，後續拖動 frames 不再重跑 registry／ItemFeature 掃描。

### 9.7 尚未核實

- 尚未在真實遊戲內量測 frame time；需要玩家在最新 `1E5A` 版拖動普通物品與鑑定物品做主觀驗收。
- 事件覆蓋率以遊戲源碼的四個入口為準；若某個 debug／特殊流程直接改
  `PlayerStore.currentClientInstance` 而不經過這四個方法，overlay 可能不會立即更新。
  正常遊玩路徑已覆蓋，但尚未逐一實測所有 debug 指令。
