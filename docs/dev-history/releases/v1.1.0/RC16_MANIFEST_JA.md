# Ferry v1.1.0 RC16 — Manifest

## 目的

RC15実機試験で残った「無修飾Arrowで選択/focusは現在行へ進むが、破線Selection Anchorだけが1 item前に残る」同期遅延を局所修正する。

## 実機で確定した基準挙動

- Explorer: Aをclick → true backgroundで選択解除 → Down でBに選択とfocusが移る。
- Explorer: Aをclick → true backgroundで選択解除 → Shift+Down でA/Bが選択される。
- Explorer: 無修飾Arrowで現在行を移動した後、Shift+Arrowはその現在行をrange originにする。
- RC15ではempty-selection DownおよびShift+DownはPASSしたが、通常Arrow中のFerry破線anchorが1件遅れた。

## 原因

RC15はWindowの`PreviewKeyDown`から`DispatcherPriority.Input`で後処理を予約していた。WPF ListView/ListBoxのnative keyboard navigation/focus更新と同じinput priority帯で順序競合が起き、callbackが移動後ではなく移動前itemを読むケースがあった。

## RC16実装

- `PreviewKeyDown`ではempty-selection救済のみを担当。
- Windowにhandled-events込みのbubble-phase `KeyDown` handlerを追加。
- 無修飾Up/Downについて、ListView/ListBoxがnative navigationを処理した後に現在のsingle selected itemを優先して取得し、必要時だけfocused itemへfallback。
- そのitemへ`KeyboardNavigationItem`、`SelectionAnchorItem`、WPF extended-selection anchorを同期。
- Shift+Arrow中はanchorをendpointへ更新しない。
- RC15でPASSしたempty-selection Arrow/Shift+Arrowのロジックは変更しない。

## Version metadata

- Window title: `Ferry - RC 16`
- InformationalVersion: `1.1.0-rc16`
- Portable package label: `1.1.0-rc16`

## Runtime gate

`V1.1.0_RC16_TEST_JA.md` の最重要A～DをPASSするまでfinalへ昇格しない。
