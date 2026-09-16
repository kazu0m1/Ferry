# Ferry v1.0.2 Rubber Band Integration Prototype 14 — Static Validation

**Baseline:** Prototype 13

## Intent
Prototype 13で確認できたF5/Refreshおよびautoscroll経路は変更せず、選択gestureだけを修正する。

- Ctrl/Shift の未選択行余白MouseDownをWPFへ先に流さず、FerryがPendingから所有する。
- stationary clickはMouseUpで明示的に Ctrl toggle / Shift range / normal single-selection として確定する。
- Ferry側にlogical Shift anchorを保持し、Ctrl+Shift rubber-bandのbaseline計算をWPF private anchorだけに依存させない。
- Ctrl+Shiftのselection式自体は Prototype 13 の `anchor-to-start baseline + moving-band XOR (start row excluded)` を維持する。
- autoscroll上限は25 lines/secを維持する。

## Static checks
- title Ferry - prototype 14: PASS
- AssemblyInformationalVersion prototype14: PASS
- logical anchor field: PASS
- owned modified rubber-band routing: PASS
- Ctrl stationary toggle helper: PASS
- Shift stationary range helper: PASS
- Ctrl+Shift logical anchor lookup: PASS
- autoscroll max 25: PASS
- F5 key handler unchanged: PASS
- SafeRefreshFolderIncremental body unchanged: PASS
- brace balance: PASS
- paren balance (coarse): PASS

## Limitation
Linux環境ではWindows .NET Framework/WPFの実ビルドは実行できないため、`Build.cmd` とWindows実機試験が最終ゲート。
