# Phase 7-2 Static Validation — RenameEngine execution split

## Scope
Move only RenameEngine's file-system execution/undo responsibility from `RenameEngine.cs` to `RenameEngine.Execution.cs` and make `RenameEngine` partial.

## Baseline
- Branch: `refactor/v1.1.2`
- Windows-validated Phase 7-1 baseline: `23f6c8232c5d8045774c8ca07d333eb4437fac3e`
- Phase 7-2 final implementation head: `123811aeeeab8a01291b1a721087fa1378778e90`

## Final diff from validated baseline
- `Build.cmd`: +1 / -0
- `Source/Ferry/Ferry.csproj`: +1 / -0
- `Source/Ferry/RenameEngine.Execution.cs`: +69 / -0
- `Source/Ferry/RenameEngine.cs`: +1 / -60

## Boundary validation
Moved unchanged:
- `ExecuteRename`
- `Undo`
- `MovePath`
- `Rollback`
- `PathsExactlyEqual`
- private `MoveRecord`

Kept in `RenameEngine.cs` unchanged:
- rename template token generation
- template name building
- base-name/extension helpers
- target path helpers
- Windows filename validation
- batch collision validation
- path normalization / case-insensitive comparison
- reserved device-name construction

The only structural change to the existing type declaration is `partial`.

## Build wiring
`RenameEngine.Execution.cs` is listed once in both:
- `Build.cmd`
- `Source/Ferry/Ferry.csproj`

Expected C# source count after Phase 7-2: 46.

## Behavior preservation
No rename algorithm, temporary-file naming, two-stage move, rollback order, collision validation, Undo semantics, comments, or error text was changed.

## Result
**PASS — static validation.**

## Windows gate
1. `Build.cmd` succeeds.
2. Ferry launches normally.
3. F2 rename one file and commit.
4. Undo Rename restores the original name.
5. Run a small batch rename and verify the preview/rename result.
6. If convenient, cancel a rename dialog and confirm no file-system change occurs.