# Ferry v1.1.0 RC17 — Manifest

## 目的

RC16でPASSしたList keyboard navigationを維持しつつ、Gridで残った4方向Arrowとbackground-clear recoveryを局所修正する。

## 変更

- Gridの無修飾`Left / Right / Up / Down`後にSelection Anchorを現在選択tileへ同期。
- Gridの0件選択 + Selector focus状態から`Left / Right / Up / Down`をFerryが回復。
- Gridの縦方向は`VirtualizingWrapPanel`の列数を使い、真上/真下のtileを算出。
- ListのRC16 PASS済み処理は変更しない。

## 変更しないもの

- Rubber-band state machine / modifier semantics
- D&D
- autoscroll
- List行のselection/focus geometry
- Breadcrumb / toolbar / Sidebar / Settings

## Metadata

- Window title: `Ferry - RC 17`
- InformationalVersion: `1.1.0-rc17`
- Portable package label: `1.1.0-rc17`

## 判定

`V1.1.0_RC17_TEST_JA.md` のA〜GをPASSするまでfinalへ昇格しない。
