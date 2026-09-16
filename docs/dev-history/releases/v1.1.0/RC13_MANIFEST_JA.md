# Ferry v1.1.0 RC13 Manifest

## 基準線

- Application: v1.1.0 RC12 diagnostic
- RC12 result: 再現継続。logにより、`Down`直前は0件選択かつListView本体がkeyboard focusを持ち、Ferry anchorはindex 0のまま、WPF `LastActionItem`は別itemを指していた。`Down`後に`LastActionItem`が末尾itemへ移動し、viewportも末尾へjumpした。
- RC13 goal: Selector本体focus + 0件選択という特殊状態の最初の矢印navigationだけをFerry側で決定的に処理する。
- Selection engine baseline: Prototype 27

## RC13のapplication変更

1. Keyboard navigation originの追跡
   - `TabViewContext.KeyboardNavigationItem`を追加。
   - Selector配下でitem containerへkeyboard focusが入るたび、最後のfocus itemを記録。
   - item MouseDown時にも同じoriginを更新し、virtualizationでcontainerが後に破棄されてもitem identityを保持。

2. 0件選択 + Selector focus時のUp/Down補正
   - 条件: modifierなし / `Up`または`Down` / visible Selectorが`IsKeyboardFocused` / selected count = 0。
   - originは`KeyboardNavigationItem`、無効時のみFerry `SelectionAnchorItem`をfallbackに使用。
   - `Down`はoriginの次item、`Up`は前itemをtargetにする。
   - targetをsingle selectionにし、Ferry Selection Anchor / WPF Anchor / LastActionItem / keyboard focusを同期。
   - targetがvirtualizationで未実体化なら`ScrollIntoView`後にfocus同期を完了。
   - この最初の矢印だけ`e.Handled = true`とし、その後は通常WPF navigationへ戻る。

3. RC12診断log継続
   - RC13でもdiagnostic logを強制ON。
   - `keyboardOrigin`とFerry補正時のorigin/targetを`[RC13_NAV]`で記録。

## 変更しないもの

- Rubber-band state machine / modifier semantics
- Selection Anchor visual
- List↔Grid selection sync
- D&D
- autoscroll 30–300
- Breadcrumb / Location Box / toolbar
- Sidebar auto-fit

## Version

- Window title: `Ferry - RC 13`
- AssemblyVersion/FileVersion: `1.1.0.0`
- InformationalVersion: `1.1.0-rc13`
- Portable package label: `1.1.0-rc13`
