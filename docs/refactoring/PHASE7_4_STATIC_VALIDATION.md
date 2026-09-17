# Phase 7-4 Static Validation — Recycle Bin enumeration split

## Scope
Behavior-preserving partial split of Recycle Bin enumeration/parsing from Recycle Bin mutation operations.

## Branch
- `refactor/v1.1.2`

## Baseline
- Windows-validated baseline: `874e1fdceab01ec6bcd76096a4b2e018add49236`

## Implementation
- Implementation head before this record: `7457083665d6d4f1dfcda034e060910369215dc4`
- Added `Source/Ferry/RecycleBinService.Enumeration.cs`.
- `RecycleBinService` changed to `partial`.
- Moved unchanged:
  - `EnumerateCurrentUser`
  - `ReadEntry`
  - `SafeFileName`
- Retained unchanged in `RecycleBinService.cs`:
  - `SHEmptyRecycleBin`
  - `Empty`
  - `Restore`
  - `DeletePermanently`
  - `TryDeleteMetadata`
- Added the new source file exactly once to `Build.cmd` and `Ferry.csproj`.

## Static checks
- Compare from baseline shows only four implementation files changed.
- Enumeration/parsing methods exist only in the new partial.
- Mutation methods exist only in the original partial.
- Method bodies, exception handling, metadata parsing, sort order, and Shell empty operation were not intentionally changed.
- RenameEngine remains in the reverted/frozen single-file structure after the Phase 7-2 regression investigation.

## Result
**PASS — ready for Windows validation.**
