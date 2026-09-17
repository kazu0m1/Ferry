# Phase 5-3 Static Validation — TabViewContext file rename

## Scope
Rename `MainWindow.Types.cs` to `MainWindow.TabViewContext.cs` now that `TabViewContext` is the only remaining nested type in that partial.

## Baseline
- Branch: `refactor/v1.1.2`
- Phase 5-2 Windows validation commit: `1529192027f53f902b41ae719071deb760ab9f8a`
- Phase 5-3 final implementation commit: `b71f7127669ef46e25a94290ddfdec2b78aa24cc`
- Validation date: 2026-09-17

## Result
**PASS — static structural validation.**

GitHub comparison from the Phase 5-2 validated baseline reports only:
- `Build.cmd`: 1 line replaced (`MainWindow.Types.cs` → `MainWindow.TabViewContext.cs`).
- `Source/Ferry/Ferry.csproj`: 1 line replaced (`MainWindow.Types.cs` → `MainWindow.TabViewContext.cs`).
- `Source/Ferry/MainWindow.Types.cs` → `Source/Ferry/MainWindow.TabViewContext.cs`: detected as a rename with 0 additions and 0 deletions.

## Invariants
- `TabViewContext` body is byte-for-byte unchanged by the rename.
- No `MainWindow.cs` executable method changed.
- No event ordering, selection, refresh, paste, drag/drop, archive, or Shell behavior changed.
- Build and project source lists continue to reference the same source set, with only the filename updated.

## Notes
This removes the now-ambiguous `MainWindow.Types.cs` filename and makes the remaining high-coupling nested tab/view state object explicit before later MainWindow decomposition.