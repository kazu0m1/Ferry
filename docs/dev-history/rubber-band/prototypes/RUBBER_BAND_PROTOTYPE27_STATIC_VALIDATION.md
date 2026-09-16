# Prototype 27 Static Validation

## Baseline
- Prototype 26 source copied, then Prototype 26 keyboard FocusVisualStyle visualization removed.
- Prototype 25 behavior/fixes otherwise retained through Prototype 26 baseline.

## Change
- WPF `FocusVisualStyle` based dashed outline removed from ListViewItem/ListBoxItem.
- Added visual-only `SelectionAnchorRectangle` to the existing non-hit-testable overlay.
- Rectangle is positioned over `TabViewContext.SelectionAnchorItem`, i.e. Ferry's logical Shift-range anchor.
- Active List/Grid selector is used; virtualized/offscreen anchor containers hide the rectangle until realized again.
- No `ScrollIntoView()` is called.
- `LayoutUpdated` only repositions one already-realized anchor rectangle; it does not enumerate items.

## Unchanged
- Selection/rubber-band state machine.
- Shift/Ctrl/Ctrl+Shift selection semantics.
- D&D routing.
- Autoscroll (max 100).
- List/Grid selection synchronization.
- Prototype 24 double-click guard.
- Prototype 25 keyboard-focus synchronization for owned stationary clicks.

## Expected distinction
- Keyboard arrows may move current selection / keyboard focus.
- `SelectionAnchorItem` may remain at the original mouse anchor.
- The dashed rectangle now visualizes the latter because that state determines Ferry's subsequent Shift-range operation.
