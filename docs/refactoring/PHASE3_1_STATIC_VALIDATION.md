# Ferry v1.1.2 Refactoring — Phase 3-1 Static Validation

## Scope

Phase 3-1 establishes the Sidebar partial-class destination without changing runtime behavior.

Changes:

- Added `Source/Ferry/MainWindow.Sidebar.cs`.
- Moved the nested `PinnedSidebarItem` type unchanged from `MainWindow.Types.cs` to `MainWindow.Sidebar.cs`.
- Added `MainWindow.Sidebar.cs` to both `Build.cmd` and `Source/Ferry/Ferry.csproj`.
- `MainWindow.cs` was not modified.

No Sidebar methods, event handlers, D&D behavior, layout behavior, settings behavior, or interaction code were changed in this batch.

## Diff audit

Candidate implementation commit: `79fae6f02198b0c9c0d0b10c2a2afe19525726a8`

Expected four-file diff confirmed:

- `Build.cmd`: +1
- `Source/Ferry/Ferry.csproj`: +1
- `Source/Ferry/MainWindow.Sidebar.cs`: +14
- `Source/Ferry/MainWindow.Types.cs`: -7

The `PinnedSidebarItem` implementation is textually unchanged apart from its physical file location and surrounding partial-class/file wrapper.

`MainWindow.cs` has zero changes in this batch.

## Build-list invariant

The new C# source file is explicitly listed in both build definitions:

- `Build.cmd`
- `Source/Ferry/Ferry.csproj`

The source-set delta is exactly one new C# file, and both explicit source lists receive exactly that same file.

## Behavior-risk review

This batch does not modify executable method bodies, event registration, WPF event phase/order, selection state, rubber-band state, drag-and-drop handling, paste feedback, refresh reconciliation, FileItem identity, sorting, Shell operations, or archive behavior.

The moved type is a two-property private nested data holder used by Sidebar pinned-folder UI.

## Static result

**PASS**

Windows validation remains required before Phase 3-2.

Recommended gate:

1. Run `Build.cmd` and confirm `Portable\Ferry.exe` is produced.
2. Launch Ferry.
3. Confirm Sidebar renders normally.
4. Click a normal Sidebar place (for example Home or Downloads).
5. If pinned folders exist, click one pinned folder.

Full selection/rubber-band/D&D regression testing is not required for this batch because no interaction method changed.
