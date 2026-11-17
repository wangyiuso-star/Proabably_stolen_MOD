# 製卡與銷贓（CardCraftAndFence）規格

> §3.1–3.11 全文仍未補齊。本文件目前包含：P1/P2 已定案事實、
> §3.3 箱體定案表（含 T3 箱）與 loot 表（含 T3 loot／EV）、貼圖決策與 errata、
> §3.4b 的 T3 卡／核定價、§3.5 的 P3 實作（空白卡晶片、專業密序器、批量模組），
> 以及 P4 的 §3.8（退休製卡匠／博士）、§3.10(2)（小偷分級箱）、
> §3.11（Wild Network 9 個升級）、§3.12（CMD 箱執行期染色貼圖）、
> P5 的 §3.9（索耶換卡／芯片／常駐服務／紙本目錄）、P6 的 §3.13（江洋大盜），
> 和 P7 的 §3.14（贓物熱度：箱子繼承＋兩級洗白）。

## §3.3 箱子 — 定案表（P2／P4 已實作）

| 家族 | T1 id | T1 容量 | T1 價 | T2 id | T2 容量 | T2 價 | T3 id | T3 容量 | T3 價 | sprite |
|---|---|---|---:|---|---:|---:|---|---:|---:|---|
| SEC | `sec_box`（原版） | 7×5 | 200 | `cf_sec_box_t2` | **10×8**（0.7.2 起） | 225 | `cf_sec_box_t3` | **13×8**（0.7.2 起） | 300 | `sec_box` |
| MED | `med_box`（原版） | 7×5 | 200 | `cf_med_box_t2` | **8×8** | 225 | `cf_med_box_t3` | 9×8 | 300 | `med_box` |
| ENG | `eng_box`（原版） | 7×5 | 200 | `cf_eng_box_t2` | 7×7 | 225 | `cf_eng_box_t3` | 9×8 | 300 | `eng_box` |
| SER | `service_box`（原版） | 7×5 | 175 | `cf_ser_box_t2` | 7×7 | 225 | `cf_ser_box_t3` | 9×8 | 300 | `service_box` |
| CMD | `cf_cmd_box_t1`（新） | **10×8**（0.7.2 起） | 200 | `cf_cmd_box_t2` | **12×9**（0.7.2 起） | 350 | `cf_cmd_box_t3` | **15×9**（0.7.2 起） | 500 | `cmd_box`（執行期染色，§3.12） |

- **容量校正（歷史）**：MED T2 用 8×8、CMD T1 用 7×7、CMD T2 用 9×8，
  是舊版 10／11 rolls 的 fit 結果。0.5.7 新池件數變少（T2 4–7、T3 5–8），
  容量維持不動；容量只影響「放不放得下」，不改 EV／價與件數。
- **0.7.2 容量重算（fit）**：實機 SEC T3 掉件；用 66 個存檔的實際 `itemShape`
  面積＋10 萬次 rolls 模擬重算。重點：
  - `LootTables.Fill` 改成**先 roll 全部、rare 先放、common 再按 shape 面積
    大→小放入**（遊戲的 grid 放置本身會試 4 種旋轉＋2 種翻轉）；`smg`／
    `shotgun`（7×3）從 SEC T3／CMD T2／CMD T3 的 **common** 池移除，只留 rare。
  - 原始 worst-case（7×3 還在 common、用 bounding box）是 SEC T3 105、
    CMD T3 168、CMD T2 147；移到 rare-only 後降到 69／105／93。
  - 容量：SEC T2 10×8、CMD T1 10×8、CMD T2 12×9、SEC T3 13×8、CMD T3 15×9；
    MED／ENG／SER 不變。9 高是因為 6–7 個 4×3 common ＋ 1 個 7×3 rare 需要
    3～4 排。**SEC T2 10×8=80 仍小於 worst 93**；0.7.8 起 rare 先放，容量
    不足時只會掉 common。
  - 三張表權重重配後 **common EV 與整箱 EV 完全不變**（SEC T3 238.50、
    CMD T2 172.75、CMD T3 252.75；model EV/價仍是 4.068／3.701／4.117）。
  - overflow 改成**每箱一行**：`[CF-LOOT] overflow box=… expected=… actual=…
    dropped=… items=…`；不再只在同一張表第一次掉件時印。
- T1 SEC／MED／ENG／SER 沿用原版 id，不重新註冊、不加 `CF_CRATE_*`，
  走原版 `LockHelper.TryOpenLock`；原版價格就是 200／200／200／175。
- **§3.3 T1 價格 errata**：spec 表寫 T1 一般箱 150，與「T1 沿用原版 id」衝突；
  以代碼為準，T1 實際是 SEC／MED／ENG = 200、SER = 175，不能改。
  T1 CMD 是新物品，用 200；T2 才用 225／350。
- 新箱加 `CF_CRATE_FAMILY`、`CF_CRATE_TIER`；`LOCK_ID` 由
  `LockHelper.InitLockedContainerItem` 設定，`LockUpContainer` 上鎖。
- **T3 箱（P4 新增）**：五家族 id `cf_*_box_t3`，0.7.2 起一般 9×8
  （SEC 13×8、CMD 15×9）、價 300／500；0.5.7 起 rolls 4／4／4／4／7
  （件數 5／5／5／5／8）。`CF_CRATE_TIER=3`、同一組 `LOCK_ID_*`；
  T3 卡（`CF_CARD_TIER=3`）可開。T3 loot 見下一節。
- **CMD 箱來源**：`cf_cmd_box_t1/t2/t3` 在「江洋大盜」（P6）之前**沒有正常
  來源**（小偷只賣 SEC／MED／ENG／SER），先留著給 P6 與 F9 測試用。

## §3.3 Loot — 原版基準、校準與實作

### 原版基準（runtime `[CF-EV]`，200 samples/box）

`CfEvProbe`（已移除）當時對原版箱各生成 200 個，統計直接子物品 `unitValue` 平均；
`CloseContentWindow(true)` 後 `Destroy()`，不存檔。

| 原版箱 | mean | 原版價 | EV/價 |
|---|---:|---:|---:|
| `sec_box` | 698.91 | 200 | 3.49 |
| `med_box` | 706.95 | 200 | 3.53 |
| `eng_box` | 604.42 | 200 | 3.02 |
| `service_box` | 458.11 | 175 | 2.62 |

> 上一版提案的 MED T1 0.96 是**靜態提案表**的數字，不是 runtime；實測原版
> `med_box` 是 3.53。T1 SEC／MED／ENG／SER 箱子是原版物品、內容由
> `Fill*` 生成，`RegisterLootTable` 不會呼叫；因此 T1 表已刪除，只保留這張
> runtime 基準。T1 CMD（`cf_cmd_box_t1`）是新物品，下表生效。

### 價值地板（0.5.7 起，取代「T1 池重配權重」）

T2／T3 不再把 T1 的低價雜物（繃帶、止痛藥、廢金屬、種子、零食）放進池子再調權重。
改成**硬地板**：

- T2 common：`unitValue >= 60`，且必須符合同家族主題。
- T3 common：`unitValue >= 120`，同家族主題。
- rare：**高階鑰匙卡 + 該家族最高價裝備**；不再放核心。
- 10 張表（5 T2 + 5 T3）**完全移除** `crypto_module_*` 與
  `cf_crypto_module_*_t2/t3`；核心只由卡匠賣（§3.8.4）。

#### 候選清單（含 `unitValue`）

| 家族 | 候選 itemId（value） |
|---|---|
| SEC | `flashbang_grenade` 85、`smoke_grenade` 60、`stun_baton` 165、`combat_knife` 75、`tazer` 100、`glock_receiver` 200、`stun_gun` 250、`smg` 280、`shotgun` 310、`current_converter` 300 |
| MED | `salve` 80、`large_purple_injector` 160、`surgery_tool` 200、`blue_blood_bag` 250、`black_injector` 350 |
| ENG | `crowbar` 75、`welder` 80、`module_extractor` 100、`module_bay_expansion_kit` 150、`system_capped_neural_core` 250、`current_converter` 300、`turbo_booster_adv` 300、`advanced_flux_agent` 300、`module_extractor_advanced` 350 |
| SER | `module_bay_expansion_kit` 150、`current_converter` 300、`turbo_booster_adv` 300、`advanced_flux_agent` 300 |
| CMD | `flashbang_grenade` 85、`smoke_grenade` 60、`stun_baton` 165、`combat_knife` 75、`tazer` 100、`glock_receiver` 200、`stun_gun` 250、`smg` 280、`shotgun` 310、`current_converter` 300 |

T3 只從上表取 `>=120` 的項目（所以 SEC 不用 smoke／combat_knife／tazer，
MED 不用 salve，ENG 不用 crowbar／welder／module_extractor，CMD 不用
smoke／combat_knife／tazer）。

0.7.2 起 `smg`／`shotgun`（7×3）只出現在 rare：SEC T3／CMD T2／CMD T3 的
common 池已移除它們（容量與幾何原因，見 §3.3 容量段），權重已重配且 EV 不變。

### 生效表（T2 五箱 + CMD T1 + T3 五箱）

`EV = rolls × EV_common + 1 × EV_rare`；每表權重合計 100；件數 = rolls + 1。
rare 保底 1。

#### CMD T1

**CMD T1**（rolls 8；common EV 55.50、rare EV 238.75）

| itemId | value | weight | 池 |
|---|---:|---:|---|
| `heavy_pistol_ammo` | 30 | 25 | common |
| `heavy_pistol_ammo_p` | 36 | 15 | common |
| `flashbang_grenade` | 85 | 10 | common |
| `smoke_grenade` | 60 | 10 | common |
| `bandage_item` | 8 | 10 | common |
| `common_electronic` | 33 | 10 | common |
| `stun_baton` | 165 | 10 | common |
| `combat_knife` | 75 | 10 | common |
| `cmd_keycard` | 125 | 25 | rare |
| `stun_gun` | 250 | 15 | rare |
| `glock_receiver` | 200 | 10 | rare |
| `smg` | 280 | 10 | rare |
| `shotgun` | 310 | 20 | rare |
| `current_converter` | 300 | 20 | rare |

> 0.5.8 起 rare 移除 `crypto_module_cmd`；權重已重配到 keycard／shotgun／converter，
> EV/價維持 3.414。

#### T2

**SEC T2**（rolls 6；common EV 109.75、rare EV 208.50）

| itemId | value | weight | 池 |
|---|---:|---:|---|
| `flashbang_grenade` | 85 | 20 | common |
| `smoke_grenade` | 60 | 20 | common |
| `stun_baton` | 165 | 30 | common |
| `combat_knife` | 75 | 15 | common |
| `tazer` | 100 | 10 | common |
| `glock_receiver` | 200 | 5 | common |
| `cf_sec_keycard_t2` | 150 | 35 | rare |
| `glock_receiver` | 200 | 25 | rare |
| `stun_gun` | 250 | 30 | rare |
| `shotgun` | 310 | 10 | rare |

**MED T2**（rolls 4；common EV 152.50、rare EV 235.00）

| itemId | value | weight | 池 |
|---|---:|---:|---|
| `salve` | 80 | 40 | common |
| `large_purple_injector` | 160 | 30 | common |
| `surgery_tool` | 200 | 15 | common |
| `blue_blood_bag` | 250 | 10 | common |
| `black_injector` | 350 | 5 | common |
| `cf_med_keycard_t2` | 110 | 25 | rare |
| `surgery_tool` | 200 | 20 | rare |
| `blue_blood_bag` | 250 | 25 | rare |
| `black_injector` | 350 | 30 | rare |

**ENG T2**（rolls 3；common EV 190.75、rare EV 255.00）

| itemId | value | weight | 池 |
|---|---:|---:|---|
| `crowbar` | 75 | 15 | common |
| `welder` | 80 | 15 | common |
| `module_extractor` | 100 | 20 | common |
| `system_capped_neural_core` | 250 | 15 | common |
| `current_converter` | 300 | 15 | common |
| `module_extractor_advanced` | 350 | 10 | common |
| `turbo_booster_adv` | 300 | 5 | common |
| `advanced_flux_agent` | 300 | 5 | common |
| `cf_eng_keycard_t2` | 110 | 25 | rare |
| `system_capped_neural_core` | 250 | 25 | rare |
| `current_converter` | 300 | 20 | rare |
| `module_extractor_advanced` | 350 | 30 | rare |

**SER T2**（rolls 3；common EV 225.00、rare EV 180.00）

| itemId | value | weight | 池 |
|---|---:|---:|---|
| `module_bay_expansion_kit` | 150 | 50 | common |
| `current_converter` | 300 | 20 | common |
| `turbo_booster_adv` | 300 | 15 | common |
| `advanced_flux_agent` | 300 | 15 | common |
| `cf_ser_keycard_t2` | 60 | 50 | rare |
| `current_converter` | 300 | 20 | rare |
| `turbo_booster_adv` | 300 | 15 | rare |
| `advanced_flux_agent` | 300 | 15 | rare |

**CMD T2**（rolls 6；common EV 172.75、rare EV 259.00）

| itemId | value | weight | 池 |
|---|---:|---:|---|
| `flashbang_grenade` | 85 | 11 | common |
| `smoke_grenade` | 60 | 6 | common |
| `stun_baton` | 165 | 12 | common |
| `combat_knife` | 75 | 10 | common |
| `glock_receiver` | 200 | 12 | common |
| `stun_gun` | 250 | 29 | common |
| `current_converter` | 300 | 8 | common |
| `tazer` | 100 | 12 | common |
| `cf_cmd_keycard_t2` | 200 | 30 | rare |
| `stun_gun` | 250 | 20 | rare |
| `smg` | 280 | 20 | rare |
| `shotgun` | 310 | 30 | rare |

#### T3

**SEC T3**（rolls 4；common EV 238.50、rare EV 266.50）

| itemId | value | weight | 池 |
|---|---:|---:|---|
| `stun_baton` | 165 | 20 | common |
| `glock_receiver` | 200 | 19 | common |
| `stun_gun` | 250 | 31 | common |
| `current_converter` | 300 | 30 | common |
| `cf_sec_keycard_t3` | 225 | 30 | rare |
| `stun_gun` | 250 | 20 | rare |
| `smg` | 280 | 20 | rare |
| `shotgun` | 310 | 30 | rare |

**MED T3**（rolls 4；common EV 249.50、rare EV 239.50）

| itemId | value | weight | 池 |
|---|---:|---:|---|
| `large_purple_injector` | 160 | 20 | common |
| `surgery_tool` | 200 | 25 | common |
| `blue_blood_bag` | 250 | 25 | common |
| `black_injector` | 350 | 30 | common |
| `cf_med_keycard_t3` | 165 | 30 | rare |
| `surgery_tool` | 200 | 20 | rare |
| `blue_blood_bag` | 250 | 25 | rare |
| `black_injector` | 350 | 25 | rare |

**ENG T3**（rolls 4；common EV 250.00、rare EV 281.25）

| itemId | value | weight | 池 |
|---|---:|---:|---|
| `module_bay_expansion_kit` | 150 | 30 | common |
| `system_capped_neural_core` | 250 | 20 | common |
| `current_converter` | 300 | 20 | common |
| `module_extractor_advanced` | 350 | 10 | common |
| `turbo_booster_adv` | 300 | 10 | common |
| `advanced_flux_agent` | 300 | 10 | common |
| `cf_eng_keycard_t3` | 165 | 25 | rare |
| `current_converter` | 300 | 20 | rare |
| `module_extractor_advanced` | 350 | 30 | rare |
| `turbo_booster_adv` | 300 | 15 | rare |
| `advanced_flux_agent` | 300 | 10 | rare |

**SER T3**（rolls 4；common EV 262.50、rare EV 226.50）

| itemId | value | weight | 池 |
|---|---:|---:|---|
| `module_bay_expansion_kit` | 150 | 25 | common |
| `current_converter` | 300 | 25 | common |
| `turbo_booster_adv` | 300 | 25 | common |
| `advanced_flux_agent` | 300 | 25 | common |
| `cf_ser_keycard_t3` | 90 | 35 | rare |
| `current_converter` | 300 | 25 | rare |
| `turbo_booster_adv` | 300 | 20 | rare |
| `advanced_flux_agent` | 300 | 20 | rare |

**CMD T3**（rolls 7；common EV 252.75、rare EV 289.00）

| itemId | value | weight | 池 |
|---|---:|---:|---|
| `stun_baton` | 165 | 15 | common |
| `glock_receiver` | 200 | 5 | common |
| `stun_gun` | 250 | 44 | common |
| `current_converter` | 300 | 36 | common |
| `cf_cmd_keycard_t3` | 300 | 30 | rare |
| `stun_gun` | 250 | 20 | rare |
| `smg` | 280 | 20 | rare |
| `shotgun` | 310 | 30 | rare |

### 模型 EV / 200 樣本 / 目標

| 箱 | 價 | common EV | rare EV | rolls | 件數 | model EV | EV/價 | measured 200 | error | 目標 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| SEC T2 | 225 | 109.75 | 208.50 | 6 | 7 | 867.00 | **3.853** | 874.48 | +0.86% | 3.6–3.9 |
| MED T2 | 225 | 152.50 | 235.00 | 4 | 5 | 845.00 | **3.756** | 818.60 | -3.12% | 3.6–3.9 |
| ENG T2 | 225 | 190.75 | 255.00 | 3 | 4 | 827.25 | **3.677** | 805.62 | -2.61% | 3.6–3.9 |
| SER T2 | 225 | 225.00 | 180.00 | 3 | 4 | 855.00 | **3.800** | 838.20 | -1.96% | 3.6–3.9 |
| CMD T1 | 200 | 55.50 | 238.75 | 8 | 9 | 682.75 | **3.414** | 669.92 | -1.88% | 不變 |
| CMD T2 | 350 | 172.75 | 259.00 | 6 | 7 | 1295.50 | **3.701** | 1285.55 | -0.77% | 3.6–3.9 |
| SEC T3 | 300 | 238.50 | 266.50 | 4 | 5 | 1220.50 | **4.068** | 1207.85 | -1.04% | 3.9–4.5 |
| MED T3 | 300 | 249.50 | 239.50 | 4 | 5 | 1237.50 | **4.125** | 1214.38 | -1.87% | 3.9–4.5 |
| ENG T3 | 300 | 250.00 | 281.25 | 4 | 5 | 1281.25 | **4.271** | 1274.08 | -0.56% | 3.9–4.5 |
| SER T3 | 300 | 262.50 | 226.50 | 4 | 5 | 1276.50 | **4.255** | 1261.35 | -1.19% | 3.9–4.5 |
| CMD T3 | 500 | 252.75 | 289.00 | 7 | 8 | 2058.25 | **4.117** | 2050.30 | -0.39% | 3.9–4.5 |

T2 ≥ 同家族 T1 + 0.2：

| 箱 | T2 EV/價 | T1 EV/價 | 差 | 判定 |
|---|---:|---:|---:|---|
| SEC T2 | 3.853 | 3.49 | +0.363 | PASS |
| MED T2 | 3.756 | 3.53 | +0.226 | PASS |
| ENG T2 | 3.677 | 3.02 | +0.657 | PASS |
| SER T2 | 3.800 | 2.62 | +1.180 | PASS |
| CMD T2 | 3.701 | 3.413 | +0.288 | PASS |

> 200 樣本是用 scratch-only 的離線模擬器（`.scratch/p5loot/parse_code.py`），
> 直接解析本檔的權重、seed 20261116、抽取演算法同 `LootRegistry.Pick`；
> 不是 in-game `CfLootProbe`。in-game `[CF-EV]` 仍待人類補跑。

### T3／T2 容量（0.7.2 重算，0.7.8 對帳）

T3 不是「5 件就一定放得下」：用 bounding box 算，7×3 還在 common 時 SEC T3
最大 105、CMD T3 168、CMD T2 147，舊 9×8／10×8 會掉件。0.7.2 依實際
`itemShape` 重算成 SEC T3 13×8、CMD T3 15×9；0.7.8 把 7×3 移到 rare-only 後
降到 69／105／93，並改成 rare 先放。MED／ENG／SER T3 仍 9×8（最大 40／30／30）；
T2 同步調整：SEC T2 10×8、CMD T1 10×8、CMD T2 12×9，其餘不變。SEC T2
10×8=80 < 93 是已知例外，rare 先放保證只掉 common。詳見 §3.3 容量段。

## 箱子貼圖決策（asset 字串掃描，2026-11-16）

從 `Probably Stolen_Data/resources.assets` 掃到的 `Items/items_container` 箱類
sprite：

- 有 locked／unlocked 成對：`sec_box`／`sec_box_u`、`med_box`／`med_box_u`、
  `eng_box`／`eng_box_u`、`service_box`／`service_box_u`、
  `evidence_box`／`evidence_box_u`。
- 沒有 `_u` 的箱類：`eleanor_box`、`expedition_box`、`cardboard_box`、
  `toolbox_new`、`toolbox_old`。
- **沒有 `cmd_box`／`cmd_box_u`**。

**決策更新（0.5.1）**：CMD 箱（`cf_cmd_box_t1/t2/t3`）改用 §3.12 的
執行期染色 `cmd_box`／`cmd_box_u`，不再直接共用 `sec_box`／`sec_box_u`。
原本「用 `sec_box` + 讓 `UnlockContainer` 自動換 `_u`」的方案已淘汰，原因：
`LockHelper.UnlockContainer` 的 `_u` 換圖走 `Resources.LoadAll`，看不到 mod
sprite；而且 CMD 箱與 SEC 箱同圖。T1 SEC／MED／ENG／SER 用原版箱，不動貼圖。

> 這是 asset 字串掃描，不是 `Resources.LoadAll<Sprite>` 的 runtime 結果；
> 實機第一次進遊戲時仍應以 `Resources.LoadAll` 印出的清單為準。

## P2 測試備註

- **前置**：`Player.log` 需有 `[DevTools] Developer tools enabled`，否則 F9 的
  debug spawner 不會開；測試卡要先檢查這行。
- `GraphUtils.TryAcceptAll` 的回傳是**剩餘量**（IL：`amount - accepted`），
  不是接受量。測試 log 應寫 `remaining=0 accepted=1/1`，不要寫
  `add accepted=0`。
- `ModHook.OnGameLoadedLate` 只在 `PlayerStore.LoadGame()` 觸發；新遊戲走
  `NewGameData.HandleInitialItem()` → `OnNewGameStarted`。P2 測試卡只寫
  「載入存檔」，不寫「或新遊戲」。
- 開鎖 prefix 的規則：cf 箱有 `CF_CRATE_TIER` 才接管；CMD 卡可開任何家族，
  但 `cardTier >= crateTier`；其他卡需 `cardFamily == crateFamily` 且
  `cardTier >= crateTier`。可開 → `audioClickClick` + `key.Destroy()` +
  `UnlockContainer`；不可開 → `audioNegativeTwoBeep`，不消耗卡。
- 非 cf 家族的 `ACCESS_CARD`（例如 `sci_keycard`／`sup_keycard`／
  `blank_keycard`）打 cf 箱時 **一律拒絕**，不 fallback 到原版
  `LOCK_ID_COMMAND` 通殺規則。
- `[CF-LOCK]` log 會帶 `key=… keyLockId=… box=… card=…/… crate=…/… canOpen=…`。

## §3.4 卡／核心價格與工作量 — Errata

- **SER T1 核心工作量 = 75（不是 100）**。原版 `crypto_module_ser` 由
  `ModuleDirectory.CrypoModuleSer()` 以
  `MachineProgressHelper.InitProgressSourceItem(item, 75)` 建立
  （`ModuleDirectory.cs:430-445`）。T1 是原版物品，沿用不可改；T2 才是 150。
- 其餘 T1 核心工作量：CMD 150、SEC 125、MED 100、ENG 100；T2：CMD 250、SEC 200、
  MED 150、ENG 150、SER 150。
- 價格表不變（順序 CMD／SEC／MED／ENG／SER）：
  - T1 卡：125／100／75／75／40；T1 核：350／300／250／250／200。
  - T2 卡：200／150／110／110／60；T2 核：600／500／400／400／300。

## §3.4b T3 卡與核（P4 新增，定價／工作量）

P1–P3 只建了 T1／T2。P4 的退休製卡匠庫存要求「1 張 T3 卡 + 1 個 T3 核」，
所以本輪補 5 家族各一張 T3 卡、一個 T3 核。定價規則（沿用 T2 → T3 的
+50% 曲線，四捨五入到整數）：

| 家族 | T3 卡 id | T3 卡價 | T3 核 id | T3 核價 | T3 核工作量 |
|---|---|---:|---|---:|---:|
| CMD | `cf_cmd_keycard_t3` | 300 | `cf_crypto_module_cmd_t3` | 900 | 350 |
| SEC | `cf_sec_keycard_t3` | 225 | `cf_crypto_module_sec_t3` | 750 | 300 |
| MED | `cf_med_keycard_t3` | 165 | `cf_crypto_module_med_t3` | 600 | 250 |
| ENG | `cf_eng_keycard_t3` | 165 | `cf_crypto_module_eng_t3` | 600 | 250 |
| SER | `cf_ser_keycard_t3` | 90 | `cf_crypto_module_ser_t3` | 450 | 250 |

- 卡價 = T2 卡價 ×1.5；核價 = T2 核價 ×1.5；核工作量 = T2 工作量 +100。
- T3 沿用同家族 T2 的 sprite，`CF_CARD_FAMILY` / `CF_CARD_TIER=3`；
  開鎖規則 `cardTier >= crateTier` 自然成立。
- **T3 箱已是本輪新增**（§3.3：9×8、CMD 10×8、300／500、`CF_CRATE_TIER=3`；
  0.7.2 起 SEC 13×8、CMD 15×9，見 §3.3），
  所以 T3 卡可以直接開 T3 箱；T3 卡只從箱子 rare（與小偷箱）來，
  卡匠／博士不再賣卡。
- T3 核的 `DESEQUENCER_TARGET_ID_TAG` 指向同家族 T3 卡，專業解序器可直接做。
- **0.5.7 起核心不再從任何 T2/T3 箱掉落**：T2/T3 loot 表已移除全部
  `crypto_module_*`／`cf_crypto_module_*`。核心唯一穩定來源是卡匠
  （每個已解鎖等級、五家族各 1 個，§3.8.4）。
- **0.5.6 id errata**：0.5.4 的 `CryptoModuleId(family,3)` 回
  `cf_<family>_crypto_module_t3`（家族在前），但 T2、loot 表與本表都是
  `cf_crypto_module_<family>_t3`（crypto_module 在前）。MED T3 rare 因此
  spawn 出未註冊的 `cf_crypto_module_med_t3` → dummy；CMD 也一樣，只是先被
  貨架上的舊 id 擋住視線。0.5.6 統一為 `cf_crypto_module_<family>_t3`；
  舊的 `cf_<family>_crypto_module_t3` 仍註冊成 alias，讓存檔裡的舊物品
  不會變 unknown（0.5.6 之後不再產生舊 id）。

## §3.5 P3 — 空白卡晶片、專業密碼解序器、批量模組（0.3.0 已實作）

> 本節只寫「已寫進代碼、且已用離線驗證核對過」的事實。實機驗收（測試卡 1–7）
> 尚未跑，見 `CARD_CRAFT_STATUS.md`。

### §3.5.1 3D 印表機晶片 `cf_printer_chip_blank_keycard`

- 照 `ModuleDirectory.CreateMeleeChip` / `CreateCncChip` 既有模式建立，**沒有新增 patch**：
  - `Items/items_tool` + `printer_chip2`；`EnableTag("PRINTER_CHIP_TAG")`。
  - `MachineProgressHelper.SetDualAdditionalRequiredItem(chip, ("common_electronic",1), ("printer_plastic",1))`。
  - `MachineProgressHelper.InitProgressSourceItem(chip, 40, "blank_keycard", useDefaultTooltip: true, targetItemCount: 1)`。
  - `MachineProgressHelper.AddRequiredMachineTag(chip, "CNC Module")`（CNC 模組由
    原版 `PrinterModuleCNC` 在插模組時 `EnableTag("CNC Module")` 到機器上）。
  - `AudioHelper.InitSmallModule(chip)`。
- 晶片不消耗：`MachinePrinter.FinishPrinting` 只 `UnlockItem`，不 destroy。
- 面板顯示工作量 40／額外材料／需要 CNC 模組，全部由 `MachineProgressHelper`
  的既有 tooltip 產生。
- `unitValue = 250`、sprite `printer_chip2`：**規格未指定，為本輪暫定值**，
  只影響圖示與售價，不影響機制。

### §3.5.2 專業密碼解序器 `cf_pro_desequencer`

- **視窗外觀照 3D 印表機**（`MachinePrinter.CreateMachineInventoryWindow` 的同一組
  panel、同一順序）：電源／模組／輸入（9×6）／芯片槽／輸出（9×5）／便條。
  與印表機唯一的外觀差異是 **芯片槽 2 格**（`new GameSlotInventory(2, 1)`；
  vanilla 解序器與印表機都是 1 格）。此為使用者看圖後指定。
- 機制仍照 `MachineDesequencer.Desequencer()`：`ItemDirectory.CreateEmptyItem` +
  `MachineHelper.CreateMachineryWindow` + `MachineHelper.SetupBatterySlot`
  + `MachineryHelper.InitMachinery(machine, 20)`
  + `MachineProgressHelper.InitProgressTypeMachine(machine, 50, useDefaultTooltip: false)`。
- **輸入／輸出不是 vanilla 的單格 `GameSlotInventory`，而是 `GameGridInventory`**：
  `blank_keycard` 不可堆疊（`GameItem.MaxNumAdd` 在 `maxSlotAmountItemFunc` 為 null
  時上限 = 1），單格輸入永遠只有 1 張，驗收 #4「2 張空白卡 → 2 張卡」不可能成立。
  尺寸用印表機的 9×6／9×5，於是輸入盤同時變成一個「排隊槽」：一次開工仍只取
  `min(batch, 空白卡數)` 張。此差異已由使用者在 P3 開工時裁定（選項 A）。
- 過濾：input 只吃 owned 且 `identifier == blank_keycard`；output 只吃 owned、
  非 `blank_keycard` 的 `ACCESS_CARD`；chip slot 照 vanilla（owned、非 MACHINE、
  有 `DESEQUENCER_TARGET_ID_TAG`）。
- `useDefaultTooltip: false` 也是照 vanilla：進度由
  `MachineDesequencer.CreateMachineDesequencerTooltip`（以 `DESEQUENCER_STATE_TAG`
  判斷）顯示，開預設 tooltip 會重複一條進度。
- 每輪流程（`onCycleEndSlotItemFunc`，每晚一次，不輪詢、不記時間）：
  1. 沒電 → 直接 return（進度、輸入卡、產出全部保留）。
  2. `STATE_READY` 且有晶片／有空白卡／輸出還有位 → 抽電、`OnMachineActioned`、
     把晶片的目標 id 寫進 machine 的 `DESEQUENCER_TARGET_ID_TAG`、
     `StartProgressTypeMachine(machine, chip)`、`ContinueProgressTypeMachine`。
  3. 開工時 `plan = min(batch, 空白卡張數)`，把其中 `plan` 張標 `CF_CARD_WIP_TAG`、
     `GeneralHelper.LockItem`、改名「已修改的通行卡」＋描述改成進度。
     **不改貼圖**：vanilla 會換成 `blank_card_wip`，但那是在 1×1 slot 裡做的，
     該切片的真實 footprint 無法離線核實，改 shape 是唯一可能讓 grid 失效的
     動作，所以保留原卡面。
  4. `STATE_DESEQUENCING` → 先看輸出還有沒有位（沒有就不抽電、不加進度、不產出），
     有就抽電＋Continue；`IsProgressTypeMachineFinished` 才結算。
  5. 結算：先把 wip 張數的成品**實際放進輸出格**；全部放得下才摧毀 wip 卡並
     `FinishProgressTypeMachine` + 回 `STATE_READY`；放不下就撤回剛放的全部成品、
     保留進度、輸入卡不動，下一晚重試。「做能做的數量」只用於輸入不足，
     不用於輸出不足（部分批次會白吃輸入卡）。
- 核心不消耗（同 vanilla）：目標 id 在開工時已存在 machine tag，中途換／拔核心
  不影響當前批次。
- 進度與狀態全部在 machine 的 tag（`MACHINE_STATE_TAG`、`MACHINE_PROGRESS_CURRENT_TAG`、
  `MACHINE_PROGRESS_TARGET_TAG`、`CURRENT_PROCESSING_SPEED_TAG`、`DESEQUENCER_STATE_TAG`、
  `DESEQUQUENCER_PROGRESS_TAG`、`DESEQUENCER_TARGET_ID_TAG`）與 wip 卡上的
  `CF_CARD_WIP_TAG`，存檔自動帶走；mod 不另存進度。
- `unitValue = 800`、貼圖沿用 `desequencer`／插電 `desequencer_r`：**規格未指定，
  為本輪暫定值**。

### §3.5.3 批量模組 `cf_batch_card_module`

- `Items/items_module` + `long_module2`（1×4）、`SetGameItemType` 由
  `ModuleHelper.InitModuleItem` 設成 `MODULE`。
- `ModuleHelper.InitModuleItem(module, "MODULE_TYPE_UNIVERSAL", -50, -50, 0,
  isShipModule: true, "每輪制卡產出 +1")`：速度／耗電完全交給框架，
  `ApplyBasicModuleEffect` 會算成「速度 -50%、耗電 +50%」，mod 不乘任何係數。
- `MODULE_TYPE_UNIVERSAL` 讓同一顆模組同時能插 3D 印表機（收 UNIVERSAL／PRINTER）
  與專業解序器（收 UNIVERSAL／ALARM）。
- 效果標籤 `CF_CARD_BATCH_MODULE_TAG`。`batch = 1 + (模組欄裡有這個 tag ? 1 : 0)`，
  **上限 2，第二顆不再增加產出**；但兩顆的 -50/-50 統計仍會被
  `ModuleHelper` 疊加（vanilla 模組行為），所以插第二顆只有懲罰、沒有收益。

### §3.5.4 印表機側批量（P3 唯一的 patch）

`CfPrinterBatchPatch` = prefix on `MachinePrinter.FinishPrinting`，只在
`chipInv.childItem.identifier == cf_printer_chip_blank_keycard` 時做事：

- `batch == 1`（沒有批量模組）→ 直接放行，完全走 vanilla。
- `batch == 2`：
  - `sets` = 輸入盤（panel index 2）上、依晶片 `REQUIRED_ITEM_ID_*`／
    `REQUIRED_ITEM_COUNT_*` 能湊出的完整套數。
  - `extra = min(batch, sets + 1) - 1`；`+1` 是 vanilla 在開工時已經吃掉的那一套，
    所以 `2 電子 + 2 耗材` → `extra = 1`，總共消耗 2 套、產出 2 張。
  - 先把 `extra` 張卡真的放進輸出格，再探測「vanilla 即將放的那一張」還有沒有位
    （先放再探測才看得到剛放進去的卡）。
  - 任一放不下 → 撤掉剛放的卡、回傳 `false` 中止 `FinishPrinting`：進度保留、
    不額外消耗、不產出；下一晚重試（重試那晚仍會抽電，這是沿用 vanilla 的
    「先抽電再判定完成」順序）。
  - 放得下 → 每多 1 張呼叫一次 `MachineProgressHelper.ConsumeRequiredAdditionalItems`。
    **每次都要重讀 `tray.childItems`**：舊快照裡還留著上一次已摧毀的物件，
    而 `Destroy()` 第二次是 no-op，會出現「說消耗 2 套、其實只掉 1 套」。
- 工作量不變：progress target 永遠是晶片的 40，batch 不乘 target、不乘 speed。

### §3.5.5 實機驗證與未核實

- **2026-11-16 實機通過（`Player.log`）**：印表機批量 `2 電子 + 2 耗材 → 2 張空白卡`
  （`[CF-PRINT] batch=2 extra=1 setsBefore=1 setsAfter=0` ×2）；解序器 `plan=1` 與
  `plan=2` 都跑過，T1 `cmd_keycard` 與 T2 `cf_cmd_keycard_t2` 都實際產出
  （`[CF-DESQ] finished … produced=N consumed=N`）。逐行對照見
  `CARD_CRAFT_STATUS.md`「P3 實機驗證」。
- 仍未由 log 直接證明：驗收 #5（拔電／輸出滿）、#6（存檔重載）、#7（第二顆批量
  模組不疊加）。#5／#6 使用者口頭確認可用；#7 目前只有程式硬上限（`count > 0 ? 2 : 1`）。
- `blank_card_wip` 的真實格子大小（因此 §3.5.2 第 3 點不改貼圖）。
- `long_module2` 是否真的是 1×4（由 sprite 切片決定，未在 runtime 量過）。
- 專業解序器與晶片在存檔中的實際序列化結果（tag 應自動帶走，未逐欄驗）。


### §3.5.6 Errata — 輸出探測必須用「實際產出的卡」

- 0.3.0 初版 `Start`/`Continue` 用 `CardBatch.OutputHasRoom(outputGrid, "blank_keycard")`
  判斷輸出還有沒有位。但輸出格本來就拒絕 `blank_keycard`（`mayInventoryAddItemFunc`），
  `GraphUtils.CanAccept` 因此**永遠 false** → 機台每晚都走「輸出滿」分支直接 return，
  看起來就是「完全沒實裝」。
- 修正：探測改成**實際會產出的卡 id** —— `Start` 用晶片上的
  `DESEQUENCER_TARGET_ID_TAG`，`Continue` 用 machine 上記下的同一個 id。
  印表機側不受影響（輸出盤接受任何 owned 物品，且實機 log 已有
  `[CF-PRINT] batch=2 extra=1 setsBefore=1 setsAfter=0` 證明它會動）。
- 回歸檢查進 `.scratch/p3check`：`Start`/`Continue` 必須呼叫 `OutputHasRoom`，
  且方法內不得出現 `"blank_keycard"` 字面值（防止有人把探測改回空白卡）。
- 同時補診斷 log（同一原因只印一次）：
  `[CF-DESQ] waiting: no charged battery / no crypto module in the chip slot /
  no blank keycard in the input grid / output full`；開工印
  `[CF-DESQ] start plan=… target=… work=…/… speed=… power=…`；每晚印
  `[CF-DESQ] night progress=cur/target speed=…`；結算印
  `[CF-DESQ] finished target=… produced=… consumed=…`。
- **修正已在實機驗證**（2026-11-16）：同一份 `Player.log` 有 4 筆
  `[CF-DESQ] start/night/finished`，含 `plan=1` 與 `plan=2`、T1 與 T2 目標。

## §3.8 退休製卡匠（P4）

> 本節只寫「已寫進代碼、且已用離線驗證核對過」的定案。實機（製卡匠到訪、
> 買賣流程）仍需人類開遊戲驗，見 `CARD_CRAFT_STATUS.md`。

### §3.8.1 出現規則

- 解鎖條件：Wild Network 升級 `CF_FENCE_PRO`。
- 到訪日：`StoreStation.Instance.dayCounter % 7 == 6`（每週第 6 天），
  在 `ModHook.OnGenerateCustomerVeryLate` 掛 handler 後 `manager.AddClient(...)`。
  `dayCounter == 0` 不生成。
- **首次解鎖補訪**：`CF_FENCE_PRO` 買下後第一次
  `GenerateClient`（通常就是當天，不限星期）補一次到訪，避免玩家買完還要等
  最多 6 天。用 `ModHelper` 的存檔內 modData key `maker_last_day` 記錄最後到訪日；
  `maker_last_day < 0` 代表從未到訪。
- 一天最多一位：`maker_last_day == dayCounter` 時不再加。
- 每週到訪不消耗原版 `clientTracker`／cooldown，避免和其他客戶互相擋。

### §3.8.2 客戶欄位

| 欄位 | 值 | 說明 |
|---|---|---|
| `identifier` | `cf_retired_cardmaker` | 供 log／排除重複 |
| `displayName` | `退休制卡匠` | 在地化 key `name_retired_cardmaker`，缺 key 用中文 fallback |
| 立繪 | `SpriteDict.GetRandomLowerLevelSpriteName()` | 下層市民 |
| `clientFaction` | `FACTION_LOWER_LEVEL` | 與原版退休匠一致 |
| `clientIntent` | `SELL` | 客戶賣、玩家買 |
| `nonShootable` | `true` | 同原版退休匠 |
| `clientItemFeatureBuying` | `premiumBuy15` | 同原版退休匠 |
| `acceptRetailMarkup` | `false` | 固定售價，不套零售加成 |
| 對話 | `dialog_retired_cardmaker_1`（缺 key 用中文 fallback）+ `AddBasicDialog()` / `CompleteClientCreation()` | 與原版退休匠相同流程 |

### §3.8.3 CF_FENCE_PRO 的一次性工具包

`CF_FENCE_PRO` 買下時，`onUnlock` 立刻發一組（**只發一次**，升級不可重複）：

| 物品 id | 數量 | unitValue |
|---|---:|---:|
| `printer` | 1 | 350 |
| `cf_printer_chip_blank_keycard` | 1 | 250 |
| `printer_module_cnc` | 1 | 400 |
| `common_electronic` | 2 | 33 |
| `printer_plastic` | 2 | 12 |
| `energy_credit` | 1 | 10（100 電／bandwidth 10） |

- **電池**：`energy_credit` 就是原版 `PowerHelper.InitPowerSourceItem(item, 100, 10)`；
  沒有它印表機無法開機，所以 PRO 工具包必帶一顆。不另外改 50 電版本。
- 全部 `GeneralHelper.SetItemOwned(item, true)` 後
  `GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.backInvinvElement, item)`；
  後台滿了會自動落到其他店內庫存，不會憑空消失。
- 這是「讓 P1–P3 不再只能靠 F9」的起點：有了印表機＋CNC＋空白卡晶片＋材料，
  玩家就能自己做第一張空白卡。

### §3.8.4 製卡匠庫存（售價 = 物品 `unitValue`，不標 stolen、不賣箱子）

**每次到訪最多 11 件，實際 7–9 件**：printer 固定 1，chip／CNC 依數量差
各加 0–1，材料 2+2，核心 2。不保存庫存，不做翻頁。核心已從所有 loot 表移除，
所以卡匠是核心唯一穩定來源。**同一天不重複**：一天最多一位製卡匠，
`maker_last_day` 已擋。

| 類別 | 物品 | 數量 | 單價 |
|---|---|---:|---:|
| 固定 | `printer` | 1 | 350（premiumBuy15） |
| 條件 | `cf_printer_chip_blank_keycard` | 0 或 1 | 250 |
| 條件 | `printer_module_cnc` | 0 或 1 | 400 |
| 材料 | `common_electronic` | 2 | 33 |
| 材料 | `printer_plastic` | 2 | 12 |
| 核心 | 2 家族輪換，最高已解鎖等級 | 2 | 依 id |

- **printer 每次固定 1 台**，售價 = `unitValue` 350，client 已掛
  `premiumBuy15`。
- **chip／CNC 用數量差**：先放 printer，再算
  `CountOwned(id) < CountOwned("printer")`（含剛上架那台），成立才各加 1；
  一次最多補 1 個，缺很多台就多來幾次。`CountOwned` 用
  `PlayerStore.Instance.FindAllItem()` 數 `identifier`。
- 沒有擴產需求時 **7 件**（printer＋4 材料＋2 核心）；chip／CNC 都補時 **9 件**；
  上限 11 不會補滿。
- 核心等級：只取已解鎖的最高等級（T3 → T2 → T1）。
- 核心家族：`start = day % 5`，取 `AllFamilies[start..start+1]`（環繞），
  所以 5 天輪完五家族，每天 2 顆。
- **每週總價（設計參考，不是硬門檻）**：

| 等級 | 無擴產（7 件） | 擴產週（9 件） | 參考帶 |
|---|---:|---:|---:|
| T1 | 890–1090 | 1540–1740 | 1000–1350 |
| T2 | 1140–1540 | 1790–2190 | 1300–1900 |
| T3 | 1490–2090 | 2140–2740 | 2000–2700 |

  價格帶只當節奏參考；**不要為了壓價去改核心 `unitValue` 或移除 chip／CNC**。
  材料 90；chip 250、CNC 400 依需求。
- 卡匠不再賣卡；卡只從箱子 rare／小偷箱來。博士也不賣卡／核。
- **不賣箱子**（任何 T1／T2／T3 箱都不在工匠庫存）。
- **不賣金屬模組**（`printer_module_metal`）；printer 本體有賣，CNC 條件賣。
- 全部 `isOwend: false`：玩家要付 `unitValue` 買；沒有額外 markup。
- **櫃台滿＝該件本次不販售**：先 `GraphUtils.CanAccept(front, item)` 探測；
  放不下就 `Destroy()`＋`[CF-STOCK] counter full, skipped <id>`，
  **絕對不** `EmporiumTryAdd(backInvinvElement, …)`，也不進玩家背包。
  `TryAddDirectSellingItemToTable` 在失敗時本來就會銷毀，先探測是為了留下
  可讀的 skip log。

#### 多台機器（2026-11-16 擴產）

- 每台 `printer` 是獨立 GameItem，夜間 tick、進度、晶片與模組狀態都掛在
  該台自己的 tag／machine tag 上，不共用全域狀態；多台可同時跑。
- 卡匠每次固定賣 1 台 printer；chip／CNC 依「擁有數 < printer 數」各補 1，
  所以第 2、3 台要分幾次到訪才能補齊配件。
- **批量模組是 per-machine**：只有插了模組的那台 batch = 2；其他台仍是 1。
  同一台插兩張模組不會變 3／4，`BatchSize` 上限就是 2（第二張不生效）。

### §3.8.5 博士（住所 hook + 臨時店 patch）

**兩條路並存**，都是固定 `cf_pro_desequencer`（800）＋
`cf_batch_card_module`（250），不抽 T2/T3 卡核：

1. **住所**：`OnEnable` 訂閱
   `ModHook.OnPlaceInventorInventoryItemEarly += CfInventorStock.OnInventorCounter`，
   `OnDisable` 取消。`PlaceInventorInventory` 與 `PlaceInventorSpecialStock`
   都會觸發，所以特殊庫存日也有兩件。
   - `OnInventorCounter(List<GameItem> owned)`：PRO 未解鎖就 return；
     否則呼叫 `PlayerStore.Instance.TryAddDirectSellingItemToTable(id)`。
   - 回 false 只印 `[CF-INVENTOR] counter full, skipped <id>`；
     **不手動 Destroy、不丟後台、不進玩家背包**。
2. **博士臨時來店裡**：`CfInventorPatch` postfix 包 `CreateInventor` 最後一句
   `endAction`，先跑原版再上架同兩件。
3. **`CreateInventorStorage`**：另有 `CfInventorStoragePatch` postfix，只印
   `[CF-INVENTOR] client=inventorStorage pro=…`；住所庫存由 hook 負責，
   不在 storage patch 再 Attach，避免同一批加兩次。
4. 進入 patch 一律先印
   `[CF-INVENTOR] client=<identifier> pro=<bool>`，用來確認 patch 命中與
   client 種類。

- 兩件都 `isOwend: false`，售價 = `unitValue`；PRO 未解鎖時不賣。

### §3.8.6 診斷 log

- `[CF-MAKER] visit day=… gate=weekly|catchup stock=fixed,materials,cores,t2,t3`
- `[CF-MAKER] skip: already visited today day=…`
- `[CF-MAKER] skip: CF_FENCE_PRO not unlocked`
- `[CF-INVENTOR] client=<identifier> pro=<bool>`（CreateInventor／Storage postfix）
- `[CF-INVENTOR] counter desequencer=<bool> batch=<bool> owned=<n>`
- `[CF-INVENTOR] counter full, skipped <id>`（住所 hook 放不下）
- `[CF-STOCK] counter full, skipped <id>`（僅卡匠貨架）

## §3.9 索耶換卡（P5，0.7.0 實作／0.7.1 修訂）

> 本節只寫「已寫進代碼、且已用離線核對過」的規則。實機（每晚換卡、芯片升級、
> 紙本渲染）尚未驗，見 §3.9.9 與 `CARD_CRAFT_STATUS.md`。

### §3.9.1 觸發與前置

- 掛 `ModHook.OnHandlingNightlyServicesEarly`：`OnEnable` 先 `-=` 再 `+=`
  `CfSawyerExchange.OnHandlingNightlyServicesEarly`，`OnDisable` `-=`；
  **沒有新增 Harmony patch**。
- 這個 hook 在 `PlayerStore.EndNight()` 內、`HandleJanitorial()` 之前
  （PlayerStore.cs:1687）。這個位置讓三件事同時成立：
  - 卡與核心還在垃圾桶（`ClearTrash()` 還沒跑）；
  - 原版 `exchangeManager.HandleExchange()` 也還沒跑，被索耶吃掉的卡
    不會再被原版 barter 拿去換；
  - 產出放進 `exchangeManager.itemsBuffer`，而 `TransferBuffer()` 在
    `ClearTrash()` 之後才跑（PlayerStore.cs:1937-1943），所以新箱子不會被清掉。
- 前置（缺一就整晚跳過，同一原因只印一次 log）：
  1. `StoreService.GetJanitorialService().unlocked && .isUsing`；
  2. `PlayerStore.Instance.exchangeManager.isExchangeUnlocked`；
  3. `CF_SAWYER_PERMANENT` 已解鎖。
- 掃描垃圾桶：`GraphUtils.FindAllChildrenType<GameItem>(EmporiumEntry.Instance.trashcan)`
  （與原版 `ExchangeBarter.GetRequiredItems` 同一個掃法，含箱子裡的子物品）。
- handler 全程 try/catch：例外只 `Log.Error`，**不讓 `EndNight` 中斷**。

### §3.9.2 換卡（垃圾桶裡的同家族鑰匙卡 → 物資箱）

- 比例（一張卡換 1 箱的張數）：**SEC 3、MED 4、ENG 4、SER 4、CMD 3**。
- 同家族的卡按等級**高→低**排序，每滿一組切一組；**箱子等級 = 該組最低等級**
  （降序後是組內最後一張）：
  - 3 T3 + 1 T1（比例 3）→ 1 個 T3 箱（T1 留下）；
  - 2 T1 + 3 T2 → 1 個 T2 箱（2 張 T1 留下）；
  - 1 T1 + 2 T2 → 1 個 T1 箱（混組仍取最低，防止低卡升箱）；
  - 3 T1 + 3 T2 → 1 個 T1 + 1 個 T2；4 T1 + 2 T2 → 2 個 T1 箱。
  - 結果與垃圾桶的擺放順序無關（先按等級排序再切組）。
- 卡識別：`ItemFactory.TryGetCardInfo`（既有）——只認有 `CF_CARD_FAMILY` /
  `CF_CARD_TIER` 的卡，或原版 T1 id（`sec_keycard`／`med_keycard`／
  `eng_keycard`／`ser_keycard`／`cmd_keycard`）。等級夾在 1–3。
- 箱體（**都是已填 loot＋已上鎖**）：
  - T1 非 CMD：`PreBuiltItemHelper.LootCrateSecurity()`／`LootCrateMedical()`／
    `LootCrateEngineering()`／`LootCrateService()`——原版 T1 箱 id、原版
    `crateFillers` 填 loot、`LockHelper.LockUpContainer` 上鎖；
  - 其餘（T2／T3 五家族、CMD T1）：`DirectoryMaster.Item(ItemFactory.ContainerId(family,tier))`，
    由既有 `CfCrateFillPatch` 自動填 loot 並上鎖。
- 建立失敗（`null`，或拿回的東西沒有 `contentWindow`＝`DirectoryMaster.Item`
  對未知 id 的 dummy fallback）→ **該組卡不銷毀**，跳過該組（其他組照常；
  正常註冊下不應發生）。
- 每組**成功後**才 `Destroy()` 該組的卡（避免同一晚再被原版 barter 用）。
- 箱加進 `exchangeManager.itemsBuffer`。
- **批次上限**：`CF_SAWYER_BATCH` 未解鎖 → 全夜總產出上限 **1 箱（跨家族）**；
  已解鎖 → 全部做完。

### §3.9.3 換卡的實作裁定

- **固定處理順序**（可重現）：先按家族 `SEC → MED → ENG → SER → CMD` 收集
  每家族的完整組（同家族內等級高→低切組），再把所有組按**箱子等級高→低**
  產出；等級相同才用上面的家族順序。未解鎖批量時 cap 就是靠這個順序先保留
  最高級的箱（例：3 T3 + 3 T1 只做 T3）。
- 未解鎖批量時，達到 1 箱就**立刻停止整個換卡流程**，剩下的卡不消耗、
  留給原版 barter／`ClearTrash`。只有「垃圾桶裡還有其他湊得滿的組」時才印
  `[CF-SAWYER] cap: 1 box/night until CF_SAWYER_BATCH`（真的是被上限扣下一個
  箱子才印；剛好湊滿 1 箱、或剩下的零散卡本來就湊不成箱，都不會多印）。
- 箱子的 loot 與鎖**不自己填**：一律交給原版 helper 或既有
  `CfCrateFillPatch`，本輪不新增任何填箱程式碼。
- 箱子的 `unitValue`／id 完全照 §3.3 表；T1 沿用原版 id（SEC／MED／ENG 200、
  SER 175、CMD T1 200）。
- 箱子放 `itemsBuffer` 後由原版 `TransferBuffer` → `TryAcceptAllOrDestroy`
  進垃圾桶；垃圾桶滿時原版會印 `exchange delivery, trashcan full` 並丟棄，
  這是原版行為，本 MOD 不另外搬運。

### §3.9.4 芯片升級（加密模組）

- 前提：`CF_SAWYER_PERMANENT` + `CF_SAWYER_CHIP` 都已解鎖（handler 本身也要求
  PERMANENT）。
- **3 個同家族同級核心 → 1 個同家族下一級核心**：T1→T2、T2→T3；**T3 不再升**。
- **每晚每家族最多 1 次**（不是每級）：同家族同時有 3 個 T1 與 3 個 T2 時，
  只做 T1→T2 那一組，T2 那組留著（實作：T1 先試、成功就 `break` 出該家族）。
- 核心來源同樣是垃圾桶。核心**照 id 識別、不看 tag**（T1 原版核心與本 MOD 核心
  都沒有 family/tier tag）：

  | id | 家族 | 等級 |
  |---|---|---|
  | `crypto_module_sec`／`med`／`eng`／`ser`／`cmd` | 對應家族 | T1 |
  | `cf_crypto_module_<slug>_t2` | 對應家族 | T2 |
  | `cf_crypto_module_<slug>_t3` | 對應家族 | T3 |
  | `cf_<slug>_crypto_module_t3`（0.5.4 舊 alias） | 對應家族 | T3（**只讀，不再產生**） |

- 產出用 `ItemFactory.NewCryptoModule(family, tier+1)`；**成功才** `Destroy()`
  那 3 個輸入核心，新核心加進 `itemsBuffer`。
- 芯片產出**不佔**換卡的「1 箱」上限（批量沒解鎖時照樣可以升芯片）。

### §3.9.5 常駐服務與原版促銷 barter 的接管（`CF_SAWYER_PERMANENT`）

- **只有 PERMANENT 已解鎖時才動原版資料**：
  - 從 `ExchangeManager.promotionalBarter` 移除 `sec_crate_barter`、
    `med_crate_barter`、`eng_crate_barter`；
  - 同時把它們從 `activePromotionalBarterIds` 移除（舊存檔若已抽到這三個，
    UI 才不會留一條點不動的條目）；
  - 由自訂 handler（§3.9.2／§3.9.4）接管。
- **未解鎖時完全不動原版資料**：三個 crate barter 仍會隨機出現在促銷池、
  也仍能正常換（測試 6）。
- 移除要在多個時機 **idempotent** 執行：`OnGameLoadedLate`、`OnNewGameStarted`、
  PERMANENT 的 `onUnlock`、以及每晚 handler 開頭（防
  `ExchangeManager.Init()`／`ResetPromotional()` 重建清單）。全部有 null 防護。
- 只在**成功移除**時印一次 log（process 內只印一次，避免每晚洗版）：
  `[CF-SAWYER] removed promotional barters sec_crate_barter,med_crate_barter,eng_crate_barter`。

### §3.9.6 換卡目錄紙本 `cf_sawyer_card_catalog`

- `CfSawyerPaper`：`BookId = "cf_sawyer_card_catalog"`、
  `ItemId = "cf_sawyer_card_catalog_item"`。
- `EnsureRegistered()` 建 `PaperBookData` 後呼叫
  `ModHelper.RegisterPaperBook(CardCraftAndFenceMod.ModId, data)`。
  **每次呼叫都重新註冊**（registry 內部是 remove+add；item factory 也每次
  重註冊），刻意**不用**「只註冊一次」的 bool——否則在 `ModItemDirectory`
  還沒 live 時註冊的那一次會讓後來的 item 註冊被跳過。
- 書籍內容：title「索耶的换卡目录」、background `notebook_manual`、
  designWidth 314／designHeight 402、`chrome = PaperStarters.DefaultChrome(new Vector2(314f,402f), 2f)`；
  一頁 name「换卡比例」，頁面用一個 text element 直接寫（x=24、y=34、w=266、h=336、
  style `body`、wrap），**不用 template**。文字列出：五家族比例、同家族高→低排序、
  箱子取組內最低等級、`3 T3 + 1 T1 → T3`／`2 T1 + 3 T2 → T2`／`1 T1 + 2 T2 → T1`
  的例子、未解鎖批量每晚 1 箱且優先最高等級、芯片 3 同家族同級→下一級、
  每晚每家族 1 次、T3 不升。
- **遊戲 build 缺 PaperUI 資源**：`Resources/PaperUI/PaperAssetCatalog` 不存在，
  `notebook_manual` 與 `paper_buttons_*` 不會從原版解析（實機 log 是五次
  `No sprite found`，chrome 按鈕變成藍方塊）。`CfPaperSprites` 以 `Texture2D`
  執行期產生 5 張 sprite：628×804 紙底（纖維＋顆粒＋邊緣壓暗）與 64×64 的
  X／放大鏡／左箭頭／右箭頭，註冊進 `LoadedMod.PaperSprites`
  （`PaperAssets.FromMod` 會先查這個字典）。`OnModAssetsLoaded` 與
  `EnsureRegistered()` 都會補註冊，`Grant()` 前也再確保一次；零美術檔。
- `item`：`PaperItemData { id=ItemId, name="索耶的换卡目录",
  description="记录旧城交易所的换卡比例与凑单规则。",
  sprite="items_misc/manual1", value=50, important=false }`。
- 註冊時機：`OnEnable`（純 static，不碰 PlayerStore）、`OnGameLoadedLate`、
  `OnNewGameStarted`，以及 `Grant()` 之前。
- **發放時機**：PERMANENT 的 `onUnlock`（`CfSawyerPaper.Grant()`）：先
  `EnsureRegistered()`，再 `ModHelper.SpawnPaperItem(ModId, BookId)`；
  非 null 才 `GeneralHelper.SetItemOwned(book,true)`＋
  `GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.backInvinvElement, book)`
  （後台滿會落到其他店內庫存，不會憑空消失），並印
  `[CF-SAWYER] paper book granted id=cf_sawyer_card_catalog`。
- 只用 registry 的紙本 item factory（`PaperBookRegistry.CreateItem` 會設
  `DOCUMENT`／`GUIDE_PAPER_TAG`／`BookHelper.InitPaperGuide`），本 MOD 不自己
  建紙本物品、不加新 id 到 `CfItemDirectory`。

### §3.9.7 三個新升級（`tab="CardFence"`、`subtitle="索耶"`）

| id | 價格 | 前置 | 效果 |
|---|---:|---|---|
| `CF_SAWYER_PERMANENT` | 400 | —（無 prerequisites） | 索耶常駐：每夜換卡＋接管原版三個 crate barter；`onUnlock` 發紙本目錄 |
| `CF_SAWYER_BATCH` | 700 | `CF_SAWYER_PERMANENT` | 取消「每晚 1 箱」上限 |
| `CF_SAWYER_CHIP` | 900 | `CF_SAWYER_PERMANENT` | 開啟芯片升級（3 同級→下一級） |

- 「交換所已解鎖」不是 prerequisites，而是既有 `CfSawyerHidePatch`
  （`NetworkUpgrade.IsHiddenForRun` postfix，§3.11.4）負責把 `CF_SAWYER_*`
  在交換所未解鎖前藏起來；**不要**再加一個前置 id。
- 三個都在 `CfNetwork.EnsureRegistered()` 裡用既有 `Add(...)` 包裝與 try/catch；
  註冊 log 改成 `[CF-NET] registered N/9 CardFence upgrades tab=CardFence width=792/1060`
  （0.7.0–0.8.0 是 `N/7`，0.9.0 起 `N/9`），只有 `ok == 9` 才印 hide 那行。
- 分頁寬度不變：`CardFence` 仍是第 3 個分頁、佔 792/1060（§3.11.1），
  **沒有第 4 個分頁**。
- PERMANENT 的 `onUnlock` 用 try/catch 包住
  `CfSawyerExchange.EnsurePromotionalRemoved()` 與 `CfSawyerPaper.Grant()`，
  不可讓購買流程炸掉。

### §3.9.8 診斷 log

- `[CF-SAWYER] skip: janitorial off`
- `[CF-SAWYER] skip: exchange locked`
- `[CF-SAWYER] skip: CF_SAWYER_PERMANENT not unlocked`
  （以上同一原因只印一次）
- `[CF-SAWYER] night cards=N cores=M boxes=B chip=K`
- `[CF-SAWYER] box family=SEC tier=1 id=sec_box cards=3`
- `[CF-SAWYER] cap: 1 box/night until CF_SAWYER_BATCH`
- `[CF-SAWYER] chip family=SEC 1->2 id=cf_crypto_module_sec_t2`
- `[CF-SAWYER] removed promotional barters sec_crate_barter,med_crate_barter,eng_crate_barter`
- `[CF-SAWYER] paper book granted id=cf_sawyer_card_catalog`
- `[CF-PAPER] registered runtime paper sprites notebook_manual + 4 chrome buttons`
- `[CF-LOOT] overflow table=… item=… remaining=…; item destroyed, crate is short one roll`
- `[CF-NET] registered N/9 CardFence upgrades tab=CardFence width=792/1060`
  （0.7.0–0.8.0 是 `N/7`）

### §3.9.9 實機驗證與未核實

**0.7.0 實機已通過（2026-11-17，`Player.log`）**：

- 載入、`[CF-NET] registered 7/7`、`Harmony: 8`、無 `Init failed`、無 `cf_` dummy。
- `[CF-SAWYER] removed promotional barters …` 與 `[CF-SAWYER] paper book granted …`
  都出現；`[CF-SAWYER] skip: janitorial off` 正常。
- 夜間 handler 有跑；T1／T2／T3 箱都建得出來；T2／T3 有
  `[CF-LOOT] postfix fill`＋`fill rolls`；箱子有上鎖；T1 鑰匙開 T3 箱得到
  `canOpen=False`（正確）。
- 存檔裡的 T3 SEC 箱 5 件、已上鎖、`CF_CRATE_FILLED=true`。
- T1 `sec_box` 沒有 `[CF-LOOT]` 是正常的（走 `PreBuiltItemHelper`，不經 fill patch）。

**0.7.1 已修，實機已通過**（2026-11-17 退回單之後）：

- 紙本：`CfPaperSprites` 執行期產生紙底與 X／放大鏡／左右箭頭，開書不再有
  `No sprite found`、右上角不是藍方塊。
- 分組：降冪排序＋組內取最低；3 T3 + 1 T1 → T3、2 T1 + 3 T2 → T2、
  1 T1 + 2 T2 → T1、3 T1 + 3 T2 → T1 + T2。
- SEC T2 rare：8×8＋rare 先放後，開箱 7 件齊全。0.7.2 曾改成純面積排序，
  0.7.8 改回 **rare 先放、common 再按面積大→小**，避免容量不足時先掉 rare。

**0.7.2／0.7.8 容量與 rare 修正（待實機重測）**：

- 0.7.1 實機仍抓到 SEC T3 掉件；CMD T3／T2 高度可疑。用 66 個存檔的實際
  `itemShape` bounding box＋10 萬次 rolls 重算：7×3 還在 common 時 SEC T3
  最大 105、CMD T3 168、CMD T2 147（0.7.1 的 9×8／10×8／9×8 明顯不夠）；
  把 7×3 移到 rare-only 後降到 69／105／93。
- 修法：`LootTables.Fill` 先 roll 全部、**rare 先放**、common 再按 shape 面積
  **大→小**放入（grid 放置本身會試 4 旋轉＋2 翻轉）；`smg`／`shotgun`（7×3）
  從 SEC T3／CMD T2／CMD T3 的 **common** 池移除（rare 保留），三張表權重
  重配、common EV 與整箱 EV 完全不變；容量見 §3.3（SEC T2 10×8、CMD T1 10×8、
  CMD T2 12×9、SEC T3 13×8、CMD T3 15×9）。SEC T2 80 小於 worst 93，
  但 rare 已先放，overflow 只會掉 common。
- MED／ENG／SER T3 用同一套 shape 清單複算：benchmark 最大面積 40／30／30，
  9×8=72 足夠（`module_extractor_advanced` 2×3、`turbo_booster_adv` 3×2、
  `advanced_flux_agent` 2×2、`system_capped_neural_core` 2×2、
  `module_bay_expansion_kit` 2×2）。實機各 3 箱仍待測。
- overflow 改成**每箱一行**，並分開記 `rolled／spawned／expected／actual／
  dropped`；null roll 另印 `[CF-LOOT] spawn short …`。
- 待實機：T3 SEC ×5、T3 CMD ×5、T2 CMD ×3，每箱件數 = rolls + 1，
  log 沒有 `[CF-LOOT] overflow`；SEC T2 也順便看。

**仍未實機（0.7.8）**：

- 紙本 item 註冊在 `ModItemDirectory` 尚未 live 時的第一輪會靜默失敗，
  依賴 `OnGameLoadedLate`／`OnNewGameStarted`（與 `Grant` 前）補註冊；
  這個時序沒有實機 log 證明。
- `activePromotionalBarterIds` 移除後，`ExchangeUIManager` 的促銷條目是否
  立即消失、重開 UI 後是否復活。
- `_removalLogged` 是 process 內 static：同一行程切換存檔時，第二個存檔
  的移除不會再印 log（行為正確、只是沒有第二條 log）。
- 芯片升級「每晚每家族最多 1 次」與舊 alias 讀取（`cf_<slug>_crypto_module_t3`）
  只有離線邏輯核對，沒有實機把 3 T1＋3 T2 放進垃圾桶跑過。
- 新容量（尤其 CMD T3 15×9）在 UI 上的實際格子大小與可讀性，只有離線
  packing 模擬（0/3000 失敗、adversarial 全 4×3＋7×3 也放得下）與
  面積計算，未進遊戲量過。

## §3.10 黑市與小偷（P4 只做第 2 項）

> 第 2 項「小偷賣分級箱」P4 實作；其餘黑市內容 P4 不做。
> 紙本與索耶在 P5（§3.9）補上。

### §3.10.1 觸發

- 掛在 `StoreClientList.CreateThief` 的 postfix：**先放 1 個 CF 分級箱，
  再跑原版 `endAction`**。
- 這樣 6×4 箱一定先拿到六個連續空欄；若原版 thief 商品因此被擠掉，
  是**刻意取捨**（櫃台 18×4，原版先鋪會讓箱子永遠 `counter full, skipped`）。
- 每次 thief 到訪只給 1 箱；不因 `GetBMRepStockMult()` 增加數量。
- **T1 一律不加**：原版 thief 已經會賣 T1 箱，CF 這條只補 T2／T3。
- **永不給 CMD 箱**：小偷的家族池只有 SEC／MED／ENG／SER。
- 不改 thief 的 `acceptRetailMarkup`（上一版曾設 false，本輪移除）。
- **P7-1（0.9.0）**：被打開的贓物箱會把自身熱度繼承給箱內 loot，箱子外殼則被洗淨；
  這是刻意的行為變更（§3.14.1）。

### §3.10.2 箱等級（黑市聲望 → 等級）

`bm = StoreReputation.GetBMReputation()`（原版範圍 −200～200）。

| 條件（由上往下第一個成立） | 箱等級 |
|---|---|
| 已解鎖 `CF_FENCE_T3` 且 `bm >= 85` | T3 |
| `bm >= 60` | T2 |
| `bm >= 30` 且已解鎖 `CF_FENCE_THIEF_T2` | T2 |
| 其餘 | **不給 CF 箱**（不退回 T1） |

- `CF_FENCE_THIEF_T2`（2200，無前置）把 T2 門檻從 60 降到 30，不改內容與價格。
- T3 箱要另外解鎖 `CF_FENCE_T3`（製卡匠／博士的高級門路）。

### §3.10.3 家族權重（合計 100，無 CMD）

| 家族 | 權重 | T2 id／價 | T3 id／價 |
|---|---:|---|---|
| SEC | 32 | `cf_sec_box_t2`／225 | `cf_sec_box_t3`／300 |
| MED | 27 | `cf_med_box_t2`／225 | `cf_med_box_t3`／300 |
| ENG | 21 | `cf_eng_box_t2`／225 | `cf_eng_box_t3`／300 |
| SER | 20 | `cf_ser_box_t2`／225 | `cf_ser_box_t3`／300 |

- 用累積權重 + `RNG.GetRandomInt(0, 99)` 抽家族，再依等級取該家族 id。
- **卡／核等級 ≤ 箱等級**：T2 箱的 loot 表只放 T2 卡／核；T3 箱只放 T3 卡／核
  ／T2 卡核與高價稀有物（T3 表見 §3.3 Loot）。由表的內容保證，不需要 runtime 過濾。
- T2／T3 箱都走 `DirectoryMaster.Item` → §3.3 的 postfix 自動填 loot + 上鎖；
  本輪已沒有 T1 分支，所以不再需要 `PreBuiltItemHelper.LootCrate*`。

### §3.10.4 販售標記

- 所有 CF 箱（只有 SEC／MED／ENG／SER）都 `isStolen: true`。
- 售價 = 箱子的 `unitValue`（T2 225、T3 300），不加零售 markup。
- CMD 箱不經過小偷，`isStolen` 的 CMD 例外不再適用。
- **櫃台形狀限制**：原版前台是 **18×4**，箱子是 **6×4**，因此需要
  **6 個連續空欄**才放得下；這是原版 `GameGridInventory` 的限制，不是本 MOD
  造成的。**櫃台滿＝該件本次不販售**：先 `GraphUtils.CanAccept(front, box)`
  探測，放不下就 `Destroy()`＋`[CF-THIEF] counter full, skipped <id>`，
  不進後台、不進玩家背包。
- **先 CF 箱、再原版庫存**：若原版 thief 商品因此被擠掉，這是刻意取捨；
  CF 箱需要 6 個連續空欄，原版先鋪會讓箱子幾乎永遠放不下。
- **P7-1（0.9.0）**：被打開的贓物箱會把自身熱度繼承給箱內 loot（刻意的行為變更，
  §3.14.1）；未開箱直接轉賣則不繼承。

### §3.10.5 診斷 log

- `[CF-THIEF] box family=… tier=… id=… bm=… added=…`
- `[CF-THIEF] counter full, skipped <id>`
- `[CF-THIEF] skip: no T2/T3 box (bm=…)`（同原因只印一次）

## §3.11 Wild Network「CardFence」分頁（P4／P5）

### §3.11.1 分頁寬度先量 tabBar

`WildNetworkStyle` 的實測常數（不是猜的）：

- 視窗寬 `WINDOW_W = 1664`；分頁條位置 `TABS_X = 536`、寬 `TABS_W = 1060`。
- 單一分頁 `TAB_W = 256`、`TAB_GAP = 12`；`WildNetworkUI.BuildTabs` 的步進是
  `num += 268f`（= 256 + 12），所以 n 個分頁實際佔 `n * 268 - 12`。

現有分頁（原版）：`""`（Basic）與 `"Advanced"` 共 2 個。

| 分頁數 | 佔用寬度 | 對 1060 |
|---:|---:|---|
| 2（現況） | 524 | 剩 536 |
| 3（＋CardFence） | **792** | **剩 268，夠** |
| 4（＋第 4 個分頁） | 1060 | 剛好貼齊、無餘量 |

結論：**CardFence 用第 3 個分頁，`tab = "CardFence"`，不需要塞進 Advanced。**
但**不准再加第 4 個分頁**（例：P5 索耶若想要獨立分頁，會剛好 1060、沒有邊距）；
P5 的索耶升級要嘛放進 `CardFence`，要嘛放進 `Advanced`，不得新增分頁。
**P5 裁定：三個索耶升級全部放進 `CardFence`（`tab="CardFence"`），分頁數仍是 3、
寬度仍是 792/1060。**
若未來真的需要第 4 個分頁，必須先重做 `tabBar` 寬度（改 prefab），不是改這段算術。

### §3.11.2 標籤與註冊

- `OnEnable` 呼叫 `ModHelper.SetNetworkTabLabel("CardFence", () => "制卡")`；
  `GetTabLabel` 會先找 `UI` 表的 `network_ui_tab_CardFence`，沒有才用這個 provider。
- **9 個升級**全部用 `ModHelper.AddNetworkUpgrade(...)`，`tab: "CardFence"`，
  `subtitle` 只有 `Cardmaker`、`Thief`、`索耶` 與 `洗白` 四種。
- title／description 用 `Func<string>` provider 直接回中文（本 MOD 沒有自己的
  在地化檔）；`GetLocalizedTitle` 會先找 `NetworkUpgrade` 表的 `network_<id>_title`
  key，找不到才用 provider，所以之後補在地化不用改代碼。

### §3.11.3 9 個升級（P7 追加兩級洗白）

| id | 價格 | 前置 | subtitle | 效果 |
|---|---:|---|---|---|
| `CF_FENCE_PRO` | 1500 | — | Cardmaker | 一次性發工具包（§3.8.3）；解鎖製卡匠到訪；博士開始賣 `cf_pro_desequencer`／`cf_batch_card_module` |
| `CF_FENCE_T2` | 1200 | `CF_FENCE_PRO` | Cardmaker | 製卡匠與博士加賣 3 張 T2 卡 + 3 個 T2 核 |
| `CF_FENCE_T3` | 3000 | `CF_FENCE_T2` | Cardmaker | 製卡匠與博士加賣 1 張 T3 卡 + 同家族 1 個 T3 核；小偷 T3 箱的前置 |
| `CF_FENCE_THIEF_T2` | 2200 | — | Thief | 黑市聲望 30 就能買 T2 箱（原本 60） |
| `CF_SAWYER_PERMANENT` | 400 | — | 索耶 | 索耶常駐：每夜換卡、接管原版三個 crate barter；`onUnlock` 發換卡目錄紙本（§3.9） |
| `CF_SAWYER_BATCH` | 700 | `CF_SAWYER_PERMANENT` | 索耶 | 取消「每晚 1 箱」上限 |
| `CF_SAWYER_CHIP` | 900 | `CF_SAWYER_PERMANENT` | 索耶 | 芯片升級：3 個同家族同級核心 → 下一級（T3 不升） |
| `CF_LAUNDRY_FAST` | 1200 | — | 洗白 | 贓物熱度的每晚自然衰減翻倍（18 → 36，§3.14.3） |
| `CF_LAUNDRY_CLEAN` | 3000 | `CF_LAUNDRY_FAST` | 洗白 | 今後買進的物品與其開箱子物品熱度清零（§3.14.4） |

- 9 個升級都是**取得管道**的開關；只有 `CF_FENCE_PRO` 與 `CF_SAWYER_PERMANENT`
  會直接發物品（各一次性）。
- `CF_SAWYER_*` 的三個**沒有**「交換所已解鎖」的 prerequisites id：那條由
  `IsHiddenForRun` postfix（§3.11.4）負責，不要重複加前置。
- `state = 1`（未買）、不可重複、無 cooldown；`recommended` 不設，避免佔用
  recommended 卡位。
- 圖示 `iconKey` 不指定：`WildNetworkUI.ResolveIcon` 會 fallback 到 `BUY_REFERAL`，
  不會出現空圖；9 個升級都還沒有專屬 icon。
- **刪除的舊 id**：`CF_FENCE_CONTACT`、`CF_FENCE_BATCH`、`CF_FENCE_SUPPLY`、
  `CF_FENCE_T1`、`CF_FENCE_THIEF`。舊存檔若買過這些 id，`networkUpgrade`
  字典裡會留下孤兒條目，但 `GetDefinitionsInOrder` 只認已註冊的定義，
  不會顯示、也不會影響新升級。

### §3.11.4 CF_SAWYER_* 隱藏

- Postfix `NetworkUpgrade.IsHiddenForRun(string id)`：`id` 以 `CF_SAWYER_` 開頭，
  且 `PlayerStore.Instance.exchangeManager.isExchangeUnlocked == false` 時回 `true`。
- 也就是：**交換所未解鎖前**索耶鏈完全隱藏；交換所解鎖後回歸原版判斷
  （原版仍會依 `Advanced`／`IsPropertyPaid` 決定）。0.7.0（P5）起三個
  `CF_SAWYER_*` 已註冊，這條規則才真正看得到效果。
- postfix 只在 `__result` 為 false 時補 true，不動其他 id、不覆寫原版已判定的隱藏。

### §3.11.5 診斷 log

- `[CF-NET] register start id=… prereq=…`
- `[CF-NET] register ok id=…`
- `[CF-NET] register failed id=… stage=ModHelper.AddNetworkUpgrade …`
- `[CF-NET] registered N/9 CardFence upgrades tab=CardFence width=792/1060`
  （歷史：0.7.0–0.8.0 是 `N/7`，0.9.0 起 `N/9`）
- `[CF-NET] hide CF_SAWYER_* until the exchange is unlocked`（只有 `ok == 9` 才印）
- `[CF-NET] PRO kit granted printer+chip+cnc+2 electronic+2 plastic+battery`
- `[CF-INVENTOR] stock desequencer+batch`
- 索耶本身的 log（`[CF-SAWYER] …`）見 §3.9.8。

### §3.11.6 註冊時序（0.5.2 修正）

0.5.1 在 `OnEnable` 直接呼叫 `CfNetwork.EnsureRegistered()`，實機啟動拿到：

```text
[CF-LOOT] registered 22 tables
Error: NullReferenceException ... at CardCraftAndFenceMod.OnEnable () [0x000a]
[ModLoader] Init failed: CardCraftAndFence v0.5.1
```

根因**不是 prerequisites**，而是：

- `ModHelper.AddNetworkUpgrade` → `NetworkUpgrade.RegisterModUpgrade` 會讀
  `PlayerStore.Instance`；這個 getter 在沒有存檔時**會自己 `new PlayerStore()`**。
- 主選單階段 `NewGameData.Instance` 尚未準備好，`PlayerStore` 建構子就會 NRE。
- `RegisterModUpgrade` 是**先把 definition 放進 `modDefinitions`、再去碰
  `PlayerStore.Instance`**，所以 PRO 被加進去後才炸；T2／T3／THIEF_T2 完全沒跑到。
  UI 因此只看到一張 Cardmaker 卡、3 格中 1 格有內容（其餘是 layout 的空格），
  Thief 卡不存在。

修正：

- `OnEnable` **只做** `CfNetwork.EnsureTabLabel()`（純 static 字典，不碰 PlayerStore）；
  0.7.0 起再加一個同樣純 static 的 `CfSawyerPaper.EnsureRegistered()`（只碰
  `PaperBookRegistry`／`ModItemDirectory`，不碰 PlayerStore）。
  `CfNetwork.EnsureRegistered`／`CfCmdBoxSprite.EnsureRegistered` 仍然不在 OnEnable。
- 升級改在 `ModHook.OnGameLoadedLate`（讀檔）與 `ModHook.OnNewGameStarted`
  （新遊戲）註冊；这时 `PlayerStore.Instance` 已存在且 `networkUpgrade` 可用。
- 每個 `Add` 各自 `try/catch`，印 `register start/ok/failed id=…`，
  最後一定印 `registered N/9`（0.5.2 當時是 `N/4`，0.7.0–0.8.0 是 `N/7`）；
  任何一個失敗都不會讓後面的消失。
- 每個讀檔／新遊戲都重跑一次（`RegisterModUpgrade` 對同 id 是 remove+add，
  `PlayerStore.networkUpgrade` 只在缺 id 時補），所以第二輪新遊戲也會有全部定義
  （0.7.0 起 7 個，0.9.0 起 9 個）。
- 這條也讓 `CF_FENCE_*` 在製卡匠／小偷第一次生成前就存在，gate 不會讀到缺 id。

## §3.12 CMD 箱貼圖＝執行期染色（0.5.1）

CMD 箱不引入任何美術檔：用 `sec_box`／`sec_box_u` 在執行期把來源 hue 換成藍色
（HSV 換色，保留原陰影與飽和），
註冊成本 MOD 自帶的 `cmd_box`／`cmd_box_u`，再由 `SetSpriteAndShapeFromMod` 使用。
輪廓（6×4 格）、alpha 與明暗結構都跟原版治安箱一致。

### §3.12.1 註冊與時序

- 目標字典：`ModLoader.Mods` 裡本 MOD 的 `LoadedMod.Sprites`；
  key `cmd_box`／`cmd_box_u`，已存在就跳過。
- **時序陷阱**：`ModLoader.LoadModDll` 是先呼叫 `mod.OnEnable()`、之後才
  `Mods.Add(loadedMod)`（`ModLoader.cs:1167/1195`），所以 OnEnable 的
  `Mods.Find(...)` 一定找不到自己。實作因此是兩段式（0.5.2 起 OnEnable 完全不呼叫）：
  1. `ModHook.OnModAssetsLoaded` 拿到真正的 `LoadedMod` 後註冊（正常路徑）；
  2. `BuildContainer`／`OnGameLoadedLate` 再懶註冊一次（保險）。
- 註冊成功 log：`[CF-SPRITE] registered cmd_box/cmd_box_u (runtime tint 0.24,0.35,0.62)`。

### §3.12.2 Colorize

- 取子區域：`Rect r = src.rect;` →
  `src.texture.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height)`；
  不要拿整張 atlas。
- 逐像素：轉 HSV；`s<0.15`（白標籤）或綠色指示燈（`h` 0.25–0.45）保留原色；
  其餘 `out = HSVToRGB(0.62, s, min(1, v*1.00))`——把紅／橙底換成指揮卡的
  深藍（卡片取樣 H 0.62），保留原陰影與飽和，不再對亮部加白（0.7.7 起）。
- `BlueHue = 0.62`、`BlueValueGain = 1.00`；**只調這兩個常數**再實機看。
- `new Texture2D(w, h, TextureFormat.RGBA32, false)`、`filterMode = Point`、
  `wrapMode = Clamp`、`SetPixels` + `Apply`；
  `Sprite.Create(tex, new Rect(0,0,w,h), new Vector2(0.5f,0.5f), 100f)`。
- `sec_box` 是 96×64 px，`gridSize = 16` → 6×4 格；`SetSpriteAndShapeFromMod`
  用 `sprite.rect.width / gridSize` 算形狀，所以形狀與治安箱一致。

### §3.12.3 建立、解鎖、存讀檔

- **建立**（`ItemFactory.BuildContainer`，只 CMD 家族）：
  `item.SetSpriteAndShapeFromMod(ModId, "cmd_box")`；
  若 `useSpriteFromMod == false` 或 `spritePath != "cmd_box"` → 先把
  `useSpriteFromMod` 設回 false，再
  `SetSpriteAndShape("Items/items_container", "sec_box")`。
  其他四家族完全不動。
- **解鎖**（既有 `CfTryOpenLockPatch`，沒有新增 patch）：
  1. `box.RemoveGameItemType("SUPPLY_CRATE")`（讓原版 `UnlockContainer` 的
     `Resources.LoadAll` `_u` 換圖分支跳過）；
  2. `LockHelper.UnlockContainer(box)`；
  3. `box.SetSpriteAndShapeFromMod(ModId, "cmd_box_u")`；
     失敗則 `useSpriteFromMod=false` + `SetSpriteAndShape("Items/items_container","sec_box_u")`。
  - 必須先移除 `SUPPLY_CRATE`：原版 `LockHelper.UnlockContainer` 會在該 tag 存在時
    用 `spriteAtlasPath` + `"_u"` 去 `Resources.LoadAll`，看不到 mod sprite。
- **存讀檔**：`ModHook.OnGameLoadedLate` 掃
  `PlayerStore.Instance.FindAllItem(false)`，對 `spriteAtlasPath == ModId` 且
  `spritePath` 是 `cmd_box`／`cmd_box_u` 的物品重跑 `SetSpriteAndShapeFromMod`；
  避免 `useSpriteFromMod` 掉旗標後被畫成 `unknown`。
  log：`[CF-SPRITE] reapplied mod sprite to N CMD crate(s) after load`。
- 兩個 fallback 都要先把 `useSpriteFromMod` 設為 false：`SetSpriteAndShape`
  本身不會清這個旗標，否則 renderer 會繼續拿 `Items/items_container` 當 mod id
  去查字典，畫成 `unknown`。

### §3.12.4 不變的承諾

- 不改 `sec_box`／`sec_box_u` 原版 sprite，也不改其他四家族的箱子外觀。
- 不新增 Harmony patch：用既有的 `CfTryOpenLockPatch`、`ModHook.OnGameLoadedLate`
  與 `ModHook.OnModAssetsLoaded`。
- 零美術檔；紋理是執行期 `Texture2D`，`Point` filter、無 mipmap。

## P4 未核實（截至本輪）

- 工匠、博士、小偷的實際 UI／對話流程（需人類開遊戲）。
- T3 箱的 12 件在 9×8／10×8 是否真的放得下（未在 runtime 量）。
- T3 模型 EV 的「200 樣本」是離線同權重模擬，不是 in-game probe（見 §3.3）。
- 工匠一次到訪 10 個 T1 + 6 個 T2 + 2 個 T3 是否會把前台塞滿。
- **CMD 箱染色**：深藍 `0.24/0.35/0.62` 的實際觀感、6×4 輪廓與原版治安箱的
  對齊、`SetNativeSize` 後的大小、解鎖 `_u` 換圖、存讀檔重貼，都只有離線
  IL／字串核對，未進遊戲看過。
- `ModLoader.Mods` 在 `OnEnable` 之後才 `Add`：本輪用 `OnModAssetsLoaded` 補註冊；
  這個時序在實機是否早於第一個 CMD 箱，未用 `Player.log` 驗。

## §3.13 江洋大盜（P6，0.8.0）

> 本節只寫「已寫進代碼、且已用離線檢查核對過」的規則；實機到訪、購買、開箱
> 尚未驗，見 §3.13.6。

### §3.13.1 客戶欄位

- `identifier = "cf_highwayman"`；`displayName` 用
  `LocHelper.GetLocalizedOrDefault("Client", "name_cf_highwayman", "江洋大盗")`。
- `StoreClientFactionSetup.InitBlackMarket(client)`（`FACTION_BLACK_MARKET`＋
  `BLACK_MARKET` tag）、`clientIntent = ClientIntent.SELL`、`nonShootable = true`、
  `spriteName = SpriteDict.GetRandomShadySpriteName()`、
  `acceptRetailMarkup = false`。
- 對話兩行：`dialog_cf_highwayman_1`（黑市一箱封好的 CMD 物資、不拆不驗）、
  `dialog_cf_highwayman_2`（不問來路、當場結帳）；`SetEndAction` 掛在**最後一行**，
  執行 `Offer()`。
- `AddBasicDialog()`、`CompleteClientCreation()`、
  `ModHelper.SetClientSource(client, ModId)`，最後由呼叫端
  `manager.AddClient(client)`。
- **沒有新增 Harmony patch**（總數仍是 8）：只用既有的
  `ModHook.OnGenerateCustomerVeryLate`；`OnEnable` 先 `-=` 再 `+=`，
  `OnDisable` `-=`。

### §3.13.2 每日 roll（黑市聲望）

- 前置（缺一就跳過；同一原因 process 內只印一次 log）：
  1. `manager`、`StoreStation.Instance`、`PlayerStore.Instance` 都不是 null；
  2. `CF_FENCE_PRO` 已解鎖；
  3. `StoreReputation.GetBMReputation() >= 30`；
  4. `StoreStation.Instance.dayCounter > 0`。
- 每天只 roll 一次：`RNG.GetRandomInt(1, 100) <= chance`；結果寫進存檔型
  modData：`highwayman_roll_day`（預設 -1）、`highwayman_roll_ok`（0/1，預設 0）、
  `highwayman_added_day`（預設 -1）。同一天重複 `GenerateClient` 只讀快取，
  每天最多印一行 roll／cached。
- roll 成功才加入 1 個 SELL client；`highwayman_added_day == day` 時不再加第二個。
- **沒有** `day % N`、沒有固定星期、沒有補訪、沒有追趕（catch-up）。

| 黑市聲望 | 每日機率 | 平均間隔（天） |
|---:|---:|---:|
| 30–59 | 8% | 12.5 |
| 60–84 | 14% | 7.1 |
| 85–149 | 20% | 5 |
| >=150 | 33% | 3.03 |

- `ChanceFor(int bm)` 的門檻固定為 `>=150 -> 33`、`>=85 -> 20`、`>=60 -> 14`、
  其餘 `8`。機率是離線 10 萬日模擬（`check.py`）核對，不是實機統計。

### §3.13.3 箱等級（到訪時重讀聲望）

| tier | 條件（由上往下第一個成立） | id | 價格 |
|---:|---|---|---:|
| 3 | `CF_FENCE_PRO` + `bm>=30` + `CF_FENCE_T2` + `CF_FENCE_T3` + `bm>=85` | `cf_cmd_box_t3` | 500 |
| 2 | `CF_FENCE_PRO` + `bm>=30` + `CF_FENCE_T2` | `cf_cmd_box_t2` | 350 |
| 1 | `CF_FENCE_PRO` + `bm>=30` | `cf_cmd_box_t1` | 200 |

- id 一律由 `ItemFactory.ContainerId(CardFamily.CMD, tier)` 取得，**不硬寫**；
  價格／容量／loot 表完全不動（200／350／500，容量 10×8／12×9／15×9）。
- 每次到訪**只給 1 箱**；不因聲望或任何加成增加數量。

### §3.13.4 上架

- 先 `GraphUtils.CanAccept(EmporiumEntry.Instance.frontInvinvElement, box)` 探測；
  放不下就 `box.Destroy()`＋`counter full, skipped <id>`，不進後台、不進玩家背包。
- 放得下才 `PlayerStore.Instance.TryAddDirectSellingItemToTable(
  box, isOwend: false, isStolen: true)`；CMD 箱一律標 stolen。
- `TryAddDirectSellingItemToTable` 回 `false` 時**不再 Destroy**（原版已銷毀），
  只印 `added=false`。
- 全程 try/catch：hook 例外印 `visit hook failed`、endAction 例外印
  `offer failed`，都不讓例外外洩。

### §3.13.5 診斷 log

- `[CF-HIGHWAY] skip: CF_FENCE_PRO not unlocked`
- `[CF-HIGHWAY] skip: bm=<n> < 30`
- `[CF-HIGHWAY] roll day=<d> bm=<n> chance=<c> ok=<0|1>`
- `[CF-HIGHWAY] roll cached day=<d> bm=<n> chance=<c> ok=<ok>`（每天最多一行）
- `[CF-HIGHWAY] visit day=<d> tier=<1|2|3> id=<id> bm=<n>`
- `[CF-HIGHWAY] no item for <id>`
- `[CF-HIGHWAY] counter full, skipped <id>`
- `[CF-HIGHWAY] box tier=<n> id=<id> added=<true|false>`
- `[CF-HIGHWAY] visit hook failed: <Type>: <Message>`
- `[CF-HIGHWAY] offer failed: <Type>: <Message>`
- 以上都帶 `[CardCraftAndFence] ` 前綴。
- 每日診斷**最多一行**：新 roll 當天先佔用 `roll-day-<d>` 這個 diag key，之後
  同日重複 `GenerateClient` 不會再印 `roll cached`；換 process（存檔重載）後
  `_diag` 清空，才會再印一次 `roll cached`。
- `Offer()` 在到訪時重讀聲望；若已被降回 bm<30／PRO 失效，沿用上面兩條
  `skip:` 格式，不新增別的格式。

### §3.13.6 實機驗收（尚未跑）

- 啟動：`Init ... v0.8.0`、`enabled v0.8.0`、Harmony 仍 8、無 `Init failed`。
- `CF_FENCE_PRO` + bm>=30：log 出現 `[CF-HIGHWAY] roll …` 與
  `[CF-HIGHWAY] visit … tier=1 id=cf_cmd_box_t1`，店裡出現「江洋大盜」；
  買下後前台有 1 個 CMD T1 箱，開箱有 loot、有鎖、貼圖是深藍 CMD 箱。
- `CF_FENCE_T2`：tier=2、`cf_cmd_box_t2`、價 350；`CF_FENCE_T3` + bm>=85：
  tier=3、`cf_cmd_box_t3`、價 500。
- 同一天重複 `GenerateClient`／存檔重載：只有一行 roll（或 cached），
  只出現 1 個江洋大盜。
- 前台塞滿：`counter full, skipped cf_cmd_box_*`，沒有例外、沒有重複 Destroy。
- bm<30：`skip: bm=… < 30` 只印一次；未買 PRO 時
  `skip: CF_FENCE_PRO not unlocked` 只印一次。

## §3.14 P7 贓物熱度（0.9.0）：箱子繼承＋兩級洗白

> 本節只寫「已寫進代碼、且已用離線檢查核對過」的規則；**沒有任何實機結果**。
> 開箱繼承、夜間衰減、購買洗白、UI、存讀檔都尚未在遊戲內看過（§3.14.6）。
> 0.9.0 只新增兩個升級與一個 Harmony patch；P5／P6 的類別本體不變。

### §3.14.1 設計與刻意行為變更（P7-1）

P7-1 在既有的 `CfTryOpenLockPatch.Prefix` 裡，位置是 `if (canOpen)` 之內、
`LockHelper.UnlockContainer(box)` 之前；**沒有新增 patch**。

- 只處理 `box.IsTag("CF_CRATE_TIER")` 的 CF 箱；原版箱子（沒有這個 tag）在
  prefix 開頭就 `return true`，照原版解鎖規則走。
- 只有 `box.IsTag("STOLEN_TAG")` 才進入繼承分支：
  1. `int boxHeat = StolenHelper.GetFoundChance(box)`。
  2. `boxHeat > 0` 時，對 `GraphUtils.FindAllChildrenType<GameItem>(box)`
     的每個子物品（遞迴 BFS、不含箱子本身；`null` 跳過）：
     `childHeat = child.IsTag("STOLEN_TAG") ? StolenHelper.GetFoundChance(child) : 0`，
     再 `StolenHelper.SetHeat(child, Math.Max(childHeat, boxHeat))`。
  3. 不論 `boxHeat` 是否為 0，最後都 `StolenHelper.RemoveStolenStatus(box)`，
     把箱子外殼洗淨。
  4. log `[CF-LAUNDRY] open box=<id> heat=<n> children=<n>`。
- 刻意後果（**行為變更**）：
  - **P4 小偷的 T2／T3 分級箱與 P6 江洋大盜 CMD 箱**（都是 CF 箱、都會標
    stolen；原版 T1 箱不是 CF 箱、不受影響）：開箱後熱度轉到箱內 loot，
    箱子本身變乾淨。這是 P7-1 有意為之，不是 bug。
  - **未開箱的贓物箱直接賣給客戶**：買方拿到的是原本的熱箱；繼承只發生在
    「開箱」這條路徑，轉賣不繼承、也不洗白。
  - **索耶換卡產出的箱子**：產出時就是乾淨的（沒有 `STOLEN_TAG`），整個分支
    不會進入，箱內物件維持乾淨。
  - **`boxHeat == 0`**：不打 `SetHeat`、箱內子物品原樣不動；但外殼仍會
    `RemoveStolenStatus`（若它還帶著 `STOLEN_TAG`）。
  - 只有 CF 箱會走這個 prefix；原版箱子的解鎖行為完全不變。
- `GraphUtils.FindAllChildrenType<GameItem>` 是遞迴走訪，箱內多層容器也算在內；
  `children` 計數是實際走到的子物品數，不是「有被改熱」的數量。
- 開箱仍需要對應的鑰匙卡（`canOpen` 條件不變）；繼承只在使用者真的開箱時發生。

### §3.14.2 兩級升級（`tab="CardFence"`、`subtitle="洗白"`）

| id | 價格 | 前置 | title | 效果 |
|---|---:|---|---|---|
| `CF_LAUNDRY_FAST` | 1200 | —（無 prerequisites） | 快速洗白 | 贓物熱度的每晚自然衰減翻倍（18 → 36） |
| `CF_LAUNDRY_CLEAN` | 3000 | `new[] { CF_LAUNDRY_FAST }` | 彻底洗白 | 今後買進的物品及其開箱子物品熱度清零 |

- 兩個都註冊在 `CfNetwork.EnsureRegistered()`，`tab: "CardFence"`、`subtitle: "洗白"`；
  既有 7 個升級（Cardmaker／Thief／索耶）與 `CF_SAWYER_*` 的隱藏規則
  （§3.11.4）都不變。
- 註冊 log 變成 `[CF-NET] registered N/9 CardFence upgrades tab=CardFence width=792/1060`；
  hide 那行只在 `ok == 9` 時印。
- **仍然沒有第 4 個分頁**：`CardFence` 是第 3 個分頁，寬度仍是 792/1060
  （§3.11.1）；兩個新升級只能放在這個分頁裡。
- CLEAN 的前置就是 FAST；FAST 未買時 CLEAN 應保持鎖定。兩者都不可重複購買、
  沒有 cooldown、`recommended` 不設、沒有專屬 icon。

### §3.14.3 FAST 實作（被動，夜間多跑一趟）

- `OnEnable` 對 `ModHook.OnHandlingNightlyServicesLate` 先 `-=` 再 `+=`
  `CfLaundry.OnHandlingNightlyServicesLate`；`OnDisable` `-=`。
- hook 點在 `PlayerStore.EndNight` 的 `PlayerStore.cs:1744`，早於原版
  `StolenHelper.UpdateStolenItem` 的迴圈（1757，呼叫點 1759），所以這裡多跑的
  一趟會疊在原版那一趟之前。
- handler 立即返回的條件：`CF_LAUNDRY_FAST` 未解鎖、`PlayerStore.Instance` 或
  `StoreStation.Instance` 為 null、`dayCounter <= 0`。
- 通過後：對 `PlayerStore.Instance.FindAllItem()` 裡每個
  `StolenHelper.IsStolenItem(item)` 的物品，多做一次
  `StolenHelper.UpdateStolenItem(item)`；原版那一趟之後照常再跑一次。
- 因此基礎衰減是 **18 × 2 = 36／晚**；熱度 100 的贓物一晚變 64，未買 FAST
  的對照是 82。帶 `CONTRABAND_LEVEL_LOW/MID/HIGH/CRITICAL` 時，**兩趟各自**
  少 2／4／6／8，合計一晚 32／28／24／20（比 36 少 4／8／12／16）。
- `laundry_last_day`（`ModHelper` modData，預設 -1）記錄上次執行的
  `StoreStation.Instance.dayCounter`；同一天再 `EndNight` 直接返回，避免
  reload 後同一天扣兩次。寫入發生在衰減迴圈之後。
- 只處理 `IsStolenItem` 為 true 的物品；乾淨物品不碰。log
  `[CF-LAUNDRY] night day=<d> items=<n>`，`items` 是實際多跑一趟的件數。
- **FAST 沒有新增 Harmony patch**，只用既有的 `ModHook`。

### §3.14.4 CLEAN 實作（購買後洗白）

- 這是 P7 唯一的新 patch（總數 8 → 9）：
  `[HarmonyPatch(typeof(PlayerStore), nameof(PlayerStore.BuyItem))]`，postfix
  `CfLaundryBuyPatch.Postfix(GameItem item)`。
- gate：`CfNetwork.Unlocked(CfNetwork.LaundryClean)` 且 `item != null`；
  其餘直接 return。
- 先 `StolenHelper.RemoveStolenStatus(item)`，再對
  `GraphUtils.FindAllChildrenType<GameItem>(item)` 的每個子物品
  `RemoveStolenStatus`；子物品的 `STOLEN_TAG` 與 `STOLEN_VALUE_INT` 都會清掉。
- log `[CF-LAUNDRY] bought clean id=<id> children=<n>`，而且只有在
  「物品本身原本是 stolen」或「至少一個子物品原本是 stolen」時才印；
  買乾淨物品不會有這行。
- `PlayerStore.BuyItem(GameItem)` 是共同購買路徑：`BuyItemFromTable()` 與
  `NegociationUIManager.BuyMultipleItem` 最後都呼叫它，所以一個 postfix 就涵蓋
  檯面購買與多選購買。postfix 只做事後清理，不改價格、不擋交易。
- **只影響今後的購買**：已經在背包／店裡的贓物不會回溯洗白，照常每晚衰減。
- 因為 `STOLEN_TAG` 消失，原版的贓物售價加成與 `GetStolenItemTradingRepBonus`
  的聲望加成也一併不再適用（`ApplyStolenDiscount`／
  `GetStolenItemTradingRepBonus` 都以 `STOLEN_TAG` 為條件）。
- 沒有其他 patch、沒有新物品、沒有改 loot 表。

### §3.14.5 Patch 數 8 → 9 與實際清單

`Probe-Patches.ps1` 對 `CardCraftAndFence.dll` 的實際輸出（節錄）：

```text
patched 9 method(s)
  DirectoryMaster.Item
  LockHelper.CreateLockerContainerTooltip
  LockHelper.TryOpenLock
  MachinePrinter.FinishPrinting
  NetworkUpgrade.IsHiddenForRun
  PlayerStore.BuyItem
  StoreClientList.CreateInventor
  StoreClientList.CreateInventorStorage
  StoreClientList.CreateThief
```

- 第 9 個就是 `PlayerStore.BuyItem`（`CfLaundryBuyPatch`）。
- P7 新增的診斷 log（都帶 `[CardCraftAndFence] ` 前綴）：
  - `[CF-LAUNDRY] open box=<id> heat=<n> children=<n>`
  - `[CF-LAUNDRY] night day=<d> items=<n>`
  - `[CF-LAUNDRY] bought clean id=<id> children=<n>`
  - `[CF-LAUNDRY] night handler failed: <Type>: <Message>`
- 其餘舊 log 不變；`[CF-NET] registered` 由 `N/7` 變 `N/9`（§3.11.5）。

### §3.14.6 離線驗收（實際輸出）與實機卡（尚未跑）

**版本與產物**

- `src/CardCraftAndFence/CardCraftAndFence.cs`：`internal const string ModVersion = "0.9.0";`；
  `manifest.xml`：`<ModVersion>0.9.0</ModVersion>`。
- `dist/CardCraftAndFence/CardCraftAndFence.dll` = **81,408 bytes**，SHA256
  `672BB474B90EC59B3A6E92CF834D4D25639D3E972602235F46B24128F9BB20CD`。
- `AppraisalAssistant.dll` 15,872 bytes／`44BAFD769ACDF9B0BE35C56CF636D2E885102F00403281A06D90C8C6B335EA2E`；
  `AugSpotter.dll` 9,216 bytes／`CA20B617507A1FC1A9D53F4773F0A7A459BC40D71BCB7C0933E85DAE82332DA7`。
- `Build-Mods.ps1` exit 0，3 個 DLL。

**Verify-Mods.ps1**：`RESULT: PASS`；CardCraft 9 patches，包含新的
`DeepSpaceQoL.CfLaundryBuyPatch -> PlayerStore.BuyItem`；無 banned API；
`HarmonyPatch attributes checked (9)`。

**Probe-Patches.ps1**：`RESULT: PASS`；實際 `patched 8 method(s)`（AppraisalAssistant）、
`patched 4 method(s)`（AugSpotter）、`patched 9 method(s)`（CardCraftAndFence，
清單含 `PlayerStore.BuyItem`）。
**已知差異**：P7 交辦單預期 `9 / 4 / 9`，但 AppraisalAssistant 歷來就只有
**8** 個 patch attribute，且它的 DLL 與 P7 前建置位元相同；**不要**為了湊 9
去改 AppraisalAssistant。實際以 **8 / 4 / 9** 為準。

**Probe-Logic.ps1**：`RESULT: PASS`；
`[Scan] CardCraftAndFence.dll refused=False capabilities=[] findings=0`。

**離線檢查**

- `python .scratch\p7laundry\check.py`：`passed: 107  failed: 0`／
  `P7 LAUNDRY CHECK: PASS`，exit 0。
- `python .scratch\p6highwayman\check.py`：`passed: 88  failed: 0`／
  `P6 HIGHWAYMAN CHECK: PASS`，exit 0。標題行仍印 `P6 HIGHWAYMAN CHECK (0.8.0)`
  （只允許改 patch 數 8→9 與 h1/h2 版本 0.8.0→0.9.0）；這是美觀上的已知項目，
  不是失敗。

**實機驗收卡（A–F，全部尚未跑；agent 不啟動遊戲）**

> 安裝：完全退出遊戲 → 備份存檔 → 把 `dist\CardCraftAndFence\*`
> （81,408／`672BB474…`）覆蓋到 Mods → 重新核可。啟動 log 應有
> `Init … v0.9.0`、`enabled v0.9.0`、`[CF-NET] registered 9/9`、`Harmony: 9`。

- **A. 開箱繼承／索耶乾淨箱（P7-1）**：買一個上鎖的贓物箱（小偷或江洋大盜），
  記下開箱前箱子的熱度；用對應鑰匙卡打開，log 應有
  `[CF-LAUNDRY] open box=<id> heat=<n> children=<n>`。箱子本身應不再帶
  `STOLEN_TAG`（不再被查），箱內每件 loot 的熱度應為
  `max(原 childHeat, boxHeat)`。反向：索耶換卡產出的乾淨箱開箱時**不應**出現
  `[CF-LAUNDRY] open`，箱內物件維持乾淨。
- **B. FAST 衰減**：已買 FAST，帶一件熱度 100 的贓物睡一晚 → 熱度應為 **64**
  （18×2）；未買 FAST 的對照存檔應為 **82**（18）。log 應有
  `[CF-LAUNDRY] night day=<d> items=<n>`；同一天存檔→重載→再睡，不應再扣第二次。
  帶 `CONTRABAND_LEVEL_*` 的物件，兩趟各減 2／4／6／8（合計 32／28／24／20）。
- **C. CLEAN 購買**：已買 CLEAN，買一個帶箱內贓物的箱子（或直接買贓物）→
  log `[CF-LAUNDRY] bought clean id=<id> children=<n>`；買到的箱子熱度 0、
  `STOLEN_TAG` 消失、箱內 children 也全部乾淨。已存在的舊贓物不受影響，
  照常每晚衰減；買乾淨物品不應印這行。
- **D. 未買升級／未開箱轉賣**：未買 FAST／CLEAN 時，贓物照原版 18／晚衰減、
  買進仍是贓物，不應有 P7 的洗白 log。把未開箱的贓物箱直接賣給客戶：
  買方拿到的是熱箱，**不繼承、不被洗白**；log 不應有 `[CF-LAUNDRY] open`。
- **E. UI**：CardFence 分頁應有 **9 個**升級（7 舊 + 快速洗白 + 彻底洗白）；
  UI 依 subtitle 分成 4 張卡片（Cardmaker、Thief、索耶、洗白），洗白那張
  有 2 個格子；log 應有
  `[CF-NET] registered 9/9 CardFence upgrades tab=CardFence width=792/1060`
  與 hide 那行。仍是 3 個分頁、792/1060、**沒有第 4 個分頁**、沒有卡片重疊／
  overflow；CLEAN 在 FAST 未買時應鎖定。
- **F. 存讀檔**：買 FAST／CLEAN 後存檔→重載，升級仍為已買、`9/9` log 重印、
  效果持續；同一天（`dayCounter` 不變）存檔→重載→再睡，`laundry_last_day`
  不讓同一天扣第二次。贓物熱度、箱子 `STOLEN_TAG`、箱內 children 狀態都應
  正確保存。

以上 A–F **全部尚未執行**；在人類回報 `Player.log` 之前，不得寫成已驗收。
