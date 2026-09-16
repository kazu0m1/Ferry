# Ferry v1.0.2 Rubber Band Integration Prototype 22 — Grid autoscroll 実機確認

Prototype 21 は List 表示の rubber-band 基準線として PASS。Explorer Grid 観察では開始領域・hit 判定・Ctrl/Shift/Ctrl+Shift・D&D は問題なく、**Grid 表示だけ autoscroll が動かなかった**ため、本版は Grid autoscroll のみを追加する。

## ビルド
1. `Build.cmd`
2. `Portable\Ferry.exe`
3. タイトルが **Ferry - prototype 22** であること。

## A. 最重要 — Grid 下方向 autoscroll
1. Grid 表示にし、画面内に収まらない数のファイルがあるフォルダーを開く。
2. true background または未選択tile余白から通常 rubber-band を開始する。
3. rectangle を画面下端まで伸ばし、そのまま少し外側へ動かす。

確認：
- [ ] 下方向へ autoscroll が開始する。
- [ ] スクロールで新しく現れた tile も rectangle に入れば選択される。
- [ ] 途中まで選択した tile が、単に画面外へ仮想化されたことだけを理由に解除されない。
- [ ] pointer を中央側へ戻すと autoscroll が止まる。
- [ ] MouseUp で rectangle が消え、選択が維持される。

## B. Grid 上方向 autoscroll
1. フォルダーを途中まで下へスクロールした状態から rubber-band を開始する。
2. rectangle を上端へ伸ばし、そのまま少し外側へ動かす。

確認：
- [ ] 上方向へ autoscroll が開始する。
- [ ] 新しく現れた tile も正しく選択対象になる。
- [ ] 一気に先頭へ飛ぶ／ちらつく／選択が大量に消える現象がない。

## C. 最小回帰
- [ ] Grid の通常 rubber-band は従来どおり。
- [ ] 選択済みtileからの通常dragは file D&D のまま。
- [ ] List 表示の autoscroll も従来どおり動く。

## 今回不要
- autoscroll の速度評価（上限100は維持）
- Grid の Ctrl / Shift / Ctrl+Shift の再精査（既観察で問題なし）
- drag途中ModifierのGrid完全一致
