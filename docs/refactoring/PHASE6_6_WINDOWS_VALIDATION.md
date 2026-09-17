# Phase 6-6 Windows Validation — Archive conflict models split

## Scope
Windows build/runtime validation after moving only the archive conflict/overwrite models from `ArchiveModels.cs` to `ArchiveConflictModels.cs`.

## Baseline
- Branch: `refactor/v1.1.2`
- Phase 6-6 implementation head: `4974fbf96d59a0bd7a80f2d0f3b8e6e49d6c24d2`
- Static validation commit: `73bad91c89da75cc1425e70221e9c5dfe660fdac`
- Validation date: 2026-09-17

## Result
**PASS — Windows build/runtime validation.**

Confirmed:
- `Build.cmd` succeeds.
- Ferry launches normally.
- ZIP extraction with an existing-name conflict shows the normal conflict choice UI.
- Existing conflict choices continue to work normally.
- Normal conflict-free extraction also remains functional.

## Notes
Phase 6-6 moved only `ArchiveOverwriteDecision`, `ArchiveOverwriteRequest`, and `ArchiveConflictResolution`. Archive execution, progress, cancellation, safety thresholds, and result handling were not changed.
