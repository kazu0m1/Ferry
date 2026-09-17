# Ferry v1.1.2 Refactoring — Phase 5-1 Static Validation

## Scope

Establish `MainWindow.PasteFeedback.cs` as the dedicated partial for Paste feedback state types.

## Baseline

- Branch: `refactor/v1.1.2`
- Starting commit: `60a230988e77f95808df0995c42f4a042a0b5bbc`
- New partial commit: `23b13217212bcecc345f21840e7552137c670e37`
- Wiring/type relocation commit: `b77014ad13eb44e4181798f29a9f095053ca2b9d`

## Exact structural changes

- Added `Source/Ferry/MainWindow.PasteFeedback.cs`.
- Moved the unchanged nested data types `PasteEntryStamp` and `PasteFeedbackSession` from `MainWindow.Types.cs` into the new partial.
- Added `MainWindow.PasteFeedback.cs` to both explicit source lists:
  - `Build.cmd`
  - `Source/Ferry/Ferry.csproj`
- No Paste execution method was moved or rewritten.
- `MainWindow.cs` was not changed.

## Diff check

Compared with the Phase 4-2 Windows-validated baseline:

- `Build.cmd`: +1 / -0
- `Source/Ferry/Ferry.csproj`: +1 / -0
- `Source/Ferry/MainWindow.PasteFeedback.cs`: +26 / -0
- `Source/Ferry/MainWindow.Types.cs`: +0 / -18

The removed 18 lines are exactly the two relocated nested data types.

## Result

**PASS — static structural validation.**

A Windows build/startup smoke test is the next gate. Because no executable method body changed, a full Paste regression test is not required for this phase.
