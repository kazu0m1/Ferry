# Ferry v1.0.2 Rubber Band Integration Prototype 11 — 実機確認

Prototype 10 の動画で確認された **WPF ListBox内蔵autoscrollとの競合** を切り分ける版。Grid確認は不要。

## ビルド
1. `Build.cmd`
2. `Portable\Ferry.exe`
3. タイトルが **Ferry - prototype 11** であること。

## A. 下方向 autoscroll
- [ ] 通常rubber-bandを下端まで伸ばすと、下方向autoscrollが開始する。
- [ ] スクロール中に「フォーカス行」が1行ずつ追従せず、画面がフォーカスに引っ張られてチラつかない。
- [ ] pointerをさらに外へ出すと速度が上がるが、急激な別位置へのジャンプは起きない。
- [ ] 新しく現れた行が順次選択される。
- [ ] pointerを中央へ戻すとautoscrollが停止する。

## B. 上方向 autoscroll
- [ ] 下側の位置から上端へrubber-bandを伸ばすと、上方向autoscrollが継続する。
- [ ] 加速させても一気に先頭へジャンプしない。
- [ ] 途中まで選択済みだった画面外の行が、スクロールだけを理由に解除されない。
- [ ] 画面内でrubber-bandを少し動かさなくても、その時点の表示行/通過行の選択状態が正しい。

## C. Prototype 9/10 最小回帰
- [ ] true background単クリックで選択解除できる。
- [ ] 行内余白1回クリック → `Shift+Click` のanchorが正しい。
- [ ] 通常rubber-band / `Ctrl + rubber-band` XOR / `Shift + rubber-band` UNION が従来どおり。
- [ ] 選択済み行からの通常dragはfile D&Dになる。
- [ ] ウィンドウ外MouseUpでrubber-bandが正常終了する。

## 今回まだ対象外
- Grid表示のautoscroll
- `Ctrl+Shift + rubber-band`
- Modifierをdrag途中で押す/離す場合のExplorer完全一致
