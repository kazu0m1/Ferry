# Ferry v1.1.2 Refactoring — Phase 1 Static Validation

**Phase:** 1 — establish partial `MainWindow` plumbing  
**Behavioral baseline:** Ferry v1.1.2 release commit `a688e59caffca1ac62af86ebc217ef293a137ad0`  
**Implementation commit:** `1b0a03d7caf7562bfd4c5831f9746be1498b44fd`  
**Branch:** `refactor/v1.1.2`

## Intended change

Phase 1 is a behavior-preserving structural change only:

- change `MainWindow` to a `partial` class;
- move nested helper/data types from the end of `MainWindow.cs` to `MainWindow.Types.cs`;
- add the new source file to `Build.cmd`;
- add the new source file to `Ferry.csproj`.

Moved types:

- `PinnedSidebarItem`
- `InverseBooleanToVisibilityConverter`
- `PasteEntryStamp`
- `PasteFeedbackSession`
- `TabViewContext`

No executable method is intentionally moved or edited in this phase.

## Git diff audit

Comparison of the Phase 0 audit commit and Phase 1 implementation commit shows exactly four changed paths:

1. `Build.cmd` — one source-list entry added.
2. `Source/Ferry/Ferry.csproj` — one `<Compile>` entry added.
3. `Source/Ferry/MainWindow.Types.cs` — new partial-class source containing the five moved nested types.
4. `Source/Ferry/MainWindow.cs` — `partial` added to the class declaration and the five nested types removed from the end of the file.

`MainWindow.cs` method bodies are unchanged by the Git patch. No event registration, condition, Dispatcher usage, selection logic, D&D logic, refresh logic, Paste logic, or runtime constant appears in the Phase 1 patch.

## Source-list invariant

The v1.1.2 baseline contained 29 C# source files.

Phase 1 adds exactly one C# source file:

- `Source/Ferry/MainWindow.Types.cs`

Therefore the Phase 1 source set contains **30 C# files**.

`Build.cmd` adds exactly the same file and therefore references **30 C# files**.

`Ferry.csproj` adds exactly the same file and therefore references **30 C# files**.

Result: **PASS — source / Build.cmd / Ferry.csproj sets remain aligned for the Phase 1 delta.**

## Protected behavior review

Because the Git patch changes only the class declaration, nested-type location, and build lists, the following v1.1.2 implementation areas remain textually untouched:

- Selection and Selection Anchor logic;
- rubber-band state machine and autoscroll;
- keyboard recovery and WPF event-order handling;
- List/Grid selection synchronization;
- incremental refresh and `FileItem` identity preservation;
- stable unsorted-tail sorting behavior;
- Paste-result reconciliation;
- Ferry↔Ferry and Explorer→Ferry D&D;
- Ferry→Explorer external D&D completion logic;
- `ClipboardHelper.WasUnoptimizedMovePerformed`;
- `ShellFileOperations.DeleteAfterExternalMove`;
- ZIP, Search, Rename, Shell and Recycle Bin services.

Result: **PASS — no protected implementation area is part of the Phase 1 diff.**

## Metadata / repository hygiene

Phase 1 does not change:

- application title;
- assembly/file/informational version;
- app manifest version;
- portable release version;
- release notes / changelog / README release pointers;
- v1.1.2 tag;
- `main`.

No generated binary, user settings, log, `bin`, `obj`, `.vs`, or `dist` artifact is introduced by the Phase 1 commit.

Result: **PASS.**

## Static validation result

**PASS — Phase 1 is structurally consistent and its Git diff is behavior-neutral by inspection.**

## Windows validation still required

The connected environment cannot execute Ferry's Windows/.NET Framework 4.8 WPF runtime path. Before Phase 2, validate the Phase 1 branch on Windows:

1. Switch GitHub Desktop to branch `refactor/v1.1.2` and pull/fetch the latest branch state.
2. Run `Build.cmd` and confirm `Portable\Ferry.exe` builds successfully.
3. Launch Ferry.
4. Basic smoke:
   - initial folder displays;
   - open a folder;
   - open/close a tab;
   - switch List ⇄ Grid;
   - select an item and clear selection on true background.

No exhaustive interaction-engine regression is required for this phase because no executable interaction method changed. If the build or any smoke item fails, stop before Phase 2 and repair/revert Phase 1 first.
