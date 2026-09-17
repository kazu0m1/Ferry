# Phase 3-2 Static Validation

## Scope

Phase 3-2 physically moves the front-half Sidebar construction and sizing methods from `MainWindow.cs` into the existing `MainWindow.Sidebar.cs` partial class.

Moved without intentional behavior changes:

- `SidebarSplitterPreviewMouseLeftButtonDown`
- `FitSidebarWidthToContent`
- `MeasureSidebarText`
- `BuildSidebar`
- `AddKnownFolder`
- `AddSidebarHeading`
- `SidebarButton`
- `AddSidebarButton`
- `BuildPinnedSidebarList`

The existing `PinnedSidebarItem` type remains in `MainWindow.Sidebar.cs`.

## Explicitly Not Moved Yet

The later Sidebar/Pinned interaction block remains in `MainWindow.cs` for a subsequent small batch, including `PinFolder`, pinned-folder reorder and drag/drop handlers, and Sidebar drop handling. `ApplySidebarLayoutFromSettings` also remains in `MainWindow.cs`.

## Diff Audit

Candidate commit: `a56c554ec9642e18075bfacfe3a47193bbea5962` (`Phase 3-2: move Sidebar construction`).

Compared with the Phase 3-1 validated head `8b46b2da98b5f338dfdccfd3bda661b5403c7d4e`:

- `Source/Ferry/MainWindow.Sidebar.cs`: +208 / -0
- `Source/Ferry/MainWindow.cs`: +0 / -202
- No other files changed.

The `MainWindow.cs` hunk removes only the Sidebar block beginning at `SidebarSplitterPreviewMouseLeftButtonDown` and ending after `BuildPinnedSidebarList`. The following method, `OpenNewTab`, remains unchanged and is the first method after the removed block.

## Build Enumeration Invariant

`MainWindow.Sidebar.cs` was already added to both `Build.cmd` and `Source/Ferry/Ferry.csproj` in Phase 3-1, so Phase 3-2 requires no build-list changes. The source enumeration invariant is preserved.

## Behavior-Preservation Review

Method bodies, comments, event registration order, and call relationships inside the moved block were preserved. This batch does not modify selection, rubber-band selection, keyboard handling, file-item identity, refresh/reconciliation, paste feedback, file-view drag/drop, or the v1.1.2 external move completion logic.

## Static Result

PASS.

Windows validation is still required before the next Sidebar batch.
