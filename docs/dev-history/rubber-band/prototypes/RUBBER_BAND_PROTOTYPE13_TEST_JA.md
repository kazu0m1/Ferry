# Ferry v1.0.2 Rubber Band Integration Prototype 13 — 実機確認

Prototype 12 は不採用。Windows実機でautoscrollまでPASSした **Prototype 11** を直接基準にし、Shift-anchorの追加修正と Ctrl+Shift rubber-band のgesture routingだけを再実装した。Grid確認不要。高速スクロールは25へ変更済みで速度試験不要。

## ビルド
1. `Build.cmd`
2. `Portable\Ferry.exe`
3. タイトルが **Ferry - prototype 13** であること。

## A. 基準線回帰
- [ ] true background単クリックで選択解除できる。
- [ ] 通常 rubber-band が成立する。
- [ ] `Ctrl + rubber-band` XOR が成立する。**開始点は true background または未選択行の行内余白**にする。
- [ ] `Shift + rubber-band` UNION が成立する。
- [ ] 選択済み行から通常dragすると file D&D になる。

## B. 新しく判明した Shift anchor 経路
1. Aを通常クリック → Eを `Shift+Click` して A～E を選択。
2. その選択範囲内の C を修飾キーなしで **1回だけ**クリックして C だけにする。
3. Gを `Shift+Click`。
   - [ ] **C～Gだけ**が選択される。A～Gへ戻らない。
4. 逆方向（E→Shift+A → Cを1回クリック → GをShift+Click）でも、
   - [ ] **C～Gだけ**になる。

## C. Ctrl+Shift — B → C
連続した3行を A / B / C とする。

1. true backgroundで選択解除。
2. Aを通常クリック。
3. `Ctrl+Shift` を押したまま、未選択B行の **行内余白** でMouseDown。
4. B行内で横へ数px動かしてrubber-bandを開始。
5. 下へ伸ばしてC行へ入る。

- [ ] MouseDown直後は A/B が選択状態になる。
- [ ] rubber-bandが開始する。
- [ ] C行へ入ると **A/B/C** が選択される。
- [ ] MouseUp後もA/B/Cが維持される。

## D. Ctrl+Shift — C → B
1. true backgroundで選択解除。
2. Aを通常クリック。
3. `Ctrl+Shift` を押したまま、未選択C行の **行内余白** でMouseDown。
4. C行内で横へ数px動かしてrubber-bandを開始。
5. その後、上へ伸ばしてB行へ入る。

- [ ] MouseDown直後は **A/C** が選択される。
- [ ] rubber-band開始時点で **A/B/C** が選択される。
- [ ] B行へ入ると **Bだけ解除され、A/C** が選択される。
- [ ] C行内へ戻すと **A/B/C** に戻る。

## E. D&D境界
- [ ] 既に選択済みの行からdragした場合、Ctrl+Shiftを押していてもfile D&D側になる。
- [ ] 未選択行のicon/name等のfile hot zoneからはrubber-bandを開始しない。

## 対象外
- Grid表示
- Ctrl+Shiftをtrue backgroundから開始する完全一致
- drag途中でModifierを押す/離す場合のExplorer完全一致
- autoscroll速度の追加評価
