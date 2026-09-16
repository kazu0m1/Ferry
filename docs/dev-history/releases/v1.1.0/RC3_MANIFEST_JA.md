# Ferry v1.1.0 RC3 Manifest

## 基準線

- Application: v1.1.0 RC2
- RC2 Windows validation: PASS
- Selection engine baseline: Prototype 27

## RC3で追加したapplication変更

1. Selection Anchor visual調整
   - DashArrayをPrototype 27の`1,1`へ戻した。
   - 色はRC2のdark gray `#707070`を維持。
   - 1 px。
   - selection/anchorロジック自体は変更なし。

2. Rubber-band autoscroll速度設定
   - 設定範囲を30–300へ拡張。
   - default 100。
   - 100では従来検証済みcurveをそのまま維持。
   - RC2の「最大値だけをcapする」方式から、設定値/100でcurve全体をscaleする方式へ変更。
   - 50 / 100 / 200 / 300が同じpointer深度でも体感差を持つようにした。
   - 25 ms timerのper-tick step上限を設定値に応じて動的計算し、300まで処理可能。
   - settings normalize / Save / Reset / Import / Exportは30–300へ更新。

3. Sidebar splitter double-click auto-fit
   - Sidebarとfile viewの境界（既存5 DIP透明GridSplitter）をdouble-clickするとSidebar幅を表示文字列へfit。
   - Heading / Places・Drives・System buttons / Pinned folder namesを計測。
   - Sidebar vertical scrollbar表示中はその幅も加味。
   - 既存の50–480制限内へclamp。
   - fit後の幅をsettingsへ保存。
   - 通常のdrag resizeは維持。

4. Location Box
   - RC2 Windows PASSの挙動を変更していない。

## バージョン

- Window title: `Ferry - RC 3`
- AssemblyVersion/FileVersion: `1.1.0.0`
- InformationalVersion: `1.1.0-rc3`
- Portable package label: `1.1.0-rc3`

## 変更していない主要領域

- Prototype 27 rubber-band selection semantics
- Shift anchor semantics
- D&D routing
- List/Grid selection synchronization
- double-click item-open guard
- stationary whitespace keyboard-focus fix
- Ctrl+L Location Box dismissal / Esc / re-press behavior
- sorting / folder-first
- ZIP subsystem

## 実機確認

`V1.1.0_RC3_TEST_JA.md`を使用する。
