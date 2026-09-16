# Prototype 23 Static Validation

Baseline: Prototype 22.

## Intentional changes
- Window title: `Ferry - prototype 23`.
- Assembly informational version: `1.0.2-rubberband-prototype23`.
- `SetTemporaryView` now snapshots the selection of the currently visible selector before changing `currentViewMode`, then reconciles the destination selector to exactly the same `FileItem` set.
- Added `ReconcileSelectorSelection` using HashSet-based incremental remove/add, avoiding unconditional `UnselectAll` + full re-add.
- The tab-level logical Shift anchor is mirrored into the newly visible selector after view switching.

## Not changed
- Rubber-band candidate routing and geometry.
- Normal/Ctrl/Shift/Ctrl+Shift selection policies.
- Mid-drag modifier state machine.
- D&D routing.
- List/Grid autoscroll implementation and max-speed constant (100).
- F5/refresh logic.

## Root cause addressed
ListView and GridView are separate WPF `Selector` instances. Sharing one ItemsSource does not share their `SelectedItems`; hidden-view selection could therefore be stale when the user switched views. Prototype 23 makes the visible source view authoritative at the moment of switching.
