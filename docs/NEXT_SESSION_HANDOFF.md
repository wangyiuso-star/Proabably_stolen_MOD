# 下一個對話交接（Probably_Stolen_MOD）

> 寫於 2026-11-17（Asia/Hong_Kong）。給接手的 agent／對話：
> 先讀這份，再讀 `docs/P5_HANDOFF.md`、`docs/CARD_CRAFT_STATUS.md`、`docs/CARD_CRAFT_SPEC.md`。
> 本專案是 MOD 專案，不是遊戲本體；不要啟動 Steam／遊戲，實機由使用者跑。

## 0. 一句話現況

- **P8 0.10.0 已實作、離線驗收通過、實機尚未跑**（9 個升級 normal base、小偷
  賣卡、製卡匠 3 同家族核心）。
- **P7 0.9.0 A–F 仍待跑**；P7 洗白行為沒有被 P8 改動，排在 P8 之後。
- 版本 **0.10.0**（`82,432 bytes／FCECBE0A…`）；dist 已建好；**未 push**。
- 遊戲 `Mods` 目前仍是 **0.8.0**（`79,360／8E6238AF…`）；要測 0.10.0 要先覆蓋
  dist、完全重開並重新核可。
- **P6 實機現況不變**：核心觸發／T3 箱已通過實機；6／8 件數未核實（非阻塞）；
  T1／T2／低機率／櫃台塞滿／同日重載未測。
- **P5 已關閉**（P5-01／04／06／07、P5-08 於 2026-11-17 通過；P5-09／10 未測；
  P5-11／12 延後到 1.0）。
- P8 code commit `c29ccce`（docs 另 commit）；P7 code commit `9d4b307`。

## 1. Repo／產物

| 項目 | 值 |
|---|---|
| HEAD | P8 docs commit 後以 `git log -1` 為準；P8 code commit `c29ccce` |
| 0.10.0 實作 | `c29ccce`（code；docs 另 commit） |
| 0.9.0 實作 | `9d4b307`（code；docs 另 commit） |
| 0.8.0 實作 | `479c270`（P6 江洋大盜，含 docs） |
| 0.7.8 實作 | `7f10b7c`（+ `8256e40` STATUS） |
| 0.7.7 | `3e1c4fa` |
| 0.7.6–0.7.3 | `225f17b`／`c59107c`／`d13d49d`／`4fe6b3f` |
| dist DLL | `dist/CardCraftAndFence/CardCraftAndFence.dll`，82,432 bytes，SHA256 `FCECBE0A5B6FAA85ABFAE54699560FB5BEB5F193D3E3AB287EBE18833C58EB7E` |
| 已安裝 | `.../Probably Stolen/Mods/CardCraftAndFence`，**0.10.0 待覆蓋**；目前仍是 0.8.0，79,360／`8E6238AF…` |
| git status | P8 code `c29ccce` 與 docs 已提交；工作區只剩 `docs/ACCEPTANCE_REVIEW.md`、`docs/DEVELOPER_DIAGNOSTIC.md` 兩個 11-02 舊修改（未提交） |
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
- P5-04 MED／ENG／SER T3 的 40／30／30 離線複算；**2026-11-17 實機各 3 箱已通過**。

**歷史：P5 關閉必要實機（4 項；2026-11-17 已通過）**
1. **P5-01**：T3 SEC ×5（5 件）、T3 CMD ×5（8 件）、T2 CMD ×3（7 件）；無 `[CF-LOOT] overflow box=…`。
2. **P5-04**：T3 MED／ENG／SER 各 3 箱（各 5 件），同樣看 overflow。
3. **P5-06**：芯片 3 同級→下一級、每家族每晚 1 次、T3 不升、不佔 cap。
4. **P5-07**：促銷 UI 立即消失／重開／重載／重啟不復活。

**歷史：不擋 P5 關（P5-08 已通過；P5-09／10 未測）**
- P5-08 紙本重載不重複、P5-09 `CF_SAWYER_*` 隱藏、P5-10 CMD T1 開箱 9 件。

**1.0 前**
- P5-11 `BM_DISTRUSTED`／箱內卡／垃圾桶滿的決策；P5-12 `_diag`／`_removalLogged` 的 process-static 範圍。

## 3. 接手第一步（照順序）

1. 讀 `docs/P5_HANDOFF.md` 與 `docs/CARD_CRAFT_STATUS.md` 的「P8 0.10.0」段。
2. 確認 `git log -1`、`dist` hash、`Mods` 版本；不要假設 0.10.0 已安裝。
3. 把 `dist\CardCraftAndFence\*`（82,432／`FCECBE0A…`）複製到 `Mods`，
   完全退出遊戲再開、重新核可。
4. 啟動 log 應有 `Init … v0.10.0`、`enabled v0.10.0`、
   `[CF-NET] registered 9/9`、`Harmony: 9`；沒有 `Init failed`。
5. 接著跑 §4d 的 A–F；P8 不過 → 把 `Player.log` 原文貼進 work item，
   不要為了過而改數字或放寬 gate。
6. P8 之後再跑 §4c 的 P7 A–F（P7 洗白行為未被 P8 改動）。

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
- BM 數值上限是 200（`StoreReputation.CheckReputation` 的 clamp）；若存檔／debug
  仍到不了 150，先回報「設計前提不成立」，**不要自行降門檻**。

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

## 4c. P7 實機測試卡（0.9.0，人類跑；agent 不啟動遊戲）

安裝：
- 完全退出遊戲 → 備份 `AppData\LocalLow\Questing Goose Studio\Probably Stolen\save_*.es3`、`SaveFile.es3`。
- 複製 `dist\CardCraftAndFence\*`（81,408／`672BB474…`）到
  `...\Probably Stolen\Mods\CardCraftAndFence\`；重新核可。
- 啟動 log 要有 `Init … v0.9.0`、`enabled v0.9.0`、
  `[CF-NET] registered 9/9`、`Harmony: 9`；沒有 `Init failed`。

**A. 開箱繼承（P7-1）**
- 用小偷或江洋大盜買一個上鎖的贓物箱；記下開箱前的箱子熱度（tooltip／現有 log）。
- 用對應鑰匙卡打開：log 應有
  `[CF-LAUNDRY] open box=<id> heat=<n> children=<n>`。
- 箱子本身不再帶 `STOLEN_TAG`（不再被查）；箱內每件 loot 熱度 =
  `max(原 childHeat, boxHeat)`。
- 反向：索耶換卡產出的乾淨箱開箱時**不應**出現 `[CF-LAUNDRY] open`，
  箱內物件維持乾淨。

**B. FAST 衰減**
- 已買 FAST，帶一件熱度 100 的贓物睡一晚 → 熱度應 **64**（18×2）。
- 未買 FAST 的對照存檔／角色 → 熱度 **82**（18）。
- log 應有 `[CF-LAUNDRY] night day=<d> items=<n>`；同一天存檔→重載→再睡，
  不應再扣第二次（`laundry_last_day`）。
- 帶 `CONTRABAND_LEVEL_*` 的物件：兩趟各減 2／4／6／8，合計 32／28／24／20。

**C. CLEAN 購買**
- 已買 CLEAN，買一個帶箱內贓物的箱子（或直接買贓物）→ log
  `[CF-LAUNDRY] bought clean id=<id> children=<n>`。
- 買到的箱子熱度 0、`STOLEN_TAG` 消失、箱內 children 全部乾淨。
- 已存在的舊贓物不受影響，照常每晚衰減；買乾淨物品不應印這行。

**D. 未買升級／未開箱轉賣**
- 未買 FAST／CLEAN：贓物照原版 18／晚衰減、買進仍是贓物；不應有 P7 洗白 log。
- 把未開箱的贓物箱直接賣給客戶：買方拿到的是熱箱，**不繼承、不被洗白**；
  log 不應有 `[CF-LAUNDRY] open`。

**E. UI**
- CardFence 分頁應有 **9 個**升級（7 舊 + 快速洗白 + 彻底洗白）；UI 依
  subtitle 分成 4 張卡片（Cardmaker、Thief、索耶、洗白），洗白那張有 2 個格子。
- log 應有 `[CF-NET] registered 9/9 CardFence upgrades tab=CardFence width=792/1060`
  與 hide 那行。
- 仍是 3 個分頁、792/1060、**沒有第 4 個分頁**、沒有卡片重疊／overflow；
  CLEAN 在 FAST 未買時應鎖定。

**F. 存讀檔**
- 買 FAST／CLEAN 後存檔→重載：升級仍為已買、`9/9` log 重印、效果持續。
- 同一天（`dayCounter` 不變）存檔→重載→再睡：`laundry_last_day` 不讓同一天
  扣第二次。
- 贓物熱度、箱子 `STOLEN_TAG`、箱內 children 狀態都應正確保存。

log 過濾：
```powershell
Select-String -Path "$env:USERPROFILE\AppData\LocalLow\Questing Goose Studio\Probably Stolen\Player.log" `
  -Pattern 'v0\.9\.0|CF-LAUNDRY|registered 9/9|Harmony' |
  ForEach-Object { $_.Line }
```

## 4d. P8 實機測試卡（0.10.0，人類跑；agent 不啟動遊戲）

安裝：
- 完全退出遊戲 → 備份 `AppData\LocalLow\Questing Goose Studio\Probably Stolen\save_*.es3`、`SaveFile.es3`。
- 複製 `dist\CardCraftAndFence\*`（82,432／`FCECBE0A…`）到
  `...\Probably Stolen\Mods\CardCraftAndFence\`；重新核可。
- 啟動 log 要有 `Init … v0.10.0`、`enabled v0.10.0`、
  `[CF-NET] registered 9/9`、`Harmony: 9`；沒有 `Init failed`。

**A. 價格 base／hard**
- normal 難度 9 個升級：350／350／900／500／400／500／700／350／1800
  （PRO／T2／T3／THIEF_T2／PERMANENT／BATCH／CHIP／FAST／CLEAN）。
- hard 難度：700／700／1800／1000／800／1000／1400／700／3600（同順序）。
- MOD 不應另外加價或打折；hard 的 ×2 由遊戲自己算。

**B. 小偷 T2 卡＋箱（bm 60）**
- T2 卡先上架，再上 T2 箱：log 依序
  `[CF-THIEF] card family=<F> tier=2 id=… price=… added=true`、
  `[CF-THIEF] box family=<F> tier=2 …`。
- 卡價 = `CardPrice × 1.5`（SEC 225、MED／ENG 165、SER 90）；高價卡不應被箱
  或原版商品擠掉。用那張卡開同家族 T2 箱。

**C. 小偷 T3 卡＋箱（bm 85＋`CF_FENCE_T3`）**
- log 順序是 T3 卡 → T2 卡 → 箱；三件都應在 18×4 櫃台上。
- T3 卡 338（SEC）／248（MED／ENG）／135（SER）；T2 卡 225／165／165／90。
- 箱子仍只有 1 個。

**D. 製卡匠核心**
- 到訪時櫃台先出現 **3 顆同家族、最高已解鎖等級**的核心（同一天 3 顆）。
- log 不應有 `[CF-STOCK] counter full, skipped <core id>`（核心先上架）。
- 已買 `CF_SAWYER_CHIP` 時，3 顆同級核心可換 1 顆下一級（索耶，隔夜）。

**E. 存讀檔**
- 買卡／買升級後存檔→重載：升級仍已買；卡與箱的 `unitValue`、`STOLEN_TAG`
  保持一致。
- P7 的 A–F（§4c）另外照舊跑一輪，不要因為 P8 跳過。

**F. log 總檢查**
- 沒有 `cf_` 的 `Could not find GameItem identifier` dummy、沒有 `Init failed`、
  `Harmony: 9`、`registered 9/9`。
```powershell
Select-String -Path "$env:USERPROFILE\AppData\LocalLow\Questing Goose Studio\Probably Stolen\Player.log" `
  -Pattern 'v0\.10\.0|CF-THIEF card|CF-STOCK|registered 9/9|Harmony' |
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
python .scratch\p7laundry\check.py       # P7 107/0
python .scratch\p8balance\check.py       # P8 111/0（0.10.0 價格／小偷卡／核心順序）
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
- `docs/CARD_CRAFT_STATUS.md`：build 表 0.10.0／82,432／`FCECBE0A…`；Mods 行仍是
  0.8.0（0.10.0 待覆蓋）。
- `docs/CARD_CRAFT_SPEC.md`：§3.3 容量數字 69／105／93、SEC T2 80<93 例外；
  §3.12.2 HSV 換色；**§3.14 P7**（開箱繼承＋兩級洗白；其 §3.14.6 的輸出是
  P7 當下 0.9.0）；**§3.15 P8**（9 個 base 價、小偷卡、核心順序）。
- **兩個舊 docs**（`ACCEPTANCE_REVIEW.md`、`DEVELOPER_DIAGNOSTIC.md`）自 11-02 未提交，是 AppraisalAssistant 的舊帳，非 P5；要嘛 commit 要嘛 revert，不要一直掛 `git status`。
- P5-14：CMD T2 統一 -0.77%（1285.55／1295.50）；舊 -7.37／-1.64 作廢。
- P5-15（未列入交辦單）：`P5_HANDOFF.md` 的歷史表保留原始清單，狀態看「0.7.8 狀態」欄，不要把「離線已關」重做。

## 8. P6（江洋大盜，0.8.0 已實作／離線已驗／實機部分通過）

- P6 實作 `479c270`；P6 docs 修正 `136f77e`。
- 新增黑市 client `cf_highwayman`（江洋大盜）：每日以黑市聲望 roll 一次
  （30–59 8%、60–84 14%、85–149 20%、>=150 33%），成功才加 1 個 SELL client；
  到訪只給 1 箱 CMD 物資箱：T1 需 `CF_FENCE_PRO` + bm>=30、T2 加
  `CF_FENCE_T2`、T3 加 `CF_FENCE_T3` 且 bm>=85。CMD T1／T2／T3 因此有了正常來源。
- 離線已驗：`python .scratch\p6highwayman\check.py` 88/0 PASS；Harmony 仍 8 個
  （P7 起 9）、CMD 價格 200／350／500、容量與 loot 表不動；五個既有類別本體與
  0.7.8 相同。
- 實機現況（2026-11-17）：核心觸發（到訪／對話／買箱）與 T3 箱已通過；
  **6／8 件數未核實、不阻斷**；T1／T2 箱、低檔 chance（30–59）、前台塞滿、
  同日重載、未買 PRO／bm<30 的 skip log 尚未測。
- 靜態核對：BM 上限是 200（`StoreReputation.CheckReputation` 把 amount 夾在
  −200～200，StoreReputation.cs:120），所以 150 在數值範圍內；`PlayerStore.EndDay()`
  會 `DismissClientStack()` 清空 `clientStack`（PlayerStore.cs:1651／
  StoreClientManager.cs:1489），未接待的江洋大盜不會跨日堆疊。兩者仍待實機確認。

## 8b. P7（贓物熱度，0.9.0 已實作／離線已驗／實機待跑）

- 0.9.0；`ModVersion` 與 manifest 都已改 0.9.0；`dist` 81,408 bytes
  （`672BB474…`）。（P8 起是 0.10.0／82,432／`FCECBE0A…`，見 §8c。）
- P7-1：`CfTryOpenLockPatch.Prefix` 在 `canOpen` 內、`UnlockContainer` 之前，
  只對有 `STOLEN_TAG` 的 CF 箱，把 `boxHeat` 以 `Math.Max` 繼承給遞迴子物品，
  最後 `RemoveStolenStatus(box)`；P4 小偷 T2／T3 CF 箱／P6 CMD 箱（原版 T1
  箱不覆蓋）的行為因此刻意改變。
- FAST：`CfLaundry` 在 `OnHandlingNightlyServicesLate`（`PlayerStore.cs:1744`，
  原版迴圈 1757 之前）多跑一趟 `UpdateStolenItem`，18→36／晚；
  `laundry_last_day` 擋同日重複。無新 patch。
- CLEAN：新 patch `CfLaundryBuyPatch`（`PlayerStore.BuyItem` postfix，8→9），
  購買後物品＋子物品 `RemoveStolenStatus`；只影響今後購買。
- 兩個升級 `CF_LAUNDRY_FAST` 1200／`CF_LAUNDRY_CLEAN` 3000（前置 FAST），
  subtitle `洗白`，註冊 `N/9`。**P8（0.10.0）起 base 改成 350／1800
  （hard 700／3600），行為不變**，見 §8c。
- 離線已驗：Build exit 0（3 DLL）、Verify PASS（9 patches，含
  `CfLaundryBuyPatch -> PlayerStore.BuyItem`）、Probe PASS
  （實際 **8／4／9**；AppraisalAssistant 歷來 8 個、DLL 位元相同，不要改）、
  Logic PASS、`p7laundry/check.py` 107/0、`p6highwayman/check.py` 88/0
  （標題 P8 起改印 0.10.0）。
- 實機待跑：§4c A–F（開箱繼承／FAST 64 vs 82／CLEAN 購買／未買升級與未開箱
  轉賣／9 卡 UI／存讀檔）。
- 未核實：UI 9 卡版面與 overflow、存讀檔保留、runtime 熱度 100→64／82 都還沒
  在遊戲內看過。

## 8c. P8（經濟平衡，0.10.0 已實作／離線已驗／實機待跑）

- 0.10.0；`ModVersion` 與 manifest 都已改 0.10.0；`dist` 82,432 bytes
  （`FCECBE0A5B6FAA85ABFAE54699560FB5BEB5F193D3E3AB287EBE18833C58EB7E`）。
- **價格**：`CfNetwork` 只改 9 個 base 常數：PRO 350、T2 350、T3 900、
  THIEF_T2 500、PERMANENT 400、BATCH 500、CHIP 700、FAST 350、CLEAN 1800；
  hard 由遊戲自動 ×2（700／700／1800／1000／800／1000／1400／700／3600）。
  沒有難度 API、沒有預先加倍；沒有改原版箱／卡／核價。
- **小偷賣卡**：`CfThiefBoxes.Offer` 在建箱前 `OfferCards(family, tier)`；
  T3 → T3 卡 → T2 卡 → 箱，T2 → T2 卡 → 箱。同家族（非 CMD）、
  `price = (long)Math.Round(CardPrice * 1.5f)`、`isStolen:true`；
  `CanAccept` 失敗 `Destroy()`＋`[CF-THIEF] card full, skipped <id>`；
  `OfferCard` 包 try/catch，卡失敗不中止箱子；箱仍每次最多 1 個。
- **製卡匠核心**：`AddMakerStock` 先上 3 顆**同家族**核心
  （`HighestCoreTier()`、`start = ((day % 5) + 5) % 5`），再 printer／chip／
  CNC／材料；無 chip／CNC 8 件、全補 10 件（上限 11），核心不會被 skip。
- 沒有新 Harmony patch（仍 9）、沒有新物品／loot；P7 laundry 行為不動，
  只有 FAST／CLEAN 價格改 350／1800。
- 離線已驗：Build exit 0（3 DLL）、Verify PASS（9 patches，沒有新 patch，
  清單同 0.9.0）、Probe PASS（實際 **8／4／9**；P8 交辦單本身也是 8／4／9）、
  Logic PASS、`p8balance/check.py` 111/0、`p6highwayman/check.py` 88/0、
  `p7laundry/check.py` 107/0（標題都已 0.10.0）。
- P6／P7 檢查的最小更新：P7 價格期望改 350／1800、版本 0.10.0；兩支都只剝掉
  P8 的 `OfferCards`／`OfferCard` 再比舊 `CfThiefBoxes`，通過數不變。
- 實機待跑：§4d A–F（base／hard 價格、T2／T3 卡＋箱、3 核心、存讀檔、log 乾淨）。
- 未核實：hard 實際顯示、卡在 18×4 的排版與優先順序、核心 skip 行為、1.5× 的
  `.5` 進位顯示、存讀檔後 stolen 狀態、整體經濟曲線（沒有 runtime 模擬）。

## 9. 禁用

- 不要 push、不要啟動 Steam／遊戲、不要改使用者存檔／Steam registry／進程、不要改 `Assembly-CSharp.dll`。
- 不要為 P5 收尾新增 Harmony patch（P7 起 9 個）或新物品。
- 臨時腳本寫 `.scratch/`，不要編譯進 MOD；不要改 `dist/` 以外的產物。
- 讀檔再改；`git status` 只應留下那兩個舊 docs。

## 10. 如果要一句話回報

> P8 0.10.0 已實作、離線驗證通過（111/0、Verify 9、Probe 8／4／9）、
> 實機尚未跑；dist 82,432／`FCECBE0A…`；Mods 仍 0.8.0（0.10.0 待覆蓋）；
> P7 A–F 仍待跑；P6 狀態不變（核心觸發／T3 箱已過實機、6／8 件數未核實）；
> P5 已關閉；未 push。
