# Prototype 18 — static validation

## 基準線
- 基準: `Ferry-v1.0.2-rubberband-prototype17`
- Prototype 17 = Prototype 16のPASS済み選択ロジック + autoscroll上限50。

## 変更範囲
アプリケーションコードの意図的変更は `MainWindow.cs` と `AssemblyInfo.cs` のみ。

1. `Source/Ferry/MainWindow.cs`
   - Window titleを `Ferry - prototype 18` に更新。
   - autoscroll高速側上限を `50.0` → `80.0 lines/sec` に変更。
   - Ctrl開始→Ctrl解放後のtransition modeはPrototype 16のまま。再押下しても元のXORへ戻らない。
   - Ctrl+Shift開始で **Ctrlだけ解放（Shift保持）** の場合は、既存Ctrl+Shift state machineを継続する。
   - Ctrl+Shift開始で **Shiftだけ解放（Ctrl保持）** の場合に専用transitionを追加。解放時の選択を維持し、その後のhit境界通過で、観察されたanchor→start間（start除外）の状態変化を再現する。
2. `Source/Ferry/AssemblyInfo.cs`
   - InformationalVersionを `1.0.2-rubberband-prototype18` に更新。

## 変更していないもの
- true-background clear
- Shift anchor修正
- 通常/Ctrl/Shiftの基本rubber-band routing
- Ctrl+ShiftのMouseDown時基準線とB→C/C→B基本動作
- D&D routing
- neutral-container mouse capture
- autoscroll edge zone / timer /基礎速度 / 加速度式（上限値以外）
- F5/Refresh

## 静的確認
- title: `Ferry - prototype 18`
- autoscroll: `Math.Min(80.0, ...)`
- new state is Begin/Endで初期化・破棄される。
- Windows/.NET Framework 4.8 GUI実機がないため、WPF実動作は実機確認対象。
