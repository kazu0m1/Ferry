# Phase 6-7 Static Validation — Archive progress models split

## Scope
Move only the archive progress models out of `ArchiveModels.cs` into `ArchiveProgressModels.cs`.

## Baseline
- Branch: `refactor/v1.1.2`
- Windows-validated Phase 6-6 baseline: `150e01813bb7406286d66bee839e54778cfacad2`
- Phase 6-7 final implementation head: `ce3bf9da12ea190ac599b4a2fdaa2b19a4c5f9f1`

## Final diff from validated baseline
- `Build.cmd`: +1 / -0
- `Source/Ferry/ArchiveModels.cs`: +0 / -46
- `Source/Ferry/ArchiveProgressModels.cs`: +50 / -0
- `Source/Ferry/Ferry.csproj`: +1 / -0

## Boundary validation
Moved unchanged:
- `ArchivePhase`
- `ArchiveProgressInfo`
- overall progress percentage calculation
- current-item percentage calculation
- byte/file counters, throughput, and ETA fields

Kept in `ArchiveModels.cs` unchanged:
- `ArchiveOperationStatus`
- `ArchiveOperationResult`

No archive execution, compression/extraction logic, progress dispatch/update behavior, cancellation, conflict handling, safety thresholds, result handling, Selection, D&D, Search, Paste feedback, Settings, Recycle Bin, or Shell behavior was changed.

## Build wiring
`ArchiveProgressModels.cs` is listed once in both:
- `Build.cmd`
- `Source/Ferry/Ferry.csproj`

Expected C# source count after Phase 6-7: 42.

## Result
**PASS — static validation.**

## Windows gate
1. `Build.cmd` succeeds.
2. Ferry launches normally.
3. Compress a small folder/file set and confirm archive progress/status and completion remain normal.
4. Extract a small ZIP and confirm archive progress/status and completion remain normal.
5. If convenient, start a longer archive operation and confirm the progress display updates and Cancel still behaves normally.
