# Prototype 15 Static Validation

基準: Prototype 14（Windows実機オールクリア）。

## 意図した差分
- `MainWindow.cs`
  - Window title: `Ferry - prototype 14` → `Ferry - prototype 15`
  - rubber-band autoscroll max: `25.0` → `30.0` lines/sec
  - 対応コメント 25 → 30
- `AssemblyInfo.cs`
  - informational version: `...prototype14` → `...prototype15`
- Prototype 15 test document追加

## 非変更領域
Gesture routing、selection set algebra、Ctrl+Shift policy、Shift anchor、D&D、mouse capture、F5/Refresh、autoscrollのedge判定・加速度式・timer intervalは変更していない。

## 結論
Prototype 14の挙動を基準線として、高速autoscroll上限だけを30へ変更した局所版。
