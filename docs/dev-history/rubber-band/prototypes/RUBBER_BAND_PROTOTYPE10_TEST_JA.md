# Ferry v1.0.2 Rubber Band Integration Prototype 10 — 実機確認

Prototype 9 を基準線とし、今回は **List表示のrubber-band自動スクロール** を追加。Grid確認は不要。

## ビルド
1. `Build.cmd`
2. `Portable\Ferry.exe`
3. タイトルが **Ferry - prototype 10** であること。

## A. Prototype 9 最小回帰
- [ ] true background単クリックで選択解除できる。
- [ ] 行内余白を1回クリック → 別行を `Shift+Click` して、最初の行がanchorになる。
- [ ] 通常rubber-band / `Ctrl + rubber-band` XOR / `Shift + rubber-band` UNION が従来どおり。
- [ ] 選択済み行からの通常dragはfile D&Dになる。

## B. List — 下方向autoscroll
アイテム数が多く、縦スクロールバーが出るフォルダーで確認する。

- [ ] 未選択行の**行内余白**から通常rubber-bandを開始し、pointerを表示領域の下端付近まで移動すると下方向へゆっくりスクロールが始まる。
- [ ] pointerをウィンドウ下側へさらに出すと、スクロール速度が上がる。
- [ ] pointerを下端から中央付近へ戻すと、autoscrollがすぐ止まる。
- [ ] スクロールで新しく現れた行がrubber-band選択へ順次追加される。
- [ ] MouseUpするとrubber-bandが即終了し、その時点の選択が残る。
- [ ] MouseUp後に上へスクロールして確認し、途中で画面外へ消えた行の選択が不自然に解除されていない。

## C. List — 上方向 / 反転
- [ ] 途中まで下へautoscrollした状態でpointerを上端へ移動すると、上方向へautoscrollできる。
- [ ] 上下を切り替えても選択が大量に点滅・反転したり、rubber-bandが固まったりしない。
- [ ] ウィンドウ外でMouseUpしてもrubber-bandが正常終了する。

## 今回まだ対象外
- Grid表示のautoscroll
- `Ctrl+Shift + rubber-band`
- Modifierをdrag途中で押す/離す場合のExplorer完全一致
