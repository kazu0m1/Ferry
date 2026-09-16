# Ferry v1.1.0 RC11 Manifest

## Baseline

- Source baseline: cleaned v1.1.0 final candidate prepared before public release
- Selection engine baseline: Prototype 27 + v1.1.0 RC1–RC10 accepted changes
- RC11 scope: one keyboard-focus/current regression route after true-background selection clear

## Application change

### True-background stationary click

1. Before Ferry moves focus to the Selector for the pending rubber-band gesture, it records the currently focused `FileItem` when that item belongs to the active selector.
2. Plain true-background selection clear remains immediate.
3. If MouseUp occurs before rubber-band activation, Ferry restores focus to the recorded **already-realized** item.
4. If focusing that item ever causes framework/theme selection coupling, Ferry explicitly clears selection again so the visible selection remains zero.
5. If the item container is not realized, Ferry does not call `ScrollIntoView`; no forced viewport movement is introduced.
6. If a real rubber-band drag activates, no focus restoration is performed and the previous validated drag route is preserved.

## Explicitly unchanged

- Normal / Ctrl / Shift / Ctrl+Shift rubber-band semantics
- Selection Anchor logic and visualization
- WPF extended-selection anchor synchronization
- List/Grid selection synchronization
- D&D routing
- Rubber-band autoscroll 30–300
- Breadcrumb 10px rectangular scrollbar and always-visible line buttons
- Location Box behavior
- 30×30 toolbar controls
- Sidebar auto-fit

## Version

- Window title: `Ferry - RC 11`
- AssemblyVersion/FileVersion: `1.1.0.0`
- InformationalVersion: `1.1.0-rc11`
- Portable package label: `1.1.0-rc11`
