# Ferry v1.1.0 RC6 Manifest

## 基準線

- Application: v1.1.0 RC5
- RC3 Windows validation: PASS
- RC5 issue: Breadcrumbは再表示できたが、horizontal scrollbarを隠したため、ユーザー要望の「横スクロールバーも表示する」を満たしていなかった。
- Selection engine baseline: Prototype 27

## RC6で追加したapplication変更

1. Breadcrumb固定高さの再調整
   - Breadcrumb用`ScrollViewer.HorizontalScrollBarVisibility`を`Hidden`から`Auto`へ戻した。
   - `pathHost`の固定高さを「Location Boxの実高さ + Windows horizontal scrollbar height」に変更した。
   - 長いpathでhorizontal scrollbarが出ても、Scrollbarがcrumb文字列の縦表示領域を奪わない。
   - Breadcrumb / Location Boxのどちらを表示しても、toolbar/path領域の高さは同じ固定値のまま。
   - Breadcrumb再構築後の`ScrollToRightEnd()`は維持し、現在位置側を優先表示する。

2. RC3〜RC5機能
   - Location Box dismissal、Selection Anchor gray `1,1`破線、autoscroll 30〜300、Sidebar double-click auto-fitを変更していない。
   - Rubber-band / Shift anchor / D&D / List↔Grid同期等の選択ロジックを変更していない。

## バージョン

- Window title: `Ferry - RC 6`
- AssemblyVersion/FileVersion: `1.1.0.0`
- InformationalVersion: `1.1.0-rc6`
- Portable package label: `1.1.0-rc6`

## 実機確認

`V1.1.0_RC6_TEST_JA.md`を使用する。
