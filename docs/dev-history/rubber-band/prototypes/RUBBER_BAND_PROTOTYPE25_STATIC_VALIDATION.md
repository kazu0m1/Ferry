# Ferry v1.0.2 Rubber-band Integration Prototype 25 — Static Validation

## Baseline

Prototype 24 を直接基準にした。

## Root cause

Ferry が row/tile whitespace の stationary click を所有する経路では、WPF の native `ListBoxItem` MouseDown を通さないため、
`SelectedItem` と Shift anchor は更新されても、keyboard focus/current が item container ではなく Selector 本体に残る経路があった。

WPF ListBox の arrow-key navigation は、event source が ListBoxItem ならその item から `MoveFocus(...)` するが、
ListBox 本体が source の場合は start navigation に入る。このため大規模virtualized folderでは先頭/末尾方向への大ジャンプと
container realization に伴う長い停止として現れ得る。

## Change

`MainWindow.cs` に `FocusSelectorItem(Selector, FileItem)` を追加。
既にrealizeされているクリック対象containerに `Focus()` するだけで、`ScrollIntoView()` は呼ばない。

適用箇所：
- normal row/tile-whitespace stationary click
- multi-selection memberのnormal stationary click collapse
- Ctrl stationary click
- Shift range endpoint
- Ctrl+Shift stationary row/tile-whitespace click

rubber-band active selection、D&D、autoscroll、view sync、sort、refresh、double-click guard は変更していない。

## Diff scope

Prototype 24との差分は以下のみ。
- `Source/Ferry/MainWindow.cs`
- `Source/Ferry/AssemblyInfo.cs`
- Prototype 25 test/static-validation documents

## Runtime note

この環境では Windows / .NET Framework 4.8 WPF GUI の実機動作確認はできない。
`RUBBER_BAND_PROTOTYPE25_TEST_JA.md` に従い Windows 実機で keyboard navigation と既存selection回帰を確認する。
