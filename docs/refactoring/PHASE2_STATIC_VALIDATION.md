# Phase 2 Static Validation — MainWindow UI split

## Scope

Phase 2 physically separates root UI construction from `MainWindow.cs` without intentionally changing runtime behavior.

Moved unchanged into `Source/Ferry/MainWindow.UI.cs`:

- `BuildUi()`
- `ToolbarButton(...)`
- `MakeSquareToolbarButton(...)`
- `ApplyBreadcrumbScrollButtonChrome(...)`
- `ApplyBreadcrumbScrollButtonChromeRecursive(...)`
- `StyleBreadcrumbLineButtonsRecursive(...)`
- `StyleBreadcrumbThumbRecursive(...)`
- `CreateBreadcrumbThumbTemplate()`
- `CreateBreadcrumbLineButtonTemplate(...)`

Sidebar behavior begins with `SidebarSplitterPreviewMouseLeftButtonDown(...)` and remains in `MainWindow.cs` for a later phase.

## Baseline

- Branch: `refactor/v1.1.2`
- Phase 1 Windows-validated baseline: `b561ff3c0d598bdd9f6c6fb44a3fb5e0a82fbf0f`
- Phase 2 code commit: `23d0a03cc3ff0fdd1f664b548aca9b94792a6b12`

## Diff audit

The Phase 2 code commit changes exactly four files:

- `Build.cmd`: +1 line
- `Source/Ferry/Ferry.csproj`: +1 line
- `Source/Ferry/MainWindow.UI.cs`: new file, 321 lines
- `Source/Ferry/MainWindow.cs`: 0 additions / 310 deletions

The zero-addition `MainWindow.cs` diff is important: the existing post-UI implementation was not rewritten as part of the physical split. The removed block is the root UI construction and its toolbar/Breadcrumb visual helpers.

## Build source-list invariant

Phase 1 contained 30 C# source files. Phase 2 adds one C# source file, so the expected count is 31.

`MainWindow.UI.cs` is added to both explicit source lists:

- root `Build.cmd`
- `Source/Ferry/Ferry.csproj`

Expected invariant after Phase 2:

`Source/Ferry/*.cs` = 31 files = `Build.cmd` C# entries = `Ferry.csproj` `<Compile Include>` entries.

## Protected behavior review

No intentional changes were made to:

- constructor lifecycle or event registration order
- bubbling `KeyDown` observer used for RC15 keyboard/Selection Anchor behavior
- selection / rubber-band state machine
- mouse capture ownership
- drag-and-drop logic or external move completion
- `FileItem` identity and incremental refresh reconciliation
- stable unsorted-tail behavior
- Paste feedback selection reconciliation
- tab creation, navigation, Sidebar behavior, search logic, archive logic, or settings behavior

`BuildUi()` retains its original event attachment order and calls into the same existing methods through the partial class.

## Static result

**PASS — static structural validation.**

This is not yet a Windows runtime PASS. The next gate is a Windows 11 `Build.cmd` build and a small smoke test of startup/UI construction, navigation, tabs, List/Grid switching, Location Box/Breadcrumb, and basic selection/background clear.
