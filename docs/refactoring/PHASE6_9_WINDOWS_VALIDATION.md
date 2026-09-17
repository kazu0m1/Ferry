# Phase 6-9 Windows Validation — Create ZIP setup window split

## Scope
Windows build/runtime validation after moving only `CreateZipSetupWindow` from `ArchiveSetupWindows.cs` to `CreateZipSetupWindow.cs`.

## Baseline
- Branch: `refactor/v1.1.2`
- Phase 6-9 implementation head: `ab40b7c4f504a9f83f24cfc66b24e842cf974a00`
- Static validation commit: `6aad23f90b6bf6d18247199f04ec640468304ba9`
- Validation date: 2026-09-17

## Result
**PASS — Windows build/runtime validation.**

Confirmed:
- `Build.cmd` succeeds.
- Ferry launches normally.
- Create ZIP setup window shows the selected-item list and destination controls normally.
- Browse, Cancel, and Start behave normally.
- A small ZIP can be created successfully.
- Extract ZIP setup window still opens and behaves normally.

## Notes
Phase 6-9 moved only `CreateZipSetupWindow`. Archive execution, progress, cancellation, safety thresholds, conflict handling, result handling, and `ExtractZipSetupWindow` behavior were not changed.
