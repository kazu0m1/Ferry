# Ferry v1.1.0 RC2

RC2 is a focused UX-polish candidate built on the Windows-passed v1.1.0 RC1.

## Location Box dismissal

- `Ctrl+L` still opens the editable Location Box.
- `Esc` and a second `Ctrl+L` still restore the Breadcrumb.
- New in RC2: clicking/interacting with the List or Grid file view also restores the Breadcrumb.
- The file-view mouse gesture continues normally; Ferry does not restore the pre-`Ctrl+L` keyboard focus in this path because the mouse interaction itself owns the next focus.
- Current file selection is not cleared merely because the Location Box is dismissed.

## Selection Anchor appearance

- The logical Shift-range Selection Anchor remains visually distinct from the current keyboard-selected item.
- The outline is now a 1 px dark-gray dashed rectangle rather than a dense black dashed rectangle.
- Dash spacing is increased so the indicator remains visibly dashed on FHD displays while drawing less attention than the selection highlight.

## Rubber-band autoscroll setting

- Settings now includes **Rubber-band autoscroll speed (30–150)**.
- Default remains **100**.
- The validated near-edge slow-scroll behavior and acceleration curve are retained; the setting controls the maximum speed.
- Existing pre-RC2 settings files that do not contain the new property automatically use the default value of 100.
- Import/export/reset include the new setting.

## RC status

v1.1.0 RC1 passed Windows validation. RC2 should receive a focused Windows regression covering the three changes above plus a minimal rubber-band/selection sanity check before promotion to v1.1.0 final.
