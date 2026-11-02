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

命令（需要 `.tools/mono`；設定方式見 `docs/TEST_EVIDENCE.md`）：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Probe-Patches.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Probe-Logic.ps1
```

期望：

- HarmonyProbe：`patched 8 method(s)`（AppraisalAssistant）、`patched 4 method(s)`（AugSpotter）、`RESULT: PASS`。
- LogicProbe：`aug=true/false/null`、真假／缺陷／複製品／假酒全部 PASS、`appraisal_value hand=100`、兩個 DLL 的 `[Scan]` 都是 `refused=False capabilities=[] findings=0`、`RESULT: PASS`。
- 這只能證明 patch 與純邏輯，不代表 UI 已在遊戲畫面驗證；UI 仍需第 6 節手動驗收。

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

- `AppraisalAssistant` 的 `N = 8`（tooltip、force-estimate、OnInspectItem 重置兼 auto-fill、PlayerStore.Reset 清 session、三個 confront 標題、真假 tooltip）。
- `AugSpotter` 的 `N = 4`（到達、特殊顧客、離開、Reset 四個事件；不再輪詢顧客）。
- 不得出現 `Init failed`、`LoadError`、`scan refused`、`MissingMethodException`、`NullReferenceException`（與本 MOD 相關者）。

### B2 內建掃描器

MOD 不再在執行期主動掃描自己的 DLL（已移除 debug self-test／監聽程式碼）。
掃描結果由 `tools/Verify-Mods.ps1` 的靜態檢查與遊戲內建 Mods 面板的啟用掃描負責。

### B3 無多餘監聽／debug 程式碼

最終版本不安裝任何自測協程、不輪詢日誌、不建立背景監聽：
- `AppraisalAssistant` 只有 8 個 Harmony patch；沒有 `UpdateAppraisalPanel` 每幀 patch；`PlayerStore.Reset` 只清 session。
- `AugSpotter` 只有 4 個事件 patch；首次建立 overlay 後 `Update` 會自動停用，沒有顧客輪詢。
- `Player.log` 不應出現 `[SelfTest]` 或反覆的 `NullReferenceException`。

## 3. 功能驗收

### C1 價值類鑑定（APPRAISAL_ITEM）

遊戲代碼依據：

- `AppraisalHelper.GetCorrectBaseValue(GameItem)` 回傳正確基礎價值。
- `InspectionUIManager.UpdateAppraisalPanel()` 更新估價輸入框。
- `InspectionUIManager.OnAppraisalServiceEstimated()` 讀取輸入並呼叫 `AppraisalServiceHelper.OnEstimate`。

驗收：

1. 對 `APPRAISAL_ITEM` 顯示 tooltip 時，額外出現 `真值：<正整數>` 與 `精確區間：...`，數值等於 `GetCorrectBaseValue`。
2. 打開放大鏡／鑑定面板時，輸入框自動填入上述正確值（玩家未手動編輯時）。
3. 玩家按「估算」時，即使輸入框被改成錯誤值，MOD 會以正確值提交，結果為 perfect（`GetEstimatePrecision == 1`）。
4. 同一顧客的多件鑑定品，每一件都套用自己的正確值（`OnInspectItem` 會重置共用輸入框）。
5. 離線邏輯測試以 `PreBuiltItemHelper` 建立 `HAND_TAG` 物品，`appraisal_value hand=100`。

### C1.1 鑑定 auto-fill 改為事件驅動

`InspectionUIManager.Update()` 每幀呼叫 `UpdateAppraisalPanel()` 是遊戲自己的 UI 刷新；
MOD 不應該在這個每幀函式上掛 Postfix。真正的鑑定事件是
`InspectionUIManager.OnInspectItem(GameItem)`，MOD 已在該 Postfix 填入正確值與 tag。

驗收：

1. 移除 `AppraisalAutoFillPatch`，不再 patch `InspectionUIManager.UpdateAppraisalPanel`。
2. `OnInspectItem` Postfix 仍會：清除 `isEditingAppraisalField`、填入輸入框、設定
   `APPRAISAL_DEFAULT_VALUE`；`auto_fill=false` 時跳過填入。
3. `AppraisalForceEstimatePatch` 仍在提交時校正價格。
4. `AppraisalAssistant` 移除 auto-fill 後為 7，新增 `PlayerStore.Reset` 清 session 後為 8；`tools/Verify-Mods.ps1` PASS。
5. 原始碼中不得再出現 `UpdateAppraisalPanel` 的 Harmony patch 類別。

### C1.2 只在放大鏡檢查時掃描一次

`ItemTooltipHandler.OnEventUpdate()` 在滑鼠指著物品時每幀重建 tooltip；`AppraisalAssistant`
的兩個 tooltip Postfix 仍會被呼叫，但**普通物品必須立刻 return**，不能做任何鑑定計算。

真正的掃描點是 `InspectionUIManager.OnInspectItem`：

- 每次放大鏡檢查只呼叫一次 `Truth.TryGet()` 與 `Log.TryGetCorrectValue()`。
- 結果存進 `AppraisalSession`（`Item`、`HasValue`、`Value`、`HasTruth`、`Truth`）。
- tooltip 只在 `AppraisalSession.IsCurrent(item)` 為真時加行；`IsCurrent` 同時要求
  `InspectionUIManager.currentInspectingItem == item`，所以關閉檢查後普通 hover／拖動不再顯示。
- tooltip 加行時只格式化 session，不再掃 `AppraisalItemRegistry` 或 `ItemFeature`。

驗收：

1. `AppraisalTooltipPatch`／`AuthenticityTooltipPatch` 內不得再呼叫 `Truth.TryGet()` 或
   `Log.TryGetCorrectValue()`。
2. 普通物品（未經放大鏡檢查）hover／拖動時，兩個 Postfix 都必須在第一個 reference check 返回。
3. 同一物品在一次檢查中只掃一次；tooltip 每幀重建只讀 `AppraisalSession`。
4. `Probe-Logic.ps1` 全部 PASS；遊戲內拖動普通物品不應再因鑑定助手掉幀。

### C1.3 用 SetTextWithoutNotify 避免多一次背包重建

`TMP_InputField.text` 的 setter 會觸發 `InspectionUIManager.OnValueChanged`，後者呼叫
`GameItem.ModifyTag(..., skipValidate:false)` → `ValidateShapeState()` → 背包 UI 重建。
MOD 的自動填入與提交前校正必須使用 `SetTextWithoutNotify()`，只更新顯示，不觸發事件。

驗收：

1. `AppraisalInspectResetPatch` 與 `AppraisalForceEstimatePatch` 不得使用
   `apprisalServiceInputField.text = ...`。
2. 兩處都使用 `SetTextWithoutNotify(text)`。
3. `AppraisalTag.SetDefaultValue()` 仍要在後面同步 tag；否則下一幀
   `UpdateAppraisalPanel()` 會把輸入框改回舊值。
4. `tools/Verify-Mods.ps1` PASS；每次鑑定只應有 tag 真的變更時的一次
   `ValidateShapeState`，不再多一次 OnValueChanged。

### C2 真假類鑑定（genuine / counterfeit）

遊戲代碼依據：

- 香菸：`CATEGORY_GENUINE_CIGARETTE` 的 `realCondition.identifier`（`genuineCigarette` / `counterfeitCigarette`）。
- 郵票：`CATEGORY_GENUINE_STAMP`（`genuineStamp` / `counterfeitStamp`）。
- 注射器：`CATEGORY_GENUINE_INJECTOR`（`genuineInjector` / `expiredInjector` / `unusableInjector` / `counterfeitInjector`）。
- Ribwich：`RibwichHelper.IsCounterfeit(GameItem)`。
- 葡萄酒：`WINE_COUNTERFEIT_TAG`。

驗收：

1. 對上述物品顯示 tooltip 時，額外出現（簡體中文、對齊遊戲用詞）：
   - `真伪判定：真品`（綠）；或
   - `真伪判定：假货`（紅）＋ `原因：...`；或
   - `真伪判定：有缺陷`（橙）＋ `原因：已过期／无法使用...`；或
   - `真伪判定：复制品`（橙）＋ `原因：复制品，非原版`。
2. 原因必須是具體的可見問題，例如 `防伪标问题`、`商标问题`、`字母印刷问题`、`指示灯问题`、`标签问题`；不能只寫「假的」。
3. 對質面板標題只顯示 `[真品]` / `[假货]` / `[有缺陷]` / `[复制品]`，**不改字級、不加原因**；原因在 hover tooltip。
4. 不顯示任何收購建議（沒有 `建议：可收购` / `建议：不要收购`）。
5. 離線邏輯測試（`tools/Probe-Logic.ps1`）覆蓋假郵票、真郵票、假注射器、過期、報廢、複製品、假酒，全部 PASS。

### C3 改造體（Aug）辨識

遊戲代碼依據：

- `StoreClient.isAug`、`StoreClient.augTier`（public 欄位）。
- `PlayerStore.Instance.currentClientInstance.GetClientBlueprint()` 取得目前顧客。

驗收：

1. 只有櫃檯前的顧客是改造體時，左上角 overlay 才顯示紅色中文 `侦测到改造体  等级 <n>`。
2. 普通顧客：overlay 完全不顯示。
3. 改造體在 `MakeDisguisedAug` 後、任何互動前就能被標示，不需要掃描儀或消耗電池。
4. 不修改 `StoreClient` 狀態、不消耗物品、不影響存檔。

### C3.1 改造體更新改為事件驅動

原本 `AugOverlay.Update()` 每 0.5 秒讀一次 `PlayerStore.currentClientInstance`。改成只在
顧客事件發生時更新，避免無意義輪詢：

- 正常顧客到櫃檯：`StoreUIManager.OnNextClientArrived` Postfix。
- 早晨／特殊顧客直接生成：`PlayerStore.SetupWaitingClient` Postfix。
- 顧客離開：`PlayerStore.DismissCurrentClient` Postfix。
- 讀檔／新遊戲重置：`PlayerStore.Reset` Postfix。
- 首次建立 overlay 時讀一次目前顧客，覆蓋「載入存檔時顧客已在櫃檯」。
- 更新成功後 `AugOverlay.enabled = false`，不再有每幀 Update。

驗收：

1. `AugSpotter` 的 Harmony patch 數為 4（上述四個事件），不是 0。
2. 原始碼中不得再出現 `_playerProbeAt`、`0.5f` 顧客輪詢或 `Time.realtimeSinceStartup`。
3. `tools/Probe-Patches.ps1` 顯示 `patched 4 method(s)`，且 `Verify-Mods.ps1` PASS。
4. 遊戲內：普通顧客不顯示；改造體到櫃檯後顯示；顧客離開後立即消失；載入存檔時
   若顧客已在櫃檯也能顯示。

### C3.2 Overlay 必須被真正拆除

`AugSpotterOverlay` 是另一個 `DontDestroyOnLoad` root，不在 `RuntimeHost._root` 底下。
只 destroy `RuntimeHost._root` 不會清掉 Canvas；因此 `AugOverlay` 必須自己記住
`_overlayRoot`，並在 `OnDestroy`／`RuntimeHost.Destroy`／`PlayerStore.Reset`／
`OnDisable` 時呼叫 `TearDownCanvas()`。

驗收：

1. `RuntimeHost.Destroy()` 先呼叫 `AugOverlay.TearDownCanvas()`，再 destroy holder。
2. `AugOverlay.OnDestroy()` 只在 `_instance == this` 時清自己的 canvas，避免同一幀
   重建新 overlay 時把新 canvas 誤刪。
3. 櫃檯有改造體 → Esc 回主菜單 → 載入同存檔，紅字立即消失，不是留在畫面上。
4. 重複 5 次「改造體 → Esc → 載入」，場景內 `AugSpotterOverlay` Canvas 數量維持 1。
5. 停用 MOD 後紅字立即消失。

### C4 安全性

1. 不讀寫任何本機檔案（除遊戲內建 `ModConfig` 經由遊戲 API 儲存設定）。
2. 不發送網路請求。
3. 不執行外部程序。
4. 不載入額外組件、不使用反射修改遊戲。
5. 原始碼可由 `git log` 追溯，無混淆或打包惡意 payload。

## 4. 效能回歸驗收（切換顧客卡頓）

### D1 每幀寫入防護

`InspectionUIManager.Update()` 在估價面板開啟時每幀呼叫 `UpdateAppraisalPanel()`；MOD 的
Postfix 不得在 `APPRAISAL_DEFAULT_VALUE` 已等於正確值時再呼叫 `GameItem.ModifyTag()`。
`ModifyTag()` 會觸發 `ValidateShapeState()`，後者會重建 modified shape 並呼叫
`OverlayHandler.UpdateInventoryStateUI()`；每幀重複是本次卡頓的主因。

`APPRAISAL_DEFAULT_VALUE` 是純數值 tag，不影響 shape／feature／inventory slot；因此
`AppraisalTag.SetDefaultValue()` 使用 `ModifyTag(..., skipValidate: true)`，再手動同步
`modifiedState` 的同一個 tag，避免鑑定事件本身也觸發一次背包 UI 重建。

命令：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Probe-Logic.ps1
```

期望新增輸出：

```text
PASS appraisal_write_guard initial=true same=false changed=true
PASS appraisal_write_value=100
```

- `initial=true`：tag 未設定時允許寫入。
- `same=false`：tag 已是同一個值時不得再寫。
- `changed=true`：值不同時允許寫入。
- `appraisal_write_value=100`：跳過 shape validation 後，`modifiedState` 仍同步為 100。

### D2 靜態 patch 數量不變

`tools/Probe-Patches.ps1` 應為 `patched 8 method(s)`（AppraisalAssistant）與
`patched 4 method(s)`（AugSpotter），`RESULT: PASS`；不得為了修效能而移除功能 patch。

### D3 遊戲內主觀驗收（需玩家操作）

同一存檔、同一顧客隊列，連續切換至少 10 位顧客：

1. 切換顧客不應再出現持續掉幀；單次切換允許既有的一次性 UI 重建。
2. 左上角 Aug overlay 仍只在改造體出現時顯示。
3. 估價面板仍自動填入正確值；按估算仍以正確值提交。
4. `Player.log` 不得出現新的 `NullReferenceException`、`MissingMethodException` 或
   `[AppraisalAssistant]` 警告洗版。

## 5. 手動遊玩驗收（最終）

1. 進入存檔，接待估價顧客（手上／Ribwich／電漿燃料任一），確認 tooltip 真值與輸入框自動填入，按估算得到 perfect 評價。
2. 接待帶香菸／郵票／注射器的顧客，滑鼠移到物品上，確認真假標籤；打開對質面板確認標題標籤。
3. 遇到改造體顧客時，確認左上角紅色 `侦测到改造体`；普通顧客顯示綠色 `不顯示`。
4. 重新啟動遊戲，確認兩個 MOD 仍在 Mods 清單中啟用且日誌無錯誤。
5. 卸載測試：移除 `Mods/AppraisalAssistant` 與 `Mods/AugSpotter` 後遊戲可正常啟動，日誌無殘留 patch 錯誤。


