# Phase 6-4 Static Validation — RenameUndoRecord split

## Scope
Move only the top-level `RenameUndoRecord` data type out of `RenameEngine.cs` into `RenameUndoRecord.cs`.

## Baseline
- Branch: `refactor/v1.1.2`
- Windows-validated Phase 6-3 baseline: `944f3dbf72ccce1cadb75d29a920ba42bd5e249c`
- Phase 6-4 implementation commit: `bf4e43664080b9547e5db1d5d90b050872b3f707`

## Final diff from validated baseline
- `Build.cmd`: +1 / -0
- `Source/Ferry/Ferry.csproj`: +1 / -0
- `Source/Ferry/RenameEngine.cs`: +0 / -6
- `Source/Ferry/RenameUndoRecord.cs`: +8 / -0

No temporary/staging files remain in the final tree.

## Boundary validation
Moved unchanged:
- `RenameUndoRecord`

Kept in `RenameEngine.cs` unchanged:
- `ExecuteRename`
- `Undo`
- `MovePath`
- `Rollback`
- path normalization/comparison helpers
- reserved-name construction
- private `MoveRecord`

No rename validation, execution, rollback, undo behavior, event ordering, selection behavior, D&D behavior, Paste feedback, Search, Archive, Recycle Bin, Settings, or Shell behavior was changed.

## Build wiring
`RenameUndoRecord.cs` is listed once in both:
- `Build.cmd`
- `Source/Ferry/Ferry.csproj`

Expected C# source count after Phase 6-4: 39.

## Result
**PASS — static validation.**

## Windows gate
1. `Build.cmd` succeeds.
2. Ferry launches normally.
3. Rename one file with F2 and confirm with Enter.
4. Use Undo Rename if available in the tested flow, or perform the existing rename undo shortcut/path used in Ferry and confirm the original name is restored.
5. Optionally rename a small multi-selection batch and confirm completion.
