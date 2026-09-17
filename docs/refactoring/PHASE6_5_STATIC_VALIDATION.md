# Phase 6-5 Static Validation — Archive safety models split

## Scope
Move only the archive safety-related model types from `ArchiveModels.cs` to `ArchiveSafetyModels.cs`.

## Baseline
- Branch: `refactor/v1.1.2`
- Windows-validated Phase 6-4 baseline: `5e2a1684b5ab9db8579f5dcf3e4d434193b2cfd3`
- Phase 6-5 implementation commit: `c7c5e2c5b9b69d315455eb80a38dbbfe7b4f8d8e`

## Final diff
- `Build.cmd`: +1 / -0
- `Source/Ferry/Ferry.csproj`: +1 / -0
- `Source/Ferry/ArchiveModels.cs`: +0 / -47
- `Source/Ferry/ArchiveSafetyModels.cs`: +51 / -0

## Boundary validation
Moved unchanged:
- `ArchiveSafetyIssueType`
- `ArchiveThresholds`
- `ArchiveSafetyIssue`
- `ArchiveSafetyReport`

Preserved unchanged, including comments and default thresholds:
- Expanded size warning: 20 GiB
- File count warning: 50000
- Compression ratio warning: 100.0

Kept in `ArchiveModels.cs` unchanged:
- `ArchivePhase`
- `ArchiveOverwriteDecision`
- `ArchiveOperationStatus`
- `ArchiveProgressInfo`
- `ArchiveOverwriteRequest`
- `ArchiveConflictResolution`
- `ArchiveOperationResult`

No archive scanning, compression, extraction, progress, conflict handling, confirmation, cancellation, or finalization logic was changed.

## Build wiring
`ArchiveSafetyModels.cs` is listed once in both:
- `Build.cmd`
- `Source/Ferry/Ferry.csproj`

Expected C# source count after Phase 6-5: 40.

## Result
**PASS — static validation.**

## Windows gate
1. `Build.cmd` succeeds.
2. Ferry launches normally.
3. Compress a small file or folder to ZIP and confirm completion.
4. Extract a small ZIP and confirm completion.
5. No need to force the 20 GiB / 50000-file / 100x safety warnings in this phase because the safety logic and threshold values were not changed.
