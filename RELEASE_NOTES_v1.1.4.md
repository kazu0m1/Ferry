# Ferry v1.1.4

Ferry v1.1.4 is a focused responsiveness bugfix release for long Windows Shell Copy/Move operations, built on the validated v1.1.3 baseline.

## Fixed

- Long Copy/Move operations no longer block Ferry's WPF UI thread until the Windows Shell transfer finishes.
- Ferry can now be minimized and restored while a large Copy/Move is still running.
- Tabs can be opened/switched and folders can be browsed while a Ferry-owned Shell transfer is active.
- Ferry-to-Ferry drag-and-drop no longer keeps the sending Ferry trapped in `DragDrop.DoDragDrop(...)` for the full duration of the target transfer.
- Ferry prevents application close while a Ferry-owned Copy/Move is active, avoiding abandonment of the transfer worker.

## Implementation

Ferry continues to delegate the actual Copy/Move operation, progress UI, conflict handling, and cancellation to Windows `SHFileOperation`.

The change is deliberately narrow:

- Copy/Move work runs on a dedicated STA worker.
- Paste and other synchronous callers retain their existing operation-completion contract while the WPF Dispatcher continues processing UI work.
- Ferry-to-Ferry target-side Drop returns promptly and the target's STA worker continues the accepted Shell transfer.
- Delete operations are unchanged.
- Ferry still allows only one Ferry-owned Shell Copy/Move transfer at a time.

Ferry does not introduce a custom copy engine.

## Windows validation

The final implementation passed real-machine checks for:

- large cross-volume Cut → Paste Move from C: to D:;
- large cross-volume Cut → Paste Move from D: to C:;
- minimize / restore during an active Move;
- opening and switching tabs during an active Move;
- folder navigation during an active Move;
- large Copy responsiveness and normal completion;
- active-transfer close protection;
- Ferry → Ferry D&D with both receiving and sending windows minimizing/restoring normally;
- tab opening and folder navigation in the sending Ferry during an active D&D Copy; and
- same-drive Ferry → Ferry D&D Move with the source removed and a single destination item present.

Windows GitHub Actions `Build.cmd` also passed during development of both the STA-worker change and the D&D handoff follow-up.

## Preserved

- v1.1.3 removable-drive arrival/removal and safe-eject behavior.
- v1.1.2 Ferry → Explorer external D&D Move completion behavior.
- v1.1.1 Paste-result selection behavior.
- v1.1.0 rubber-band selection, Selection Anchor, keyboard navigation, autoscroll, Breadcrumb/Location Box, and toolbar behavior.
- Windows remains authoritative for Shell file-operation behavior.

Release status: the responsive-transfer implementation passed Windows validation before final promotion; v1.1.4 finalization changes version/release metadata and packaging only.
