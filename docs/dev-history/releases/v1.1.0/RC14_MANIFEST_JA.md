# Ferry v1.1.0 RC14 Manifest

## 基準線

- Application: v1.1.0 RC13
- RC13 result: 実機PASS。先頭item→true background→Downの末尾jumpが解消し、逆方向の末尾item→true background→Upも正常。
- RC14 goal: RC13の修正挙動を一切変えず、強制diagnostic loggingを外した最終release candidateとして確認する。
- Selection engine baseline: Prototype 27

## RC14 application state

1. Keyboard navigation origin tracking（RC13継承）
   - `TabViewContext.KeyboardNavigationItem`を保持。
   - Selector配下のitem containerへkeyboard focusが入るたび、最後のfocus itemを記録。
   - item MouseDown時にもoriginを更新。

2. 0件選択 + Selector focus時のplain Up/Down補正（RC13継承）
   - 条件: modifierなし / `Up`または`Down` / visible Selectorが`IsKeyboardFocused` / selected count = 0。
   - originは`KeyboardNavigationItem`、無効時のみFerry `SelectionAnchorItem`をfallback。
   - `Down`はoriginの次item、`Up`は前itemへ移動。
   - targetをsingle selectionにし、Ferry Selection Anchor / WPF Anchor / keyboard focusを同期。
   - virtualizationでtarget containerが未実体化なら`ScrollIntoView`後にfocus同期。
   - 最初の矢印だけFerryがhandleし、その後は通常WPF navigationへ戻る。

3. RC14 cleanup
   - RC13の強制`Logger.Configure(true)`を撤去。
   - 通常の`Logger.Configure(settings.DebugLogging)`へ復帰。
   - `[RC13_NAV]`診断state logging/helperを削除。
   - keyboard navigation logic自体は変更なし。

## 変更しないもの

- Rubber-band state machine / modifier semantics
- Selection Anchor visual
- List↔Grid selection sync
- D&D
- autoscroll 30–300
- Breadcrumb 10px scrollbar / always-visible end buttons
- Location Box / toolbar
- Sidebar auto-fit

## Version

- Window title: `Ferry - RC 14`
- AssemblyVersion/FileVersion: `1.1.0.0`
- InformationalVersion: `1.1.0-rc14`
- Portable package label: `1.1.0-rc14`

## Windows validation

`V1.1.0_RC14_TEST_JA.md`を使用する。
