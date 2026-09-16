# Ferry v1.1.0 RC12 Diagnostic Manifest

## 目的

RC11で直らなかった次の再現経路を、推測ではなくWPF内部状態の差分で特定する。

1. 先頭itemを通常クリック
2. Listのtrue backgroundを通常クリックして0件選択
3. `Down`を1回押す
4. viewportが末尾へjumpする

## RC12の性格

- **修正版ではなく診断版**。
- RC11のfocus restore処理はそのまま残す。
- Rubber-band / Selection Anchor / List↔Grid sync / autoscroll等の機能ロジックは変更しない。
- diagnostic buildだけ`Logger.Configure(true)`とし、設定に関係なく`Ferry.log`をexeと同じディレクトリへ出す。

## 記録する状態

`[RC12_NAV]`として以下を記録する。

- true-background MouseDown前
- selection clear直後
- Selector focus直後
- MouseUpのfocus restore前 / 直後 / Dispatcher Input / ContextIdle
- 最初の`Down`/`Up` PreviewKeyDown前 / event処理後(ContextIdle)
- Keyboard.FocusedElement
- Selector IsKeyboardFocused / IsKeyboardFocusWithin
- SelectedIndex / selected count
- Ferry `SelectionAnchorItem`
- WPF private `AnchorItem`
- WPF private `LastActionItem`
- default CollectionView `CurrentItem` / `CurrentPosition`

## Version

- Window title: `Ferry - RC 12 diagnostic`
- AssemblyVersion/FileVersion: `1.1.0.0`
- InformationalVersion: `1.1.0-rc12`
- Portable package label: `1.1.0-rc12`
