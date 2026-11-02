# 驗收標準（v1.0）

本文件在動工前寫好。每一條都要有可重跑的命令或可檢查的日誌；不以「看起來可以」作驗收。

## 0. 範圍與前提

- 遊戲：Steam《深空當舖：可能是偷的 / Probably Stolen》，Unity 2021.3.45f2（Mono），安裝於
  `C:\Program Files (x86)\Steam\steamapps\common\Probably Stolen`。
- 只使用遊戲內建 Mod Loader（`Mods/<資料夾>/manifest.xml` + `IMod`），不修改遊戲本體 DLL。
- 交付兩個獨立 MOD：
  1. `AppraisalAssistant`（鑑定助手）：價值估算 + 真假判斷。
  2. `AugSpotter`（改造體辨識）：自動標示目前顧客是否為改造體（Aug）。
- 所有臨時分析檔放 `.scratch/`，工具放 `.tools/`，兩者不進 git。

## 1. 構建與靜態驗收（可自動重跑）

### A1 構建

命令：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Build-Mods.ps1
```

期望：

- 退出碼 `0`。
- 生成：
  - `dist/AppraisalAssistant/AppraisalAssistant.dll`
  - `dist/AppraisalAssistant/manifest.xml`
  - `dist/AugSpotter/AugSpotter.dll`
  - `dist/AugSpotter/manifest.xml`
- 每個 DLL 大小 > 0，且 `manifest.xml` 的 `<Assemblies>` 指向實際 DLL。

### A2 靜態驗證

命令：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Verify-Mods.ps1
```

期望：退出碼 `0`，最後一行為 `RESULT: PASS`，且逐項檢查：

1. 每個 `manifest.xml` 可解析；`ID` 合法；`<Capabilities/>` 為空（MOD 不使用檔案／網路／程序／動態碼／原生／反射／Steam）。
2. 每個 DLL 恰好有一個 `IMod` 實作，且實作 `Init(ModManifest)`、`OnEnable()`、`OnDisable()`。
3. DLL 的組件參考只包含遊戲自帶組件或自身（不得捆綁 `0Harmony`、`Mono.Cecil` 等）。
4. IL 中不得出現：`System.IO.*`、`System.Net.*`、`System.Diagnostics.Process*`、`System.Reflection.Emit.*`、`Assembly.Load*`、`AppDomain.Load*`、`DllImport`。
5. 所有 `[HarmonyPatch]` 的目標型別／方法存在於 `Assembly-CSharp.dll`，且參數名稱與型別可匹配。
6. 不使用 `System.IO`／`System.Net`／`System.Reflection` 直接呼叫。

### A3 Git

命令：

```powershell
git status --short
```

期望：無輸出（工作區乾淨）。`git log --oneline -1` 有對應提交。

### A4 離線 Harmony / 邏輯測試

命令（需要 `.tools/mono` 與 `.tools/harmonyprobe/gamedeps`；設定方式見 `docs/TEST_EVIDENCE.md`）：

```powershell
.tools\mono\Mono\bin\mono.exe .tools\harmonyprobe\HarmonyProbe.exe <Managed 路徑> dist\AppraisalAssistant\AppraisalAssistant.dll dist\AugSpotter\AugSpotter.dll
.tools\mono\Mono\bin\mono.exe .tools\logicprobe\LogicProbe.exe <Managed 路徑> dist\AppraisalAssistant\AppraisalAssistant.dll dist\AugSpotter\AugSpotter.dll
```

期望：

- HarmonyProbe：`patched 7 method(s)`（AppraisalAssistant）、`patched 0 method(s)`（AugSpotter）、`RESULT: PASS`。
- LogicProbe：`aug=true/false/null`、真假／缺陷／複製品／假酒全部 PASS、`appraisal_value hand=100`、兩個 DLL 的 `[Scan]` 都是 `refused=False capabilities=[] findings=0`、`RESULT: PASS`。
- 這只能證明 patch 與純邏輯，不代表 UI 已在遊戲畫面驗證；UI 仍需第 4 節手動驗收。

## 2. 遊戲內載入驗收

遊戲日誌：`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen\Player.log`。

### B1 載入日誌

期望包含（大小寫一致）：

```text
[ModLoader] Loading: AppraisalAssistant.dll
[ModLoader] Loading: AugSpotter.dll
[ModLoader]   Harmony: N method(s) patched
[ModLoader]   Initialized: AppraisalAssistant v1.0.0
[ModLoader]   Initialized: AugSpotter v1.0.0
```

- `AppraisalAssistant` 的 `N = 7`（tooltip、auto-fill、force-estimate、三個 confront 標題、真假 tooltip）。
- `AugSpotter` 的 `N = 0`（用自建 MonoBehaviour 更新 overlay，不強制 patch）。
- 不得出現 `Init failed`、`LoadError`、`scan refused`、`MissingMethodException`、`NullReferenceException`（與本 MOD 相關者）。

### B2 內建掃描器日誌

每個 MOD 啟動時主動呼叫 `ModScanner.Scan` 掃描自己的 DLL，並輸出：

```text
[AppraisalAssistant] [Scan] refused=False capabilities=[] findings=0
[AugSpotter] [Scan] refused=False capabilities=[] findings=0
```

期望 `refused=False`、`capabilities=[]`（或只包含 manifest 已宣告者；本 MOD 應為空）、`findings=0`。

### B3 Self-test

在載入存檔並進入店內後（`StoreUIManager.Instance != null`），自動執行：

```text
[AppraisalAssistant] [SelfTest] PASS ...
[AppraisalAssistant] [SelfTest] SUMMARY pass=<n> fail=0
[AugSpotter] [SelfTest] PASS ...
[AugSpotter] [SelfTest] SUMMARY pass=<n> fail=0
```

- 任一 `FAIL` 即為未通過，必須修正後重跑。
- 若因尚未進入遊戲而跳過，日誌必須明寫 `SKIP` 與原因，不能假裝 PASS。

## 3. 功能驗收

### C1 價值類鑑定（APPRAISAL_ITEM）

遊戲代碼依據：

- `AppraisalHelper.GetCorrectBaseValue(GameItem)` 回傳正確基礎價值。
- `InspectionUIManager.UpdateAppraisalPanel()` 更新估價輸入框。
- `InspectionUIManager.OnAppraisalServiceEstimated()` 讀取輸入並呼叫 `AppraisalServiceHelper.OnEstimate`。

驗收：

1. 對 `APPRAISAL_ITEM` 顯示 tooltip 時，額外出現 `TRUE VALUE: <正整數>`，數值等於 `GetCorrectBaseValue`。
2. 打開放大鏡／鑑定面板時，輸入框自動填入上述正確值（玩家未手動編輯時）。
3. 玩家按「估算」時，即使輸入框被改成錯誤值，MOD 會以正確值提交，結果為 perfect（`GetEstimatePrecision == 1`）。
4. Self-test 以 `PreBuiltItemHelper.CreateHandRandom()` 驗證 `GetCorrectBaseValue > 0`，並記錄實際值。

### C2 真假類鑑定（genuine / counterfeit）

遊戲代碼依據：

- 香菸：`CATEGORY_GENUINE_CIGARETTE` 的 `realCondition.identifier`（`genuineCigarette` / `counterfeitCigarette`）。
- 郵票：`CATEGORY_GENUINE_STAMP`（`genuineStamp` / `counterfeitStamp`）。
- 注射器：`CATEGORY_GENUINE_INJECTOR`（`genuineInjector` / `expiredInjector` / `unusableInjector` / `counterfeitInjector`）。
- Ribwich：`RibwichHelper.IsCounterfeit(GameItem)`。
- 葡萄酒：`WINE_COUNTERFEIT_TAG`。

驗收：

1. 對上述物品顯示 tooltip 時，額外出現：
   - `GENUINE`（真品，綠色）；或
   - `COUNTERFEIT`（假貨，紅色）；或
   - `DEFECTIVE: EXPIRED` / `DEFECTIVE: UNUSABLE`（有缺陷，橙色）；或
   - `REPLICA`（複製品，橙色）。
2. 香菸／郵票／注射器的「對質」面板標題額外顯示 `[GENUINE]` 或 `[COUNTERFEIT]`，讓玩家在決定收購前知道真假。
3. 對假貨顯示 `DO NOT BUY` 提示；對真品顯示 `SAFE TO BUY`。
4. Self-test：
   - `PreBuiltItemHelper.CreateFakeStamp()` → `COUNTERFEIT`
   - `PreBuiltItemHelper.CreateRealStamp(160)` → `GENUINE`
   - `PreBuiltItemHelper.CreateRealCounterfeitInjector()` → `COUNTERFEIT`
   - `PreBuiltItemHelper.CreateRealGenuineInjector()` → `GENUINE`

### C3 改造體（Aug）辨識

遊戲代碼依據：

- `StoreClient.isAug`、`StoreClient.augTier`（public 欄位）。
- `PlayerStore.Instance.currentClientInstance.GetClientBlueprint()` 取得目前顧客。

驗收：

1. 顧客在櫃檯時，畫面左上角 overlay 顯示：
   - 改造體：紅色 `AUG DETECTED - Tier <n>`。
   - 普通顧客：綠色 `NOT AN AUG`（可設定關閉）。
   - 無顧客：overlay 隱藏。
2. 改造體在 `MakeDisguisedAug` 後、任何互動前就能被標示，不需要掃描儀或消耗電池。
3. Self-test：`isAug=true` → `AUG`；`isAug=false` → `NOT AUG`。
4. 不修改 `StoreClient` 狀態、不消耗物品、不影響存檔。

### C4 安全性

1. 不讀寫任何本機檔案（除遊戲內建 `ModConfig` 經由遊戲 API 儲存設定）。
2. 不發送網路請求。
3. 不執行外部程序。
4. 不載入額外組件、不使用反射修改遊戲。
5. 原始碼可由 `git log` 追溯，無混淆或打包惡意 payload。

## 4. 手動遊玩驗收（最終）

1. 進入存檔，接待估價顧客（手上／Ribwich／電漿燃料任一），確認 tooltip 真值與輸入框自動填入，按估算得到 perfect 評價。
2. 接待帶香菸／郵票／注射器的顧客，滑鼠移到物品上，確認真假標籤；打開對質面板確認標題標籤。
3. 遇到改造體顧客時，確認左上角紅色 `AUG DETECTED`；普通顧客顯示綠色 `NOT AN AUG`。
4. 重新啟動遊戲，確認兩個 MOD 仍在 Mods 清單中啟用且日誌無錯誤。
5. 卸載測試：移除 `Mods/AppraisalAssistant` 與 `Mods/AugSpotter` 後遊戲可正常啟動，日誌無殘留 patch 錯誤。
