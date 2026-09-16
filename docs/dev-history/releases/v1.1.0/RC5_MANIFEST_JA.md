# Ferry v1.1.0 RC5 Manifest

## 基準線

- Application: v1.1.0 RC4
- RC3 Windows validation: PASS
- RC4 issue: fixed-height path host clipped Breadcrumb content when the horizontal scrollbar consumed the Location Box-height viewport.
- Selection engine baseline: Prototype 27

## RC5で追加したapplication変更

1. Breadcrumb表示領域の修正
   - RC4の「pathHostをLocation Boxの高さで固定」は維持。
   - Breadcrumb用`ScrollViewer`のhorizontal scrollbarを`Auto`から`Hidden`へ変更し、固定高さの中でScrollbar行が文字表示領域を奪わないようにした。
   - Breadcrumb再構築後に`ScrollToRightEnd()`し、狭いWindowや深いpathでも現在位置側のcrumbを優先して表示する。
   - Location Boxの表示高さ・Ctrl+L/Esc/List操作による復帰挙動は変更していない。

2. RC3/RC4機能
   - Selection Anchor gray 1,1破線、autoscroll 30〜300、Sidebar double-click auto-fitを変更していない。
   - Rubber-band / Shift anchor / D&D / List↔Grid同期等の選択ロジックを変更していない。

## バージョン

- Window title: `Ferry - RC 5`
- AssemblyVersion/FileVersion: `1.1.0.0`
- InformationalVersion: `1.1.0-rc5`
- Portable package label: `1.1.0-rc5`

## 実機確認

`V1.1.0_RC5_TEST_JA.md`を使用する。
