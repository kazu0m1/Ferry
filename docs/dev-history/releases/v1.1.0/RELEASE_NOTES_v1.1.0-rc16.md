# Ferry v1.1.0 RC16

RC16 is a focused correction for the RC15 one-item-late Selection Anchor synchronization observed in Windows testing.

## Fixed

- Ordinary Up/Down still uses native WPF ListView/ListBox keyboard navigation.
- Ferry now synchronizes `KeyboardNavigationItem`, visible/logical `SelectionAnchorItem`, and WPF extended-selection anchor from the Window's bubbling `KeyDown` phase, after the control has completed native navigation.
- This replaces RC15's `PreviewKeyDown` + dispatcher synchronization, which could observe the pre-navigation item and leave the dashed anchor one row behind.
- The RC15 empty-selection recovery remains unchanged: `A -> true background clear -> Down` moves to B, and `A -> true background clear -> Shift+Down` selects A/B.

## Unchanged by design

- Rubber-band selection and modifier state machine.
- D&D routing.
- List/Grid synchronization architecture.
- Autoscroll behavior/settings.
- Breadcrumb / Location Box / toolbar / Sidebar behavior.

## Candidate metadata

- Window title: `Ferry - RC 16`
- InformationalVersion: `1.1.0-rc16`
- Portable package label: `1.1.0-rc16`
