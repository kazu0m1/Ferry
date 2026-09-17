# Ferry v1.1.2 Refactoring — Phase 4-1 Static Validation

## Scope

Phase 4-1 physically moves the Archive command/orchestration entry points from `MainWindow.cs` into a new `MainWindow.Archive.cs` partial class.

Moved unchanged:

- `CompressSelected`
- `ExtractZip`
- `ConfirmArchiveSafetyRiskAsync`
- `ConfirmArchiveConflictAsync`

Archive progress/status helpers remain in `MainWindow.cs` for a later, separate batch.

Implementation commit: `bec389931c0f52f67fbfd4d95e083aaaebd671cc`

## Diff audit

Expected four-file diff confirmed:

- `Build.cmd`: +1 / -0
- `Source/Ferry/Ferry.csproj`: +1 / -0
- `Source/Ferry/MainWindow.Archive.cs`: +211 / -0
- `Source/Ferry/MainWindow.cs`: +0 / -199

The `MainWindow.cs` deletion begins at `CompressSelected` and ends after `ConfirmArchiveConflictAsync`. The preceding `CreateShortcut` method and following `EmptyRecycleBin` method remain unchanged.

`MainWindow.Archive.cs` contains the same four method bodies plus only the namespace/partial-class wrapper and required using directives.

## Build-list invariant

The new partial is explicitly listed in both build descriptions:

- `Build.cmd`
- `Source/Ferry/Ferry.csproj`

Expected source count after this batch: **33 C# files**.

## Behavior-risk review

This batch is a physical move only. No archive workflow, overwrite/conflict decision, safety warning, cancellation, progress reporting, destination selection, refresh scheduling, or error handling behavior was intentionally changed.

The following are untouched:

- file-view selection and rubber-band state machines
- keyboard event phase/order
- file-view and external drag-and-drop behavior
- v1.1.2 Shell `Performed DropEffect` external-move safety path
- FileItem identity / incremental refresh / unsorted-tail behavior
- Paste feedback
- Search
- Sidebar/Pinned behavior
- Recycle Bin behavior
- Settings

## Static result

**PASS**

## Windows validation gate

Before the next refactor batch:

1. Run `Build.cmd` and confirm successful build.
2. Launch `Portable\Ferry.exe`.
3. Confirm ordinary startup/navigation works.
4. Confirm the context menu still exposes `Compress to ZIP` for a normal file/folder selection.
5. Compress a small file/folder to ZIP and confirm completion.
6. Extract a small ZIP (`Extract Here` or the named-folder option) and confirm completion.

A full selection/rubber-band/external-D&D regression pass is not required for this batch because those areas were not changed.
