# Ferry v1.1.0 RC1 Manifest

## 基準線

- Application: Prototype 27
- Documentation: Prototype 27 docs Stage 1

## RC1で追加したapplication変更

1. `Ctrl+L`をtoggle化。
   - Breadcrumb → Location Box
   - Location Box表示中の再押下 → Breadcrumb
2. Location Box表示中の`Esc`をMainWindow PreviewKeyDownで最優先処理。
   - Breadcrumbへ復帰。
   - 現在のfile selectionをclearしない。
3. Ctrl+L直前のkeyboard-focused elementを保存し、Esc/再押下で可能なら同じelementへfocusを戻す。
   - Selector本体へfocusが落ちて↑/↓が端へ飛ぶ大規模フォルダ回帰を避けるため。
4. Enterによる有効path navigation / invalid path messageは従来経路を維持。

## バージョン昇格

- Window title: `Ferry - RC 1`
- AssemblyVersion/FileVersion: `1.1.0.0`
- InformationalVersion: `1.1.0-rc1`
- App manifest: `1.1.0.0`
- Portable release package label: `1.1.0-rc1`

## 変更していない主要領域

- Prototype 27 rubber-band / selection state machine
- Selection Anchor visualization
- D&D routing
- autoscroll
- List/Grid selection synchronization
- sorting / folder-first logic
- ZIP subsystem

## 実機確認

`V1.1.0_RC1_TEST_JA.md`を使用する。
