# 給 MOD 開發者的診斷報告與修改方向

- 對象：`AppraisalAssistant` / `AugSpotter`（交付 hash `63A9F4C9…` / `A3207236…`，15,872 / 9,216 bytes）
- 依據：`docs/ACCEPTANCE_REVIEW.md`（第三方覆核）＋以 Cecil 讀真實 `Assembly-CSharp.dll` 的 IL
- 一句話：**功能與 API 兼容性沒問題，問題全部集中在「物件生命週期」與「交付驗證流程」。**

---

## 1. 先講結論：哪些是對的

先把不用改的部分講清楚，避免過度改動。

1. **Harmony 目標全部正確**：8＋4 個 patch 的型別、方法、參數名、可見性都跟真實組件對得上；
   `PatchAll` 在 Mono 6.12 + 遊戲 `0Harmony 2.4.2.0` 上實跑成功。
2. **安全面乾淨**：`<Capabilities/>` 為空、無檔案／網路／程序／反射／P-Invoke，
   遊戲內建 `ModScanner` 給 `refused=False capabilities=[] findings=0`。
3. **事件驅動的方向是對的**：`isAug=true` 的所有寫入點都發生在顧客建立時
   （`OnShutterOpened → GenerateClient → MakeDisguisedAug`、`CreateTrailerAug/Med/Material`），
   都早於 `OnNextClientArrived`，所以拿掉 0.5 秒輪詢不會漏標記。這是這次改版最大的正確決定。
4. **移除 `UpdateAppraisalPanel` 每幀 patch 是對的根因修正**：
   `InspectionUIManager.Update()` 真的每幀呼叫它；舊版每幀 `ModifyTag → ValidateShapeState`
   （內部會重算 `modifiedShape` 與整包 `modifiedState`）確實是切換顧客卡頓的來源。
5. **`AppraisalTag.SetDefaultValue` 的手動 `modifiedState` 同步是必要的，不是多餘的**：
   `ModifyTag` 只寫 `item.state`；`ValidateShapeState` 才會把 `state` 重算進 `modifiedState`；
   而遊戲的 `UpdateAppraisalPanel` 讀的是 `GetTagReadonly`＝`modifiedState` 的 clone。
   跳過 `skipValidate` 時若不手動同步，下一幀輸入框就會被舊值蓋掉。這段邏輯是對的。

所以下面的修改，**只需要動生命週期與幾處成本**，不要重寫功能。

---

## 2. P0：`AugSpotter` 的 overlay 洩漏（必須修，現在的修法無效）

### 2.1 目前的物件圖

```text
GameObject "AugSpotterRuntime"        ← RuntimeHost._root，DontDestroyOnLoad
  └─ AugOverlay (component)           ← _instance，_canvas/_label/_background 欄位

GameObject "AugSpotterOverlay"        ← EnsureOverlay 另外 new 出來的「另一個 root」
  ├─ Canvas (ScreenSpaceOverlay, sortingOrder 32000)
  ├─ CanvasScaler
  ├─ Label (Instantiate 自 storeCalendar.dayTMP)
  └─ Background (Image)
       ↑ 自己也 DontDestroyOnLoad，跟上面那個 root 沒有任何父子關係
```

### 2.2 因果鏈

1. `PlayerStore.Reset` → `AugOverlay.ResetForSceneChange()` → `RuntimeHost.Destroy()`；
2. `RuntimeHost.Destroy()` 只 `Destroy(_root)` → 掛掉的只有 `"AugSpotterRuntime"` 與上面的
   `AugOverlay` 元件；`"AugSpotterOverlay"` 沒有父物件、又自己 `DontDestroyOnLoad` → **存活**；
3. `OnDestroy` 只做 `_instance = null`，沒有清 canvas；
4. 下一個顧客事件 → `Refresh()` 看到 `_instance == null` → `RuntimeHost.Create` → 建**第二個**
   canvas；舊的仍在畫面上（還帶著上一位顧客的 `侦测到改造体  等级 N`）；
5. 每切一次場景累加一層；`OnDisable`（Mods 面板停用／結束遊戲）走同一條路徑，
   所以**停用 MOD 之後標記還會留在畫面上**，只能重開遊戲。

### 2.3 為什麼它「看起來」修好了

因為 `Refresh()` 會建立新 overlay，新顧客的標記顯示正常；舊 canvas 的文字在第一時間
可能剛好是空字串（顧客已離開），所以肉眼驗收很容易過關。但只要在「有顧客在櫃檯時」
按 Esc 回主選單，殘留的紅字就會出現在主選單／下一個存檔。這也是為什麼 §9.7 只寫
「尚未核實」，而不是「已修好」。

### 2.4 與那兩次 native crash 的關係（假設，非定論）

`ntdll.dll 0xc0000005` 是 native access violation，不是 managed exception。洩漏的 canvas
最可疑的地方在於：`Instantiate(storeCalendar.dayTMP.gameObject)` 會複製 TMP 的材質／
sub-mesh 設定，而那些材質 instance 是跟著**原場景**的 TMP 資產走的；場景卸載後，
複製體仍被 `DontDestroyOnLoad` 保留並繼續參與 UI rebuild／render，時序上就可能在
native 端踩到已釋放的資源。加上每切一次場景多一組，命中率只會上升。

這條目前**無法證明**（也可能是遊戲本身或 Steam 的問題），但：
- 30 天事件記錄裡 `Probably Stolen.exe` 只有 20:53:13、21:19:46 兩次崩潰，都在 MOD 測試當晚；
- 兩次都在「返回主選單／載入」附近；
- 8fe90b9 的修法在邏輯上不可能生效（destroy 錯對象）。

所以修完 §2.5 之後重測，是唯一能把這條從「可疑」變「排除」的方法。

### 2.5 建議修法（兩選一，建議 B，或 A+B）

**A. 讓 canvas 成為 holder 的子物件**（最小改動，2 行）

```csharp
// RuntimeHost：把 root 曝露給 overlay 用
internal static Transform RootTransform
{
    get { return _root != null ? _root.transform : null; }
}

// AugOverlay.EnsureOverlay()
GameObject root = new GameObject("AugSpotterOverlay");
root.transform.SetParent(RuntimeHost.RootTransform, false);  // ← 加這行
// 刪掉 UnityEngine.Object.DontDestroyOnLoad(root);
//     （父物件已經是 DontDestroyOnLoad；對子物件呼叫它 Unity 會印警告）
```

**B. 自己記住並明確銷毀**（防禦性較好，能蓋掉「元件被別的路徑 destroy」的情況）

```csharp
internal sealed class AugOverlay : MonoBehaviour
{
    private static AugOverlay _instance;
    private static GameObject _overlayRoot;   // ← 新增

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
        TearDownCanvas();                     // ← 新增
    }

    internal static void TearDownCanvas()
    {
        if (_overlayRoot != null)
        {
            UnityEngine.Object.Destroy(_overlayRoot);
            _overlayRoot = null;
        }
    }

    private void EnsureOverlay()
    {
        if (_canvas != null) return;
        ...
        GameObject root = new GameObject("AugSpotterOverlay");
        _overlayRoot = root;                  // ← 新增
        _canvas = root.AddComponent<Canvas>();
        ...
    }
}

internal static class RuntimeHost
{
    public static void Destroy()
    {
        AugOverlay.TearDownCanvas();          // ← 先清 canvas，再清 holder
        if (_root != null) { UnityEngine.Object.Destroy(_root); _root = null; }
    }
}
```

**C. 一次性清掉舊版遺留（可選，但建議加，當作保底）**

```csharp
// AugSpotterMod.OnEnable() 開頭
foreach (Canvas c in Resources.FindObjectsOfTypeAll<Canvas>())
{
    if (c != null && c.gameObject.name == "AugSpotterOverlay")
        UnityEngine.Object.Destroy(c.gameObject);
}
```

> 注意：`OnDestroy` 在「結束遊戲」時也會跑，Destroy 另一個 GameObject 在 quit 期間
> Unity 允許，但如果你不想賭，可以把 `TearDownCanvas()` 只放在 `RuntimeHost.Destroy()`
> （Reset 與 OnDisable 都會走它），`OnDestroy` 只留 `_instance = null`。

### 2.6 修完怎麼證明（不要再用「看起來正常」）

1. 進存檔 → 讓一位改造體到櫃檯（紅字出現）→ 按 Esc 回主選單 → 再載入同一個存檔：
   紅字必須**立刻消失**，主選單也不能有殘留。
2. 重複 5 次，然後在遊戲內跑一次除錯指令（或臨時 log）：
   `Resources.FindObjectsOfTypeAll<Canvas>().Where(c => c.name == "AugSpotterOverlay").Count()`
   → 必須是 **1**（或 0，如果還沒建立）。
3. 在 Mods 面板停用 `AugSpotter` → 標記必須立刻消失。
4. 看 `Player.log`：不得出現 `DontDestroyOnLoad only works for root GameObjects`。

---

## 3. P0：最終版沒有被驗證過（流程問題，不是程式問題）

### 3.1 事實

| 項目 | 值 |
| --- | --- |
| 最後一次遊戲執行（`Player.log`） | 21:19:43，`Harmony: 7 method(s) patched` |
| 該次執行結束 | 21:19:46 `ntdll.dll 0xc0000005` |
| 目前安裝的 DLL | 21:28:04，8 個 patch |
| 交付 zip 內 DLL | 與目前安裝同 hash |

也就是說：**你交付的是 8-patch 版，但遊戲只跑過 7-patch 版**；`8fe90b9` 新加的
`PlayerStore.Reset` patch 與 overlay 拆除邏輯，從未在 Unity 裡執行過一次。
再加上 7-patch 版在「返回主選單」崩了兩次，這個 build 目前是「紙上修好」。

### 3.2 建議的流程修正

1. **build stamp**：`Build-Mods.ps1` 編譯時把 `ModVersion` 加上 git short hash＋時間，
   或在 `Init()` 印 `Log.Info("build " + BuildStamp)`；`Player.log` 就能直接對上 zip。
2. **release checklist 寫進 `docs/ACCEPTANCE.md`**：每次交付前必須有
   「本次 build hash ＝ `Player.log` 內那一次執行的 hash」這一條，否則不算驗收。
3. **bisect 手段**：若修完 §2 後仍崩，下一步是「暫時關掉 overlay 建立」（`EnsureOverlay`
   直接 return），再跑同樣的「返回主選單 → 載入 ×10」。
   - 不崩 → 問題在 overlay（再細分：先不 clone TMP、改用 `AddComponent<TextMeshProUGUI>` +
     手動指 `font = template.font`，看是不是 clone 造成）。
   - 仍崩 → 與 overlay 無關，回頭查 `AppraisalAssistant` 的 session 或遊戲本身。
4. **崩潰檢查指令**（放進 checklist）：

```powershell
Get-WinEvent -FilterHashtable @{LogName='Application'; ProviderName='Application Error'; StartTime=(Get-Date).AddHours(-2)} |
  Where-Object { $_.Message -match 'Probably Stolen' } | Select-Object TimeCreated
```

---

## 4. P1：每次鑑定仍有一次背包 UI 重建（可修，且修法很便宜）

### 4.1 原因

你寫 `apprisalServiceInputField.text = text`；TMP 的 `set_text` 會呼叫
`SendOnValueChanged()`，而 `InspectionUIManager.OnValueChanged(string)` 會做
`ModifyTag("APPRAISAL_DEFAULT_VALUE", …, skipValidate:false)` → `ValidateShapeState()`
→ 背包 UI 重建。等於你前面用 `skipValidate` 省下來的那一次，又從欄位回呼補回去了。

（`onValueChanged` 是否真的接上取決於場景接線，IL 看不到；但方法名與簽名就是給它用的，
幾乎確定有接。§6 列為未核實項。）

### 4.2 修法

這個遊戲的 TMP 版本有 `SetTextWithoutNotify(string)`（已用 Cecil 確認
`Unity.TextPro`/`Unity.TextMeshPro.dll` 內存在），直接改用：

```csharp
// AppraisalInspectResetPatch.Postfix
__instance.apprisalServiceInputField.SetTextWithoutNotify(text);

// AppraisalForceEstimatePatch.Prefix
__instance.apprisalServiceInputField.SetTextWithoutNotify(text);
```

**但 `AppraisalTag.SetDefaultValue(gameItem, value)` 一定要保留**，因為
`UpdateAppraisalPanel` 下一幀會用 `APPRAISAL_DEFAULT_VALUE` 重寫輸入框；
不寫 tag 的話欄位會被舊值蓋掉。也就是：`SetTextWithoutNotify` 負責「當下顯示」，
tag 負責「持續顯示」，兩者都要。

### 4.3 驗證

- 在 `AppraisalTag.SetDefaultValue` 的 `ModifyTag` 前後各放一個 `Time.frameCount` log，
  或直接在 `OverlayHandler.UpdateInventoryStateUI` 上暫時掛一個計數 Postfix，
  確認「開放大鏡一次」的呼叫次數從 2 → 1（理想是 1，因為 tag 真的變了）。
- 注意：這裡不追求 0；`APPRAISAL_DEFAULT_VALUE` 真的變了，就該重建一次。

---

## 5. P2：`AppraisalSession` 的靜態引用

`AppraisalSession.Item` 是 `static GameItem`，只在 `PlayerStore.Reset` 清。
在同一個存檔內，最後一次用放大鏡看過的物品（含 feature/shape 圖）會被一直強引用。

- 影響很小（單一物件），但這跟「返回主選單後不要把舊場景物件帶到新存檔」是同一個原則；
- 修法：`InspectionUIManager.CloseUI` 沒有 patch 點，但 `currentInspectingItem` 在
  `CloseUI` 會被設為 null，所以可以在 `AppraisalTooltipPatch`／`AuthenticityTooltipPatch`
  發現 `AppraisalSession.Item` 已不再是 `currentInspectingItem` 時順手 `Clear()`；
- 或在 `OnInspectItem` 開頭就 `AppraisalSession.Clear()`（語意更清楚，成本 5 個欄位赋值）。

`Truth` / `Log` 的 `ConditionalWeakTable` 是弱引用，沒有洩漏問題，不用動。

---

## 6. P3：文件、manifest、安裝器

### 6.1 必改的文件錯誤

| 位置 | 現在寫的 | 實際 |
| --- | --- | --- |
| `TEST_EVIDENCE.md` §7 | 最終 DLL 14,848 / 8,192 bytes | **15,872 / 9,216**（§9.3 才對） |
| `TEST_EVIDENCE.md` §7 | `AugSpotter`：0 個 Harmony patch | **4** |
| `ACCEPTANCE.md` C1.1.4 | patch 數 8 → 7 | 最終是 **8**（B1 是對的） |
| `README.md`、`TEST_EVIDENCE.md` §9.7 | 「`PlayerStore.Reset` 時會銷毀 `DontDestroyOnLoad` overlay」 | **錯**，只 destroy 了 holder（§2） |

最後一條最重要：它不是數字不一致，是**驗收聲明與程式碼相反**。文件說已修，實際沒修，
會讓下一個接手的人直接跳過這個 bug。

### 6.2 manifest / 版本

`<GameVersions>` 寫 `1.0` + `VERSION 100-Rev6`，遊戲 `Application.version` 是 `0.46D`，
`PlayerStore.VERSION` 目前剛好是 `VERSION 100-Rev6` 所以 `trust=Match`。遊戲一改版就會
被標成不相容。建議在 README 寫明「遊戲更新後要同步 `GameVersions`」，並在 `Init()`
log 出實際 `PlayerStore.VERSION`，方便玩家回報。

### 6.3 安裝器的信任紀錄

`Enable-Mods.ps1` 用 Python 直接改 `SaveFile.es3` 的 `modOrder` / `modTrust`，
我核對過遊戲 `ModFingerprint.Compute`：

```text
Combined = SHA256(UTF8(manifestHash + "\n" + "<Assembly 條目>|<檔案 hash>"))
```

腳本算的完全一致，所以記錄格式是對的（上一版 `trust=Match` 也證明會過）。
但這是「代玩家按同意」，屬於要揭露的行為，建議 README 明確寫：
「會寫入 modTrust 以省去遊戲內核准步驟；不想改存檔就只用 Mods 面板啟用」。

---

## 7. 方向：三個原則，之後就不會再犯同類錯

### 7.1 每個 MOD 只能有一個「生命週期擁有者」

目前 `DontDestroyOnLoad` 物件有兩個 owner（`RuntimeHost._root` 與 `AugOverlay` 自己 new 的
canvas），所以 destroy 一定會漏。原則是：

- 所有 `DontDestroyOnLoad` 的物件都掛在某一個 host 底下（父子），或全部登記在一個清單；
- `Destroy()` 只允許走 host，host 負責把子物件／清單全部清掉；
- `OnDisable`、`PlayerStore.Reset`、`OnApplicationQuit` 走同一個 `TearDown()`。

### 7.2 每一條 patch 都要寫「不成立時誰補」

你這次的事件驅動改造，正確做法是對每個 patch 寫一行覆蓋分析，例如：

| patch | 觸發時機 | 若沒觸發，誰補？ |
| --- | --- | --- |
| `OnNextClientArrived` | 走路進場動畫結束 | `SetupWaitingClient`（特殊顧客）、`Reset` 後首次 overlay |
| `SetupWaitingClient` | 早班／特殊顧客直接生成 | — |
| `DismissCurrentClient` | 顧客離開（含交易完成） | — |
| `Reset` | 新遊戲／返回主選單 | — |

這次的覆蓋是完整的（我逐個呼叫點查過），但這個表格應該留在 `GAME_CODE_NOTES.md` 裡，
下次改版才不會又退回輪詢。

### 7.3 成本分級要寫進註解

- per-frame：只能做 reference 比較，不能配置、不能 `ModifyTag`；
- per-event：可以讀 `StoreClient`、寫 UI 文字；
- per-inspect：可以做一次 feature 掃描、一次 `ValidateShapeState`。

`AppraisalAssistant` 目前的分級是對的（tooltip 只讀 session、掃描只在 `OnInspectItem`），
只要把 §4 的 `onValueChanged` 補上，per-inspect 就只剩一次重建。

---

## 8. 回歸測試清單（修完照這個跑）

| # | 操作 | 預期 | 怎麼看 |
| --- | --- | --- | --- |
| 1 | 改造體在櫃檯 → Esc → 載入同存檔 | 紅字立即消失，主選單無殘留 | 畫面 |
| 2 | 重複 1 五次 | `AugSpotterOverlay` canvas 數量 = 1 | 除錯指令／log |
| 3 | Mods 面板停用 AugSpotter | 標記立刻消失 | 畫面 |
| 4 | 普通顧客 | 完全不顯示 | 畫面 |
| 5 | 連續 10 位顧客切換 | 無持續掉幀 | Profiler `Time.unscaledDeltaTime` p95 |
| 6 | 估價（手／Ribwich／電漿燃料） | tooltip 真值＋精確區間、輸入框自動填、估算 perfect | 畫面 |
| 7 | 香菸／郵票／注射器／複製品／假酒 | `真伪判定：…`＋具體原因；對質標題只加 `[…]` | 畫面 |
| 8 | 同顧客兩件鑑定品 | 第二件用自己的價值 | 畫面 |
| 9 | 返回主選單 → 載入 ×10 | 無新增 `Application Error` | 事件記錄 |
| 10 | `Player.log` | `Harmony: 8 method(s) patched`、`4 method(s) patched`，無 `[AppraisalAssistant]` 洗版 | log |

通過 1–4、9 之後，§2 的 P0 才算關閉；通過 5–8 才是功能驗收；10 是每次交付都要看的。

---

## 9. 風險登記表

| ID | 風險 | 等級 | 狀態 | 動作 |
| --- | --- | --- | --- | --- |
| R1 | overlay canvas 洩漏／殘留／重複 | P0 | 已定位，未修 | §2.5 |
| R2 | `ntdll` 0xc0000005 歸因不明 | P0 | 未解 | 修 R1 後重測 §3.2 |
| R3 | 每次鑑定多一次背包 UI 重建 | P2 | 可修 | §4 |
| R4 | `AppraisalSession.Item` 強引用 | P3 | 可修 | §5 |
| R5 | `GameVersions` 對不上未來版本 | P3 | 觀察 | §6.2 |
| R6 | 安裝器代寫 `modTrust` | P3 | 文件揭露 | §6.3 |

---

## 10. 建議的提交順序

1. `fix(aug): tear down overlay canvas on scene reset / mod disable`（§2.5，含 §2.6 驗證 log）
2. `chore(aug): guard against duplicate overlay roots`（§2.5-C，一次性清理）
3. `test: rerun menu↔load ×10 on the new build`（把事件記錄與 `Player.log` 貼進 `TEST_EVIDENCE.md`）
4. `perf(appraisal): avoid onValueChanged rebuild via SetTextWithoutNotify`（§4）
5. `refactor(appraisal): clear session as soon as inspection closes`（§5）
6. `docs: correct build sizes, patch counts, and the Reset-teardown claim`（§6.1）

每步都要能單獨回滾；1–3 沒過之前，不要發 1.0。
