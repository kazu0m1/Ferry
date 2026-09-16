# Ferry v1.0.2 Rubber-band Integration — 最終回帰チェックリスト

基準線：**Prototype 23**

目的：Prototype 1〜23で確認・修正してきた主要な回帰ポイントを、正式実装前に一度だけ通して確認する。  
対象：List / Grid の rubber-band、Shift anchor、Ctrl / Shift / Ctrl+Shift、D&D共存、autoscroll、View切替、仮想化。  
※ 速度の再チューニングや20,000 item級の追加ストレス試験は不要。

---

## 0. ビルド・起動

- [OK] `Build.cmd` が成功する。
- [OK] `Portable\Ferry.exe` が正常起動する。
- [OK] タイトルが **Ferry - prototype 23** である。
- [OK] List / Grid の切替が正常にできる。
- [OK] 外部でフォルダー内容を追加・削除・変更後、`F5` で表示へ反映される。

---

## 1. 基本クリック / true background / Shift anchor

### 1-1. true background

- [OK] 1件選択 → true background単クリック → **0件選択**。
- [OK] `Ctrl+Click` で複数選択 → true background単クリック → **0件選択**。
- [OK] `Shift+Click` 範囲選択 → true background単クリック → **0件選択**。
- [OK] rubber-band複数選択 → true background単クリック → **0件選択**。

### 1-2. Shift anchor

- [OK] true backgroundで0件 → 未選択行の行内余白を1回通常クリック → 別行を `Shift+Click` → **最初の1クリック行がanchor**。
- [OK] Aを通常クリック → Eを `Shift+Click` → A〜E。
- [OK] その選択範囲内のCを修飾キーなしで1回クリック → Cだけ選択 → Gを `Shift+Click` → **C〜Gだけ**。
- [OK] 逆方向の範囲選択後でも、単クリックした行が新しいShift anchorになる。
- [OK] 「同じ行を2回クリックしないとanchorが更新されない」旧症状が再発しない。

---

## 2. D&Dとの境界

- [OK] 未選択行の **icon / name等file hot zone** からdrag → rubber-bandにならず、file gesture側。
- [OK] 選択済み行から通常drag → **file D&D**。
- [OK] 選択済み行から `Ctrl+Shift` を押してdragしても、既存のfile D&D経路を壊さない。
- [OK] 未選択行の **行内余白** からdrag → rubber-bandを開始できる。
- [OK] true backgroundからdrag → rubber-bandを開始できる。

---

## 3. List — 通常 rubber-band

- [OK] threshold未満ではrectangleが出ない。
- [OK] threshold超過後、MouseDown起点からrectangleが出る。
- [OK] 上下左右4方向へdragできる。
- [OK] B/Cを囲う → B/C選択。
- [OK] Cを外す → Bだけ。
- [OK] Cを再度入れる → B/C。
- [OK] 縮小・再拡大で選択状態が安定する。
- [OK] MouseUpですぐrectangleが消える。
- [OK] window外へdragしても継続し、window外MouseUpで正常終了する。

---

## 4. List — Ctrl rubber-band

事前選択：A/C。rectangle：B/C。

- [OK] `Ctrl + rubber-band` → **A/B**（初期選択 XOR current hit）。
- [OK] 矩形を縮めたり戻したりしても、同じ位置で同じ選択状態になる。
- [OK] true background開始でも成立する。
- [OK] 未選択行の行内余白開始でも成立する。

---

## 5. List — Shift rubber-band

- [OK] Shiftを押したままtrue backgroundから開始 → active rubber-bandは通常band同様に動く。
- [OK] 事前選択A/C、B/Cを囲う → **B/C**。
- [OK] Shiftを離しても、その瞬間には選択集合が変わらない。
- [OK] 範囲を変えない小移動でも選択集合が変わらない。
- [OK] Stationaryな通常 `Shift+Click` は従来どおり範囲選択になる。

---

## 6. List — Ctrl+Shift / 未選択行余白開始

連続行 A / B / C。

### 6-1. B → C

- [OK] Aを通常クリック。
- [OK] `Ctrl+Shift` を押したまま未選択B行の行内余白でMouseDown。
- [OK] B行内で数px動かしてrubber-band開始 → **A/B**。
- [OK] Cへ入る → **A/B/C**。
- [OK] MouseUp後も A/B/C。

### 6-2. C → B

- [OK] Aを通常クリック。
- [OK] `Ctrl+Shift` を押したまま未選択C行の行内余白でMouseDown。
- [OK] C行内で数px動かしてrubber-band開始 → **A/B/C**。
- [OK] Bへ入る → **A/C**。
- [OK] C側へ戻す → **A/B/C**。

---

## 7. List — Ctrl+Shift / true background開始

### 7-1. 初期選択 A

Dより下のtrue backgroundから開始し、下→上へ D → C → B → A の順にrectangleへ入れる。

- [OK] active開始直後：A
- [OK] D：A/D
- [OK] Cまで：A/C/D
- [OK] Bまで：A/B/C/D
- [OK] Aまで：B/C/D
- [OK] Aを外す：A/B/C/D
- [OK] Bを外す：A/C/D
- [OK] Cを外す：A/D
- [OK] Dを外す：A

### 7-2. 初期選択 A/C

- [OK] active開始直後：A/C
- [OK] Dへ入る：A/C/D
- [OK] Cへ入る：A/D
- [OK] Bへ入る：A/B/D
- [OK] Bを外す：A/D
- [OK] Cを外す：A/C/D
- [OK] Dを外す：A/C

---

## 8. drag途中Modifier

### 8-1. Normal開始後

事前選択A/C → Modifierなしでtrue background開始 → B/Cを囲う。

- [OK] B/C。
- [OK] active中にCtrlを押す → B/Cのまま。
- [OK] Ctrlを離す → B/Cのまま。
- [OK] Shiftを押す → B/Cのまま。
- [OK] Shiftを離す → B/Cのまま。
- [OK] Dへ広げる → D選択。
- [OK] Dを外す → D解除。
- [OK] D再侵入 → D再選択。

### 8-2. Ctrl開始 → Ctrl解放

事前選択A/C → Ctrl開始 → B/C。

- [OK] Ctrl保持中：A/B。→
- [OK] Ctrl解放直後：A/B。
- [OK] 小移動：A/B。
- [OK] Cだけをrectangle外へ出す：A/B。
- [OK] C再侵入：A/B/C。

### 8-3. Ctrl開始 → 解放 → Ctrl再押下
→試験手順が不明なためスキップ
- [ ] Ctrl再押下直後：A/B/C。
- [ ] Cを外す：A/B。
- [ ] C再侵入：A/B/C。
- [ ] Ctrl再押下でXORモードへ戻らない。

### 8-4. threshold前

- [OK] ModifierなしMouseDown → threshold前にCtrl押下 → drag開始：**MouseDown時Normalのまま**。
- [OK] ModifierなしMouseDown → threshold前にShift押下 → drag開始：**MouseDown時Normalのまま**。
- [OK(Aから開始前提と理解)] Ctrl MouseDown → threshold前にCtrl解放 → B/Cを囲う：A/B/C。
- [OK] そこからCを外す：A/B。
- [OK] C再侵入：A/B/C。

### 8-5. Ctrl+Shift途中解放
→これも試験手順が不明。
- [ ] B→CでA/B/C → Shiftだけ解放（Ctrl保持） → 直後A/B/C。
- [ ] Cを外す → B/C。
- [ ] C再侵入 → A/B/C。
- [ ] C→BでA/C → Ctrlだけ解放（Shift保持） → 直後A/C。
- [ ] C側へ戻す → A/B/C。
- [ ] 再びBへ入る → A/C。

---

## 9. List autoscroll

高速側上限：**100**

- [OK] 下端へdragするとautoscroll開始。
- [OK] 上端へdragするとautoscroll開始。
- [OK] edge近傍では低速、外へ離すほど加速する。
- [OK] newly visible itemもrectangleに入れば選択される。
- [OK] pointerを中央側へ戻すとautoscroll停止。
- [OK] 下→上へ逆走しても突然先頭/末尾へ飛ばない。
- [OK] ちらつき・focus行への引っ張られがない。
- [OK] 画面外へ仮想化されたitemが、それだけを理由に選択解除されない。
- [OK] MouseUp後に選択が維持される。

---

## 10. Grid基本回帰

- [OK] true backgroundからrubber-band開始。
- [OK] 未選択tile余白からrubber-band開始。
- [OK] icon/name上からdragしてもrubber-bandにならない。
- [OK] 選択済みtileからdragするとfile D&D。
- [OK] tile余白へのrectangle交差でも選択対象になる。
- [OK] 通常rubber-bandの縮小・再拡大が安定。
- [OK] Ctrl rubber-bandがList相当。
- [OK] Shift rubber-bandがList相当。
- [OK] Ctrl+Shift B→C / C→BがList相当。
- [OK] Ctrl+Shift true background開始がList相当。

---

## 11. Grid autoscroll

- [OK] 下方向autoscroll開始。
- [OK] 上方向autoscroll開始。
- [OK] newly visible tileも選択対象になる。
- [OK] 画面外へ仮想化されたtileが、それだけを理由に解除されない。
- [OK] pointerを中央側へ戻すと停止。
- [OK] 一気に先頭/末尾へ飛ばない。
- [OK] ちらつかない。
- [OK] MouseUp後に選択が維持される。

---

## 12. List ⇄ Grid 選択同期

大量選択状態でも確認する。

- [OK] Listで複数選択 → Gridへ切替 → **同一item集合**が選択される。
- [OK] Grid → Listへ戻す → **同一item集合**が維持される。
- [OK] 1,000 item程度を選択した状態でも件数が増減しない。
- [OK] View切替後の `Shift+Click` でanchorが不自然な過去位置へ戻らない。
- [OK] View切替後もrubber-band / D&Dが正常。

---

## 13. 大量ファイル / 仮想化 最終スモーク

10,000 itemフォルダーを使用。

- [OK] 通常スクロール・List/Grid切替が実用上破綻しない。
- [OK] `Ctrl+A` 全選択が実用上問題ない。
- [条件付きOK] 全選択状態から `Ctrl + rubber-band` を行ってもフリーズ感がなく、XORが安定。
→やや動きがもっさりしているが、ユーザーにフリーズの誤解を与えるようなものではない。フレームレートが60から25に落ちた感じ。あと、マウスポインターの動きにレクタングルが遅れて付いてくる。
- [OK] 数百〜1,000 item規模の長距離rubber-band + autoscrollが安定。
- [OK] 下→上→下の逆走でも選択集合が破綻しない。

### 既知の性能特性

- 5,000 itemをShiftで端から端まで範囲選択：約2〜3秒。
- 10,000 itemをShiftで端から端まで範囲選択：約16秒。
- `Ctrl+A` はほぼ瞬時。
- 上記Shift大量選択は通常利用では稀な極端ケースとして許容し、現段階では最適化対象外。
→パソコン再起動後では10000itemをShiftで端から端まで範囲選択：約10秒。
---

## 14. 終了判定

以下を満たしたらrubber-band仕様凍結候補とする。

- [OK] 重大NG 0件。
- [OK] 選択集合の破損 0件。
- [OK] D&D誤発火 / rubber-band誤発火 0件。
- [OK] List / Grid切替で選択ズレ 0件。
- [OK] autoscrollのワープ / ちらつき / 大量選択消失 0件。
- [OK] 10,000 itemで通常利用上の破綻なし。
- [OK] Prototype 23で確認済みの挙動に新しい回帰なし。

**判定： PASS / HOLD / FAIL**
→PASS

メモ：

-
-
-
