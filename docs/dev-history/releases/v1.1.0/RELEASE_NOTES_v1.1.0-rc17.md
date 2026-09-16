# Ferry v1.1.0 RC17

RC17 is a focused Grid keyboard-navigation correction on top of the RC16 List fix.

## Fixed

- Selection Anchor now follows ordinary Grid Left/Right navigation as well as Up/Down.
- Keyboard navigation resumes from the remembered Grid tile after a true-background clear for all four arrow directions.
- Grid Up/Down recovery uses the actual `VirtualizingWrapPanel` column count instead of List-style `index ± 1`.

## Unchanged

List keyboard-navigation behavior validated in RC16, rubber-band selection, drag-and-drop, autoscroll, Breadcrumb, toolbar, Sidebar, and Settings are unchanged.

## Metadata

- Window title: `Ferry - RC 17`
- InformationalVersion: `1.1.0-rc17`
- Portable package label: `1.1.0-rc17`
