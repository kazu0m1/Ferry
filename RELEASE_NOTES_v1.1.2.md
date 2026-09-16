# Ferry v1.1.2

Ferry v1.1.2 is a small drag-and-drop interoperability fix built on the validated v1.1.1 baseline.

## Fixed

- Fixed Ferry → Windows Explorer drag-and-drop on the same drive sometimes behaving like a copy: the destination received the item, but the original remained visible in Ferry.
- Ferry now completes Windows Shell unoptimized Move semantics only when both the WPF final drop result and Shell `Performed DropEffect` confirm Move.
- Already-moved source paths are detected before cleanup so optimized Shell moves are not deleted twice.

## Safety / regression validation

The v1.1.2 prototype 1 passed Windows real-machine checks for:

- same-drive Ferry → Explorer Move;
- `Ctrl+D&D` Copy safety;
- cancelled / invalid drops leaving the source untouched;
- folder and multi-item external D&D;
- Ferry → Ferry D&D regression;
- Explorer → Ferry D&D regression;
- v1.1.1 Paste-result feedback regression; and
- List/Grid selection and rubber-band smoke checks.

Cross-volume D&D was not run because the test machine did not have a second drive available. It remains explicitly unverified rather than inferred from the same-volume test.

## Unchanged

- v1.1.1 Paste-result selection behavior.
- v1.1.0 rubber-band selection, Selection Anchor, keyboard navigation, autoscroll, Breadcrumb/Location Box and toolbar behavior.
- Windows remains authoritative for destination-side drag/drop and conflict handling.

Release status: prototype 1 behavior accepted as the v1.1.2 final baseline; final promotion changes only release/version metadata and public documentation.
