# Ferry v1.0.2 Rubber Band Integration Prototype 20 — 実機確認

Prototype 19 の選択ロジックをそのまま維持し、rubber-band autoscroll の高速側上限だけを **80 → 100 lines/sec** に変更した版。次フェーズは Explorer の **Ctrl+Shift + true background 開始**の観察を先に行うため、本版では選択ロジックの追加変更はしない。

## ビルド
1. `Build.cmd`
2. `Portable\Ferry.exe`
3. タイトルが **Ferry - prototype 20** であること。

## 今回の変更
- autoscroll高速側上限: **100 lines/sec**
- Prototype 19の加速カーブは維持。
- timer: 25 msのまま。
- 1 tickあたり最大3行のguardも維持（理論上限120 lines/sec）。
- 通常 / Ctrl / Shift / Ctrl+Shift / drag途中Modifier / Shift anchor / D&D の選択ロジックは変更なし。

## 今回の扱い
- 100はユーザー指定の定数として採用する。
- 速度の追加評価は不要。
- 次は `RUBBER_BAND_CTRL_SHIFT_TRUE_BACKGROUND_EXPLORER_OBSERVATION_JA.md` に従い、Explorer側の残件仕様を観察する。
