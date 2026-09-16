# Ferry v1.1.0 RC18

RC18 is a focused List interaction-geometry refinement on top of the RC17 keyboard-navigation baseline.

## Changed

- Added a narrow 10px Explorer-style gutter to the left of the first List data column.
- Limited the dashed Selection Anchor rectangle to the actual data-column span instead of the full ListView width.
- Treats space to the right of the last visible data column as true background even when the pointer is horizontally aligned with an item row.
- List rubber-band intersection now uses the same data-column geometry.

## Interaction consistency

The same List item bounds are now used for click selection, double-click open, right/middle mouse lookup, drag/drop target lookup, and rubber-band candidate routing. The internal gutter is not persisted as a user column.

## Unchanged

RC17 Grid keyboard navigation, RC16 List keyboard navigation, rubber-band modifier semantics, D&D behavior, autoscroll, Breadcrumb, toolbar, Sidebar, and Settings are otherwise unchanged.

## Metadata

- Window title: `Ferry - RC 18`
- InformationalVersion: `1.1.0-rc18`
- Portable package label: `1.1.0-rc18`
