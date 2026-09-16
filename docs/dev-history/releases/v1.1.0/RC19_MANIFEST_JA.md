# Ferry v1.1.0 RC19 — Manifest

## 位置づけ
RC18で成立したList geometryはそのままに、**WPF標準の選択背景だけがListView全幅へ伸びる視覚的不整合**を局所修正する候補。

## RC19変更
- Listの実現済み`ListViewItem` visualを、`left gutter → 最右データカラム`の範囲へclip。
- WPF/Windows themeの選択色そのものは変更しない。
- 左gutterと最右カラム右側を、挙動だけでなく見た目もtrue backgroundにする。

## 非変更
- RC18 hit testing / true-background geometry
- rubber-band selection engine
- D&D routing
- keyboard navigation
- Grid navigation
- autoscroll
- column persistence

## Metadata
- Window title: `Ferry - RC 19`
- InformationalVersion: `1.1.0-rc19`
- Portable package label: `1.1.0-rc19`
