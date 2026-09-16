# Ferry v1.0.2 Rubber-band Integration Prototype 26 — 静的検証

基準：Prototype 25。

## 変更点

- `MainWindow.cs`
  - window titleを `Ferry - prototype 26` へ更新。
  - ListViewItem / ListBoxItem の `FocusVisualStyle` のみ追加。
  - keyboard focus用visualは1pxの破線rectangle。hit test無効。
- `AssemblyInfo.cs`
  - informational versionを `1.0.2-rubberband-prototype26` へ更新。

## 非変更領域

- selection / Shift anchor state machine
- rubber-band hit testing / modifier semantics
- D&D routing
- autoscroll（上限100を含む）
- List⇄Grid selection sync
- Prototype 24 scrollbar double-click guard
- Prototype 25 keyboard-focus synchronization logic

## 設計意図

Selection Anchor と Keyboard Focus/current item は別状態。今回のvisualはWPFの `FocusVisualStyle` を使うため、選択templateを置換せず、keyboard navigation時のcurrent containerだけを可視化する。Explorerの「選択範囲のanchor」と「現在keyboard focusがあるitem」が異なるケースを視覚的に判別できるようにする。

## 実機確認が必要な理由

この環境では.NET Framework 4.8 / Windows WPFの実描画を実行できないため、破線の見え方・themeとのコントラスト・focus追従はWindows実機で確認する。
