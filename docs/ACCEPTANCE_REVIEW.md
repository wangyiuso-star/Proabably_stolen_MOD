# MOD 驗收報告（第三方覆核）

- 覆核日期：2026-11-02
- 交付物：`dist/AppraisalAssistant/`、`dist/AugSpotter/`、`dist/ProbablyStolen-QoL-Mods-v1.0.0.zip`
- 遊戲本體：`C:\Program Files (x86)\Steam\steamapps\common\Probably Stolen`
  （`Assembly-CSharp.dll` SHA256 `6398D44AA1675E1F02B63BC668868F970E0F5D3AE5A50F2ACED8D06CA084E3B5`，未經修改；
  `.tools/verify/Assembly-CSharp.dll` 與其 hash 相同，可用來重跑核對）
- 覆核方式：重跑建置／驗證／探針；以 Cecil 直接讀遊戲 IL 逐項核對 patch 目標與欄位；對照 `Player.log`、
  Windows 事件記錄、安裝目錄時間戳。**沒有**啟動遊戲本體做真人操作測試。

## 0. 結論

| 項目 | 結果 |
| --- | --- |
| 載入／API 兼容（12 個 patch 目標、欄位、參照組件） | **通過**（全部在真實 `Assembly-CSharp.dll` 上核對） |
| 安全／合法性（capability、禁用 API、內建 `ModScanner`） | **通過** |
| 建置可重現性 | **通過**（重建後 hash 與 zip／已安裝檔完全一致） |
| 功能觸發點是否會在正常遊玩中發生 | **通過**（呼叫點以 IL 逐一核對） |
| 效能／卡頓 | **靜態通過，無實測**（每幀新增成本可忽略；但每次鑑定仍有 1 次背包 UI 重建） |
| **穩定性（與本體兼容最重要的一項）** | **不通過** — 見 §4.1：overlay 洩漏，修法無效；且最終版從未在遊戲內跑過 |
| 文件與實際一致性 | **不通過（輕微）** — §4.5 |

**判定：有條件不通過（暫不放行）。** 功能與 API 兼容性沒有問題，但 AugSpotter 的
`DontDestroyOnLoad` overlay 清理是錯的（destroy 錯對象），會在「返回主選單 → 載入」時累積殘留
Canvas／TMP 物件並顯示過期標記；而唯一能證明它不致命的方法——用目前這個 build 進遊戲跑一次並看
Windows 事件記錄——還沒做，上一版正是在這個流程連崩兩次（`ntdll.dll 0xc0000005`）。

---

## 1. 我重跑的驗收命令與實際輸出

### 1.1 建置（可重現）

```text
> powershell -NoProfile -ExecutionPolicy Bypass -File tools\Build-Mods.ps1
Compiling AppraisalAssistant...
  -> dist\AppraisalAssistant\AppraisalAssistant.dll (15872 bytes)
Compiling AugSpotter...
  -> dist\AugSpotter\AugSpotter.dll (9216 bytes)
Build OK
EXIT=0
```

重建後 hash 與交付 zip、與遊戲內已安裝檔**完全相同**（`/deterministic+` 生效）：

| 檔案 | 大小 | SHA256 |
| --- | --- | --- |
| `AppraisalAssistant.dll` | 15872 | `63A9F4C9F4969C681F5F0C24E2036125F1F8A57616A3427C36FDCA2A673E9844` |
| `AugSpotter.dll` | 9216 | `A3207236405D3549B616F713060497AA2113ADE4EF26C02E24BEDD45A6EDECE2` |

`dist/ProbablyStolen-QoL-Mods-v1.0.0.zip` 內四個檔案的 hash 也與上表一致，zip 內沒有額外檔案
（沒有 DLL 同名的遊戲組件、沒有 `0Harmony`／Cecil 捆綁）。

### 1.2 靜態驗證

```text
> powershell -NoProfile -ExecutionPolicy Bypass -File tools\Verify-Mods.ps1
PASS AppraisalAssistant: ID deepspace.appraisal_assistant
PASS AppraisalAssistant: capabilities empty
PASS AppraisalAssistant.dll: IMod implemented by DeepSpaceQoL.AppraisalAssistantMod
PASS AppraisalAssistant.dll: no banned IL APIs
PASS ... (8 個 HarmonyPatch 全部 target 存在)
PASS AppraisalAssistant.dll: HarmonyPatch attributes checked (8)
PASS AugSpotter: ID deepspace.aug_spotter
PASS AugSpotter: capabilities empty
PASS AugSpotter.dll: IMod implemented by DeepSpaceQoL.AugSpotterMod
PASS AugSpotter.dll: no banned IL APIs
PASS ... (4 個 HarmonyPatch 全部 target 存在)
PASS AugSpotter.dll: HarmonyPatch attributes checked (4)
RESULT: PASS
EXIT=0
```

### 1.3 Harmony 套用探針（Mono 6.12 + 遊戲 `0Harmony.dll` 2.4.2.0）

```text
patched 8 method(s)   AppraisalAssistant
  AppraisalHelper.CreateAppraisalTooltip
  InspectableHelper.CreateInspectableTooltip
  InspectionUIManager.OnAppraisalServiceEstimated
  InspectionUIManager.OnInspectItem
  InspectionUIManager.OpenCigaretteConfrontPanel
  InspectionUIManager.OpenInjectorConfrontPanel
  InspectionUIManager.OpenStampConfrontPanel
  PlayerStore.Reset
patched 4 method(s)   AugSpotter
  PlayerStore.DismissCurrentClient
  PlayerStore.Reset
  PlayerStore.SetupWaitingClient
  StoreUIManager.OnNextClientArrived
RESULT: PASS
```

（`cant resolve internal call ...` 是離線 Mono 沒有 Unity native ECall 的已知噪音，不影響 patch 套用。）

### 1.4 離線邏輯探針

```text
PASS aug=true / aug=false / aug=null
PASS fake_stamp kind=Counterfeit label=假货 reason=商标问题
PASS real_stamp kind=Genuine ...
PASS fake_injector / expired_injector / unusable_injector / replica / fake_wine
PASS unknown_item returns false
PASS appraisal_value hand=100
PASS appraisal_write_guard initial=true same=false changed=true
PASS appraisal_write_value=100
[Scan] AppraisalAssistant.dll refused=False capabilities=[] findings=0
[Scan] AugSpotter.dll        refused=False capabilities=[] findings=0
RESULT: PASS
```

`[Scan]` 是遊戲內建 `ModScanner.Scan` 的實跑結果（不是自製檢查器），兩個 DLL 都沒有被拒絕。

### 1.5 交付物／安裝狀態

- `git status --short` 無輸出；`dist/`、`.tools/`、`.scratch/`、`backup/` 都在 `.gitignore`。
- 遊戲 `Mods\` 內兩個資料夾的 DLL hash 與 zip／重建結果一致；`manifest.xml` 為合法 UTF-8，
  `<Capabilities/>` 為空，`<Assemblies>` 指向實際 DLL。

---

## 2. 與遊戲本體的 API 兼容性（逐項 IL 核對）

方法：Cecil 讀真實 `Assembly-CSharp.dll`（hash 與遊戲一致），列出成員／反編譯 IL。
**所有 12 個 patch 目標都存在、可見性足夠、參數名與型別可匹配**；`Harmony.PatchAll` 實跑亦成功。

| Patch | 目標 | 核對結果 |
| --- | --- | --- |
| `AppraisalTooltipPatch` | `AppraisalHelper.CreateAppraisalTooltip(RichTextBuilder, GameItem)` | public static，存在；**正常遊玩會呼叫**（見 §3.1） |
| `AppraisalInspectResetPatch` | `InspectionUIManager.OnInspectItem(GameItem gameItem)` | public instance，參數名 `gameItem` 相符 |
| `AppraisalForceEstimatePatch` | `InspectionUIManager.OnAppraisalServiceEstimated()` | public instance，無參數 |
| `AppraisalStoreResetPatch` | `PlayerStore.Reset()` | public instance；由 `StartNewGame`、`EscapeUIManager.MainMenuWithAnalytics`、`PlayerStore..ctor` 呼叫 |
| 三個 `*TitlePatch` | `InspectionUIManager.Open{Cigarette,Stamp,Injector}ConfrontPanel(GameItem)` | public instance，存在 |
| `AuthenticityTooltipPatch` | `InspectableHelper.CreateInspectableTooltip(RichTextBuilder, GameItem)` | public static，存在 |
| `ClientArrivedPatch` | `StoreUIManager.OnNextClientArrived()` | public instance，無參數；由 `StoreClientMono.OnArrived` 呼叫 |
| `WaitingClientPatch` | `PlayerStore.SetupWaitingClient(string id)` | public instance |
| `ClientDismissedPatch` | `PlayerStore.DismissCurrentClient(bool, bool, bool)` | public instance（Postfix 不取參數，合法） |
| `StoreResetPatch` | `PlayerStore.Reset()` | 同上；與 AppraisalAssistant 同方法兩個 Postfix，Harmony 允許 |

用到的欄位／成員（全部 public，型別相符）：

- `InspectionUIManager`: `Instance`（靜態屬性）、`currentInspectingItem`、`apprisalServiceInputField`
  (`TMP_InputField`)、`isEditingAppraisalField` (`bool`)、`confrontTitleTMP` (`TextMeshProUGUI`)
- `StoreUIManager`: `Instance`、`versionTMP` (`TextMeshProUGUI`)、`storeCalendar` (`StoreCalendar`)；
  `StoreCalendar.dayTMP` (`TextMeshProUGUI`) 為 public 欄位
- `PlayerStore`: `Instance`、`currentClientInstance` (`StoreClientInstance`)；
  `StoreClientInstance.GetClientBlueprint()` 是**純 getter**（`ldfld storeClient; ret`，無副作用）
- `StoreClient`: `isAug` (`bool`)、`augTier` (`int`)，public 欄位，`StoreClient` 是 class
- `GameItem`: `IsTag`、`GetTagReadonly`、`ModifyTag(string, Action<TagState>, bool skipValidate)`、
  `modifiedState`（`TagSystem`，public 屬性）
- `RichTextBuilder.AddLine` 10 參數版（3 個必填 + 7 個編譯期預設值，mod 端 IL 已正確補齊預設）
- `RenderHandler.ColorPalette` 的 `LightGreen` / `Red` / `Orange`

參照組件只有 `mscorlib`、`Assembly-CSharp`、`0Harmony 2.4.2.0`、`UnityEngine.*`、`Unity.TextMeshPro`，
沒有捆綁 Harmony／Cecil；`<Capabilities/>` 為空且 IL 內無 `System.IO`／`System.Net`／Process／
Reflection.Emit／P/Invoke（`Verify-Mods.ps1` 與 `ModScanner` 雙重確認）。

**安裝信任紀錄**：`tools/Enable-Mods.ps1` 自己算 `combinedHash`。我核對遊戲
`ModFingerprint.Compute` 的 IL：`Combined = SHA256(UTF8(manifestHash + "\n" + "<assembly>|<fileHash>"))`，
與腳本一致，所以寫入的 `modTrust` 記錄格式是對的（前一版在 `Player.log` 也是 `trust=Match`）。
但這等於**代玩家預先核准**，繞過遊戲內 Mods 面板的同意流程，屬於要在文件裡講清楚的行為。

---

## 3. 功能觸發點是否真的會在正常遊玩中發生

這一節回答「功能會不會其實永遠不觸發」；全部以 IL 呼叫點為證據。

### 3.1 估價 tooltip 一定會被呼叫

`GameItemElement.GetTooltipBasic()` 內：

```text
IL_03e8  call PlayerStore::get_Instance()
IL_03ed  ldfld PlayerStore::devMode
IL_03f2  brfalse.s IL_03fb              // devMode 只擋下面這一個呼叫
IL_03f6  call ModInternal::CreateDevTooltipEarly(...)
IL_03fb  ldloc.1 / ldarg.0
IL_03fd  call AppraisalHelper::CreateAppraisalTooltip(RichTextBuilder, GameItem)   // 無條件
```

`devMode` 只 gate 另一個 `ModInternal.CreateDevTooltipEarly`，**沒有** gate `CreateAppraisalTooltip`。
且 `ItemTooltipHandler.OnEventUpdate()`（每幀）→ `TooltipListener.GetTooltip*()` →
`GameItemElement.GetTooltip()` → `GetTooltipBasic()`，所以 hover 時每幀都會走進來；MOD 的 Postfix
在 session 不匹配時第一行就 return。

### 3.2 自動填入／強制正確值

- `InspectionUIManager.OnInspectItem(GameItem)`: `ResetUI()` → `currentInspectingItem = gameItem` →
  `OpenUI()` → 依 tag 分流 → APPRAISAL_ITEM 時 `appraisalPanel.SetActive(true)`。
  MOD 的 Postfix 在其後執行，時間點正確。
- `Update()` 每幀（僅當 `appraisalPanel.activeSelf`）呼叫 `UpdateAppraisalPanel()`；
  後者在 `!isEditingAppraisalField` 時把 `APPRAISAL_DEFAULT_VALUE` 寫回輸入框，
  **沒有** `ModifyTag`。所以 MOD 只要在 `OnInspectItem` 時寫一次 tag＋輸入框即可，
  遊戲自己會維持顯示 → C1.1「移除每幀 patch」的改法正確。
- `OnAppraisalServiceEstimated()` 第一件事就是讀 `apprisalServiceInputField.text` + `TryParse`，
  之後才用參數呼叫 `AppraisalServiceHelper.OnEstimate(item, value)`。
  因此 Prefix 覆寫 `.text` **保證生效**（不存在快取 int 的風險）。
- `CloseUI()` 會把 `currentInspectingItem = null`，所以 `AppraisalSession.IsCurrent` 的
  「關閉檢查後不再顯示」也成立，不會洩漏真值。

副作用（不算 bug，但要知道）：`TMP_InputField.set_text` 會觸發 `onValueChanged`；
`InspectionUIManager.OnValueChanged(string)` 會 `ModifyTag("APPRAISAL_DEFAULT_VALUE", ..., false)`
→ `ValidateShapeState()`。所以每次「自動填入」仍會做 **一次** 背包 UI 重建
（不是每幀，可接受；§4.3 另述）。

### 3.3 真假標籤與對質標題

- `OpenCigaretteConfrontPanel` / `OpenStampConfrontPanel` / `OpenInjectorConfrontPanel`
  **本身不寫** `confrontTitleTMP`；它們在方法最後呼叫 `OnGenuineClaim()` / `OnCounterfeitClaim()`，
  由那兩個方法 `set_text` 設定標題。MOD 的 Postfix 在 `ret` 之後才 append，所以：
  - 一定看得到（不會被覆寫）；
  - 每次重開面板時 claim 方法會重設標題，所以**不會疊加** `[假货][假货]`。
  - 注射器的假貨分支會 `confrontPanel.SetActive(false)` 後才 ret；此時 Postfix 仍會 append，
    只是面板已隱藏（無害）。
- `confrontTitleTMP` 在整個組件只有兩處讀取（`OnGenuineClaim` / `OnCounterfeitClaim`），
  沒有第三處會覆寫。

### 3.4 改造體標記的事件覆蓋

- `isAug = true` 的寫入點只有 4 個：`AugHelper.MakeDisguisedAug`（2 處）、
  `StoreClientList.CreateTrailerAug`、`CreateAugMed`（2 處）、`CreateAugMaterial`（2 處）——
  **全部發生在顧客建立時**，不是互動後。
- 顧客建立流程：`StoreShutterButton.OpenShutter` → `PlayerStore.OnShutterOpened` →
  `StoreClientManager.GenerateClient` → `HandleAugClient` → `AddAugClient` → `MakeDisguisedAug`；
  走路進場的 `StoreUIManager.OnNextClientArrived` 由比較晚的 `StoreClientMono.OnArrived` 觸發。
  也就是 **isAug 在進場事件之前就已設定**，事件驅動版不會漏。
- `SetupWaitingClient(string id)` 自己 `CreateClientInstance` 並寫 `currentClientInstance`
  （mob/sec/… 特殊顧客），MOD 的 Postfix 讀得到。
- `DismissCurrentClient` 在方法內就把 `currentClientInstance = null`，Postfix 讀到 null → 立刻隱藏，
  C3「離開後立即消失」成立。
- `PlayerStore.Reset` 會把 `currentClientInstance = null`；它由 `StartNewGame` 與
  `EscapeUIManager.MainMenuWithAnalytics`（返回主選單）呼叫 → MOD 的 Reset patch 會在這兩個時機跑。

---

## 4. 缺陷與風險

### 4.1 【阻斷】AugSpotter 的 overlay 從來沒有被銷毀（修法無效）

`src/AugSpotter/AugSpotter.cs`：

```csharp
// L234-235：overlay canvas 是「另一個」root GameObject，不是 RuntimeHost._root 的子物件
GameObject root = new GameObject("AugSpotterOverlay");
UnityEngine.Object.DontDestroyOnLoad(root);
...
// L194-197
public static void ResetForSceneChange() { RuntimeHost.Destroy(); }
// L120-127
public static void Destroy() {
    if (_root != null) { UnityEngine.Object.Destroy(_root); _root = null; }
}
```

`RuntimeHost._root` 是 `OnEnable` 建立的空物件 `"AugSpotterRuntime"`，`AugOverlay` 元件掛在它上面；
`EnsureOverlay()` 另外建立 `"AugSpotterOverlay"`（Canvas + CanvasScaler + 複製的 TMP + Image），
**沒有設 parent**，也自己 `DontDestroyOnLoad`。因此：

1. `PlayerStore.Reset`（返回主選單／新遊戲）只 destroy 掉 `"AugSpotterRuntime"`，
   `"AugSpotterOverlay"` 繼續活著 → 畫面左上角保留上一次顧客的
   `侦测到改造体  等级 N`，在新存檔／主選單照樣顯示；
2. 下一個顧客事件 `Refresh()` 看到 `_instance == null` → `RuntimeHost.Create` → 再建一個新
   overlay；舊的仍然在 → **每切換一次場景多一層 Canvas**（sortingOrder 32000，複製的 TMP
   材質／貼圖一併留著）；
3. `OnDisable()`（Mods 面板停用、`OnApplicationQuit`）同樣只 destroy holder →
   **停用 MOD 後 overlay 仍留在畫面上**，只能重開遊戲。

這條同時解釋了為什麼「返回主選單清理」那個 commit（`8fe90b9`）的修法在邏輯上不可能生效，
也是兩次 native crash 最可疑的來源（見 4.2）。

建議修法（任一即可，建議兩者都做）：

```csharp
// A. 把 overlay canvas 掛到 holder 底下，Destroy(_root) 就會連子物件一起消失
root.transform.SetParent(RuntimeHost.RootTransform, false);   // 需要把 _root 曝露成內部屬性

// B. 或自己記住並銷毀
private static GameObject _overlayRoot;
// EnsureOverlay(): _overlayRoot = root;
public static void ResetForSceneChange()
{
    if (_overlayRoot != null) { UnityEngine.Object.Destroy(_overlayRoot); _overlayRoot = null; }
    RuntimeHost.Destroy();
}
```

修完後驗收：主選單 ↔ 載入存檔來回 ≥ 5 次，`Hierarchy`（或 `Resources.FindObjectsOfTypeAll<Canvas>()`）
只會有一層 `AugSpotterOverlay`；停用 MOD 後標記立即消失。

### 4.2 【阻斷】最終版沒有在遊戲內跑過；上一版在同一個流程崩了兩次

證據：

| 事件 | 時間 |
| --- | --- |
| `Player.log` 最後一次寫入（`Harmony: 7 method(s) patched`） | 21:19:43 |
| Windows `Application Error`：`Probably Stolen.exe` / `ntdll.dll` / `0xc0000005` | 21:19:46 |
| Windows `Application Error`：同上 | 20:53:13 |
| 目前安裝的 DLL 建立時間（8 patches） | 21:28:04 |

- 最後一次遊戲內執行的 `AppraisalAssistant` 是 **7 patch** 的版本（log 明文），而現在交付／安裝的是
  **8 patch**（多了 `PlayerStore.Reset` 清 session，`8fe90b9`）→ 新增的那條 patch 從未被遊戲執行過。
- 30 天 Windows 事件記錄裡，`Probably Stolen.exe` 只有這 **2 次** 崩潰，都在 MOD 測試當晚、
  都在「返回主選單／載入」附近；MOD 之前的存檔期（10/26–11/1 多個 `GameLog`）沒有崩潰記錄。
  這不能證明是 MOD 造成（也可能是遊戲本身或 Steam），但也**不能排除**，且 4.1 的洩漏正好是
  「跨場景保留 Canvas／TMP 材質」的高風險模式。
- `docs/TEST_EVIDENCE.md` §9.7 自己也寫「尚未核實」。

放行前必須：用 21:28 之後的新 build 進遊戲，重複「返回主選單 → 載入存檔」至少 10 次，
確認 (a) 沒有新的 `Application Error`，(b) 沒有殘留／重複 overlay，(c) `Player.log` 出現
`Harmony: 8 method(s) patched` + `4 method(s) patched` 且無 `[AppraisalAssistant]` 警告洗版。

### 4.3 【中】每次鑑定仍有 1 次背包 UI 重建（設計取捨，不是卡頓）

MOD 用 `ModifyTag(..., skipValidate: true)` 想避開 `ValidateShapeState()`，但因為同時寫了
`apprisalServiceInputField.text`，若該輸入框的 `onValueChanged` 有接上（方法名就叫
`OnValueChanged`，很可能有；scene 接線 IL 看不到，見 §6.3），TMP 會觸發 `OnValueChanged` →
`ModifyTag(..., skipValidate:false)` → `ValidateShapeState()` → `OverlayHandler.UpdateInventoryStateUI()`。
結果：每次「打開放大鏡」仍重建一次背包 UI（不是每幀）。對單次操作可接受，但代表
§ACCEPTANCE D1 的「完全跳過 shape validation」在實機上只對「沒有觸發 onValueChanged 的路徑」成立。
若真要完全避免，可在寫 `.text` 前先 `onValueChanged.RemoveAllListeners()` 再 `AddListener` 還原，
或用 `SetTextWithoutNotify`（若 TMP 版本有）；但這會改變遊戲行為，需再測。

### 4.4 【中】`AppraisalSession` 持有 `GameItem` 直到 Reset

`AppraisalSession.Item` 是靜態欄位，只在 `PlayerStore.Reset`（新遊戲／返回主選單）清除。
在同一個存檔內，最後一次用放大鏡看過的東西會被一直強引用（含其 feature／shape 圖）。
`ConditionalWeakTable` 的 `Truth`／`Log` 快取本身是弱引用，沒問題；這一個靜態引用是唯一
「不必要的記憶體保留」。影響很小，但可以在 `CloseUI`（`currentInspectingItem` 清空）時一併清掉。

### 4.5 【輕】文件與實際不一致

1. `docs/TEST_EVIDENCE.md` §7 寫「最終 DLL：`AppraisalAssistant.dll` 14,848 bytes、
   `AugSpotter.dll` 8,192 bytes」——實際是 **15,872 / 9,216**（§9.3 的數字才是對的）。
2. §7 寫「`AugSpotter`：0 個 Harmony patch」——最終是 **4**（§9.4 有註明，但 §7 沒改）。
3. `docs/ACCEPTANCE.md` C1.1.4 寫「patch 數從 8 降為 7」，B1 寫「N = 8」——歷史文字未同步。
4. `README.md` 與 `TEST_EVIDENCE.md` §9.7 都寫「`PlayerStore.Reset` 時會銷毀
   `DontDestroyOnLoad` overlay」——與程式碼不符（見 4.1）。這是**錯誤的驗收聲明**，
   比數字不一致嚴重。

### 4.6 【輕】其他觀察

- Overlay `sortingOrder = 32000` 且 `DontDestroyOnLoad`，會蓋在所有 UI（含對話、安全檢查）
  之上；`raycastTarget = false` 所以不會擋點擊，但視覺上會壓住標題列。建議降到介於遊戲
  HUD 與彈窗之間，或跟隨場景重建。
- `AugOverlay.Update()` 在 overlay 建好前每幀跑 `EnsureOverlay()`（幾個 null 檢查）。
  成本可忽略，但這與「Update 已停用」的文件敘述有出入；只有在
  `StoreUIManager.Instance`／`versionTMP`／`InspectionUIManager.confrontTitleTMP` 都不存在時
  才會一直跑（例如主選單）。
- `manifest.xml` 的 `<GameVersions>` 寫 `1.0` + `VERSION 100-Rev6`，而 `Application.version`
  是 `0.46D`。目前 `ModLoader` 接受（log：`trust=Match`），但遊戲更新把 `PlayerStore.VERSION`
  改掉時，這兩個 MOD 會先被標成不相容，需要同步更新 manifest。
- `dist/` 沒進 git（`.gitignore`），交付物只能由 `src/` 重建；因為 `/deterministic+`，
  重建 hash 與 zip 一致，這點沒問題。

---

## 5. 運算量／卡頓評估（結論：不是卡頓來源，但有兩個殘留成本）

### 5.1 每幀成本

| 路徑 | 每幀次數 | 內容 | 估計 |
| --- | --- | --- | --- |
| hover 物品時的 tooltip 重建 | 1（每幀） | 2 個 Harmony Postfix，各 2 個 null 檢查 + `IsCurrent`（2 次參考比較 + 靜態屬性） | < 0.05 ms，**零配置** |
| `InspectionUIManager.Update` | 1（每幀） | 遊戲原本的 `UpdateAppraisalPanel`，MOD 已不掛 patch | 0（MOD 未增加） |
| `AugOverlay.Update` | overlay 建好後 | `enabled = false` | 0 |
| `AugOverlay.Update` | overlay 建好前 | `EnsureOverlay` 幾個 null 檢查 | 可忽略 |

沒有 `for`／`while` 迴圈、沒有每幀 `ModifyTag`、沒有協程輪詢、沒有字串拼接在每幀路徑上
（`AddLine` 只在 session 命中時才做，且只在開啟放大鏡的那件物品上）。

### 5.2 事件成本（低頻）

- 每次放大鏡：`Truth.TryGet`（最多 4 次 `FindItemFeatureByCategory`）+ `Log.TryGetCorrectValue`
  （`AppraisalItemRegistry.Find`，3 種類型）+ 1 次 `ModifyTag` → 1 次 `ValidateShapeState`。
- 每位顧客：4 個事件 Postfix 各做 1 次 `GetClientBlueprint()`（純 getter）＋字串比較。

### 5.3 殘留成本

1. 每次鑑定 1 次背包 UI 重建（§4.3）——單次操作可接受，但這正是原本「切換顧客卡頓」的同一條路徑，
   如果玩家連續快速開關放大鏡，仍會重複重建。
2. Overlay 洩漏（§4.1）——每次場景切換多一個 Canvas + TMP 實例，記憶體與 draw call 單調成長；
   這才是這個 MOD 目前最實際的「越玩越卡／越玩越不穩」風險。

### 5.4 沒有量測到的東西

沒有 frame time、GC allocation、overlay 實例數的實機數據（我沒有啟動遊戲）。
`TEST_EVIDENCE.md` §9.8 也承認這點。要下「不卡頓」的結論，至少需要一次
Unity Profiler 或 `Time.deltaTime` 記錄的 10 位顧客切換。

---

## 6. 未核實清單（必須明列）

1. 最終 8-patch build 從未在遊戲內執行（4.2）。
2. 兩次 `0xc0000005` 是否由 MOD 造成——無法從現有資料判定；修好 4.1 後需重測。
3. `TMP_InputField.onValueChanged` 是否真的接到 `InspectionUIManager.OnValueChanged`
   （scene 資料，IL 看不到）；若是，§4.3 的「每次重建一次 UI」成立，若否則是零。
4. Overlay 的中文字型在「返回主選單再載入」後是否仍正常（`storeCalendar.dayTMP` 來自哪個場景、
   字型 asset 是否為 scene 物件，需實機看）。
5. `modTrust` 新記錄是否被遊戲接受（`trust=Match`）——要等下一次啟動才知道。
6. 其他 MOD 同時載入時的 Harmony patch 順序（本專案只有這兩個，未測第三方）。
7. `docs/TEST_EVIDENCE.md` 內所有「遊戲內已確認」的敘述，我只能對照 log／事件記錄，
   沒有重現當時的畫面截圖。

## 7. 放行條件（建議）

1. 修 §4.1（overlay canvas 要真的被銷毀），重建、重裝。
2. 用新 build 進遊戲跑：返回主選單 → 載入 ×10、切換 ≥10 位顧客、停用 MOD 一次；
   確認無新 crash、overlay 不殘留／不重複、`Player.log` 為 8/4 patches。
3. 修正 §4.5 的文件錯誤（尤其「Reset 會銷毀 overlay」這句）。
4. （可選）修 §4.3 的 onValueChanged 觸發，讓「跳過 shape validation」真的成立。

完成 1–3 後，這兩個 MOD 在 API／安全／功能／效能上我都會給 PASS；目前狀態是
**功能可用的高風險版本**，不建議就這樣當成 1.0 交付。
