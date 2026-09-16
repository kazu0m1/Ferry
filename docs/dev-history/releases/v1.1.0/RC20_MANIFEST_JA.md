# Ferry v1.1.0 RC20 — Manifest

## 位置づけ
RC19の実機テストで、selection fill clipping / left gutter / right true background / rubber-band / D&D / List・Grid keyboard navigationはすべてPASS。残った **左gutter headerだけ他カラムと背景色が異なる視覚差** のみを修正する最終候補。

## RC20変更
- `GridViewColumnHeader`として作っているgutter headerの明示的な`SystemColors.ControlBrush`指定を削除。
- 通常のdata-column headerと同じWindows/WPF theme backgroundを使用。

## 非変更
- RC19 ListViewItem visual clip / selection fill width
- 10px left true-background gutter
- right-of-columns true background
- rubber-band / D&D / hit testing
- List keyboard navigation / Shift+Arrow
- Grid 4-direction keyboard navigation
- autoscroll / Breadcrumb / Sidebar / settings

## Metadata
- Window title: `Ferry - RC 20`
- InformationalVersion: `1.1.0-rc20`
- Portable package label: `1.1.0-rc20`
