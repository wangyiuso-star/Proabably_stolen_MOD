# Probably Stolen — QoL MODs

兩個獨立的《深空當舖：可能是偷的 / Probably Stolen》MOD：

| MOD | 功能 | 主要檔案 |
| --- | --- | --- |
| **AppraisalAssistant** | 估價型鑑定直接顯示真值、自動填入並以正確值提交；真假型鑑定顯示 GENUINE / COUNTERFEIT / DEFECTIVE / REPLICA 與購買建議 | `dist/AppraisalAssistant/` |
| **AugSpotter** | 顧客在櫃檯時，左上角直接標示 **AUG DETECTED**（改造體）或 **NOT AN AUG**，不需掃描儀、電池或教學 | `dist/AugSpotter/` |

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
  - `TRUE VALUE: <正確基礎價值>`
  - `PERFECT RANGE: <95%> - <105%>`
- 打開放大鏡／鑑定面板時，輸入框自動填入正確值。
- 按「估算」時，MOD 會用正確值提交，取得 perfect 評價（可在設定關閉）。
- 依據遊戲的 `AppraisalHelper.GetCorrectBaseValue`、`AppraisalItemRegistry`、
  `InspectionUIManager.UpdateAppraisalPanel` / `OnAppraisalServiceEstimated`。

### 2. 真假型鑑定（香菸／郵票／注射器／Ribwich／葡萄酒／複製品）

- 物品 tooltip 顯示：
  - `AUTHENTICITY: GENUINE`（綠）＋ `SAFE TO BUY`
  - `AUTHENTICITY: COUNTERFEIT`（紅）＋ `DO NOT BUY`
  - `AUTHENTICITY: DEFECTIVE - EXPIRED / UNUSABLE`（橙）＋ `DO NOT BUY`
  - `AUTHENTICITY: REPLICA (NOT ORIGINAL)`（橙）＋ `DO NOT BUY`
- 香菸／郵票／注射器的對質面板標題會額外顯示 `[GENUINE]` / `[COUNTERFEIT]` / `[DEFECTIVE]`。
- 真假來源是遊戲的 `ItemFeature.realCondition.identifier`、`RibwichHelper.IsCounterfeit`、
  `WINE_COUNTERFEIT_TAG`，不是猜測。

### 3. 改造體（Aug）

- 顧客在櫃檯時，畫面左上角顯示：
  - 改造體：紅色 `AUG DETECTED - Tier <n>`
  - 普通顧客：綠色 `NOT AN AUG`（可在設定關閉）
  - 無顧客：隱藏
- 直接讀 `StoreClient.isAug` / `augTier`，不改顧客狀態、不消耗掃描器電池。

## 設定

遊戲內 Mod 設定視窗（或 `ModConfigs/<mod id>.json`）：

- `show_true_value`：顯示真值與 perfect 區間（預設 true）
- `auto_fill`：自動填入估價輸入框（預設 true）
- `force_correct_estimate`：按估算時強制用正確值（預設 true）
- `show_buy_advice`：顯示 SAFE TO BUY / DO NOT BUY（預設 true）
- `run_self_test`：進入店內後記錄自測結果（預設 true）
- `show_not_an_aug`：普通顧客顯示 NOT AN AUG（預設 true）

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
- **已在離線 Mono 環境驗證**：7 個 Harmony patch 全部套用、真假判定全數通過、Aug 判定通過、遊戲內建 `ModScanner` 對兩個 DLL 給出 `refused=False, capabilities=[], findings=0`。
- **尚未在真正的遊戲執行期內完成 UI 實測**：此環境的 Steam 用戶端無法正常啟動遊戲（Steam 登錄檔權限／DRM 重啟），因此沒有截圖證明 overlay 與輸入框在實際畫面中的位置。請按 `docs/ACCEPTANCE.md` 第 4 節手動驗收；若 overlay 位置擋住 UI，只需改 `src/AugSpotter/AugSpotter.cs` 的 `anchoredPosition` 後重編譯。
