# Ferry v1.0.2 Rubber Band Integration Prototype 10 — Static Validation

## Baseline
実機PASSとなった Prototype 9 を直接ベースに作成。Shift-anchor修正、true-background clear、Ctrl XOR、Shift UNION、D&D routingは維持する。

## Intended application-logic delta
`Source/Ferry/MainWindow.cs` の追加範囲は List rubber-band autoscroll に限定する。

- active rubber-band中だけ25ms timerを使用。
- List内部ScrollViewerの上端/下端30pxをedge zoneとして扱う。
- edge付近では低速、pointerがview外へ離れるほど段階的に加速する。
- edge zoneから離れるとscroll accumulatorを即時リセットする。
- scroll後もrubber-band開始点をcontent側へ追従させ、矩形表示を更新する。
- virtualizationで画面外になった既ヒットitemをcurrent-hit setに保持し、scrollしただけで選択が消えることを防ぐ。
- drag方向がanchorを跨いで反転した場合はoff-screen hit cacheを破棄し、現在geometryから再構築する。
- Grid autoscrollは本Prototypeでは有効化しない。

## Static checks
- MainWindow delimiter balance: PASS (`{}` / `()` / `[]`)
- Prototype title marker `Ferry - prototype 10`: PASS
- Assembly informational version `1.0.2-rubberband-prototype10`: PASS
- Prototype 9のexplicit Shift-anchor helper (`AnchorItem` / `LastActionItem`) retained: PASS
- true-background clear helper retained: PASS
- Ctrl XOR / Shift UNION branches retained: PASS
- autoscroll timer / ScrollViewer / edge-zone markers present: PASS

## Windows-only gate
この環境では Windows/.NET Framework 4.8 WPF 実行ビルドは行えない。`Build.cmd` と実機試験を最終ゲートとする。
