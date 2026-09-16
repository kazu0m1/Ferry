# Ferry v1.0.2 Rubber Band Integration Prototype 11 — Static Validation

## Baseline
Prototype 10 を直接ベースにし、autoscroll速度・選択集合・Shift anchorロジックは変更しない。

## Root cause addressed
WPF `ListBox` / `ListView` はコントロール自身がmouse captureを得ると内部autoscroll timerを開始する。Prototype 10はFerry独自timerと同時にListView自身もcaptureしていたため、WPF側の行ナビゲーション/フォーカス移動とFerry側のscrollが競合し得た。

Prototype 11ではactive rubber-bandのcapture ownerを `ctx.Container` (neutral Grid) に変更する。captured move/upはContainer-level preview handlerから既存rubber-band handlerへ転送する。これによりWPF ListBox内蔵autoscrollを起動せず、Ferry timerだけをscroll authorityとする。

## Intended logic delta
- normal rubber-band開始時: `Mouse.Capture(selector)` → `Mouse.Capture(ctx.Container)`
- threshold crossing時: ListView/ListBoxではなくContainerへcapture
- Containerにcaptured `PreviewMouseMove` / `PreviewMouseLeftButtonUp` routingを追加
- Container capture喪失時の安全終了を追加
- gesture終了時はContainer captureも解放
- selection集合演算、auto-scroll速度式、current-hit cache、Shift anchor helperは変更なし

## Static checks
- MainWindow delimiter balance: PASS
- title `Ferry - prototype 11`: PASS
- informational version `1.0.2-rubberband-prototype11`: PASS
- active rubber-band capture target is `ctx.Container`: PASS
- existing Ctrl XOR / Shift UNION / explicit Shift-anchor helper retained: PASS
- Prototype 10 custom auto-scroll timer retained: PASS

## Windows-only gate
Linux環境では.NET Framework 4.8/WPF実機動作を検証できない。`Build.cmd` とWindows実機試験を最終ゲートとする。
