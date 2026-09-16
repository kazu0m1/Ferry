# Ferry v1.0.2 Rubber Band Integration Prototype 17 — 実機確認

Prototype 16 は drag途中Modifierを含めて **PASS**。本版は選択ロジックを変更せず、rubber-band autoscroll の高速側上限だけを **30 → 50 lines/sec** に変更した速度調整版。

## ビルド
1. `Build.cmd`
2. `Portable\Ferry.exe`
3. タイトルが **Ferry - prototype 17** であること。

## 確認
- [ ] 下方向の高速autoscrollが速くなっている。
- [ ] 上方向の高速autoscrollも同様に動く。
- [ ] Prototype 11で修正したチラつき・突然先頭へ飛ぶ症状が再発しない。

※ 通常/Ctrl/Shift/Ctrl+Shift rubber-band、Shift anchor、D&D、drag途中Modifierの回帰試験は不要。コード上は速度上限以外を変更していない。
