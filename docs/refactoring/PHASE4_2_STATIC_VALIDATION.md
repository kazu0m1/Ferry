# Ferry v1.1.2 Refactoring — Phase 4-2 Static Validation

## Scope

Phase 4-2 physically moves Archive progress, cancellation, and Archive-specific status helpers from `MainWindow.cs` into the existing `MainWindow.Archive.cs` partial class.

Moved unchanged:

- `BeginArchiveOperation`
- `CancelArchiveOperation`
- `UpdateArchiveProgress`
- `EndArchiveOperation`
- `ShowTimedStatusMessage`
- `ShowTransientStatusMessage`
- `ShowArchiveActivityStatus`
- `HideArchiveStatusControls`
- `FormatArchiveProgressStatus`
- `FormatArchiveBytes`
- `FormatArchiveEta`
- `FormatArchiveRatio`

`SetStatusMessage` and `UpdateStatus` remain in `MainWindow.cs`.

Implementation commit: `6a3d354989272786bad60f16eb0655d039b64e00`

## Diff audit

Expected two-file implementation diff confirmed:

- `Source/Ferry/MainWindow.Archive.cs`: +246 / -0
- `Source/Ferry/MainWindow.cs`: +0 / -244

The two additional lines in `MainWindow.Archive.cs` are the required namespace imports:

- `using System.Threading;`
- `using System.Windows.Threading;`

The removed block begins at `BeginArchiveOperation` immediately after `CloseTab` and ends after `FormatArchiveRatio`. The following general status method, `SetStatusMessage`, remains unchanged in `MainWindow.cs`.

## Build-list invariant

No C# file was added in this phase. `MainWindow.Archive.cs` was already explicitly listed in both:

- `Build.cmd`
- `Source/Ferry/Ferry.csproj`

The 33-file source-list invariant is therefore unchanged.

## Behavior-risk review

This batch is a physical move only. Archive operation state, cancellation behavior, progress/status formatting, transient completion messages, and dispatcher/timer behavior are unchanged.

No file-view selection/rubber-band/D&D behavior, external move safety, refresh reconciliation, Paste feedback, FileItem identity, sorting, search, navigation, Sidebar behavior, or keyboard event ordering was changed.

`SetStatusMessage` and `UpdateStatus` remain in `MainWindow.cs`; only the Archive-specific helper methods they call were relocated.

## Static result

**PASS**

## Windows validation gate

Before the next refactor batch:

1. Run `Build.cmd` and confirm successful build.
2. Launch `Portable\Ferry.exe`.
3. Compress a small file/folder set to ZIP and confirm progress/status and completion behave normally.
4. Extract a small ZIP and confirm progress/status and completion behave normally.
5. If practical, start a somewhat longer archive operation, use the Cancel button, and confirm cancellation completes normally and the ordinary status bar returns afterward.

A full file-view selection/rubber-band/external-D&D regression pass is not required for this batch because those paths were not modified.
