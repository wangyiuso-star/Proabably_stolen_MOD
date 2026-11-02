# 遊戲代碼逆向筆記（Probably Stolen 1.0 / Assembly-CSharp）

本筆記只記錄與本 MOD 有關、且已由反編譯 `Probably Stolen_Data/Managed/Assembly-CSharp.dll`
核對過的實作。所有類別／方法名稱均來自實際組件，不是猜測。

## 1. 內建 Mod Loader

- `ModLoader`（`UnityEngine.MonoBehaviour`）在 `Awake()` 中：
  - 讀取 `Application.dataPath/../Mods` 下每個子資料夾。
  - 每個資料夾必須有 `manifest.xml`。
  - 以 `Assembly.LoadFrom` 載入 `<Assemblies>` 列出的 DLL。
  - 找出唯一 `IMod` 實作，`Activator.CreateInstance` 後：
    1. `new Harmony("mod." + manifest.ID).PatchAll(assembly)`
    2. `mod.Init(manifest)`
    3. `mod.OnEnable()`
- `ModManifest` 欄位：`ID`, `Name`, `Author`, `ModVersion`, `Description`,
  `GameVersions`, `Prerequisites`, `Assemblies`, `Capabilities`。
- `manifest.xml` 必要元素：`<ID>`、`<Assemblies><Assembly>...</Assembly></Assemblies>`、
  `<Capabilities>`（可空 `<Capabilities/>`）。
- `IMod` 介面：
  ```csharp
  void Init(ModManifest manifest);
  void OnEnable();
  void OnDisable();
  ```
- MOD 資料夾中的 DLL 若未列在 `<Assemblies>` 會被拒絕；不得與遊戲組件同名。
- 本 MOD 不宣告任何 capability，因為只呼叫遊戲 API 與 Harmony，不直接使用
  `System.IO` / `System.Net` / `System.Diagnostics` / `System.Reflection.Emit`。

## 2. 估價型鑑定（APPRAISAL_ITEM）

相關類別：

- `AppraisalHelper`
  - `InitAppraisalItem(GameItem, int)`：設定 `APPRAISAL_ITEM` 與預設值。
  - `GetCorrectBaseValue(GameItem)`：
    ```csharp
    AppraisalItemType type = AppraisalItemRegistry.Find(item);
    return type == null ? 0 : type.getCorrectValue(item);
    ```
  - `CreateAppraisalTooltip(RichTextBuilder, GameItem)`：只有 `PlayerStore.Instance.devMode`
    時才顯示 `TRUE VALUE`。
- `AppraisalItemRegistry`：內建 `hand` / `ribwich` / `plasma_fuel` 三種類型，
  各自有 `getCorrectValue`。
- `InspectionUIManager`
  - `UpdateAppraisalPanel()`：面板開啟且未在編輯時，把
    `currentInspectingItem.GetTagReadonly("APPRAISAL_DEFAULT_VALUE").GetInt()`
    寫入 `apprisalServiceInputField.text`。
  - `OnAppraisalServiceEstimated()`：解析輸入框，呼叫
    `AppraisalServiceHelper.OnEstimate(currentInspectingItem, result)`。
- `AppraisalServiceHelper`
  - `OnEstimate(GameItem, int)`：以 `GetCorrectBaseValue` 為基準，
    誤差在 95%–105% 內為 perfect（precision 1），否則依差距分級；
    超出 50%–150% 會拒絕。
  - `GetEstimatePrecision(int correct, int value)`：完美區間為
    `correct * 95%` ～ `correct * 105%`。

**本 MOD 的做法**：

1. Postfix `AppraisalHelper.CreateAppraisalTooltip`，對 `APPRAISAL_ITEM` 加一行
   `TRUE VALUE: <GetCorrectBaseValue>`（不再需要 devMode）。
2. Postfix `InspectionUIManager.UpdateAppraisalPanel`，在玩家未編輯輸入框時，
   把正確值寫入輸入框與 `APPRAISAL_DEFAULT_VALUE`。
3. Prefix `InspectionUIManager.OnAppraisalServiceEstimated`，提交前把輸入框覆寫為
   正確值，確保結果為 perfect。

## 3. 真假型鑑定（genuine / counterfeit）

相關類別：

- `InspectableHelper`
  - `InitInspectableItem(GameItem)`：加上 `INSPECTABLE_TAG`。
  - `CreateInspectableTooltip(RichTextBuilder, GameItem)`：顯示「可用放大鏡檢查」提示。
  - `OnConfrontSuccess(GameItem)`：標記 `ALREADY_CONFRONTED_TAG`、加折扣 feature。
- `InspectionUIManager`
  - `OnInspectItem(GameItem)`：依 tag 分流到香菸／郵票／注射器／Ribwich／手／漿料／莓果。
  - `CanOpenConfrontPanel`：只有未擁有、未對質過、交易未鎖定時可開。
  - `OnConfront()`：依物品真假走 `OnCigaretteConfront` / `OnCigaretteTalk` 等。
  - `OpenCigaretteConfrontPanel` / `OpenStampConfrontPanel` / `OpenInjectorConfrontPanel`：
    依 `CATEGORY_GENUINE_*` 的 `realCondition.identifier` 決定顯示五個問題選項或單一
    「Genuine」選項，並設定 `confrontTitleTMP`。
- 真假來源（`ItemFeature.realCondition.identifier`，`ItemFeature.realCondition` 是 public）：
  - `CATEGORY_GENUINE_CIGARETTE`：`genuineCigarette` / `counterfeitCigarette`
  - `CATEGORY_GENUINE_STAMP`：`genuineStamp` / `counterfeitStamp`
  - `CATEGORY_GENUINE_INJECTOR`：`genuineInjector` / `expiredInjector` /
    `unusableInjector` / `counterfeitInjector`
  - `CATEGORY_REPLICA`：`non_replica` / `replica`
- 其他：
  - `RibwichHelper.IsCounterfeit(GameItem)`：依生產日期、設施編號、logo 等判定。
  - `WineHelper`：`WINE_COUNTERFEIT_TAG` 表示假酒。
  - 郵票的具體問題 tag：`STAMP_SEAL_ISSUE`、`STAMP_LOGO_ISSUE`、
    `STAMP_CHIP_ISSUE`、`STAMP_LETTERING_ISSUE`、`STAMP_AMOUNT_ISSUE`。
  - 香菸：`CIGARETTE_*_ISSUE`；注射器：`ISSUE_INDICATOR` / `ISSUE_LABEL` /
    `ISSUE_SERIAL` / `ISSUE_BODY` / `ISSUE_COLOR`。

**本 MOD 的做法**：

1. Postfix `InspectableHelper.CreateInspectableTooltip`，對可檢查物品加
   `GENUINE` / `COUNTERFEIT` / `DEFECTIVE` / `REPLICA` 與 `SAFE TO BUY` /
   `DO NOT BUY`。
2. Postfix 三個 `Open*ConfrontPanel`，在 `confrontTitleTMP` 後附加
   `[GENUINE]` / `[COUNTERFEIT]` / `[DEFECTIVE]`。
3. 真假判定只讀 `realCondition`、`RibwichHelper.IsCounterfeit`、`WINE_COUNTERFEIT_TAG`；
   不改動物品或顧客狀態。

## 4. 改造體（Aug）系統

相關類別：

- `StoreClient`（public 欄位）：
  - `bool isAug`
  - `int augTier`
  - `bool isAugScanned`
  - `AugIdleTell augIdleTell`
- `StoreClientMono.PlayIdleForCurrentClient()`：以 `isAug` 決定凍結／抽搐 idle。
- `AugHelper.MakeDisguisedAug(StoreClient, int)`：設定 `isAug = true`、`augTier`、
  `augIdleTell`，並換成不相符的派系立繪。
- `AugScanner`：掃描器物品，讀 `clientBlueprint.isAugScanned` / `isAug`。
- `PlayerStore.Instance.currentClientInstance` 是 `StoreClientInstance`；
  `GetClientBlueprint()` 回傳目前 `StoreClient`。

**本 MOD 的做法**：

- 建立一個 `DontDestroyOnLoad` 的 Overlay（Canvas + TextMeshProUGUI）。
- 每幀讀 `PlayerStore.Instance.currentClientInstance.GetClientBlueprint()`：
  - `isAug == true` → 紅色 `AUG DETECTED — Tier <augTier>`。
  - `isAug == false` → 綠色 `NOT AN AUG`（可設定隱藏）。
  - 無顧客 → 隱藏。
- 不改 `StoreClient`、不消耗掃描器電池、不需完成掃描器教學。

## 5. 版本與安全

- `PlayerStore.VERSION = "VERSION 100-Rev6"`；`Application.version = "0.46D"`。
  manifest 同時列出兩者，避免版本警告。
- MOD 只使用公開 API 與 Harmony patch，不寫入遊戲存檔、不讀寫本機檔案、
  不連網、不啟動程序。
