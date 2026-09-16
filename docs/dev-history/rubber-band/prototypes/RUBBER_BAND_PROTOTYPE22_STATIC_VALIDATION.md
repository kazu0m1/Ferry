# Ferry v1.0.2 Rubber Band Integration Prototype 22 — 静的検証

## 基準
- 直接の基準線: Prototype 21
- Prototype 21 の List rubber-band 選択ロジック、Ctrl/Shift/Ctrl+Shift、drag途中Modifier、D&D、autoscroll上限100は維持。
- 今回の実機観察では Grid 固有差は autoscroll が開始しない点だけだった。

## 変更点
### MainWindow.cs
1. rubber-band active 化時の autoscroll timer 開始条件から `view is ListView` 制限を除去。
   - ListView / Grid ListBox の両方で Ferry 独自 timer を開始する。
2. `HandleRubberBandAutoScrollTick` を ListView 専用から `Selector` 共通へ変更。
   - ListView のみ column header 下端を top boundary として扱う。
   - Grid ListBox は header がないため viewport 上端を boundary とする。
3. scroll offset → rubber-band content origin 補正を view ごとに分離。
   - ListView + content scroll: item単位 offset を realized row height で pixel換算（従来どおり）。
   - Grid (`VirtualizingWrapPanel`): IScrollInfo の offset 自体が pixel 値なので、そのまま pixel delta として使用。

### AssemblyInfo.cs / Window title
- `Ferry - prototype 22`
- informational version: `1.0.2-rubberband-prototype22`

## 非変更
- rubber-band hit geometry
- Normal / Ctrl / Shift / Ctrl+Shift selection state machine
- Shift anchor handling
- file D&D routing
- true-background clear
- mouse capture ownership（neutral Container）
- autoscroll acceleration curve / 100 lines/sec ceiling / 25ms timer
- F5 / refresh

## 静的確認
- Prototype 21 との差分で選択集合ロジックに変更なし。
- Grid は Ferry 独自 `VirtualizingWrapPanel` を使用し、`VerticalOffset` は pixel 座標として実装されているため、Grid側で row-height乗算を行わない補正は整合する。
- Windows/.NET Framework 4.8 実機ビルド・動作確認は未実施。
