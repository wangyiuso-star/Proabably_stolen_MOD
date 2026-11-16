# P5 收尾工作單（0.7.7 之後）

> 給 P5 實作者。這不是新功能，是把 P5 關閉前還沒被實機證明或還沒修乾淨的項補完。
> 前提：0.7.7 的 CMD 箱染色已由使用者實機確認；**P6 不開**。

## 0.7.8 進度（2026-11-17）

已離線關閉：

- **P5-02**：`Fill` 改成 rare 先放、common 再按面積大→小；SEC T2 80 < 93
  時只會掉 common。
- **P5-03**：容量數字改用 bounding box：rare-only 後 69／105／93（改池前
  105／168／147）；SEC T2 80 < 93 的例外已寫清楚。
- **P5-04**：MED／ENG／SER T3 用同一套 shape 複算 40／30／30，9×8 足夠；
  實機各 3 箱仍待測。
- **P5-05**：排序 tie-break 加 null 防護；overflow log 記
  `rolled/spawned/expected/actual/dropped`，null roll 另印 `spawn short`。
- **P5-13（P5 部分）**：類別註解改 high→low；STATUS Mods 行更新。
  兩個 11-02 舊 docs 仍未處理（非 P5）。
- **P5-14**：CMD T2 統一為 200 樣本 -0.77%。

仍待實機：**P5-01、P5-04、P5-06、P5-07**（P5 關閉必要；P5-04 離線已關，
只剩 MED／ENG／SER T3 各 3 箱），以及 P5-08／09／10；P5-11／12 留到 1.0 前。

## 已關閉（不要再改）

- **CMD 箱染色（0.7.7）**：HSV 換色 `H=0.62`、`V×1.00`，保留 S 與陰影；
  `s<0.15` 的白標籤與綠燈保留。實機 log：`hue 0.62, value x1`、
  `tint stats sec_box lum avg=0.24 max=0.69 bright=4%`、`applied cmd_box uid=…`。
- **換卡分組（0.7.1 實機）**：高→低排序、組內取最低、cap 先做最高級。
  0.7.1 log 已看到 `tier=3`（3 T3+1 T1）、`tier=2`（2 T1+3 T2）、
  `tier=1`（1 T1+2 T2）、`cap: 1 box/night`。
- **紙本 runtime sprites（0.7.3/0.7.7 實機）**：`[CF-PAPER] registered …`，
  沒有 `No sprite found`；書可開、版面可讀。
- **促銷 barter 移除（0.7.0/0.7.7 log）**：`removed promotional barters …` 有印。
- **T2 rare 先放（0.7.1 log）**：`cf_sec_t2_rare … remaining=0`。
- **CMD T1 箱填充（0.7.7 log）**：`fill table=cf_cmd_t1_common rolls=8 rare=cf_cmd_t1_rare`，
  沒有 overflow。

## 項目與 0.7.8 狀態（依 priority）

| ID | P | 項目 | 類型 | 0.7.8 狀態 |
|---|---|---|---|---|
| P5-01 | P0 | T3/T2 容量實機件數（0.7.2 尺寸從未開箱驗證） | 驗證 | **待實機** |
| P5-02 | P1 | `Fill` 的 rare 優先級在 0.7.2 被面積排序取代 | 修正 | 離線已關 |
| P5-03 | P1 | spec「最大面積」數字錯誤（57/87/77 vs 實際 69/105/93） | 文件 | 離線已關 |
| P5-04 | P1 | MED/ENG/SER T3 的容量沒有用同一套 shape 複算 | 驗證/修正 | 離線已關；**實機各 3 箱待** |
| P5-05 | P2 | `Fill` 的 `expected`/`actual` 對 null roll 算錯；sort tie-break 可能 NRE | 修正 | 離線已關 |
| P5-06 | P1 | 芯片升級（3 同級→下一級）完全沒有實機 log | 驗證 | **待實機** |
| P5-07 | P1 | 促銷 barter 移除後的 UI 即時性／重載復活 | 驗證 | **待實機** |
| P5-08 | P2 | 紙本 `ModItemDirectory` 首輪註冊時序、重載不重複 | 驗證 | 待實機（不擋 P5 關） |
| P5-09 | P2 | 交換所未解鎖時 `CF_SAWYER_*` 隱藏 | 驗證 | 待實機（不擋 P5 關） |
| P5-10 | P2 | CMD T1 箱 9 件、上鎖、T1 key 可開 | 驗證 | 待實機（不擋 P5 關） |
| P5-11 | P3 | `BM_DISTRUSTED`、箱內卡、垃圾桶滿三個邊界要定案 | 決策 | P3／1.0 前 |
| P5-12 | P3 | `_diag`／`_removalLogged` 的 process-static 範圍 | 文件 | P3／1.0 前 |
| P5-13 | P2 | 文件與註解同步（STATUS/ spec/ class comment/ 兩個舊 docs） | 文件 | P5 部分已關；兩個舊 docs 未處理 |
| P5-14 | P3 | CMD T2 的 `-7.37%` 與 `-1.64%` 兩個數字對不上 | 對帳 | 離線已關（-0.77%） |

> 0.7.8 只改離線面（P5-02／03／04 計算／05／13 P5 部分／14）。表格保留
> 原始「未關閉」清單是為了對照交辦歷史；**不要**把「離線已關」的項目重做。
> P5 關閉必要條件是 P5-01、P5-04 實機、P5-06、P5-07；P5-08／09／10 順手過即可。

---

### P5-01（P0）T3/T2 容量實機件數

**證據**：0.7.1 的 log 出現過
`[CF-LOOT] overflow table=cf_sec_t3_common item=stun_gun remaining=1; item destroyed …`，
SEC T3 9×8 掉件。0.7.2 改成 13×8／15×9／12×9＋大件先放＋smg/shotgun 只留 rare，
但**沒有 0.7.2 之後的開箱件數**；只有離線 packing 3000 次 0 失敗。

**要求**：實機開下列箱，逐箱數件數（`rolls + 1`）：

| 箱 | 件數 | 備註 |
|---|---:|---|
| `cf_sec_box_t3` ×5 | 5 | 之前掉件的家族 |
| `cf_cmd_box_t3` ×5 | 8 | 15×9 新尺寸 UI |
| `cf_cmd_box_t2` ×3 | 7 | 12×9 新尺寸 UI |
| `cf_cmd_box_t1` ×1 | 9 | 0.7.7 已 fill，未開 |

**驗收**：13 箱全部件數正確，且 log 沒有
`[CF-LOOT] overflow box=… expected=… actual=…`。若有 overflow，把整行貼回，
不要只放大箱；先看 dropped 的是哪個 id。

### P5-02（P1）rare 優先級

**證據**：0.7.2 的 `LootTables.Fill` 是「roll 全部 → 按面積大→小放」。
SEC T2 10×8 = 80 格；worst 6×glock（12 格）＋ rare shotgun 21 = 93，
或 6×glock＋ rare stun_gun 12 = 84。面積排序下，12 格的 rare 可能排在
六個 12 格 common 之後，掉的那件**可能是 rare**（0.7.1 的 rare-first 保證被拿掉）。
機率極低（glock 5%^6），但這是結構性回歸。

**要求**：`Fill` 先放 rare，再把 common 按面積大→小放；或排序 key 用
`(isRare ? 0 : 1, -area)`。容量不足時，寧可掉 common，不要掉 rare。

**驗收**：adversarial 模擬「6 glock + shotgun rare」→ `items=` 只能是 common；
實機 SEC T2 抽到 overflow 時也一樣。

### P5-03（P1）容量數字錯誤

**證據**：spec §3.3 第 28–29 行與 §3.3 容量段、§3.9.9 寫
「原始 85/136/112、rare-only 後 57/87/77」。用實際 `itemShape` 算：

- SEC T3：4×12 + rare 21 = **69**（不是 57）
- CMD T3：7×12 + 21 = **105**（不是 87）
- CMD T2：6×12 + 21 = **93**（不是 77）
- 改池前：SEC T3 105（不是 85）、CMD T3 168（不是 136）、CMD T2 147（不是 112）
- SEC T2 10×8 = 80 **小於**真實 worst 93（不是 77）；6 glock＋shotgun 仍放不下

容量結論（104≥69、135≥105、108≥93）不受影響，但數字要改成「理論 worst」
或標成「observed max」，並在 SEC T2 註明 adversarial 仍可能掉件。

**驗收**：文件數字與 shape 腳本一致；SEC T2 的例外寫清楚。

### P5-04（P1）MED/ENG/SER T3 容量複算

**證據**：這三家族仍是 9×8，dev 只寫「max 40/30/30」，但沒有列出
`module_extractor_advanced`、`turbo_booster_adv`、`advanced_flux_agent`、
`system_capped_neural_core`、`module_bay_expansion_kit` 等 id 的 shape。
若其中任何一個是 7×3，4 common + rare 可能超過 72。

**要求**：用同一套 66 存檔／332 id 的 shape 清單重算這三張表；
超過 72 就放大或降 rolls。實機各開 3 箱確認件數。

### P5-05（P2）`Fill` 診斷與 NRE

**證據**：`src/CardCraftAndFence/CardCraftAndFence.cs`：

- `int spanA = Math.Max(a.shape.width, a.shape.height);`：`ShapeArea` 有 null 防護，
  但 tie-break 沒有；shape 為 null 會 NRE。
- `expected = ids.Count` 把 `RollLootTable` 回 null 的 roll 也算進去；
  `actual = ids.Count - dropped` 不會扣掉 null；`DirectoryMaster.Item` 回 null
  時 `dropped` 也不加。日誌可能顯示 expected/actual 相同但箱子少件。

**要求**：排序 tie-break 先判 `shape == null`；只把非 null item 計入
`expected`，並分開記 `rolled`／`spawned`／`dropped`。

### P5-06（P1）芯片升級實機

**證據**：0.7.0–0.7.7 的 log 從來沒有 `[CF-SAWYER] chip`。

**要求**：四個晚上：

1. 3 個 T1 + 3 個 T2 同家族核心 → 只做 `1->2`，T2 那組不動。
2. 隔晚 3 個 T2 → `2->3`。
3. 隔晚 3 個 T3 → 不升（`chip=0`）。
4. 同晚 3 張 T1 卡 + 3 個 T1 核（BATCH 未買）→ `boxes=1 chip=1`
   （芯片不佔上限）。

**驗收**：`chip family=… 1->2 id=cf_crypto_module_…_t2`、`night … chip=1`。

### P5-07（P1）促銷 UI 即時性

**證據**：log 有 `removed promotional barters …`，但沒有 UI 觀察。

**要求**：用一個 `activePromotionalBarterIds` 含 `sec_crate_barter` 的存檔
（例如 `save_86`），買 PERMANENT：

1. 交易所面板的治安／醫療／工程箱條目是否**立即**消失。
2. 關閉再開面板、存檔重載、重啟 process 後是否復活。
3. `removed …` 是否每個 process 只印一次。

### P5-08（P2）紙本註冊時序

**要求**：買 PERMANENT 拿書 → 存檔重載 → 書還在、不重複；
log 沒有 `paper book item not registered yet`；`paper book granted` 只一次。
（0.7.7 書可開已看過，這條是時序與重載。）

### P5-09（P2）索耶升級隱藏

**要求**：新遊戲／交換所未解鎖的存檔 → Wild Network 制卡分頁**沒有**
`CF_SAWYER_*` 三張；log 有 `hide CF_SAWYER_* until the exchange is unlocked`。
解鎖交換所後重開，三張出現。

### P5-10（P2）CMD T1 箱

**要求**：開一個 `cf_cmd_box_t1`：9 件、上鎖、`cmd_keycard` 可開；
log 沒有 overflow。（0.7.7 已看到 fill，但沒開箱。）

### P5-11（P3）三個邊界要定案

1. **`BM_DISTRUSTED`**：vanilla `HandleJanitorial` 會跳過 `HandleExchange`，
   但索耶 handler 沒擋；目前會照吃垃圾桶的卡。要不要跟 vanilla 一致？
2. **箱內卡**：`FindAllChildrenType<GameItem>(trashcan)` 會掃到已交付箱內的
   鑰匙卡；留在垃圾桶過夜會被下一晚吃掉。這是設計還是要跳過容器內物品？
3. **垃圾桶滿**：`TransferBuffer` → `TryAcceptAllOrDestroy`，箱被丟棄時卡已消耗。
   文件已寫是原版行為，確認可接受即可。

### P5-12（P3）process-static 診斷

`_diag`（skip/box/fill）與 `_removalLogged` 是 static：同一 process 切換存檔時，
第二個存檔不會再印 skip／removal。行為正確、只是少 log；在 spec／STATUS 註明，
或改在 `OnGameLoadedLate`／`OnNewGameStarted` 清空（若要每次重印）。

### P5-13（P2）文件與註解

- STATUS 第 66 行仍寫「Mods 0.7.6／CAB94C59」；實際已安裝 0.7.7／`592F866F…`。
- `CfSawyerExchange` 類別註解（約 line 1920）仍寫
  「low tier before high」，0.7.1 起已是 high→low。
- spec §3.3 第 28–29 行、§3.3 容量段、§3.9.9 的 57/87/77 與 85/136/112（P5-03）。
- `docs/ACCEPTANCE_REVIEW.md`、`docs/DEVELOPER_DIAGNOSTIC.md` 自 11-02 起未提交；
  這是 AppraisalAssistant 的舊帳，決定 commit 或 revert，不要一直掛在 `git status`。

### P5-14（P3）CMD T2 EV 數字對帳

STATUS 寫「CMD T2 -7.37% 已接受」，spec 表寫 -1.64%；兩個數字沒有出處。
用同一套 200 樣本重跑一次，統一成一個數字；in-game `[CF-EV]` 仍缺。

---

## 已確認不是 bug（不要再改）

- **4 根警棍**：`LootRegistry.Pick` 用 `UnityEngine.Random.value`，有放回；
  SEC T3 警棍權重 20%，四連中機率 0.16%。`stun_baton` 的 `unitValue` 是 165，
  4 根＋1 張 T3 卡 = 885；你看到的 900 是售價加成（4×225）。不是價格或重複 bug。
- **T1 CMD 8 rolls + 1 rare**：0.7.7 log 正常，沒有 overflow。

## 關閉 P5 的條件

P5-01、P5-04、P5-06、P5-07 實機通過；P5-02／03／05／14 已離線關，
其餘可留到 1.0 前。
P6 不開，不要為這些項目新增 patch 或新物品。
