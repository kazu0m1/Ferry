# Ferry v1.1.2 Refactoring Completion Summary

## Status
Behavior-preserving refactoring of Ferry v1.1.2 is complete on branch `refactor/v1.1.2`.

Original release baseline:
- `main`: `a688e59caffca1ac62af86ebc217ef293a137ad0` (`Release Ferry v1.1.2`)
- v1.1.2 annotated tag object: `98dd4e779a019fdff118380b1360375ce138be7a`

Both remain untouched.

## Refactoring outcome
The work focused on physical separation of existing responsibilities and types without intentional feature, behavior, version, release-note, or product-policy changes.

Major structural outcomes include:
- `MainWindow` UI construction separated into `MainWindow.UI.cs`
- Sidebar construction and pinned-item interactions separated into `MainWindow.Sidebar.cs`
- Archive orchestration/status UI separated into `MainWindow.Archive.cs`
- Paste feedback state separated into `MainWindow.PasteFeedback.cs`
- converter and tab-view context types separated from the original nested-type container
- settings storage separated from `AppSettings`
- archive safety/conflict/progress/result models separated by responsibility
- ZIP setup windows separated into individual files
- `ChoiceDialog` separated into standard and archive-conflict partials
- Settings window separated into UI and persistence partials
- Recycle Bin service separated into enumeration and operations partials
- several top-level model/helper types extracted into dedicated files

Current C# source count: **47**.
`Source/Ferry/*.cs`, `Build.cmd`, and `Ferry.csproj` are aligned to the same source set.

## Intentionally frozen areas
The following areas were deliberately not further split because their runtime behavior is timing-, Shell-, selection-, or object-identity-sensitive, or because available GitHub editing tools would require unsafe whole-file replacement:
- remaining `MainWindow.cs` (~208 KB)
- selection / keyboard / pointer / rubber-band / drag-and-drop arbitration
- `TabState` and unsorted-tail behavior
- `FileItemComparer` RC14 behavior
- `ClipboardHelper` / external drag-and-drop safety contract
- Shell interop / Shell folder / Shell file operation internals
- `ArchiveService` worker internals
- Search service
- Rename engine

This is an intentional safety boundary, not an assertion that these files are ideally sized.

## Failed refactor and recovery
Phase 7-2 split `RenameEngine` execution methods into `RenameEngine.Execution.cs` with textually unchanged method bodies.

Windows validation then showed severe responsiveness/stability regression:
- batch Rename / Undo stalls of roughly 10–20+ seconds
- one possible crash/restart-like event

The split was fully reverted. `RenameEngine.cs` was restored to the exact pre-split blob (`cb509438c624948e463bbebb63aa7319b1e6f3f7`), after which the same two-file batch Rename and Undo returned to essentially immediate response.

Therefore Rename-related structural refactoring is frozen in this completion state.

## Final validation
Static completion audit: PASS.
Final Windows integration validation: PASS for all tested v1.1.2 behavior.

Validated areas include navigation, tabs, List/Grid, selection variants, keyboard recovery, rubber-band/autoscroll, Rename/Undo, Paste feedback, search, archive creation/extraction/conflicts/cancel, Recycle Bin operations, Settings persistence/import/export, and tested drag/drop directions.

### Explicitly UNVERIFIED
- Ferry -> Explorer Move across different volumes/drives

This remains the pre-existing v1.1.2 interoperability validation gap. It is neither marked PASS nor treated as a refactoring regression.

## Repository hygiene
Completion audit found no refactoring-produced executable/debug/build artifacts in the source diff such as `.exe`, `.pdb`, `bin`, `obj`, `.vs`, or `Ferry.log`.

Version metadata, README, CHANGELOG, release notes, and the released v1.1.2 tag/main baseline were left unchanged.

## Completion decision
This branch is a release candidate for the behavior-preserving v1.1.2 refactored codebase.

No further code movement should be performed as part of this refactoring effort unless a new task explicitly reopens one of the frozen areas with a dedicated regression plan and suitable editing/build tooling.
