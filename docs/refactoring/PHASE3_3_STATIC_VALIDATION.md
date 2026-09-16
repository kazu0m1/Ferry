# Ferry v1.1.2 Refactoring — Phase 3-3 Static Validation

## Scope

Phase 3-3 physically moves the remaining Sidebar/Pinned interaction methods from `MainWindow.cs` into the existing `MainWindow.Sidebar.cs` partial class.

Moved unchanged:

- `PinFolder`
- `GetPinnedListItem`
- `PinnedListMouseLeftButtonDown`
- `PinnedListMouseLeftButtonUp`
- `PinnedListMouseMove`
- `PinnedListMouseRightButtonDown`
- `GetPinnedDropSlot`
- `PinnedListDragOver`
- `PinnedListDrop`
- `ShowPinnedDropIndicator`
- `ClearPinnedDropIndicator`
- `SidebarDragOver`
- `SidebarDrop`
- `ReorderPinnedFolderToSlot`

`EmptyRecycleBin` and all following non-Sidebar methods remain in `MainWindow.cs`.

Implementation commit: `cdc6ed9938e5923d59508faf1a55550982f1fe26`

## Diff audit

Expected two-file diff confirmed:

- `Source/Ferry/MainWindow.Sidebar.cs`: +229 / -0
- `Source/Ferry/MainWindow.cs`: +0 / -228

The `MainWindow.cs` deletion consists of the 227-line Sidebar/Pinned interaction block plus one separator blank line.

The `MainWindow.Sidebar.cs` addition consists of the same 227-line block, one separator blank line, and `using System.Collections.Generic;`, which is required by `ReorderPinnedFolderToSlot`.

The removal begins at `PinFolder` and ends after `ReorderPinnedFolderToSlot`. The next method, `EmptyRecycleBin`, remains unchanged in `MainWindow.cs`.

## Build-list invariant

No new C# file was added in this phase. `MainWindow.Sidebar.cs` was already explicitly listed in both:

- `Build.cmd`
- `Source/Ferry/Ferry.csproj`

Therefore no build-list change is required for Phase 3-3.

## Behavior-risk review

This batch is a physical move only. Method bodies, event registration order, modifier checks, drag/drop effects, pinned ordering logic, settings persistence, and Sidebar drop behavior are unchanged.

The file-view selection/rubber-band/D&D engine remains in `MainWindow.cs` and was not modified. In particular, the v1.1.2 external drag-and-drop safety path (`CompleteExternalMoveIfRequired` / Shell `Performed DropEffect`) is untouched.

No Recycle Bin behavior, settings behavior, search behavior, refresh reconciliation, paste feedback, FileItem identity, sorting, archive behavior, or keyboard behavior was changed.

## Static result

**PASS**

## Windows validation gate

Before the next refactor batch:

1. Run `Build.cmd` and confirm successful build.
2. Launch `Portable\Ferry.exe`.
3. Confirm ordinary Sidebar navigation still works.
4. Use a folder context menu -> `Pin to Sidebar` and confirm the pin appears.
5. Click a pinned folder and confirm navigation works.
6. Reorder two pinned folders by drag-and-drop and confirm the order changes.
7. Drag an external folder onto the Sidebar and confirm it is pinned.
8. Use the pinned-item context menu -> `Unpin` and confirm removal.

A full file-view selection/rubber-band/external-D&D regression pass is not required for this batch because those methods were not changed.
