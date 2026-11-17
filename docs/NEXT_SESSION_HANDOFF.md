# 下一個對話交接（Probably_Stolen_MOD）

> 寫於 2026-11-17 12:12（Asia/Hong_Kong）。給接手的 agent／對話：
> 先讀這份，再讀 `docs/P5_HANDOFF.md`、`docs/CARD_CRAFT_STATUS.md`、`docs/CARD_CRAFT_SPEC.md`。
> 本專案是 MOD 專案，不是遊戲本體；不要啟動 Steam／遊戲，實機由使用者跑。

## 0. 一句話現況

- **P5 功能完成、離線驗收通過、實機已於 2026-11-17 驗收**（P5-01／04／06／07
  通過、P5-08 通過；P5-09／10 未測；P5-11／12 延後到 1.0）。
- **P6 江洋大盜已實作、離線驗收通過、實機待跑**（0.8.0）。
- 版本 **0.8.0**（`79,360 bytes／8E6238AF…`），HEAD `83197a6`（P6 提交前；
  提交後看 `git log -1`），**未 push**。
- 遊戲 `Mods` 目前還是 **0.7.7**（`75,776／592F866F…`）；要測 0.8.0 要先覆蓋並完全重開。
- P6 0.8.0 與本文件同一提交（hash 見 `git log -1`；本文件不寫死自身 commit）。

## 1. Repo／產物

| 項目 | 值 |
|---|---|
| HEAD | `83197a6`（P6 提交前）；P6 提交後以 `git log -1` 為準 |
| 0.8.0 實作 | P6 江洋大盜，與 docs 同一提交（hash 見 `git log -1`） |
| 0.7.8 實作 | `7f10b7c`（+ `8256e40` STATUS） |
| 0.7.7 | `3e1c4fa` |
| 0.7.6–0.7.3 | `225f17b`／`c59107c`／`d13d49d`／`4fe6b3f` |
| dist DLL | `dist/CardCraftAndFence/CardCraftAndFence.dll`，79,360 bytes，SHA256 `8E6238AFD611269E30DA2D199067DE9334112A2C3215B90B7B78EA6694B3650E` |
| 已安裝 | `.../Probably Stolen/Mods/CardCraftAndFence`，0.7.7，75,776／`592F866F…` |
| git status | P6 0.8.0 與 docs 同一提交；另有 `docs/ACCEPTANCE_REVIEW.md`、`docs/DEVELOPER_DIAGNOSTIC.md` 兩個 11-02 舊修改（未提交） |
| push | 沒有，也不要 push |

## 2. P5 狀態矩陣（以此為準）

> **2026-11-17 實機驗收**：P5-01／04／06／07、P5-08 已通過，P5-09／10 未測，
> P5-11／12 延後到 1.0。以下保留原始矩陣作歷史；不要再重跑 0.7.8 的 P5 卡。

**已實機通過**
- 分組高→低：3 T3+1 T1 → T3、2 T1+3 T2 → T2、1 T1+2 T2 → T1（0.7.1 log）。
- cap 優先最高級：3 T3+3 T1、未買 BATCH → 只出 T3（0.7.1 log）。
- 紙本 runtime sprites／開書（0.7.3、0.7.7 log；無 `No sprite found`）。
- 促銷 barter 移除 log（0.7.0／0.7.7）。
- T2 rare 先放（0.7.1 `remaining=0`）。
- CMD T1 箱 fill 8 rolls + rare、無 overflow（0.7.7 log）。
- CMD 箱染色 0.7.7（使用者確認「沒問題」）。

**離線已關（0.7.8）**
- P5-02 rare 先放、P5-03 容量數字、P5-05 null 診斷、P5-13（P5 部分）、P5-14 CMD T2 -0.77%。
- P5-04 MED／ENG／SER T3 的 40／30／30 已用 shape 複算，**但實機各 3 箱仍待**。

**P5 關閉必要實機（4 項）**
1. **P5-01**：T3 SEC ×5（5 件）、T3 CMD ×5（8 件）、T2 CMD ×3（7 件）；無 `[CF-LOOT] overflow box=…`。
2. **P5-04**：T3 MED／ENG／SER 各 3 箱（各 5 件），同樣看 overflow。
3. **P5-06**：芯片 3 同級→下一級、每家族每晚 1 次、T3 不升、不佔 cap。
4. **P5-07**：促銷 UI 立即消失／重開／重載／重啟不復活。

**不擋 P5 關**
- P5-08 紙本重載不重複、P5-09 `CF_SAWYER_*` 隱藏、P5-10 CMD T1 開箱 9 件。

**1.0 前**
- P5-11 `BM_DISTRUSTED`／箱內卡／垃圾桶滿的決策；P5-12 `_diag`／`_removalLogged` 的 process-static 範圍。

## 3. 接手第一步（照順序）

1. 讀 `docs/P5_HANDOFF.md` 與 `docs/CARD_CRAFT_STATUS.md` 的「P6 0.8.0」段。
2. 確認 `git log -1`、`dist` hash、`Mods` 版本；不要假設 0.8.0 已安裝。
3. P5 四項已於 2026-11-17 由驗收方通過，**不要重跑**；本輪只剩 P6 實機卡（§4b）。
4. 把 `dist\CardCraftAndFence\*` 複製到 `Mods`，完全退出遊戲再開，跑 §4b。
5. P6 不過 → 把 `Player.log` 原文貼進 work item，不要為了過而改數字或放寬 gate。

## 4. 實機測試卡（0.7.8，歷史；P5 已於 2026-11-17 驗收）

> 這一節保留 0.7.8 的 P5 卡作歷史，**不要重跑**；P6 卡見 §4b。

安裝：
- 完全退出遊戲 → 備份 `AppData\LocalLow\Questing Goose Studio\Probably Stolen\save_*.es3`、`SaveFile.es3`。
- 複製 `dist\CardCraftAndFence\*`（76,288／`EEBF0941…`）到 `...\Probably Stolen\Mods\CardCraftAndFence\`。
- 啟動時重新核可（每次 build hash 變都會問）→ log 要 `Init … v0.7.8`。

P5-01／04：用 debug spawner 或索耶換卡造箱；每箱開完數件數：
- T3 SEC 5、T3 CMD 8、T2 CMD 7、T3 MED/ENG/SER 5。
- log 不能有 `[CF-LOOT] overflow box=…`；有就貼整行（`rolled/spawned/expected/actual/dropped/items`）。

P5-06（芯片，四晚）：
1. 3 個 T1＋3 個 T2 同家族核心 → 只 `1->2`。
2. 隔晚 3 個 T2 → `2->3`。
3. 隔晚 3 個 T3 → `chip=0`。
4. 同晚 3 張 T1 卡＋3 個 T1 核（BATCH 未買）→ `boxes=1 chip=1`。

P5-07（促銷 UI）：
- 用 `activePromotionalBarterIds` 含 `sec_crate_barter` 的存檔（例：`save_86`），買 PERMANENT。
- 看交易所面板是否立即消失、關閉再開、存檔重載、重啟 process 都不復活；`removed …` 每 process 一次。

log 過濾：
```powershell
Select-String -Path "$env:USERPROFILE\AppData\LocalLow\Questing Goose Studio\Probably Stolen\Player.log" `
  -Pattern 'v0\.7\.8|CF-LOOT|overflow box=|spawn short|CF-SAWYER|CF-PAPER|CF-SPRITE' |
  ForEach-Object { $_.Line }
```

## 4b. P6 實機測試卡（0.8.0，人類跑；agent 不啟動遊戲）

安裝：
- 完全退出遊戲 → 備份 `AppData\LocalLow\Questing Goose Studio\Probably Stolen\save_*.es3`、`SaveFile.es3`。
- 複製 `dist\CardCraftAndFence\*`（79,360／`8E6238AF…`）到
  `...\Probably Stolen\Mods\CardCraftAndFence\`；重新核可。
- 啟動 log 要有 `Init … v0.8.0`、`enabled v0.8.0`、`Harmony: 8`；沒有 `Init failed`。

**A. chance 表（先驗這個）**
- 用 F9 debug 或既有存檔把 BM 調到 30／60／85／150；**不要直接改存檔檔**。
- 各睡 1–2 天，看每天最多一行（新的一天是 `roll`，同日重載是 `roll cached`）：
  `[CF-HIGHWAY] roll day=… bm=30 chance=8 ok=…`、`60 → 14`、`85 → 20`、`150 → 33`。
- 若 BM 到不了 150，先回報「設計前提不成立」，**不要自行降門檻**。

**B. 到訪與箱（BM150＋Tier3）**
- 睡到 `ok=1`（33% 下 10 天內至少一次；P(無)=0.67^10≈1.8%）。
- log 要有 `[CF-HIGHWAY] visit … tier=3 id=cf_cmd_box_t3 bm=…` 與
  `[CF-HIGHWAY] box … added=true`；店裡出現「江洋大盗」。
- 買 T3 箱：8 件、上鎖、T3 卡可開、無 `overflow`。

**C. 分級**
- BM85＋Tier2（無 Tier3）→ `tier=2`、`cf_cmd_box_t2`、7 件、價 350。
- BM85＋無 Tier2 → `tier=1`、`cf_cmd_box_t1`、9 件、價 200。

**D. 櫃台塞滿**
- 前台先擺滿（例：3 個 6×4 箱）→ log `[CF-HIGHWAY] counter full, skipped cf_cmd_box_*`；
  箱子不在背包／後台，沒有 `?` 物品。

**E. 同日重載**
- 同一天存檔→重載：log 出現 `roll cached`，**不再加第二個 client／box**。

**F. log 總檢查**
- 沒有 `cf_` 的 `Could not find GameItem identifier`、沒有 `Init failed`、`Harmony: 8`。
```powershell
Select-String -Path "$env:USERPROFILE\AppData\LocalLow\Questing Goose Studio\Probably Stolen\Player.log" `
  -Pattern 'v0\.8\.0|CF-HIGHWAY|CF-LOOT|overflow box=|Init failed|Harmony' |
  ForEach-Object { $_.Line }
```

## 5. 環境與工具（照這個跑）

- 遊戲路徑：`C:\Program Files (x86)\Steam\steamapps\common\Probably Stolen`。
- 存檔／log：`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen\`。
- 四條命令（本機沒有 `pwsh` 7，用 Windows PowerShell）：
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Build-Mods.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Verify-Mods.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Probe-Patches.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Probe-Logic.ps1
```
- 離線檢查：
```powershell
python .scratch\p5sawyer\check.py        # 108/0
python .scratch\p5loot\parse_code.py     # VALUE-FLOOR + CMD T2 -0.77
python .scratch\review\ev_check.py       # 11 張表 EV（驗收方腳本）
python .scratch\p6highwayman\check.py    # P6 88/0 + 10 萬日機率模擬
```
- 沙箱：本 session 有時是 read-only；Build 要寫 `dist/`、Verify/Probe 要寫 `.tools/`，被擋就對同一條命令提權一次（workspace-write），不要改路徑繞過。
- Mono 的 `internal call`／out-of-sync 警告是既有環境問題；看 `RESULT: PASS` 與 patch 清單。
- PowerShell 主控台是 cp1252，直接 `print` 中文會炸；用 `read` 工具或 `encode('ascii','backslashreplace')`。

## 6. 設計決策／坑（不要重做）

- **紙本**：遊戲 build 沒有 `Resources/PaperUI/PaperAssetCatalog`，`PaperAssets.Resolve` 找不到 vanilla key。解法是 `CfPaperSprites` 在執行期產生 `notebook_manual`＋4 個按鈕 sprite，塞 `LoadedMod.PaperSprites`。不要改成「關掉 chrome」——PaperUI 沒有 Esc 關書。
- **CMD 箱染色**：0.7.3 的「中性色才轉白」把治安箱的飽和淡藍亮部擋掉；0.7.4 純亮度像打光；0.7.5 gamma 太亮／幽靈；**0.7.6/0.7.7 的 HSV 換色才是對的**：`BlueHue=0.62`、`BlueValueGain=1.00`，`s<0.15` 白標籤與綠燈保留。不要回去 luminance/tint。
- **分組**：降冪、組內取最低（最後一張）、所有組再按箱子等級高→低；cap 只做最高級。不要改回升冪。
- **容量**：用實際 `itemShape` bounding box；SEC T3 13×8=104（worst 69）、CMD T2 12×9=108（93）、CMD T3 15×9=135（105）、SEC T2 10×8=80（worst 93，例外：rare 先放、只掉 common）。`smg`／`shotgun` 在 SEC T3／CMD T2／CMD T3 只留 rare；EV 不變。
- **Fill**：先 roll 全部 → rare 先放 → common 按面積大→小；overflow 每箱一行；null roll 印 `spawn short`。
- **RNG**：`LootRegistry.Pick` 用 `UnityEngine.Random.value`，有放回；4 根警棍是正常（0.16%），`stun_baton` unitValue 165，900 是售價加成，不是 bug。
- **升級 gate**：`ModHelper.IsNetworkUpgradeUnlocked`（state==2），不要用 state==1。
- **時序**：`PlayerStore.EndNight()` 先 `FireOnHandlingNightlyServicesEarly()`（1723）再 `HandleJanitorial()`；`Reset()` 不清 `itemsBuffer`，`TransferBuffer()` 在 `ClearTrash()` 後。索耶的箱子就是靠這個順序才不會被清掉。
- **Diag**：`_diag`（skip/box/fill）與 `_removalLogged` 是 process-static；同一 process 第二個存檔不會再印，不等於沒執行。
- **Trust**：每次 DLL hash 變（0.7.3→0.7.8 每次）都要重新核可；沒核可 log 不會有新版號。換檔要完全退出遊戲。

## 7. 文件與已知不一致

- `docs/P5_HANDOFF.md`：有「0.7.8 狀態」欄；關閉條件已含 P5-04（`89bd79e`）。
- `docs/CARD_CRAFT_STATUS.md`：build 表 0.8.0／79,360／`8E6238AF…`；Mods 行仍寫 0.7.7（0.8.0 待覆蓋）。
- `docs/CARD_CRAFT_SPEC.md`：§3.3 容量數字 69／105／93、SEC T2 80<93 例外；§3.12.2 HSV 換色。
- **兩個舊 docs**（`ACCEPTANCE_REVIEW.md`、`DEVELOPER_DIAGNOSTIC.md`）自 11-02 未提交，是 AppraisalAssistant 的舊帳，非 P5；要嘛 commit 要嘛 revert，不要一直掛 `git status`。
- P5-14：CMD T2 統一 -0.77%（1285.55／1295.50）；舊 -7.37／-1.64 作廢。
- P5-15（未列入交辦單）：`P5_HANDOFF.md` 的歷史表保留原始清單，狀態看「0.7.8 狀態」欄，不要把「離線已關」重做。

## 8. P6（江洋大盜，0.8.0 已實作／離線已驗／實機待跑）

- P6 0.8.0 與 docs 同一提交（hash 見 `git log -1`）。
- 新增黑市 client `cf_highwayman`（江洋大盜）：每日以黑市聲望 roll 一次
  （30–59 8%、60–84 14%、85–149 20%、>=150 33%），成功才加 1 個 SELL client；
  到訪只給 1 箱 CMD 物資箱：T1 需 `CF_FENCE_PRO` + bm>=30、T2 加
  `CF_FENCE_T2`、T3 加 `CF_FENCE_T3` 且 bm>=85。CMD T1／T2／T3 因此有了正常來源。
- 離線已驗：`python .scratch\p6highwayman\check.py` 88/0 PASS；Harmony 仍 8 個、
  CMD 價格 200／350／500、容量與 loot 表不動；五個既有類別本體與 0.7.8 相同。
- 實機待跑：到訪／購買／開箱（T1／T2／T3）、同日只 roll 一次、前台塞滿 skip、
  bm<30 與未買 PRO 的 skip log。

## 9. 禁用

- 不要 push、不要啟動 Steam／遊戲、不要改使用者存檔／Steam registry／進程、不要改 `Assembly-CSharp.dll`。
- 不要為 P5 收尾新增 Harmony patch（目前 8 個）或新物品。
- 臨時腳本寫 `.scratch/`，不要編譯進 MOD；不要改 `dist/` 以外的產物。
- 讀檔再改；`git status` 只應留下那兩個舊 docs。

## 10. 如果要一句話回報

> P5 功能完成、離線面 0.7.8 已關、實機剩 P5-01／04／06／07；P6 未開始；HEAD `89bd79e`；dist 76,288／`EEBF0941…`；Mods 還是 0.7.7。
