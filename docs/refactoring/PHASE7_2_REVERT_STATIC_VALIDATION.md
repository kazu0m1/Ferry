# Phase 7-2 Revert Static Validation — RenameEngine execution split rollback

## Scope
Temporarily revert only the Phase 7-2 `RenameEngine.Execution.cs` structural split in order to isolate a Windows runtime performance/stability anomaly observed during batch rename/Undo.

## Baseline
- Branch: `refactor/v1.1.2`
- Phase 7-3 Windows validation record: `d50b7ec568dbf7f0096009ef16942a4e59495f01`
- Revert implementation head: `09e9c521aa22e2ec2f858222af8ecad02fa58d0e`
- Validation date: 2026-09-17

## Observed anomaly prompting rollback
- Two-file batch Rename/Undo took roughly 10 seconds in an earlier run.
- A later two-file batch rename took more than 20 seconds and appeared to hang; Ferry may have crashed/restarted.
- The Phase 7-2 code review showed only a physical partial-class split, so the relationship is unproven and requires A/B-style runtime revalidation.

## Revert boundary
Restored into `RenameEngine.cs` unchanged:
- `ExecuteRename`
- `Undo`
- `MovePath`
- `Rollback`
- `PathsExactlyEqual`
- `MoveRecord`

Removed:
- `RenameEngine.Execution.cs`
- its `Build.cmd` source entry
- its `Ferry.csproj` Compile entry

Retained:
- Phase 7-3 `SettingsWindow.Persistence.cs` split and all unrelated refactoring.

## Exact-content check
The restored `Source/Ferry/RenameEngine.cs` blob SHA is:
`cb509438c624948e463bbebb63aa7319b1e6f3f7`

This exactly matches the `RenameEngine.cs` blob from before Phase 7-2, confirming that the RenameEngine implementation and file contents are restored to the pre-split state.

## Final diff from Phase 7-3 Windows-validated point
- `Build.cmd`: -1
- `Source/Ferry/Ferry.csproj`: -1
- `Source/Ferry/RenameEngine.Execution.cs`: removed
- `Source/Ferry/RenameEngine.cs`: execution block restored and `partial` removed

## Result
**PASS — static rollback validation.**

## Windows revalidation gate
1. `Build.cmd` succeeds.
2. Ferry launches normally.
3. Use the same two small files where practical.
4. Perform one two-file batch rename and note approximate elapsed time.
5. Immediately run Undo Rename and note approximate elapsed time.
6. Repeat rename + Undo at least two more times if practical.
7. Report whether Ferry freezes, becomes unresponsive, crashes, or restarts.

Interpretation:
- If latency/stability returns to normal, Phase 7-2 structural split is implicated despite no intended logic change and should remain reverted.
- If the same severe latency remains, the cause likely predates Phase 7-2 or is environmental/runtime-related, and Rename should be investigated separately before further structural changes there.
