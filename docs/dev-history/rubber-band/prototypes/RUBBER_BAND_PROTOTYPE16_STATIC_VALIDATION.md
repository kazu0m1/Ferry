# Prototype 16 — static validation

## 基準線
- 基準: `Ferry-v1.0.2-rubberband-prototype15-speed30`
- Prototype 15で確定した高速autoscroll上限 **30 lines/sec** を維持。
- F5/Refresh、archive、D&D本体、geometry、autoscroll制御には変更なし。

## 変更範囲
アプリケーションコードの意図的変更は次のみ。

1. `Source/Ferry/MainWindow.cs`
   - Window titleを `Ferry - prototype 16` に更新。
   - rubber-band hit setの前回状態を保持し、Ctrl開始後のModifier解放時だけ境界差分で選択を更新する状態を追加。
   - Normal開始後に追加されたCtrl/Shiftはactive rubber-bandの選択方式へ参加させない。
   - Shift開始のactive rubber-bandを、追加観察どおりNormal bandとして扱う。
   - Ctrlをthreshold前に解放した場合、初回hitを既存選択へ追加し、その後はNormalのenter/leave差分に移行。
   - Ctrl+Shift専用ロジックはPrototype 15の検証済み実装を維持。
2. `Source/Ferry/AssemblyInfo.cs`
   - InformationalVersionを `1.0.2-rubberband-prototype16` に更新。

## 静的確認
- `MainWindow.cs` の `{}` 数一致。
- `MainWindow.cs` の `()` 数一致。
- autoscroll最大値が `30.0` のまま。
- `RubberBandTransitionMode` はgesture開始時にfalse初期化、終了時にfalseへ戻す。
- Prototype 16のtransition modeは **MouseDown時Ctrl** のgestureでCtrlが離れた場合にのみ有効化。
- MouseDown時Normal / Shift / Ctrl+Shiftの既存gestureにtransition modeを誤適用しない。

## 実機でのみ確認できる項目
この環境ではWindows/.NET Framework 4.8 GUIの実行確認はできないため、Modifier timing、WPF mouse capture、SelectionChangedとの相互作用は `RUBBER_BAND_PROTOTYPE16_TEST_JA.md` に従ってWindows実機で確認する。
