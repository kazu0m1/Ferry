# Phase 6-10 Static Validation — Extract ZIP setup window rename

## Scope
Rename `ArchiveSetupWindows.cs` to `ExtractZipSetupWindow.cs` after Phase 6-9 left only `ExtractZipSetupWindow` in that file.

## Baseline
- Branch: `refactor/v1.1.2`
- Windows-validated Phase 6-9 baseline: `c004cb6bad05f9dc7aac68b62c5c3758a6d91816`
- Phase 6-10 implementation head: `f4d1d0bdf7a65f980c662af90caf819ae47784be`

## Final diff from validated baseline
- `Build.cmd`: +1 / -1
- `Source/Ferry/ExtractZipSetupWindow.cs`: renamed from `Source/Ferry/ArchiveSetupWindows.cs`, +0 / -0
- `Source/Ferry/Ferry.csproj`: +1 / -1

## Boundary validation
Pure filename change only:
- `ArchiveSetupWindows.cs` -> `ExtractZipSetupWindow.cs`

Unchanged content:
- `ExtractZipSetupWindow`
- source ZIP display
- extraction destination browsing
- ZIP existence validation
- destination validation
- Start/Cancel behavior
- local UI helper methods

No archive execution, compression/extraction implementation, progress, cancellation, safety thresholds, conflict handling, result handling, Selection, D&D, Paste feedback, Search, Settings, Recycle Bin, or Shell behavior was changed.

## Build wiring
`ExtractZipSetupWindow.cs` replaces `ArchiveSetupWindows.cs` exactly once in both:
- `Build.cmd`
- `Source/Ferry/Ferry.csproj`

Expected C# source count after Phase 6-10: 43.

## Result
**PASS — static validation.**

## Windows gate
1. `Build.cmd` succeeds.
2. Ferry launches normally.
3. Open Extract ZIP and confirm the setup window opens normally.
4. Confirm destination Browse and Cancel work.
5. Extract a small ZIP successfully.
