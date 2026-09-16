# Ferry v1.0.2 Rubber Band Integration Prototype 21 — 静的検証

## 基準
- 直接の基準：Prototype 20
- Prototype 20の既存選択ロジック、D&D、autoscrollカーブは維持。
- autoscroll高速側上限：100 lines/secのまま。

## 今回の変更
1. メインウィンドウタイトルを `Ferry - prototype 21` に更新。
2. InformationalVersionを `1.0.2-rubberband-prototype21` に更新。
3. `Ctrl+Shift` + true background MouseDownをFerry所有のrubber-band候補として受け取る。
4. true background開始専用フラグ `RubberBandCtrlShiftTrueBackgroundMode` を追加。
5. 同モードでは、active選択を `MouseDown時の初期選択 XOR 現在のhit集合` として計算する。
6. 行内余白開始の既存Ctrl+Shift anchor-to-startロジックには入らない。
7. 同モードでは、Prototype 18由来のCtrl+Shift途中Shift解放用ステートマシンを作動させない。
8. gesture終了時に専用フラグを必ずリセットする。

## Explorer観察との対応
- 初期A、hitなし → A
- D → A/D
- C/D → A/C/D
- B/C/D → A/B/C/D
- A/B/C/D → B/C/D
- 縮小時も同じXOR規則で逆遷移。
- 初期A/Cでも各hit行が初期選択とのXORとして反転する。

## 非変更領域
- 通常/Ctrl/Shift rubber-band
- 未選択行余白からのCtrl+Shift B→C / C→B
- Shift anchor修正
- drag途中Modifier対応
- file D&D
- true-background通常クリックclear
- F5/Refresh
- autoscrollロジック（上限100を含む）
- Grid表示

## Windows実機で必要な確認
Linux環境では.NET Framework 4.8/WPF実機ビルド・UI操作は実施できないため、`RUBBER_BAND_PROTOTYPE21_TEST_JA.md` のWindows実機確認を最終判定とする。
