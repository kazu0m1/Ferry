# Phase 7-1 Static Validation — ChoiceDialog archive conflict partial split

## Scope
Move only `ChoiceDialog.ShowArchiveConflict` out of `ChoiceDialog.cs` into `ChoiceDialog.ArchiveConflict.cs`, while preserving the public/internal call surface and behavior.

## Baseline
- Branch: `refactor/v1.1.2`
- Windows-validated Phase 6-11 baseline: `bbc56bdb208ffada786864e1fb55e0dbae9420a8`
- Phase 7-1 final implementation head: `5c31b2b015dd20dbf6e29b4376772928c4b1bd26`

## Final diff from validated baseline
- `Build.cmd`: +1 / -0
- `Source/Ferry/ChoiceDialog.ArchiveConflict.cs`: +200 / -0
- `Source/Ferry/ChoiceDialog.cs`: +1 / -190
- `Source/Ferry/Ferry.csproj`: +1 / -0

## Boundary validation
Moved without changing the `ShowArchiveConflict` signature or interaction flow:
- archive conflict message construction
- MERGE / REPLACE availability rules
- KEEP BOTH / SKIP / CANCEL handling
- merged-folder apply-to-remaining option
- dialog sizing/ownership/default/cancel behavior

Kept in `ChoiceDialog.cs`:
- `ShowYesNo`
- `ShowYesNoCancel`
- private generic `Show`
- shared private `CreateButton`

Both class declarations are now `internal static partial class ChoiceDialog`, so the shared private helper remains accessible without changing call sites or duplicating code.

No MainWindow, archive execution, extraction/compression, progress, cancellation, safety thresholds, result handling, Selection, D&D, Paste feedback, Search, Settings, Recycle Bin, or Shell behavior was changed.

## Build wiring
`ChoiceDialog.ArchiveConflict.cs` is listed once in both:
- `Build.cmd`
- `Source/Ferry/Ferry.csproj`

Expected C# source count after Phase 7-1: 45.

## Result
**PASS — static validation.**

## Windows gate
1. `Build.cmd` succeeds.
2. Ferry launches normally.
3. Open a normal Yes/No or Yes/No/Cancel dialog and confirm Enter/Esc behavior remains normal.
4. Extract a ZIP into a destination with a same-name conflict and confirm the Archive conflict dialog appears normally.
5. Confirm at least one conflict action (REPLACE/MERGE, KEEP BOTH, SKIP, or CANCEL) behaves normally.
