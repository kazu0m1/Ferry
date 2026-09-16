# Prototype 17 — static validation

## 基準線
- 基準: `Ferry-v1.0.2-rubberband-prototype16`
- Prototype 16 はWindows実機で drag途中Modifierを含めてPASS。

## 変更範囲
アプリケーションコードの意図的変更は次のみ。

1. `Source/Ferry/MainWindow.cs`
   - Window titleを `Ferry - prototype 17` に更新。
   - rubber-band autoscroll の高速側上限を `30.0` → `50.0 lines/sec` に変更。
   - edge zone、基礎速度、加速度式、timer、selection/rubber-bandロジックは変更なし。
2. `Source/Ferry/AssemblyInfo.cs`
   - InformationalVersionを `1.0.2-rubberband-prototype17` に更新。

## 静的確認
- `MainWindow.cs` の autoscroll上限が `Math.Min(50.0, ...)`。
- edge zoneは `30.0` のまま。
- Prototype 16で追加したModifier transitionロジックは変更なし。
- Ctrl+Shift、Shift anchor、D&D、F5/Refreshには変更なし。

## 実機確認
Windows/.NET Framework 4.8 GUI実行環境がないため、速度感とWPF描画の安定性だけ実機確認する。
