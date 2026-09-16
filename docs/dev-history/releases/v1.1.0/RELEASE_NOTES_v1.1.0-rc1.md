# Ferry v1.1.0 RC1

This release candidate promotes the completed Prototype 27 selection work and adds the final Ctrl+L navigation refinement planned for v1.1.0.

## Explorer-style selection

- Rubber-band/marquee selection in List and Grid views.
- Normal, Ctrl, Shift and Ctrl+Shift behavior refined against Windows Explorer observations.
- Full-row List selection geometry while preserving file-content D&D hot zones.
- Edge autoscroll during rubber-band selection.
- Stable outside-window drag/release behavior.
- List/Grid selection synchronization.
- Selection Anchor visualization for Shift-range origin.
- Large-folder validation through 10,000 items, with known extreme Shift-range performance documented separately.

## Navigation refinement

- `Ctrl+L` opens the editable location field as before.
- `Esc` now exits the location field and restores the Breadcrumb without clearing the current file selection.
- Pressing `Ctrl+L` again while the location field is active also restores the Breadcrumb.
- Ferry restores the pre-address-bar keyboard focus when possible, preserving normal Up/Down navigation continuity.

## RC status

This is a release candidate. Windows validation should be completed before promotion to v1.1.0 final.
