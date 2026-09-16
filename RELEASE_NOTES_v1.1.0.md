# Ferry v1.1.0

Ferry v1.1.0 is a substantial interaction release focused on Explorer-like selection, predictable keyboard/Shift behavior, and toolbar/navigation polish.

## Highlights

- **Explorer-style rubber-band selection** in both List and Grid views.
- Validated Normal / Ctrl / Shift / Ctrl+Shift marquee semantics, including shrinking/re-entry behavior and D&D coexistence.
- **Edge autoscroll** for marquee selection with a Settings slider from **30–300** (default 100).
- **Selection Anchor indicator**: a dark-gray dashed rectangle makes the logical Shift-range anchor visible independently of keyboard movement.
- **Explorer-like List true-background geometry**: a narrow left gutter and the space to the right of the final data column remain available for background clicks and rubber-band starts; selection fill and the Selection Anchor outline stop at the final data column.
- More robust keyboard-current handling after true-background clears and row/tile-whitespace interaction, including directional Grid navigation and Shift+Arrow recovery without jumping to a viewport end.
- List ⇄ Grid view switches preserve the exact selected item set and Shift anchor.
- `Ctrl+L` Location Box now returns to Breadcrumb with `Esc`, a second `Ctrl+L`, or file-view mouse interaction.
- Toolbar/path-row height is stable when toggling between Location Box and Breadcrumb.
- Breadcrumb overflow uses a dedicated **10px horizontal scrollbar** with always-visible line buttons. Final v1.1.0 keeps this chrome rectangular for visual consistency with Ferry's square controls.
- Toolbar icon buttons are standardized to **30×30**.
- Double-clicking the Sidebar divider auto-fits the Sidebar to visible labels.
- A double-click on scrollbar chrome no longer opens the currently selected file/folder.

## Validation

The selection engine was iterated through Prototype 27 and completed a final regression pass including List/Grid selection, modifier timing, D&D boundaries, autoscroll, view synchronization, and a 10,000-item large-folder smoke test. Subsequent RC builds focused on Location Box behavior, toolbar/Breadcrumb visual polish, keyboard-current recovery, Grid directional navigation, and final List interaction geometry.

A known performance characteristic remains for extreme Shift-range selections over many thousands of items; correctness is preserved, but such operations can take several seconds.

## Upgrade

This is a backward-compatible minor release from v1.0.2. Existing settings are retained. The new rubber-band autoscroll setting defaults to 100 if absent from an older settings file.

## Platform

- Windows 11
- .NET Framework 4.8 / WPF
- MIT License
