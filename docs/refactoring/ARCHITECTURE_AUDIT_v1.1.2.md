# Ferry v1.1.2 Refactoring Architecture Audit

**Baseline:** Ferry v1.1.2 formal release  
**Exact commit:** `a688e59caffca1ac62af86ebc217ef293a137ad0`  
**Refactoring branch:** `refactor/v1.1.2`  
**Purpose:** preserve Ferry v1.1.2 behavior while turning the released codebase into a safer architecture baseline for future Ferry development.

## 1. Scope and non-goals

This refactoring is not a feature release.

### Goals

- Preserve the exact user-visible behavior of v1.1.2.
- Preserve Windows/Shell delegation boundaries already validated on real Windows hardware.
- Reduce the concentration of responsibilities in `MainWindow.cs`.
- Make future changes easier to locate, review, test, and revert.
- Retain the historical knowledge embedded in comments from prototypes/RCs where it documents non-obvious WPF or Shell behavior.
- Introduce structural boundaries incrementally, with a validation gate after each batch.

### Non-goals

- No new features.
- No selection semantics changes.
- No D&D semantics changes.
- No performance redesign, including the known extreme Shift-range cost with very large item counts.
- No MVVM rewrite.
- No replacement of Windows Shell file operations, context menus, properties, Open With, Recycle Bin behavior, or shortcut handling.
- No ZIP-engine redesign.
- No broad async/Dispatcher/event-order cleanup during the structural phase.

## 2. Baseline invariants

The refactor starts from the exact public v1.1.2 release commit. `main` and tag `v1.1.2` point to that release baseline at the time of this audit.

The source contains 29 C# files under `Source/Ferry`. Both `Build.cmd` and `Source/Ferry/Ferry.csproj` explicitly enumerate the C# source files. Any new partial-class file therefore requires an atomic update to both build source lists.

### Build-list invariant

After every structural change:

1. `Source/Ferry/*.cs` set
2. C# files listed in `Build.cmd`
3. `<Compile Include=...>` files in `Ferry.csproj`

must match exactly.

## 3. Current architecture summary

Ferry is not generally an unstructured codebase. Most platform/domain functions already have useful boundaries. The main structural debt is concentrated in `MainWindow.cs`.

### Already-separated domains

- Settings model/persistence: `AppSettings.cs`
- ZIP domain: `ArchiveHelper.cs`, `ArchiveModels.cs`, `ArchiveService.cs`, `ArchiveSetupWindows.cs`
- Clipboard/Shell drop-effect bridge: `ClipboardHelper.cs`
- File item model: `FileItem.cs`
- Sorting: `FileItemComparer.cs`, `NaturalStringComparer.cs`
- Rename domain/UI: `RenameEngine.cs`, `RenameEntry.cs`, `RenameDialog.cs`
- Search engine: `SearchService.cs`
- Recycle Bin: `RecycleBinService.cs`
- Windows Shell services: `ShellInterop.cs`, `ShellContextMenu.cs`, `ShellFileOperations.cs`, `ShellFolderPicker.cs`, `ShortcutHelper.cs`
- Per-tab logical state: `TabState.cs`
- Grid virtualization: `VirtualizingWrapPanel.cs`
- Independent dialogs/settings/startup/logging/helpers: existing dedicated files

### Main structural hotspot

`MainWindow.cs` is approximately 270 KB and owns or coordinates:

- window/UI construction;
- toolbar, Breadcrumb and Location Box;
- sidebar and pinned folders;
- tabs and per-tab visual context creation;
- List/Grid view creation and synchronization;
- selection state and selection reconciliation;
- rubber-band state machine and autoscroll;
- keyboard-current / selection-anchor recovery;
- pointer gesture arbitration and D&D;
- incremental folder loading/refresh and FileSystemWatcher coordination;
- search UI/drain/deferred metadata integration;
- clipboard/paste-result feedback;
- open/rename/create/delete/context-menu operations;
- archive UI orchestration/status;
- settings application and window shutdown persistence.

This concentration is the primary refactoring target.

## 4. Critical behavior couplings

### 4.1 Selection / keyboard / pointer / D&D

This is the highest-risk subsystem.

`MainWindow` contains explicit prototype/RC comments documenting cases where WPF event ordering matters. In particular, the window-level bubbling `KeyDown` observer intentionally runs after native WPF Arrow navigation so Ferry can synchronize its logical Selection Anchor to the actual destination item.

Mouse handling also deliberately arbitrates among:

- file hot zones;
- selected row/tile whitespace;
- unselected whitespace;
- true background;
- view chrome/scrollbars;
- pending rubber-band;
- active rubber-band;
- multi-selection click collapse;
- D&D initiation.

**Structural implication:** in the first phase these methods may move between partial files, but their bodies, event registration, handler phase, call order, modifier tests, mouse capture ownership, and state transitions must remain unchanged.

### 4.2 `TabViewContext` is the current coupling hub

`TabViewContext` stores both ordinary view references and the interaction-engine state, including:

- List/Grid selectors;
- refresh/search timers;
- watcher and thumbnail cancellation;
- Paste feedback session;
- selection reconciliation flag;
- D&D highlight and pending multi-selection state;
- rubber-band overlay and full state machine;
- Selection Anchor;
- keyboard navigation item;
- autoscroll timer/accumulator.

**Structural implication:** do not split this into multiple collaborating classes during the initial safety-first refactor. Keep it as one data holder while MainWindow is physically decomposed. A later architecture phase may reconsider it only after the partial-class split is stable.

### 4.3 Incremental refresh / object identity / stable unsorted tail

`FileItem.UpdateBasicFrom` updates an existing `FileItem` instance instead of replacing it. `TabState` tracks a stable unsorted tail, and `FileItemComparer` deliberately keeps new file-system additions there until explicit sorting.

These behaviors protect selection/focus identity and prevent frequently changing items from moving under the pointer.

**Structural implication:** treat `FileItem`, `TabState`, `FileItemComparer`, incremental refresh, and selection reconciliation as one behavioral preservation boundary. Do not simplify object replacement or sorting semantics in the structural phase.

### 4.4 Paste result feedback

Paste reconciliation selects the actual destination-level result in both List and Grid, then updates Selection Anchor and keyboard-current state. The final session is cleared after the authoritative post-operation reconciliation so later unrelated watcher refreshes cannot resurrect stale selection.

**Structural implication:** this is not merely clipboard code; it is clipboard + refresh + selection logic. Move it only as a whole and do not extract a new abstraction initially.

### 4.5 External D&D completion (v1.1.2)

The v1.1.2 safety contract must remain exact:

- capture the final WPF drag effect;
- inspect Shell `Performed DropEffect` on the same data object;
- source cleanup is permitted only when both indicate Move;
- if source paths are already gone after an optimized move, do not delete again;
- Copy/Link/None/Cancel/absent or non-Move Shell effect must never trigger source cleanup.

`ClipboardHelper.WasUnoptimizedMovePerformed` and `ShellFileOperations.DeleteAfterExternalMove` are therefore protected boundaries during early refactoring.

## 5. File-by-file disposition

| File | Current responsibility | Initial disposition |
|---|---|---|
| `AppSettings.cs` | settings DTO + JSON persistence/normalization | Keep; possible later split only |
| `ArchiveHelper.cs` | ZIP path/name helpers | Keep |
| `ArchiveModels.cs` | ZIP domain models | Keep |
| `ArchiveService.cs` | ZIP compression/extraction/safety engine | Keep/freeze initially |
| `ArchiveSetupWindows.cs` | ZIP setup windows | Keep |
| `AssemblyInfo.cs` | assembly metadata | Keep |
| `ChoiceDialog.cs` | reusable choice dialog | Keep |
| `ClipboardHelper.cs` | clipboard + Shell drop-effect bridge | Freeze initially |
| `FileItem.cs` | mutable displayed file model / identity | Freeze initially |
| `FileItemComparer.cs` | sort policy + stable unsorted tail | Freeze initially |
| `KnownFolders.cs` | known-folder resolution | Keep |
| `Logger.cs` | logging | Keep |
| `MainWindow.cs` | UI/controller + interaction engine + orchestration | Primary refactor target |
| `NaturalStringComparer.cs` | natural name comparison | Keep |
| `Program.cs` | application entry point | Keep |
| `PromptDialog.cs` | reusable prompt dialog | Keep |
| `RenameDialog.cs` | batch rename UI | Keep |
| `RenameEngine.cs` | rename validation/execution/rollback | Keep |
| `RenameEntry.cs` | rename preview model | Keep |
| `RecycleBinService.cs` | recycle-bin enumeration/actions | Keep/freeze initially |
| `SearchService.cs` | indexed/direct recursive search | Keep |
| `SettingsWindow.cs` | settings UI | Keep; possible later cleanup |
| `ShellContextMenu.cs` | native Windows detailed context menu | Freeze initially |
| `ShellFileOperations.cs` | native copy/move/delete operations | Freeze initially |
| `ShellFolderPicker.cs` | native folder picker | Keep |
| `ShellInterop.cs` | icons/thumbnails/open/properties/Open With/etc. | Keep/freeze initially |
| `ShortcutHelper.cs` | `.lnk` resolve/create | Keep |
| `TabState.cs` | per-tab logical state/history/search/unsorted tail | Freeze initially |
| `VirtualizingWrapPanel.cs` | Grid virtualization/scroll semantics | Freeze initially |

## 6. Target MainWindow decomposition

The exact final file count is intentionally not fixed up front. The following is the target responsibility map, to be reached in small batches.

- `MainWindow.cs` — constructor, shared fields, top-level lifecycle only
- `MainWindow.Types.cs` — nested data/helper types, including `TabViewContext`
- `MainWindow.UI.cs` — root UI construction and generic visual helpers
- `MainWindow.Sidebar.cs` — sidebar + pinned-folder UI/ordering
- `MainWindow.Tabs.cs` — tab creation/activation/closing and view-context setup
- `MainWindow.Navigation.cs` — path navigation/history/Breadcrumb/Location Box
- `MainWindow.Selection.cs` — selection snapshots/reconciliation/anchor helpers
- `MainWindow.RubberBand.cs` — rubber-band state machine/geometry/autoscroll
- `MainWindow.Keyboard.cs` — global keyboard dispatch and keyboard recovery
- `MainWindow.DragDrop.cs` — file D&D, hit-testing, drop highlight, external completion
- `MainWindow.Refresh.cs` — folder load/incremental refresh/watcher/metadata/sort bridge
- `MainWindow.Search.cs` — search UI integration/drain/deferred search metadata
- `MainWindow.PasteFeedback.cs` — Paste session capture/result reconciliation
- `MainWindow.FileOperations.cs` — open/create/delete/rename/context operations
- `MainWindow.Archive.cs` — ZIP UI orchestration/status/confirmation
- `MainWindow.Settings.cs` — settings application/column persistence/window closing

This is a destination map, not a requirement to create all files at once.

## 7. Staged refactoring plan

### Phase 0 — Baseline and audit

- Freeze exact v1.1.2 commit.
- Create dedicated refactoring branch from that commit.
- Preserve release documentation/history.
- Record architecture/risk map (this document).

### Phase 1 — Prove the partial-class plumbing with the lowest-risk move

Create `MainWindow.Types.cs` and move only the nested types currently at the end of `MainWindow.cs`:

- `PinnedSidebarItem`
- `InverseBooleanToVisibilityConverter`
- `PasteEntryStamp`
- `PasteFeedbackSession`
- `TabViewContext`

Required mechanical changes only:

- `internal sealed class MainWindow : Window` → `internal sealed partial class MainWindow : Window`
- new file declares the matching `internal sealed partial class MainWindow`
- update `Build.cmd`
- update `Ferry.csproj`

No method bodies, event registration, conditionals, state transitions, or runtime constants change.

**Why first:** it validates the partial-class strategy and build-source-list discipline without moving executable behavior.

### Phase 2 — Low-risk UI physical split

Move `BuildUi` and tightly related toolbar/Breadcrumb visual helper methods into `MainWindow.UI.cs` unchanged.

Gate before proceeding.

### Phase 3 — Independent/medium-risk orchestration splits

Move coherent regions one at a time, likely:

1. Sidebar/pinned UI
2. Archive orchestration/status
3. Search UI integration
4. Navigation/Breadcrumb/Location Box
5. File-operation orchestration/rename bridge
6. Tabs/view switching/settings/status

Each move is behavior-preserving physical relocation first. Cleanup follows only after the new boundary has passed validation.

### Phase 4 — High-risk interaction-engine physical split

Only after lower-risk regions are stable:

1. Selection helpers
2. Rubber-band
3. Keyboard recovery
4. D&D/hit testing
5. Paste feedback
6. Refresh/reconciliation portions tightly coupled to selection identity

Again, first move code unchanged; do not simultaneously rename/rewrite it.

### Phase 5 — Internal cleanup / code beauty

After physical decomposition is stable, consider small semantic-neutral cleanup per domain:

- naming consistency;
- constants for repeated literals;
- duplicate visual-tree helpers;
- one-statement-per-line formatting where it materially improves reviewability;
- reducing overly broad helper scope;
- documenting invariants at module boundaries;
- possible later separation of settings DTO/store or large ArchiveService internals if justified.

Every cleanup remains a separate diff from structural moves.

## 8. Validation gates

### Static gate after every batch

- Build source sets match (`Source/Ferry`, `Build.cmd`, `Ferry.csproj`).
- No source file accidentally omitted/duplicated.
- No version/title/release metadata change unless explicitly intended.
- No generated files/user settings/logs enter source tree.
- For physical-move-only commits, method bodies should be textually unchanged except indentation/containing class braces where unavoidable.
- v1.1.2 external D&D markers remain present.

### Windows gate: low-risk structural batches

Minimum:

- `Build.cmd` succeeds.
- fresh launch succeeds.
- open folder/tab/navigation basic smoke.
- List and Grid render.
- selection basic smoke.

### Windows gate: interaction-engine batches

Use the historical v1.1.0–v1.1.2 regression matrix, including at minimum:

- List/Grid normal rubber-band;
- Ctrl/Shift/Ctrl+Shift selection paths relevant to changed code;
- true-background clear;
- Arrow/Shift+Arrow recovery after background clear;
- Selection Anchor;
- selected-item D&D;
- List/Grid autoscroll;
- List⇄Grid selection preservation;
- Copy/Cut→Paste result feedback and consecutive Paste replacement;
- Ferry→Explorer same-drive Move;
- Ctrl+D&D Copy safety;
- Cancel/invalid-drop safety;
- Ferry→Ferry and Explorer→Ferry regression.

Cross-volume Ferry→Explorer D&D remains explicitly **unverified** unless a second volume becomes available; do not silently convert it to PASS.

## 9. Refactoring rules

1. One conceptual change per batch.
2. Physical move before cleanup.
3. Do not combine structural refactor with a bug fix or optimization.
4. Preserve prototype/RC comments when they explain a non-obvious behavior invariant.
5. Prefer a small reversible commit over a large elegant rewrite.
6. Treat Windows/WPF/Shell event and object-identity behavior as part of the public implementation contract.
7. `main` and tag `v1.1.2` remain untouched while the refactoring branch is being validated.
8. A failed Windows regression means revert/repair the current batch before continuing; do not stack more refactors on top.

## 10. First implementation recommendation

Proceed with **Phase 1 only**: establish `partial MainWindow` and move nested types to `MainWindow.Types.cs` with no executable-behavior changes.

This provides the smallest meaningful proof that the refactoring strategy, source enumeration, compiler path, and repository workflow are sound before touching any of Ferry's interaction logic.
