# Ferry v1.1.0 RC7 Manifest

## 基準線

- Application: v1.1.0 RC6
- RC3 Windows validation: PASS
- RC6 requirement: Breadcrumb horizontal scrollbarを表示したままpath文字列を正常表示し、Breadcrumb / Location Boxで高さを固定。
- RC7 goal: 実機比較用に選択した**8px scrollbar案**を適用し、Toolbar buttonの縦伸びを解消する。
- Selection engine baseline: Prototype 27

## RC7で追加したapplication変更

1. Breadcrumb専用horizontal scrollbarを8px化
   - `ScrollViewer.HorizontalScrollBarVisibility = Auto`は維持。
   - Breadcrumbの`ScrollViewer`配下だけにhorizontal `ScrollBar`のHeight / MinHeight / MaxHeight = `8`を適用。
   - List/Grid/Sidebarなど他のWindows scrollbarには影響しない。
   - `pathHost`固定高さは`Location Box 30px + Breadcrumb scrollbar 8px`を基準にする。

2. Toolbar sizing
   - Back / Forward / Up / Home / New tabを30×30。
   - List / Grid / Settings / Search clearも30×30。
   - `Columns`は内容幅を維持しつつ高さ30px。
   - Search mode / Search box / Location Boxも高さ30pxへ統一。
   - Toolbar上下paddingを7→6へ微調整。

3. RC6以前の機能
   - Location Box dismissal/focus restoration、Selection Anchor、rubber-band、autoscroll 30〜300、Sidebar auto-fit、List↔Grid同期等は変更していない。

## バージョン

- Window title: `Ferry - RC 7`
- AssemblyVersion/FileVersion: `1.1.0.0`
- InformationalVersion: `1.1.0-rc7`
- Portable package label: `1.1.0-rc7`

## 実機確認

`V1.1.0_RC7_TEST_JA.md`を使用する。
