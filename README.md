# Probably Stolen — QoL MODs

兩個獨立的《深空當舖：可能是偷的 / Probably Stolen》MOD：

| MOD | 功能 | 主要檔案 |
| --- | --- | --- |
| **AppraisalAssistant** | 估價型鑑定顯示真值並自動填入／提交；真假型鑑定顯示真品／假貨／有缺陷／複製品與原因（不給收購建議） | `dist/AppraisalAssistant/` |
| **AugSpotter** | 櫃檯顧客是改造體時，左上角顯示中文「偵測到改造體 等級 n」；普通顧客不顯示 | `dist/AugSpotter/` |

兩個 MOD 都使用遊戲內建 Mod Loader（`Mods/<資料夾>/manifest.xml` + `IMod`），不修改遊戲本體，也不使用檔案／網路／程序／動態碼／原生／反射 capability。

## 快速安裝

1. 遊戲完全關閉。
2. 把 `dist/AppraisalAssistant` 與 `dist/AugSpotter` 兩個資料夾複製到：

   ```text
   <Steam>\steamapps\common\Probably Stolen\Mods\
   ```

3. 啟動遊戲 → 主選單的 **Mods** 面板 → 啟用兩個 MOD。
4. 若遊戲要求重新啟動，重啟後生效。

或使用安裝腳本（會先備份既有同名資料夾）：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Install-Mods.ps1
```

想順便寫入「已核准／已啟用」設定（會先備份 `SaveFile.es3`）：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Install-Mods.ps1 -Enable
```

> `-Enable` 會直接改 `%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen\SaveFile.es3` 的 `modTrust` / `modOrder`，備份在 `backup/SaveFile/`。不想改設定就只用遊戲內 Mods 面板啟用。

### 還原 / 卸載

```powershell
# 還原最近一次 SaveFile 備份（會先備份目前檔案）
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Restore-SaveFile.ps1

# 移除兩個 MOD 資料夾（會先備份）
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Uninstall-Mods.ps1
```

## 功能細節

### 1. 估價型鑑定（`APPRAISAL_ITEM`）

- 物品 tooltip 直接顯示：
  - `真值：<正確基礎價值>`
  - `精確區間：<95%> - <105%>`
- 打開放大鏡／鑑定面板時，輸入框自動填入正確值。
- 按「估算」時，MOD 會用正確值提交，取得 perfect 評價（可在設定關閉）。
- 依據遊戲的 `AppraisalHelper.GetCorrectBaseValue`、`AppraisalItemRegistry`、
  `InspectionUIManager.UpdateAppraisalPanel` / `OnAppraisalServiceEstimated`。

### 2. 真假型鑑定（香菸／郵票／注射器／Ribwich／葡萄酒／複製品）

- 物品 tooltip 顯示：
  - `真品`（綠）
  - `假货`（紅）＋ `原因：...`
  - `有缺陷`（橙）＋ `原因：已过期／无法使用...`
  - `复制品`（橙）＋ `原因：复制品，非原版`
- 對質面板標題只顯示 `[真品]` / `[假货]` / `[有缺陷]` / `[复制品]`，不改字級、不加原因、不給收購建議。
- 原因來自遊戲的可見問題標籤（例如 `防伪标问题`、`商标问题`、`指示灯问题`），
  以及 `ItemFeature.realCondition`、`RibwichHelper.IsCounterfeit`、`WINE_COUNTERFEIT_TAG`。

### 3. 改造體（Aug）

- 只有櫃檯前的顧客真的是改造體時，左上角才顯示紅色中文：
  `侦测到改造体  等级 <n>`（`augTier`）。
- 普通顧客：完全不顯示。
- 直接讀 `StoreClient.isAug` / `augTier`，不改顧客狀態、不消耗掃描器電池。

## 設定

遊戲內 Mod 設定視窗（或 `ModConfigs/<mod id>.json`）：

- `show_true_value`：顯示真值與精確區間（預設 true）
- `auto_fill`：自動填入估價輸入框（預設 true）
- `force_correct_estimate`：按估算時強制用正確值（預設 true）

（`AugSpotter` 沒有需要調整的設定。）

## 從原始碼建置

需求：Windows PowerShell 5.1、Python 3（只用來下載 Roslyn）、遊戲的 Managed 資料夾。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Setup-Toolchain.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Build-Mods.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Verify-Mods.ps1
```

- `Setup-Toolchain.ps1`：下載 Roslyn 4.8（MIT）到 `.tools/`，複製 `DseCecil.dll` 供驗證器使用。
- `Build-Mods.ps1`：用遊戲的 `Assembly-CSharp.dll`、`0Harmony.dll`、UnityEngine / TMP 組件編譯兩個 DLL。
- `Verify-Mods.ps1`：檢查 manifest、`IMod`、組件參考、禁用 API、Harmony patch 目標與參數。

離線 patch／邏輯測試（需要 `.tools/mono`，見 `docs/TEST_EVIDENCE.md`）：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Probe-Patches.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Probe-Logic.ps1
```

## 檔案結構

```text
src/AppraisalAssistant/     MOD 原始碼 + manifest
src/AugSpotter/             MOD 原始碼 + manifest
dist/                       建置輸出（可直接安裝）
tools/                      建置、安裝、驗證、離線測試腳本
docs/ACCEPTANCE.md          動工前寫好的驗收標準
docs/GAME_CODE_NOTES.md     遊戲代碼逆向筆記（實際類別／方法）
docs/TEST_EVIDENCE.md       實際測試輸出與還原紀錄
```

## 安全與已知限制

- 不讀寫本機檔案、不連網、不啟動程序、不使用反射／動態碼／P/Invoke；manifest 的 `<Capabilities/>` 為空。
- 只 patch 遊戲公開方法，不改存檔格式、不新增物品到玩家庫存。
- **已在遊戲內確認**：8 個 Harmony patch 全部套用、真假判定全數通過、Aug overlay 中文顯示正常；遊戲內建 `ModScanner` 對兩個 DLL 給出 `refused=False, capabilities=[], findings=0`。
- **效能修正**：估價面板的 `APPRAISAL_DEFAULT_VALUE` 只在值真的不同時才寫入。`GameItem.ModifyTag()` 會連帶觸發 `ValidateShapeState()` 與 `OverlayHandler.UpdateInventoryStateUI()`；原本每幀無條件寫入會導致切換顧客時反覆重建背包 UI。修正後同一數值不再重寫。
- **仍需你實測**：同一存檔連續切換 10 位以上顧客，確認主觀卡頓是否消失；若 overlay 位置擋住 UI，改 `src/AugSpotter/AugSpotter.cs` 的 `anchoredPosition` 後重編譯。
