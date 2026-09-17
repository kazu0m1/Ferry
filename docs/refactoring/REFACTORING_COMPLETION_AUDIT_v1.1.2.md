# Ferry v1.1.2 Refactoring Completion Audit

Date: 2026-09-17
Branch: `refactor/v1.1.2`
Release baseline: `a688e59caffca1ac62af86ebc217ef293a137ad0` (`Release Ferry v1.1.2`)
Audit baseline head: `22032235c2a35ea9f5c7f16ed53415218bbe775b`

## 1. Conclusion

The behavior-preserving v1.1.2 refactoring has reached a sensible stopping point.

The implemented changes are structural: physical moves, partial-class splits, top-level type extraction, and responsibility-oriented file renames. No product feature, version, release metadata, README, CHANGELOG, release notes, or behavior contract was intentionally changed.

Further splitting of the remaining large `MainWindow.cs` is deferred because the available GitHub editing path requires full-file replacement for a ~208 KB source file. Given Ferry's timing-sensitive WPF/Shell behavior and the Phase 7-2 Rename regression discovered during this refactor, forcing a giant replacement would violate the safety-first refactoring policy.

## 2. Final source/build invariant

Current `Source/Ferry` contains **47 C# source files**.

The same 47 C# files are explicitly listed in both:
- `Build.cmd`
- `Source/Ferry/Ferry.csproj`

No source omission or duplicate build entry was found.

The source tree contains no obsolete refactoring filenames such as:
- `MainWindow.Types.cs`
- `ArchiveModels.cs`
- `SettingsWindow.cs`
- `ChoiceDialog.cs`
- `RecycleBinService.cs`
- `RenameEngine.Execution.cs`

## 3. Main structural results

### MainWindow

Extracted from the monolithic `MainWindow.cs`:
- `MainWindow.UI.cs`
- `MainWindow.Sidebar.cs`
- `MainWindow.Archive.cs`
- `MainWindow.PasteFeedback.cs` (paste feedback state types)
- `MainWindow.Converters.cs`
- `MainWindow.TabViewContext.cs`

`MainWindow.cs` remains large (~208 KB) and is still the main structural-debt concentration. This is accepted for this refactoring release rather than risking behavior-sensitive mass edits.

### Settings

Separated into:
- `AppSettings.cs`
- `SettingsStore.cs`
- `SettingsWindow.UI.cs`
- `SettingsWindow.Persistence.cs`

### Archive model/setup organization

Models separated into:
- `ArchiveSafetyModels.cs`
- `ArchiveConflictModels.cs`
- `ArchiveProgressModels.cs`
- `ArchiveResultModels.cs`

Setup windows separated into:
- `CreateZipSetupWindow.cs`
- `ExtractZipSetupWindow.cs`

Archive-specific MainWindow orchestration/status helpers are in `MainWindow.Archive.cs`.

### Choice dialogs

Separated into:
- `ChoiceDialog.Standard.cs`
- `ChoiceDialog.ArchiveConflict.cs`
- `ChoiceDialogResult.cs`

### Recycle Bin

Separated into:
- `RecycleBinEntry.cs`
- `RecycleBinService.Enumeration.cs`
- `RecycleBinService.Operations.cs`

### Other extracted top-level types

- `SearchRequest.cs`
- `RenameUndoRecord.cs`

## 4. Failed refactor intentionally reverted

Phase 7-2 split `RenameEngine` execution/undo code into a partial file.

Windows validation then showed a severe regression:
- two-file batch Rename/Undo became ~10–20+ seconds,
- one run appeared to hang and possibly restart/crash.

The split was structurally reverted. After revert:
- the same two-file Rename completed essentially immediately,
- Undo responsiveness returned to normal,
- `RenameEngine.cs` blob returned to `cb509438c624948e463bbebb63aa7319b1e6f3f7`, identical to the pre-split file,
- `RenameEngine.Execution.cs` is absent from the final tree.

Therefore Rename-related structural refactoring is frozen for this release.

## 5. Protected / intentionally frozen areas

The following remain intentionally unsplit or otherwise frozen because behavior risk outweighs structural benefit for this release:

- `RenameEngine.cs`
- `FileItemComparer.cs` unsorted-tail / RC14 behavior
- `TabState.cs`
- selection / keyboard / rubber-band / D&D interaction code still in `MainWindow.cs`
- incremental refresh and FileItem identity-sensitive code
- `ClipboardHelper.cs` and v1.1.2 external D&D contracts
- Shell interop/context menu/file operation code
- `ArchiveService.cs` core compression/extraction engine
- `VirtualizingWrapPanel.cs`

## 6. Repository hygiene

Release-baseline-to-current comparison shows no refactor-generated binary or local-workspace artifacts such as:
- `.exe`
- `.pdb`
- `bin/`
- `obj/`
- `.vs/`
- `Ferry.log`
- temporary/staging source files

The release metadata and user-facing release documentation were not altered as part of the refactor.

## 7. Validation status

Every retained refactoring phase has received static validation and an appropriate Windows runtime gate.

The notable failed Phase 7-2 change was reverted and separately revalidated.

One pre-existing v1.1.2 limitation remains unchanged: cross-volume Ferry-to-Explorer Move is not claimed as verified because no suitable second drive was available during v1.1.2 validation.

## 8. Recommended final gate

Before treating this branch as a release-ready refactoring candidate, run one consolidated Windows regression pass covering the highest-value preserved contracts:

1. Build and fresh launch.
2. Folder navigation, tabs, List/Grid switching.
3. Basic selection plus Ctrl/Shift/Ctrl+Shift and true-background clearing.
4. Arrow / Shift+Arrow selection-anchor behavior.
5. Rubber-band selection and autoscroll in List/Grid.
6. List/Grid selection preservation.
7. F2 single rename plus batch rename and Undo responsiveness.
8. Copy/Cut -> Paste feedback.
9. Search: Contains, StartsWith, Japanese width-insensitive, clear search.
10. ZIP create/extract, conflict dialog, cancellation/status restoration.
11. Recycle Bin enumeration, Restore, Delete Permanently.
12. Settings Save/persistence plus Import/Export.
13. Ferry -> Explorer same-drive Move, Ctrl+D&D Copy, cancel/invalid drop.
14. Ferry -> Ferry and Explorer -> Ferry D&D.

Cross-volume Move should remain explicitly marked UNVERIFIED unless a second volume is actually tested.

## 9. Release-readiness interpretation

If the consolidated Windows gate passes, this branch should be considered a behavior-preserving v1.1.2 refactoring candidate suitable for the next release-packaging/release-decision discussion.

No further code splitting is recommended before that gate unless a concrete defect is found.