# Ferry v1.1.0 RC8 Manifest

## 基準線

- Application: v1.1.0 RC7
- RC7 result: 8px Breadcrumb scrollbar自体とToolbar全体の見た目は採用。
- RC8 goal: scrollbar左右端の移動buttonをhover依存にせず、常時発見できるようにする。
- Selection engine baseline: Prototype 27

## RC8で追加したapplication変更

1. Breadcrumb scrollbar end-button chrome
   - Breadcrumb `ScrollViewer`内のhorizontal `ScrollBar`だけを対象。
   - `LineLeftCommand` / `LineRightCommand`を持つ`RepeatButton`だけ専用templateへ差し替え。
   - 通常背景: `#B8B8B8`
   - hover背景: `#A2A2A2`
   - pressed背景: `#8E8E8E`
   - arrow glyph: `#626262`
   - `Opacity=1`, `Visibility=Visible`を明示し、hover前から左右buttonとarrowを表示。
   - PageLeft/PageRight用のtrack内RepeatButtonやList/Grid側scrollbarには適用しない。

2. RC7 layout維持
   - Breadcrumb scrollbar高: 8px
   - icon toolbar button: 30×30
   - Location Box: 30px
   - fixed path-host height: Location Box 30px + Breadcrumb scrollbar 8px

3. Selection/navigation baseline
   - Rubber-band / modifier semantics / Selection Anchor / autoscroll 30–300 / Location Box / Sidebar auto-fit / List↔Grid syncは変更なし。

## バージョン

- Window title: `Ferry - RC 8`
- AssemblyVersion/FileVersion: `1.1.0.0`
- InformationalVersion: `1.1.0-rc8`
- Portable package label: `1.1.0-rc8`

## 実機確認

`V1.1.0_RC8_TEST_JA.md`を使用する。
