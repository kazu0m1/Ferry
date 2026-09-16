# Ferry v1.1.0 RC3

RC3 is a focused interaction-polish candidate built on the Windows-passed v1.1.0 RC2.

## Location Box

RC2 behavior is retained unchanged: file-view mouse interaction dismisses the temporary `Ctrl+L` Location Box and restores Breadcrumb view, while the mouse gesture continues normally.

## Selection Anchor appearance

- The logical Shift-range Selection Anchor remains distinct from keyboard current/selection.
- RC3 restores the original Prototype 27 dash rhythm: `DashArray 1,1`.
- The stroke remains RC2's softer dark gray (`#707070`) at 1 px.
- Selection/anchor semantics are unchanged.

## Rubber-band autoscroll speed

- Settings range is now **30–300**; default remains **100**.
- `100` preserves the previously validated Prototype 19/20 acceleration curve.
- RC3 scales the whole curve according to the selected value rather than only changing a far-distance cap, so speed changes are noticeable at the same pointer depth.
- The 25 ms timer dynamically permits enough line steps per tick for values up to 300.
- Existing settings are normalized into the new 30–300 range; older settings without the property still default to 100.

## Sidebar splitter auto-fit

- Double-click the Sidebar/file-view boundary to fit Sidebar width to the displayed Sidebar labels.
- Places, headings, drive labels, and pinned-folder names are included.
- A visible Sidebar vertical scrollbar is accounted for.
- Width remains clamped to the existing **50–480** range.
- The fitted width is saved to Ferry settings.
- Ordinary drag-resize remains available.

## RC status

RC2 passed Windows validation. RC3 should receive a focused Windows regression covering the three RC3 changes plus a minimal rubber-band/Location Box sanity check before v1.1.0 final promotion.
