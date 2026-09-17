# Phase 6-11 Static Validation — ChoiceDialogResult split

## Scope
Move only the top-level `ChoiceDialogResult` enum out of `ChoiceDialog.cs` into `ChoiceDialogResult.cs`.

## Baseline
- Branch: `refactor/v1.1.2`
- Windows-validated Phase 6-10 baseline: `3577e287d55c751eca02da43c977c87fa3511ae9`
- Phase 6-11 final implementation head: `0d62aa62462c83697857d1b7dacd251a13ffe1fc`

## Final diff from validated baseline
- `Build.cmd`: +1 / -0
- `Source/Ferry/ChoiceDialog.cs`: +0 / -7
- `Source/Ferry/ChoiceDialogResult.cs`: +9 / -0
- `Source/Ferry/Ferry.csproj`: +1 / -0

## Boundary validation
Moved unchanged:
- `ChoiceDialogResult` (`Yes`, `No`, `Cancel`)

Kept in `ChoiceDialog.cs` unchanged:
- `ShowYesNo`
- `ShowYesNoCancel`
- `ShowArchiveConflict`
- generic dialog construction
- archive conflict dialog construction
- button creation and default/cancel behavior

No dialog UI, event wiring, default-button behavior, archive conflict behavior, file operations, Selection, D&D, Paste feedback, Search, Settings, Recycle Bin, or Shell behavior was changed.

## Build wiring
`ChoiceDialogResult.cs` is listed once in both:
- `Build.cmd`
- `Source/Ferry/Ferry.csproj`

Expected C# source count after Phase 6-11: 44.

## Result
**PASS — static validation.**

## Windows gate
1. `Build.cmd` succeeds.
2. Ferry launches normally.
3. Trigger one normal Yes/No or Yes/No/Cancel confirmation and confirm buttons/default/cancel behavior.
4. Optionally trigger an archive extraction conflict once and confirm its conflict dialog still opens normally.
