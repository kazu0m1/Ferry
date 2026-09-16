# Ferry v1.0.2 Rubber Band Integration Prototype 15 — 実機確認

Prototype 14 は Windows 実機で rubber-band / Ctrl / Shift / Ctrl+Shift / Shift anchor / D&D / autoscroll がオールクリア。Prototype 15 はその基準線を維持し、**高速 autoscroll の上限だけ 25 → 30 lines/sec に変更**した速度調整版。

## ビルド
1. `Build.cmd`
2. `Portable\Ferry.exe`
3. タイトルが **Ferry - prototype 15** であること。

## 今回の確認
- [ ] List表示でrubber-bandを下端/上端へ伸ばし、高速autoscrollがPrototype 14より少し速いこと。
- [ ] スクロール中に以前の「フォーカス行に引っ張られるチラつき」「上方向で突然先頭へ飛ぶ」が再発しないこと。
- [ ] 速度30が速すぎず、実用上ちょうどよいこと。

## 変更していないもの
- 通常 rubber-band
- `Ctrl + rubber-band` XOR
- `Shift + rubber-band` UNION
- `Ctrl+Shift + rubber-band` の方向依存挙動
- Shift anchor
- file D&D 境界
- F5 / Refresh
- Grid表示

速度以外の追加回帰試験は不要。
