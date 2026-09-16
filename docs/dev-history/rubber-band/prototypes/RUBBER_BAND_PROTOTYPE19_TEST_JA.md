# Ferry v1.0.2 Rubber Band Integration Prototype 19 — 実機確認

Prototype 18 の選択ロジックは実機でPASS。本版は **autoscrollの速度カーブだけ**を変更する。最大値は80 lines/secのまま。Grid確認不要。

## ビルド
1. `Build.cmd`
2. `Portable\Ferry.exe`
3. タイトルが **Ferry - prototype 19** であること。

## A. 高速autoscrollの速度感だけ確認
- [ ] 下端から外へマウスを出すほど、Prototype 18より明確に加速する。
- [ ] 上端側でも同様に加速する。
- [ ] 端のすぐ近くでは従来どおり急に速くなりすぎない。
- [ ] スクロール中にチラつき・先頭/末尾への突然のジャンプが再発しない。
- [ ] 新しく表示された行のrubber-band選択が追従する。

## 今回は再試験不要
- 通常 / Ctrl / Shift rubber-band
- Ctrl+Shift B→C / C→B
- drag途中Modifier
- Shift anchor
- D&D
- F5
- Grid表示

## 速度カーブの変更
Prototype 18は上限を80へ上げただけで、従来の傾きでは80に達するには端から約388px相当の深さが必要だった。
Prototype 19は最初の30pxの低速域を維持し、それより外側だけ加速を強め、約183pxで80 lines/secへ到達する。
