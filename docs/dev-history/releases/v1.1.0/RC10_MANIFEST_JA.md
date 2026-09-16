# Ferry v1.1.0 RC10 Manifest

## 基準線

- Application: v1.1.0 RC9
- RC9 result: 10px scrollbar + 軽い角丸は採用。
- RC10 goal: scrollbar / end buttonの角丸をさらに強め、卵型・楕円寄りの見え方へ寄せる。
- Selection engine baseline: Prototype 27

## RC10で追加したapplication変更

1. Breadcrumb thumb chrome
   - Breadcrumb horizontal scrollbar内の`Thumb`専用templateを継続使用。
   - normal `#C8C8C8` / hover `#B8B8B8` / dragging `#A8A8A8`。
   - CornerRadius: **5px**。

2. Breadcrumb end-button chrome
   - RC9の常時表示を維持。
   - normal `#B8B8B8` / hover `#A2A2A2` / pressed `#8E8E8E`。
   - arrow glyph `#626262`。
   - CornerRadius: **5px**。
   - button高は10px scrollbarに追随。

3. RC9 baseline維持
   - Breadcrumb horizontal `ScrollBar`高さ: 10px。
   - fixed path-host height: Location Box 30px + scrollbar 10px。
   - Toolbar icon button: 30×30。
   - Location Box: 30px。
   - Rubber-band / modifier semantics / Selection Anchor / autoscroll 30–300 / Location Box dismissal / Sidebar auto-fit / List↔Grid syncは変更なし。

## バージョン

- Window title: `Ferry - RC 10`
- AssemblyVersion/FileVersion: `1.1.0.0`
- InformationalVersion: `1.1.0-rc10`
- Portable package label: `1.1.0-rc10`

## 実機確認

`V1.1.0_RC10_TEST_JA.md`を使用する。
