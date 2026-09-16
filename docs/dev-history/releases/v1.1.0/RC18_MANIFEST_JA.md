# Ferry v1.1.0 RC18 — Manifest

## 目的

RC17でPASSしたList/Grid keyboard navigationを固定したまま、List表示のinteraction geometryをExplorer寄りに整理する。

## 変更

- List最左端に固定10pxの内部gutter columnを追加。
- gutterは視覚上の余白として表示するが、Ferryのitem hit targetから除外しtrue backgroundとして扱う。
- ListのSelection Anchor破線を「gutterを除く最左データ列〜最右データ列」に限定。
- 最右データ列より右側を、rowの高さ内でもtrue backgroundとして扱う。
- rubber-bandのList item intersection boundsも同じdata-column spanへ統一。
- double-click / right-click / middle-click / external drop target等も同じhit geometryを使用。
- gutter内部列はcolumn order/width settingsへ保存しない。

## 変更しないもの

- RC17 Grid 4方向keyboard navigation
- RC16 List keyboard navigation
- Rubber-band modifier state machine
- D&D payload / shell operations
- autoscroll
- Breadcrumb / toolbar / Sidebar / Settings

## Metadata

- Window title: `Ferry - RC 18`
- InformationalVersion: `1.1.0-rc18`
- Portable package label: `1.1.0-rc18`

## 判定

`V1.1.0_RC18_TEST_JA.md` の必須項目をPASSするまでfinalへ昇格しない。
