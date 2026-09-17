# Phase 6-2 Static Validation — RecycleBinEntry split

## Scope
Move only the `RecycleBinEntry` data model out of `RecycleBinService.cs` into `RecycleBinEntry.cs`.

## Baseline
- Branch: `refactor/v1.1.2`
- Baseline commit: `d8d7595ee88d84aa103537b340639f1cb630021e`
- Final implementation commit: `697068642aa88137f7b04302b572bd9f3d3aaada`

## Static result
**PASS**

Confirmed:
- `RecycleBinEntry.cs` contains the same model members previously declared in `RecycleBinService.cs`.
- `RecycleBinService.cs` removes only the 11-line `RecycleBinEntry` class block.
- Recycle Bin enumeration, metadata parsing, restore, permanent delete, metadata cleanup, and `SHEmptyRecycleBin` logic are unchanged.
- `Build.cmd` and `Ferry.csproj` each add exactly one source entry for `RecycleBinEntry.cs`.
- Source-count invariant is now 37 C# files in both build enumerations.
- No MainWindow, selection, drag/drop, archive, search, paste-feedback, or settings behavior was changed.
