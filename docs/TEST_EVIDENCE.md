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

## 6. 還原紀錄

測試期間曾把 MOD 複製到正式遊戲的 `Mods/`，並用 `tools/Enable-Mods.ps1` 寫入
`SaveFile.es3` 的 `modTrust` / `modOrder`。測試後已還原：

- `SaveFile.es3`：
  - 測試前備份：`backup/SaveFile/SaveFile_20261102-171150.es3`
  - 還原後 SHA256：`1FA1DCCA3C9A470DD84D7322A2B1238879C8B8197FA298A8A6AE1EBAB929D878`
  - 與備份 SHA256 完全一致；`modTrust` / `modOrder` 已不存在。
- 正式遊戲 `Mods/`：已移除 `AppraisalAssistant`、`AugSpotter`，資料夾恢復為空。
- 正式遊戲 `Assembly-CSharp.dll`：`RestartAppIfNecessary present: True`，
  `Awake instruction count: 65`；與測試副本的 `False / 6` 不同，證明正式 DLL 未被 patch。
- 正式遊戲 `Probably Stolen.exe`、`UnityPlayer.dll`、`save_*.es3` 未修改。
- 完整 AppData 備份：`backup/AppData_20261102/`（注意：此備份是在 Enable-Mods 之後才建立，
  因此含 `modTrust`/`modOrder`；真正乾淨的 SaveFile 是上面的 `backup/SaveFile/` 版本）。

## 7. 未完成／限制

- **沒有在真正的遊戲執行期完成 UI 實測。** 原因：
  - 此環境的 Steam 用戶端處於離線／無法登入狀態；直接啟動 `Probably Stolen.exe` 會以
    exit code `53` 結束（Steam DRM／`RestartAppIfNecessary`）。
  - 用 sandbox 啟動 `steam.exe` 時，Steam 跳出「登錄檔路徑無法寫入」對話框；這是
    Steam 自身權限問題，不是 MOD。已關閉 `steam.exe`，沒有寫入成功。
- 因此以下項目仍需要使用者或下一個有正常 Steam 環境的回合手動驗收：
  - `Player.log` 的 `[ModLoader] Loading` / `Initialized` 行。
  - overlay 在實際畫面中的位置與字型。
  - 估價輸入框自動填入、按估算後 perfect。
  - 對質面板標題的 `[GENUINE]` / `[COUNTERFEIT]`。
- 已用工作區複本 `testgame` 避免動到正式安裝；該資料夾可直接刪除。

## 8. 可重跑的工具

- `tools/Setup-Toolchain.ps1`：下載 Roslyn 4.8、複製 `DseCecil.dll`。
- `tools/Build-Mods.ps1`、`tools/Verify-Mods.ps1`。
- `tools/Probe-Patches.ps1`、`tools/Probe-Logic.ps1`：Harmony / 邏輯探針（需要 `.tools/mono`）。
- Mono 6.12 放在 `.tools/mono`，不在 git 內。
- `tools/Install-Mods.ps1` / `tools/Enable-Mods.ps1`：安裝與寫入信任紀錄；`Enable-Mods.ps1`
  會先備份 `SaveFile.es3`。
- `tools/Restore-SaveFile.ps1`：從 `backup/SaveFile/` 還原最近一次備份，會先備份目前檔案。
- `tools/Uninstall-Mods.ps1`：移除兩個 MOD 資料夾，會先備份到 `backup/Mods/`。
