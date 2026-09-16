# Ferry v1.0.2 Rubber Band Integration Prototype 13 — Static Validation

## Baseline
Prototype 12から継ぎ足さず、Windows実機でautoscroll正常動作まで確認された **Prototype 11** を直接ベースに再構築した。

## Intended logic delta
1. 高速autoscroll上限をユーザー指定どおり **25 lines/sec** に変更。near-edge base 2.5は維持。
2. Shift-anchor追加修正。
   - 既存multi-selection内のselected itemを通常クリックして1件へcollapseする経路でも、`CommitSingleSelectionWithExplicitShiftAnchor()` を使用。
   - これにより `A→Shift+E→C単クリック→Shift+G` で古いA anchorへ戻る経路を塞ぐ。
3. Ctrl+Shift row-whitespace rubber-bandを専用gestureとして再実装。
   - 未選択row/tile whitespaceだけを対象にMouseDownからFerryが専有。
   - WPF native Shift-range MouseDownにpass-throughしない。
   - MouseDown時は既存selection + start rowを表示状態にする。
   - threshold到達後は `initial ∪ [saved anchor..start]` をbaselineとし、moving hit-setをXORする。
   - start row自身はtoggle対象から除外する。
4. Normal/Ctrl/Shift rubber-band、true-background clear、Prototype 11 neutral-container mouse capture、D&D routingはPrototype 11の経路を維持。

## Static checks
- MainWindow raw delimiter balance `{}`: PASS (780 / 780)
- MainWindow raw delimiter balance `()`: PASS (2691 / 2691)
- MainWindow raw delimiter balance `[]`: PASS (119 / 119)
- title `Ferry - prototype 13`: PASS
- AssemblyInformationalVersion `1.0.2-rubberband-prototype13`: PASS
- Prototype 11 neutral-container mouse capture retained: PASS
- true-background clear path retained: PASS
- ordinary Ctrl XOR branch retained: PASS
- ordinary Shift UNION branch retained: PASS
- selected-row D&D routing retained: PASS
- multi-selection collapse uses explicit Shift-anchor commit: PASS
- Ctrl+Shift exclusive row-whitespace MouseDown ownership present: PASS
- Ctrl+Shift anchor-to-start baseline present: PASS
- Ctrl+Shift moving XOR branch present: PASS
- autoscroll max 25 lines/sec: PASS

## Windows-only gate
このLinux環境では.NET Framework 4.8/WPFのWindows実機ビルド・実行はできない。`Build.cmd` とWindows実機確認を最終ゲートとする。
