# Ferry v1.1.0 RC9 Manifest

## 基準線

- Application: v1.1.0 RC8
- RC8 result: 8px scrollbar + end button常時表示は採用。
- RC9 goal: 掴みやすさを上げ、scrollbar / end buttonの外観を軽く丸める。
- Selection engine baseline: Prototype 27

## RC9で追加したapplication変更

1. Breadcrumb scrollbar height
   - Breadcrumb `ScrollViewer`内のhorizontal `ScrollBar`だけを8px → **10px**へ変更。
   - List/Grid等のscrollbarはWindows標準サイズのまま。
   - fixed path-host heightもLocation Box 30px + scrollbar 10pxへ追随。

2. Breadcrumb thumb chrome
   - Breadcrumb horizontal scrollbar内の`Thumb`だけ専用templateへ変更。
   - normal `#C8C8C8` / hover `#B8B8B8` / dragging `#A8A8A8`。
   - CornerRadius: **2px**。

3. Breadcrumb end-button chrome
   - RC8の常時表示を維持。
   - normal `#B8B8B8` / hover `#A2A2A2` / pressed `#8E8E8E`。
   - arrow glyph `#626262`。
   - CornerRadius: **2px**。
   - button高は10px scrollbarに追随。

4. RC8 baseline維持
   - Toolbar icon button: 30×30。
   - Location Box: 30px。
   - Rubber-band / modifier semantics / Selection Anchor / autoscroll 30–300 / Location Box dismissal / Sidebar auto-fit / List↔Grid syncは変更なし。

## バージョン

- Window title: `Ferry - RC 9`
- AssemblyVersion/FileVersion: `1.1.0.0`
- InformationalVersion: `1.1.0-rc9`
- Portable package label: `1.1.0-rc9`

## 実機確認

`V1.1.0_RC9_TEST_JA.md`を使用する。
