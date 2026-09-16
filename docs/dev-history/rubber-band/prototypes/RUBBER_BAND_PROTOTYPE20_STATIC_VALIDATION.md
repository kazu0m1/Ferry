# Prototype 20 Static Validation

## Baseline
- Prototype 19を直接コピーして作成。
- 選択ロジックには変更を加えない。

## Intended source changes
1. `MainWindow.cs`
   - title: `Ferry - prototype 20`
   - autoscroll max: `80.0` → `100.0 lines/sec`
   - 速度コメントのみ更新。
2. `AssemblyInfo.cs`
   - informational version: `1.0.2-rubberband-prototype20`

## Preserved behavior
- 25 ms autoscroll timer。
- 1 tick 最大3 line guard。
- Prototype 19のnear-edge低速域と加速傾き。
- rubber-band selection / Ctrl XOR / Shift / Ctrl+Shift / drag途中Modifier / Shift anchor / D&D routing。

## Capacity note
- 25 ms tick × 最大3 line = 120 lines/sec相当の既存guard。
- 設定値100 lines/secはこのguard内に収まる。
