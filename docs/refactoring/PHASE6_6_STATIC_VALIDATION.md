# Phase 6-6 Static Validation — Archive conflict models split

## Scope
Move only the archive conflict/overwrite models out of `ArchiveModels.cs` into `ArchiveConflictModels.cs`.

## Baseline
- Branch: `refactor/v1.1.2`
- Windows-validated Phase 6-5 baseline: `97cf34fe05a00c356ffc95dba8dcc28bf6409e7d`
- Phase 6-6 final implementation head: `4974fbf96d59a0bd7a80f2d0f3b8e6e49d6c24d2`

## Final diff from validated baseline
- `Build.cmd`: +1 / -0
- `Source/Ferry/ArchiveConflictModels.cs`: +38 / -0
- `Source/Ferry/ArchiveModels.cs`: +0 / -36
- `Source/Ferry/Ferry.csproj`: +1 / -0

## Boundary validation
Moved unchanged:
- `ArchiveOverwriteDecision`
- `ArchiveOverwriteRequest`
- `ArchiveConflictResolution`
- MERGE-scope comments and behavior-related fields

Kept in `ArchiveModels.cs` unchanged:
- `ArchivePhase`
- `ArchiveOperationStatus`
- `ArchiveProgressInfo`
- `ArchiveOperationResult`

No archive execution, extraction/compression, conflict callback behavior, progress, cancellation, safety-threshold, result, Selection, D&D, Search, Paste feedback, Settings, Recycle Bin, or Shell behavior was changed.

## Build wiring
`ArchiveConflictModels.cs` is listed once in both:
- `Build.cmd`
- `Source/Ferry/Ferry.csproj`

Expected C# source count after Phase 6-6: 41.

## Result
**PASS — static validation.**

## Windows gate
1. `Build.cmd` succeeds.
2. Ferry launches normally.
3. Extract a ZIP that creates a file-name conflict and confirm the existing overwrite/keep-both/skip/cancel choice UI still appears and behaves normally.
4. If convenient, confirm a normal conflict-free extraction still works.
