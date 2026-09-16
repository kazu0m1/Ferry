# Ferry v1.0.2 Rubber-band Integration Prototype 24 — ScrollBar double-click回帰修正 実機確認

基準線：**Prototype 23**。最終回帰はPASS済み。本版では、Selector全体へbubbleする `MouseDoubleClick` がScrollBar/true backgroundでも `OpenSelected()` を呼んでいた問題だけを局所修正する。

## A. 最重要 — ScrollBar高速クリック

1. List表示でスクロールバーが出るフォルダーを開く。
2. 任意のファイルまたはフォルダーを1件選択したままにする。
3. 縦スクロールバーの **track / thumb / arrow** を素早く連続クリックする（ダブルクリック相当の速さを含む）。

- [ ] 選択済みアイテムが勝手に開かない。
- [ ] スクロールバー操作は正常に機能する。
- [ ] 選択状態が不自然に変化しない。

同じ確認をGrid表示でも1回行う。

- [ ] Gridでも選択済みアイテムが勝手に開かない。

## B. true background double-click

1. 任意のアイテムを1件選択したままにする。
2. true backgroundを素早くダブルクリックする。

- [ ] 選択済みアイテムが開かない。

## C. 正常なitem double-click回帰

- [ ] Listでファイルをダブルクリック → 従来どおり開く。
- [ ] Listでフォルダーをダブルクリック → 従来どおりFerry内で開く。
- [ ] Gridでファイルをダブルクリック → 従来どおり開く。
- [ ] Gridでフォルダーをダブルクリック → 従来どおりFerry内で開く。

## D. 最小rubber-band回帰

- [ ] Listで通常rubber-bandが成立する。
- [ ] Gridで通常rubber-bandが成立する。
- [ ] 選択済みitemからdragするとfile D&Dになる。
- [ ] List / Grid autoscrollが正常。

## 判定

**PASS / HOLD / FAIL**
