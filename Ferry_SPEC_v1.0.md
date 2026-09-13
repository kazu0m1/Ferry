# Ferry v1.0 Specification

**Project:** Ferry  
**Target OS:** Windows 11  
**Specification revision:** 1.0.24  
**Implementation baseline:** Ferry v1.0.2  
**Document status:** Ferry v1.0.2 release baseline; living v1.0 requirements document  
**UI language:** English only (v1.0)  
**Primary distribution:** Portable  
**Implementation direction:** Windows-native file browser using Windows Shell / OS capabilities wherever practical; current implementation assumption is C# + WPF with Windows Shell APIs.

---

## 1. Product concept

Ferry is a lightweight file browser for Windows 11 inspired by selected workflow and usability ideas from GNOME Files (Nautilus).

Ferry does **not** replace `explorer.exe` as the Windows Shell. Windows continues to provide Desktop, Taskbar, Start, Shell infrastructure, file associations, Recycle Bin, thumbnail/cache infrastructure, permissions, and other OS services.

Ferry replaces the **day-to-day file browsing experience** for the user.

### 1.1 Core value

The three highest-value Nautilus-inspired capabilities are:

1. **Folder item-count visibility**
2. **Fast and easy recursive filename search**
3. **Easy but capable bulk rename**

### 1.2 Resource philosophy

Ferry shall not behave as if its own performance justifies unlimited resource consumption.

- Reuse Windows-managed caches and indexes where practical.
- Do not create a large Ferry-specific search index.
- Do not create a large persistent Ferry-specific cache.
- Prefer bounded, cancellable background work.
- Prefer predictable behavior over automatic per-folder optimization.
- Keep advanced/specialized tasks available through Windows Explorer or specialist applications.

---

## 2. Requirement classification

| Class | Meaning |
|---|---|
| **A — Ferry Implementation** | Ferry owns the UI and/or application logic. |
| **B — Windows Delegation** | Windows already provides the mature implementation; Ferry delegates to it. |
| **A+B** | Ferry provides the interaction/UI while Windows performs the underlying OS operation. |
| **C — Out of Scope** | Ferry intentionally does not implement the capability in v1.0. |

Priority:

- **Must** — required for Ferry v1.0.
- **Should** — desirable for v1.0 if implementation remains proportionate and reliable.
- **—** — not implemented in v1.0.

---

# 3. Functional requirements

## F01 — Application / Shell

| ID | Requirement | Class | Priority |
|---|---|---:|---:|
| FR-0101 | Ferry shall run as a standalone `Ferry.exe`. | A | Must |
| FR-0102 | Normal application launch shall open the configured Home folder. | A | Must |
| FR-0103 | Ferry shall accept a folder path at launch and open that location. | A | Must |
| FR-0104 | Ferry shall support multiple windows. | A | Must |
| FR-0105 | Multiple Ferry windows should normally be managed within one Ferry process where practical. | A | Should |
| FR-0106 | Ferry shall **not** replace `explorer.exe` as the Windows Shell. | C | — |
| FR-0107 | Ferry shall coexist with Windows Explorer. | B | Must |
| FR-0108 | Ferry shall provide a Portable distribution. | A | Must |
| FR-0109 | A conventional installer is not required for v1.0. Future installer distribution is not prohibited. | C | — |
| FR-0110 | Ferry shall not auto-start with Windows by itself. | C | — |
| FR-0111 | Ferry shall not use a tray-resident background process. | C | — |
| FR-0112 | Ferry shall not install a background service. | C | — |
| FR-0113 | Windows shall provide an **Open with Ferry** entry for folders after optional Ferry Shell registration. | A+B | Must |
| FR-0114 | Ferry shall not attempt to redirect every Windows folder-open action to Ferry automatically. | C | — |
| FR-0115 | Ferry shall support **Open in New Ferry Window**. | A | Must |
| FR-0116 | Ferry shall support command-line path arguments. | A | Must |
| FR-0117 | Ferry shall not implement an automatic update service in v1.0. | C | — |
| FR-0118 | Portable settings and Ferry-owned transient data shall be kept within the Ferry application structure where practical. | A | Must |
| FR-0119 | Ferry shall provide explicit per-user Shell registration and unregistration for **Open with Ferry** without requiring an installer. | A+B | Must |

### Tabs

Tabs are included because they provide high user value without requiring a heavyweight subsystem.

| ID | Requirement | Class | Priority |
|---|---|---:|---:|
| FR-0120 | Ferry shall support tabs. | A | Must |
| FR-0121 | A new tab shall be creatable from the UI and by `Ctrl+T`; a generic new tab opens Home. | A | Must |
| FR-0122 | The current tab shall close by UI action and `Ctrl+W`. | A | Must |
| FR-0123 | Keyboard tab switching shall be supported (`Ctrl+Tab` / `Ctrl+Shift+Tab`; `Ctrl+PageUp` / `Ctrl+PageDown` may also be supported). | A | Must |
| FR-0124 | Each tab shall own its own current path, back/forward navigation history, selection, and active search session. | A | Must |
| FR-0125 | A selected folder shall support **Open in New Tab**. | A | Must |
| FR-0126 | Tabs should be reorderable by drag and drop. | A | Should |
| FR-0127 | Closed-tab history / reopen-closed-tab is not required. | C | — |
| FR-0128 | Tabs shall not be restored across application restarts in v1.0. | C | — |

---

## F02 — Navigation

| ID | Requirement | Class | Priority |
|---|---|---:|---:|
| FR-0201 | Back navigation. | A | Must |
| FR-0202 | Forward navigation. | A | Must |
| FR-0203 | Navigate to parent folder. | A | Must |
| FR-0204 | Breadcrumb-style path bar. | A | Must |
| FR-0205 | Clicking a breadcrumb segment shall navigate directly to that level. | A | Must |
| FR-0206 | Direct path entry shall be available. | A | Must |
| FR-0207 | `Ctrl+L` shall activate direct location entry. | A | Must |
| FR-0208 | Back/forward history shall be maintained for the current session/tab. | A | Must |
| FR-0209 | Home navigation shall be available. | A | Must |
| FR-0210 | Path autocompletion should be provided. | A | Should |
| FR-0211 | Windows environment-variable expansion may be delegated to Windows. | B | Should |
| FR-0212 | UNC paths shall be supported where Windows can access them. | A+B | Should |
| FR-0213 | Mapped network drives shall be supported where Windows can access them. | A+B | Should |
| FR-0214 | Junction/symbolic-link path resolution shall use Windows behavior. | B | Must |
| FR-0215 | Navigation history shall not be persisted across application sessions. | C | — |
| FR-0216 | Ferry shall not automatically learn or predict frequently visited folders. | C | — |
| FR-0217 | Web URL navigation is out of scope. | C | — |
| FR-0218 | FTP/SFTP URI navigation is out of scope. | C | — |
| FR-0219 | Linux-style `~` path expansion is out of scope. | C | — |
| FR-0220 | Persistent path-entry history suggestions are out of scope. | C | — |
| FR-0221 | `Alt+Left`, `Alt+Right`, and `Alt+Up` navigation shortcuts shall be supported. | A | Must |
| FR-0222 | `F5` shall manually refresh the current view. | A | Must |

---

## F03 — Sidebar / Locations

| ID | Requirement | Class | Priority |
|---|---|---:|---:|
| FR-0301 | Ferry shall provide a sidebar. | A | Must |
| FR-0302 | Sidebar shall contain Home. | A | Must |
| FR-0303 | Desktop location. | A+B | Must |
| FR-0304 | Documents location. | A+B | Must |
| FR-0305 | Downloads location. | A+B | Must |
| FR-0306 | Pictures location. | A+B | Must |
| FR-0307 | Music location. | A+B | Should |
| FR-0308 | Videos location. | A+B | Should |
| FR-0309 | Local drives shall be listed. | A+B | Must |
| FR-0310 | Removable drives shall be listed. | A+B | Must |
| FR-0311 | Windows-mounted network drives should be listed. | A+B | Should |
| FR-0312 | Arbitrary folders shall be pinnable. | A | Must |
| FR-0313 | Pinned folders shall be removable. | A | Must |
| FR-0314 | Pinned-folder order should be manually reorderable by drag and drop, with the resulting order persisted. | A | Should |
| FR-0315 | Recent-folders history is out of scope. | C | — |
| FR-0316 | Automatic frequently-used-folder inference is out of scope. | C | — |
| FR-0317 | OneDrive shall not receive a dedicated Ferry sidebar subsystem. | C | — |
| FR-0318 | Google Drive and other cloud products shall not receive dedicated Ferry integrations. | C | — |
| FR-0319 | FTP/SFTP locations are out of scope. | C | — |
| FR-0320 | Recycle Bin shall appear in the sidebar and open inside Ferry as a virtual view of the current user's Windows Recycle Bin. Ferry shall use the Windows-managed Recycle Bin storage/operations rather than a separate Ferry bin. | A+B | Must |
| FR-0321 | A Windows Explorer-style **This PC** virtual page is out of scope. | C | — |
| FR-0322 | Sidebar should be hideable/collapsible. | A | Should |
| FR-0323 | Per-section sidebar collapse is out of scope. | C | — |
| FR-0324 | Sidebar width should be adjustable. | A | Should |
| FR-0325 | Location/drive icons shall use Windows-provided icons where practical. | B | Must |
| FR-0326 | Home shall be configurable to an arbitrary folder. | A | Must |
| FR-0327 | On first run, the default Home shall be the current user's **profile folder (`%USERPROFILE%`)**. | A+B | Must |
| FR-0328 | Pinned-folder configuration shall be persisted. | A | Must |
| FR-0329 | Dragging a folder onto the Sidebar shall pin that folder. | A | Must |
| FR-0330 | The Recycle Bin sidebar context menu shall include **Empty Recycle Bin** and delegate confirmation/execution to Windows. | A+B | Must |
| FR-0331 | Recycle Bin item context menus shall provide **Restore**, **Delete Permanently**, and **Open Original Location** when the original location is available. | A+B | Must |
| FR-0332 | In the Recycle Bin List view, Ferry shall expose **Original Location** and use **Date Deleted** in place of the normal Modified meaning. | A+B | Must |
| FR-0333 | Ferry Search and normal internal drag/drop file operations shall be disabled while the current tab is showing the Recycle Bin virtual view. | A | Must |

---

## F04 — File View

| ID | Requirement | Class | Priority |
|---|---|---:|---:|
| FR-0401 | Grid view. | A | Must |
| FR-0402 | List view. | A | Must |
| FR-0403 | Folder/file names shall be displayed. | A | Must |
| FR-0404 | In Grid view, a folder shall display its direct-child item count below the folder name. | A | Must |
| FR-0405 | Item count shall count direct children only, not descendants recursively. | A | Must |
| FR-0406 | Folder item counting shall be asynchronous. | A | Must |
| FR-0407 | List view shall use lightweight Windows Shell file-type icons; it shall not require content-thumbnail retrieval for ordinary List rows. | B | Must |
| FR-0408 | Folder icons shall use Windows Shell resources. | B | Must |
| FR-0409 | Grid thumbnails shall use Windows thumbnail infrastructure/cache where available and may be loaded progressively when Grid view is active. | B | Must |
| FR-0410 | Grid icon size should be adjustable. | A | Should |
| FR-0411 | File names shall be shown. | A | Must |
| FR-0412 | File extensions shall be shown; Ferry shall not hide them automatically. | A | Must |
| FR-0413 | Hidden-item display shall be toggleable. | A+B | Should |
| FR-0414 | A dedicated system-file display mode is out of scope; use Explorer for such work. | C | — |
| FR-0415 | Folder-first grouping shall follow the global **Sort folders before files** preference. Factory default is ON; when OFF, Name/Modified/Created/Type may intermix folders/files/shortcuts according to the selected key. | A | Must |
| FR-0416 | An empty folder shall display `0 items`. | A | Must |
| FR-0417 | Item count shall have a non-blocking loading state such as `…`. | A | Must |
| FR-0418 | Item-count failure shall not block the view; inaccessible values shall display a neutral unavailable state. | A | Must |
| FR-0419 | Recursive folder-size calculation is out of scope. | C | — |
| FR-0420 | Preview pane is out of scope. | C | — |
| FR-0421 | Ferry-specific details pane is out of scope. | C | — |
| FR-0422 | Complex multi-level zoom behavior is out of scope. | C | — |
| FR-0423 | Ferry shall not remember the last temporary Grid/List mode as an implicit preference. | C | — |
| FR-0424 | Per-folder view settings are out of scope. | C | — |
| FR-0425 | The current folder view shall refresh automatically from Windows file-system/Shell notifications where practical. | A+B | Must |
| FR-0426 | List view shall expose an **Items** column for folder item counts. | A | Must |
| FR-0427 | List column widths shall be globally consistent across folders. | A | Must |
| FR-0428 | Automatic per-folder column-width changes are out of scope. | C | — |
| FR-0429 | Item count shall match the current visibility rule: hidden entries are excluded when hidden items are OFF and included when hidden items are ON. | A | Must |
| FR-0430 | Incremental refresh shall preserve the existing `FileItem` identity for a path that still exists. Metadata/size/time changes shall update that item in place rather than replace it solely because mutable file metadata changed. | A | Must |
| FR-0431 | A file that is actively changing (for example a browser `.crdownload`) shall remain selectable and, subject to Windows file-lock rules, usable as a normal Ferry selection/D&D/rename target. Background refresh shall not repeatedly replace its UI item and steal focus. | A+B | Must |
| FR-0432 | User-requested F5 and ordinary same-path refresh/sort should preserve selection for items whose `FileItem` identity remains valid. Ferry shall prefer stable same-path item identity over unnecessary object replacement. | A | Should |
| FR-0433 | External rename/replace events shall refresh safely without crashing Ferry. Preserving selection across an external rename is not required for v1.0. | A+B | Should |
| FR-0434 | Items newly detected by ordinary background folder refresh shall appear in a stable unsorted tail at the bottom of the current folder view instead of forcing an immediate whole-view re-sort. | A | Must |
| FR-0435 | Size/time/metadata changes to an existing same-path item shall update that item in place without automatically reapplying the current sort. The view shall be re-sorted when the user explicitly requests F5/Refresh, clicks a sort column, or changes settings in a way that reloads/sorts the folder. | A+B | Must |

### Item-count UI principle

Ferry shall render the folder/file list first and fill item-count values as they become available. It shall not block initial view rendering until all counts are complete.

---

## F05 — Selection / Interaction

For the public v1.0 stabilization line, Ferry intentionally uses the proven native WPF `SelectionMode.Extended` behavior from internal build v1.0.7.2 and does **not** implement a custom selection state machine.

| ID | Requirement | Class | Priority |
|---|---|---:|---:|
| FR-0501 | Left click selects one item using native ListView/ListBox selection behavior. | A | Must |
| FR-0502 | `Ctrl+Click` supports non-contiguous multiple selection. | A | Must |
| FR-0503 | `Shift+Click` supports contiguous range selection. For v1.0 this is the supported mouse/keyboard range-selection method. | A | Must |
| FR-0504 | `Ctrl+A` selects all visible items. | A | Must |
| FR-0505 | Clicking true empty view space clears the current native selection. | A | Must |
| FR-0506 | Double-click opens a folder/file; actual file launch uses Windows association. | A+B | Must |
| FR-0507 | `Enter` opens all selected items. For multiple selected files, every selected file is launched using its Windows association. For multiple selected folders, the focused/current selected folder opens in the current Ferry tab and the other selected folders open in additional Ferry tabs. Mixed file/folder selections apply both rules. | A+B | Must |
| FR-0508 | Backspace navigation is intentionally unassigned. | C | — |
| FR-0509 | `Delete` sends selected items to Windows Recycle Bin. | A+B | Must |
| FR-0510 | `Shift+Delete` may permanently delete using Windows behavior. | A+B | Should |
| FR-0511 | `F2` invokes rename. | A | Must |
| FR-0512 | `Ctrl+C` Copy. | A+B | Must |
| FR-0513 | `Ctrl+X` Cut. | A+B | Must |
| FR-0514 | `Ctrl+V` Paste. | A+B | Must |
| FR-0515 | In v1.0, `Ctrl+Z` shall undo the last successful Ferry rename in the current session. Any subsequent mutating file operation invalidates that rename-undo record. General Move/Copy/Delete Undo is out of scope. | A | Must |
| FR-0516 | General Redo is out of scope for v1.0. | C | — |
| FR-0517 | Drag and drop shall work inside Ferry using native Extended selection as the source of truth. When multiple items are already selected, dragging any selected item shall preserve the full selection and place all selected paths into the D&D payload. A normal click without dragging may still collapse the selection to the clicked item on mouse-up. | A+B | Must |
| FR-0518 | Drag and drop shall interoperate with external Windows applications where practical. | A+B | Must |
| FR-0518A | Dropping an item back into its existing parent folder shall be treated as a no-op rather than creating a same-folder copy. | A | Must |
| FR-0519 | Rubber-band / marquee rectangle selection is **deferred and out of scope for public v1.0**. Dragging true empty background shall not begin a Ferry selection rectangle. Use `Shift+Click` for range selection and `Ctrl+Click` for non-contiguous selection. | C | — |
| FR-0520 | Normal right-click shall show a **small Ferry context menu**, not an attempted clone of the Windows 11 simplified Explorer menu. | A | Must |
| FR-0521 | Middle-clicking a folder shall open it in a new Ferry tab. | A | Must |
| FR-0522 | Space-bar preview is out of scope. | C | — |
| FR-0523 | Single-click-to-open mode is out of scope. | C | — |
| FR-0524 | Checkbox selection mode is out of scope. | C | — |
| FR-0525 | `Shift+Right-click` shall directly open the Windows Shell detailed context menu where supported. | A+B | Must |
| FR-0526 | Arrow keys, Home, End, PageUp and PageDown use normal WPF keyboard movement/selection behavior. | A | Must |
| FR-0527 | The final item in the normal Ferry context menu shall be **Show more options**, opening the Windows Shell detailed context menu. | A+B | Must |
| FR-0528 | Folder context menus shall include **Open in New Tab**. | A | Must |
| FR-0529 | When Search is not active, `Esc` shall clear the current file/folder selection. | A | Must |
| FR-0530 | During an actionable file/folder drag over a destination folder in List or Grid view, Ferry shall visually highlight that destination folder; the highlight shall follow the current drop target and clear on leave/drop. | A | Must |

### Normal Ferry context-menu concept

Typical item menu:

- Open
- Open with…
- Open in New Tab (folder)
- Open in New Ferry Window (folder)
- Cut
- Copy
- Rename
- Delete
- Copy Path
- Open Terminal Here
- Open in Explorer
- Properties
- **Show more options** ← always last after a separator

The exact set may vary by selection/context, but **Show more options** remains the escape hatch to Windows Shell extensions.

---

## F06 — File Operations

| ID | Requirement | Class | Priority |
|---|---|---:|---:|
| FR-0601 | Copy. | A+B | Must |
| FR-0602 | Move. | A+B | Must |
| FR-0603 | Paste. | A+B | Must |
| FR-0604 | Delete to Windows Recycle Bin. | A+B | Must |
| FR-0605 | Permanent delete. | A+B | Should |
| FR-0606 | New Folder. | A+B | Must |
| FR-0607 | Generic Ferry-created empty-file command is out of scope. | C | — |
| FR-0608 | Duplicate in the same location should be supported using Windows copy behavior. | A+B | Should |
| FR-0609 | Drag/drop copy. | A+B | Must |
| FR-0610 | Drag/drop move. | A+B | Must |
| FR-0611 | Copy/move progress shall use Windows-provided behavior where practical. | B | Must |
| FR-0612 | Name-conflict handling (replace/skip/etc.) shall be delegated to Windows. | B | Must |
| FR-0613 | Permissions/UAC behavior shall be delegated to Windows. | B | Must |
| FR-0614 | Long-path behavior shall follow supported Windows APIs rather than Ferry-specific path restrictions. | B | Must |
| FR-0615 | Ferry v1.0 shall delegate file operations to Windows but shall not expose a general Ferry operation-undo stack. The only in-app `Ctrl+Z` behavior in v1.0 is the session-local last-Rename undo defined by FR-0515 / FR-0717. | A+B | Must |
| FR-0616 | General file-operation Redo is out of scope for v1.0. | C | — |
| FR-0617 | Successful file operations shall not generate completion pop-up message boxes. | C | — |
| FR-0618 | Actionable operation errors shall be shown minimally and clearly. | A+B | Must |
| FR-0619 | Persistent file-operation logging is out of scope. | C | — |
| FR-0620 | Ferry shall not implement an independent high-performance copy engine. | C | — |
| FR-0621 | A Ferry-specific copy queue manager is out of scope. | C | — |
| FR-0622 | Copy-speed graphs are out of scope. | C | — |
| FR-0623 | Pause/resume may be exposed only where the underlying Windows operation provides it. | B | Should |
| FR-0624 | In-progress multi-file operations shall be cancellable where the Windows operation supports cancellation. | B | Must |
| FR-0625 | All normal operations shall accept multi-selection where applicable. | A+B | Must |
| FR-0626 | The background context menu should expose Windows-supported **New** creation choices without Ferry maintaining file-type templates itself. | A+B | Should |
| FR-0627 | **Create Shortcut** shall create a standard Windows `.lnk` shortcut using Windows Shell functionality. | A+B | Must |
| FR-0628 | Windows **Send to** shall remain available through the detailed Shell context menu. | B | Should |
| FR-0629 | Collision/duplicate-name behavior shall use Windows-standard resolution where applicable. | B | Must |

---

## F07 — Rename

Ferry integrates the useful behavior of NautilusRenamer v1.1 rather than launching a separate utility.

| ID | Requirement | Class | Priority |
|---|---|---:|---:|
| FR-0701 | Single-item file/folder rename shall use inline editing in the active List/Grid item rather than a separate rename dialog. | A+B | Must |
| FR-0702 | `F2` starts single-item rename. | A | Must |
| FR-0703 | Multiple selected items shall open the Ferry bulk-rename UI. | A | Must |
| FR-0704 | Find and Replace rename mode. | A | Must |
| FR-0705 | Numbering/template rename mode. | A | Must |
| FR-0706 | Starting number shall be configurable. | A | Must |
| FR-0707 | Number width shall be selectable (`1`, `01`, `001`, etc.; width is minimum width, not a maximum). | A | Must |
| FR-0708 | Numbering token position shall be controlled by the template, e.g. `[01, 02, 03]_[Original filename]`. | A | Must |
| FR-0709 | Live rename preview. | A | Must |
| FR-0710 | Preview shall show Current Name → New Name. | A | Must |
| FR-0711 | Duplicate targets and collisions with unrelated existing files/folders shall be detected before execution. | A+B | Must |
| FR-0712 | Windows-invalid/reserved target names shall be rejected before execution. | A+B | Must |
| FR-0713 | **Bulk rename** shall preserve file extensions automatically and exclude them from Find/Replace/template name editing. | A | Must |
| FR-0714 | **Single-item rename** shall allow editing the complete filename including its extension. For files, the initial text selection shall cover the filename stem only, leaving the extension visible but unselected by default. | A | Must |
| FR-0715 | Successful rename shall not display a completion message box. | C | — |
| FR-0716 | Ferry shall not show a post-operation “Undo available” message box. | C | — |
| FR-0717 | Ferry shall keep only the last successful Rename undo record in memory for the current session. Any later mutating file operation shall invalidate it. Persistent rename history and a general file-operation undo stack are out of scope for v1.0. | A | Must |
| FR-0718 | Persistent rename history is out of scope. | C | — |
| FR-0719 | Regular-expression rename is out of scope. | C | — |
| FR-0720 | Automatic upper/lower-case conversion features are out of scope. | C | — |
| FR-0723 | Bulk numbering order shall follow the **current visible Ferry sort order** of the selected items. | A | Must |
| FR-0724 | The rename preview list should allow straightforward reordering only if it can be implemented without obscuring the visible-order rule. | A | Should |
| FR-0725 | Folders as well as files shall be valid bulk-rename targets. | A+B | Must |
| FR-0726 | User-entered letter case shall be preserved as entered; Ferry shall not normalize it. | A | Must |
| FR-0727 | Bulk rename execution shall be collision-safe, including swaps/cycles, using a two-phase temporary-name strategy or equivalent safe mechanism. | A | Must |
| FR-0728 | **New Folder** shall create the item in the stable bottom tail, scroll it into view, select it, and immediately enter the same inline-rename interaction used by ordinary single-item rename. | A | Must |

**Note:** Standalone NautilusRenamer v1.1 used natural filename order for deterministic numbering when launched externally. Inside Ferry, FR-0723 supersedes that behavior because Ferry has an explicit visible sort order.

---

## F08 — Sort / Filter / Search

### Sorting

| ID | Requirement | Class | Priority |
|---|---|---:|---:|
| FR-0801 | Sort by Name. | A | Must |
| FR-0802 | Sort by Modified date/time. | A | Must |
| FR-0803 | Sort by Size. | A | Must |
| FR-0804 | Sort by Type. | A | Must |
| FR-0805 | Sort folders by Items count. | A | Must |
| FR-0806 | Ascending / descending toggle. | A | Must |
| FR-0807 | Folder-before-file grouping shall be user-configurable by **Sort folders before files**. Factory default is **ON**. | A | Must |
| FR-0808 | Sort configuration shall be global, not per-folder. | A | Must |
| FR-0809 | Per-folder sort memory is out of scope. | C | — |
| FR-0810 | Lightweight current-view name filtering may be provided in addition to Search. | A | Should |
| FR-0811 | Dedicated Type filter UI is out of scope. | C | — |
| FR-0812 | Dedicated Date filter UI is out of scope. | C | — |
| FR-0813 | Dedicated Size filter UI is out of scope. | C | — |

### Search scope and engine

| ID | Requirement | Class | Priority |
|---|---|---:|---:|
| FR-0814 | Search starts from the currently viewed folder. | A | Must |
| FR-0815 | Search recursively includes subfolders. | A | Must |
| FR-0816 | Windows Search Index shall be used when it can provide useful indexed results. | B | Must |
| FR-0817 | Ferry shall not build a Ferry-specific search index. | C | — |
| FR-0818 | Whole-PC specialist search is out of scope; use tools such as Everything when that capability is required. | C | — |
| FR-0819 | Persistent search-history storage is out of scope. | C | — |
| FR-0820 | Regular-expression search is out of scope. | C | — |
| FR-0821 | `*` and `?` wildcard filename search shall be supported. | A+B | Must |
| FR-0822 | Search results shall support the normal Ferry sorting rules and default to the current global Ferry sort setting. | A | Must |
| FR-0823 | Normal file operations shall be possible directly from search results. | A+B | Must |
| FR-0824 | Search results shall support **Open File Location**. | A | Must |
| FR-0825 | File-content/full-text search is out of scope. | C | — |
| FR-0826 | If useful index results are unavailable/incomplete, Ferry shall fall back to direct file-system traversal. | A+B | Must |
| FR-0827 | Search results shall be displayed progressively as they become available. | A | Must |
| FR-0828 | Search shall not block the Ferry UI. | A | Must |
| FR-0829 | Obsolete searches shall be cancelled when the query changes, search exits, or navigation moves elsewhere. | A | Must |
| FR-0830 | Search shall update incrementally while the user types. | A | Must |
| FR-0831 | Filename matching shall be case-insensitive. | A | Must |
| FR-0832 | Files and folders shall both be search targets. | A | Must |
| FR-0833 | Ferry search targets names only, not contents. | A | Must |
| FR-0834 | Ferry shall treat ZIP/7z/RAR/etc. as files; archive contents are not recursively searched. | C | — |
| FR-0835 | A Junction/Symbolic Link object may match by name, but its target shall not be recursively traversed by Ferry search. | A+B | Must |
| FR-0836 | Search shall operate on USB/network locations accessible through Windows, with non-blocking behavior. | A+B | Must |

### Search matching rules

| ID | Requirement | Class | Priority |
|---|---|---:|---:|
| FR-0838 | Normal **Contains** / partial-match mode. | A | Must |
| FR-0839 | Multiple normal search terms use AND semantics. | A | Must |
| FR-0840 | In partial-match mode, term order does not matter. | A | Must |
| FR-0841 | A dedicated exact-match UI mode is out of scope. | C | — |
| FR-0842 | Regex search is out of scope. | C | — |
| FR-0843 | Windows AQS syntax shall not be exposed as the normal Ferry user interface. | C | — |
| FR-0844 | Ferry-specific persistent search index is prohibited. | C | — |
| FR-0845 | Ferry-specific large persistent search DB is prohibited. | C | — |
| FR-0846 | Everything integration/dependency is out of scope for Ferry v1.0. | C | — |
| FR-0847 | Search shall still work when Windows Search indexing is disabled/unavailable by using direct traversal. | A+B | Must |
| FR-0848 | Ferry shall not silently change the user's Windows indexing configuration. | C | — |
| FR-0849 | Ferry shall use a hybrid strategy: consume the fastest useful existing Windows result source first and supplement it with direct traversal where needed, deduplicating by canonical path. | A+B | Must |

### Search UX

| ID | Requirement | Class | Priority |
|---|---|---:|---:|
| FR-0850 | `Ctrl+F` starts search. | A | Must |
| FR-0851 | Typing normal text while the file view has focus starts filename search. | A | Must |
| FR-0852 | Search begins without requiring Enter. | A | Must |
| FR-0853 | `Esc` exits search and returns to the normal current-folder view. | A | Must |
| FR-0854 | Search UI shall clearly show the folder/location being searched. | A | Must |
| FR-0855 | Search result count shall be displayed. | A | Must |
| FR-0856 | Search results shall display their containing location/relative path. | A | Must |
| FR-0857 | Ferry shall not impose a small arbitrary result-count cap. | A | Must |
| FR-0858 | Large search result sets shall use virtualized/incremental UI rather than materializing one heavy UI control per result. | A | Must |
| FR-0859 | Access-denied folders shall be skipped without aborting the entire search. | A+B | Must |
| FR-0860 | Results that disappear during search shall be safely removed/ignored. | A | Must |
| FR-0861 | Recursive-search cycle prevention is required. | A | Must |
| FR-0862 | USB/network removal during search shall not crash Ferry. | A+B | Must |
| FR-0863 | Partial search-source failure shall not discard already valid results. | A | Must |
| FR-0864 | Search match mode shall be switchable. | A | Must |
| FR-0865 | **Contains** mode is required. | A | Must |
| FR-0866 | **Starts with** / prefix-match mode is required. | A | Must |
| FR-0867 | Default search match mode is **Contains**. | A | Must |
| FR-0868 | The explicitly selected search match mode should be persisted as a user preference. | A | Should |
| FR-0869 | Explorer-style type-to-select is out of scope because normal typing is reserved for Ferry search. | C | — |
| FR-0870 | Hidden files/folders participate in search only when **Show hidden items** is ON. | A | Must |
| FR-0871 | Search-result default ordering inherits the user's current global Ferry sort setting. | A | Must |
| FR-0872 | When a query contains wildcard syntax, wildcard matching takes precedence over Contains/Starts-with mode. | A | Must |
| FR-0873 | When the Search field contains text, it shall expose a visible **× / Clear** control. Activating it shall cancel the active search, clear the query, and return immediately to the normal current-folder view. | A | Must |

#### Matching semantics

- **Contains:** `manga` matches `manga.pdf`, `old_manga.zip`, `MyManga2026`.
- **Starts with:** `manga` matches `manga.pdf`, `Manga_old.zip`; it does not match `old_manga.zip`.
- In Contains mode, `2026 manga` means both terms must occur somewhere in the name.
- In Starts-with mode, the entered search string is treated as a prefix of the filename/folder name.
- Wildcards such as `*.jpg`, `Manga*.jpg`, `report_??.pdf` use wildcard matching.
- Ordinary text matching shall ignore full-width/half-width character differences (for example `カタカナ` ↔ `ｶﾀｶﾅ`, `ABC` ↔ `ＡＢＣ`) while preserving kana-type distinctions such as hiragana versus katakana.

---

## F09 — Metadata / Properties

| ID | Requirement | Class | Priority |
|---|---|---:|---:|
| FR-0901 | Name metadata. | A | Must |
| FR-0902 | Folder Items count. | A | Must |
| FR-0903 | File size from Windows/file-system metadata. | A+B | Must |
| FR-0904 | Recursive folder-size computation is out of scope. | C | — |
| FR-0905 | File Type shall use Windows/Shell type information where practical. | A+B | Must |
| FR-0906 | Modified date/time. | A+B | Must |
| FR-0907 | Created date/time shall be available as an optional column but hidden by default. | A+B | Should |
| FR-0908 | Access time column is out of scope. | C | — |
| FR-0909 | Basic file attributes may be obtained from Windows where needed. | B | Should |
| FR-0910 | Image-dimension columns are out of scope for Ferry's default list metadata. | C | — |
| FR-0911 | Media-duration columns are out of scope. | C | — |
| FR-0912 | Ferry shall not implement its own EXIF/media metadata parser. | C | — |
| FR-0913 | Windows standard Properties shall be available. | A+B | Must |
| FR-0914 | `Alt+Enter` opens Properties. | A+B | Must |
| FR-0915 | Multi-selection Properties should use Windows behavior where available. | A+B | Should |
| FR-0916 | A separate Ferry-specific Properties implementation is out of scope. | C | — |
| FR-0917 | List columns shall be manually reorderable. | A | Must |
| FR-0918 | Columns shall be manually showable/hideable. | A | Should |
| FR-0919 | Column configuration is global across all folders. | A | Must |
| FR-0920 | Per-folder column configurations are out of scope. | C | — |
| FR-0921 | Slow/unknown metadata shall be populated asynchronously without blocking the file view. | A | Must |

### Default List columns

1. Name
2. Items
3. Type
4. Size
5. Modified

Created is available but initially hidden.

For folders, `Items` contains the direct-child count and `Size` is unavailable (`—`).  
For files, `Items` is unavailable (`—`) and `Size` contains the file size.

---

## F10 — Windows Integration

| ID | Requirement | Class | Priority |
|---|---|---:|---:|
| FR-1001 | Open a file using its Windows default application. | B | Must |
| FR-1002 | **Open with…** shall use Windows application association mechanisms. | A+B | Must |
| FR-1003 | Windows file icons. | B | Must |
| FR-1004 | Windows folder icons. | B | Must |
| FR-1005 | Windows thumbnail infrastructure/cache. | B | Must |
| FR-1006 | Windows Shell detailed context menu shall remain available through **Show more options** / `Shift+Right-click`. | B | Must |
| FR-1007 | `Shift+Right-click` directly invokes the detailed Shell menu. | A+B | Must |
| FR-1008 | Windows standard Properties. | B | Must |
| FR-1009 | Windows Recycle Bin. | B | Must |
| FR-1010 | Windows Clipboard. | B | Must |
| FR-1011 | Windows-compatible drag and drop. | A+B | Must |
| FR-1012 | Ferry shall not manage global Windows file-association changes. | C | — |
| FR-1013 | UAC/elevation shall use Windows mechanisms. | B | Must |
| FR-1014 | Dedicated NTFS ACL editor is out of scope; use Windows Properties/Explorer. | C | — |
| FR-1015 | Windows `.lnk` shortcuts. | B | Must |
| FR-1016 | Junction/Symbolic Link interpretation follows Windows. | B | Must |
| FR-1017 | OneDrive Files On-Demand behavior is delegated to Windows. | B | Must |
| FR-1018 | Ferry-specific OneDrive UI is out of scope. | C | — |
| FR-1019 | SMB/network sharing access is delegated to Windows. | B | Must |
| FR-1020 | Windows Search services/index may be used by Ferry search. | B | Must |
| FR-1021 | Windows file-system/Shell notifications shall drive automatic refresh where practical. | B | Must |
| FR-1022 | Windows **Send to** remains available via detailed context menu. | B | Should |
| FR-1023 | Windows Share UI integration is out of scope. | C | — |
| FR-1024 | ZIP compression/extraction shall be available from Ferry's lightweight context menu. Ferry shall own ZIP workflow/control while using .NET `System.IO.Compression` for ZIP container/compression support; ZIP work shall not depend on an external Windows archive command-line tool. | A | Must |
| FR-1025 | **Copy Path** shall be available. | A | Must |
| FR-1026 | **Open Terminal Here** shall launch the auto-selected or configured external terminal with the current folder as working directory. | A | Should |
| FR-1027 | **Open in Explorer** shall open the current/selected location in Windows Explorer. | A+B | Must |
| FR-1028 | Double-clicking a Windows `.lnk` whose resolved target is a folder shall navigate the **current Ferry tab** to that target instead of opening a new Explorer window. | A+B | Must |
| FR-1029 | **Compress to ZIP** shall be available for one or more selected files/folders. Ferry shall show a setup window with the selected source set and a changeable destination ZIP path before Start. | A | Must |
| FR-1030 | For a single `.zip` selection, Ferry shall provide **Extract Here** and **Extract to `<archive-name>\`**. Ferry shall show the source ZIP and changeable destination folder before Start. After Start, the setup window shall close and extraction shall run without blocking normal Ferry browsing. | A | Must |
| FR-1031 | `F12` shall invoke **Open Terminal Here** for the current Ferry folder regardless of item selection. The shortcut is not applicable to the virtual Recycle Bin view. | A | Must |
| FR-1032 | While ZIP compression or extraction is running, Ferry shall show one neutral-gray **overall determinate** progress bar in the bottom status area, plus processed/total data, file count, speed, ETA when available, and Cancel. Per-item progress bars are intentionally omitted. Archive progress shall remain visible across folder/tab navigation. | A | Must |
| FR-1033 | Normal Ferry navigation, tabs, search, and file browsing shall remain usable while ZIP work is active. One archive operation may run per Ferry window at a time. | A | Must |
| FR-1034 | ZIP creation progress shall use source/input bytes as the primary work measure. ZIP extraction progress shall use expanded bytes from ZIP entry metadata as the primary work measure. | A | Must |
| FR-1035 | Extraction shall pre-scan archive metadata and warn the user when the current resource thresholds are exceeded: expanded data over 20 GiB, more than 50,000 files, or compression ratio over 100×. The warning shall report measured values and offer explicit **YES** to continue or **NO** to cancel. | A | Must |
| FR-1036 | Unsafe extraction paths/names shall be blocked rather than offered as a user-overridable warning. This includes destination escape/path traversal, absolute/rooted archive paths, Windows alternate-data-stream style names, and reserved Windows device-name forms. | A | Must |
| FR-1037 | Extraction shall not write an incomplete file directly under its final name. File content shall be completed through a temporary file and finalized only after the entry copy succeeds. Cancellation/failure cleanup shall remove unfinished temporary output. | A | Must |
| FR-1038 | Folder conflicts shall offer **MERGE / KEEP BOTH / SKIP / CANCEL** when applicable. File conflicts shall offer **REPLACE / KEEP BOTH / SKIP / CANCEL** when applicable. Destructive Merge/Replace shall not be offered for incompatible item types or unsafe reparse-point cases. | A | Must |
| FR-1039 | **KEEP BOTH** shall create a collision-safe sibling name by appending `(n)` without an added space before the suffix: `Folder(1)`, `file(1).ext`, then `(2)`, `(3)`, etc. | A | Must |
| FR-1040 | After the user chooses **MERGE** for a folder, the first file conflict under that merged folder may offer `Apply this choice to all remaining file conflicts under this merged folder`. If selected, the remembered file decision applies only within that merged-folder scope; a different merged folder asks independently. | A | Must |
| FR-1041 | Cancelling extraction may leave files that were already fully completed; Ferry shall report partial results. Incomplete files shall not remain under final filenames. | A | Must |
| FR-1042 | If the user attempts to close Ferry while an archive operation is active, Ferry shall confirm closing/cancellation and allow archive cleanup to complete before application shutdown. | A | Must |
| FR-1043 | Choice labels controlled by Ferry for archive confirmations/conflicts shall remain English (`YES`/`NO`, `MERGE`, `KEEP BOTH`, `REPLACE`, `SKIP`, `CANCEL`) independent of the Windows display language. | A | Must |

### External terminal details

- The public factory default shall not contain a machine-specific terminal path.
- With no custom command configured, Ferry shall try **Windows Terminal**, then **Windows PowerShell**, then **Command Prompt**.
- Terminal executable/command shall be configurable, including commands resolvable through Windows PATH/App Execution Aliases.
- Terminal arguments shall be configurable.
- The current Ferry folder shall be supplied as the working directory.
- `F12` shall invoke the current-folder terminal command regardless of item selection.
- Ferry shall not contain its own terminal emulator.

---

## F11 — Settings / State

| ID | Requirement | Class | Priority |
|---|---|---:|---:|
| FR-1101 | Home folder shall be configurable. | A | Must |
| FR-1102 | Previous-exit folder restoration is out of scope. | C | — |
| FR-1103 | Normal launch shall always use the configured Home. | A | Must |
| FR-1104 | User shall explicitly configure the default view mode; v1.0 factory default is **List**. | A | Must |
| FR-1105 | Ferry shall not infer the preferred view from the last temporary view used. | C | — |
| FR-1106 | Global List column widths shall be persisted. | A | Must |
| FR-1107 | Global List column order shall be persisted. | A | Must |
| FR-1108 | Global List column visibility shall be persisted. | A | Must |
| FR-1109 | Global sort key shall be persisted. | A | Must |
| FR-1110 | Global sort direction shall be persisted. | A | Must |
| FR-1111 | Search match-mode preference should be persisted. | A | Should |
| FR-1112 | Hidden-item display preference shall be persisted. | A | Must |
| FR-1113 | Window size shall be persisted. | A | Must |
| FR-1114 | Window position should be persisted safely with multi-monitor fallback. | A | Should |
| FR-1115 | Maximized state should be persisted. | A | Should |
| FR-1116 | Sidebar width shall be configurable in the range **50–480** and persisted. The resize hit target may remain wider than the visual separator for usability. | A | Should |
| FR-1117 | Sidebar visibility should be persisted. | A | Should |
| FR-1118 | Grid icon size should be persisted. | A | Should |
| FR-1119 | External terminal command shall be configurable. | A | Should |
| FR-1120 | External terminal arguments shall be configurable. | A | Should |
| FR-1121 | Settings shall support Export and Import independent of Portable/Installer distribution. | A | Should |
| FR-1122 | Settings Reset shall be available. | A | Should |
| FR-1123 | Settings should take effect without requiring a full application restart where practical. | A | Must |
| FR-1124 | Ferry shall not use a database solely for preferences. | C | — |
| FR-1125 | Portable preferences shall use a small human-readable configuration file such as JSON. | A | Must |
| FR-1126 | Cloud settings synchronization is out of scope. | C | — |
| FR-1127 | Multiple settings profiles are out of scope. | C | — |
| FR-1128 | Ferry v1.0 UI language shall be **English only**. | A | Must |
| FR-1129 | A localization/i18n subsystem is out of scope for v1.0. | C | — |
| FR-1130 | Settings shall expose an **About Ferry** section showing the assembly-derived application version, creator credit (`kazu0m1`), GitHub repository, and MIT license information. | A | Should |

Suggested exported settings filename: `FerrySettings.json`.

---

# 4. Cross-functional sorting rules

These rules apply to normal views and search results.

1. **Sort folders before files** is global and defaults to **ON**.
2. For Name / Modified / Created / Type with folders-first OFF, folders and non-folders participate in one mixed ordering using the selected key. Shortcuts are files for grouping purposes and shall not form a separate third layer.
3. With folders-first ON, folders form the first group and the selected key is applied within each group; changing ascending/descending shall not reverse the folders-first group boundary.
4. When values are equal/unavailable, Name is the deterministic secondary key.
5. Name sorting uses **Natural Sort**, so `file2` sorts before `file10`.
6. **Items** is meaningful only for folders. Folders therefore remain the first meaningful group for Items sorting and are sorted by direct-child count; non-folders follow by Name.
7. Ferry does not recursively calculate folder sizes. For **Size** with folders-first OFF, files sort by actual size and folders form a stable Name-sorted group afterward. With folders-first ON, folders form the first group.
8. For Items sorting, Ferry shall render the view immediately, obtain required folder counts asynchronously, and apply the stable Items sort after required count data is available. The UI shall not repeatedly jump on every individual count completion.
9. In List view, the currently active sort column shall display `▲` for ascending order or `▼` for descending order; no inactive column shall display a sort arrow.
10. Because Ferry uses one global sort configuration, the active sort indicator shall remain synchronized across all open List-view tabs.
11. Ordinary external/background updates do not imply an immediate sort request. Newly detected items remain in a stable bottom tail and changing metadata does not move existing rows until the user explicitly requests Refresh/F5, clicks a sort column, or applies settings that reload/sort the folder. The visible sort indicator continues to represent the sort rule that will be applied on the next explicit sort.

---

# 5. Non-functional requirements — F12 Reliability / Performance

| ID | Requirement | Class | Priority |
|---|---|---:|---:|
| NFR-1201 | Startup shall not launch unnecessary background work. | A | Must |
| NFR-1202 | Initial folder rendering shall not wait for all optional metadata/counts/thumbnails. | A | Must |
| NFR-1203 | Item counting shall be asynchronous. | A | Must |
| NFR-1204 | Thumbnail retrieval shall be asynchronous and use Windows infrastructure. | A+B | Must |
| NFR-1205 | Search shall be asynchronous and progressively display usable results. | A+B | Must |
| NFR-1206 | Large folders shall not freeze the UI. | A | Must |
| NFR-1207 | List/Grid/Search result UI shall use virtualization or equivalent bounded UI materialization. | A | Must |
| NFR-1208 | Obsolete background work shall be cancellable when navigating, closing a tab, changing a query, or closing a window. | A | Must |
| NFR-1209 | Access-denied files/folders shall not crash Ferry. | A+B | Must |
| NFR-1210 | Files disappearing during view/metadata operations shall be handled safely. | A | Must |
| NFR-1211 | Unexpected removable-drive disconnect shall not crash Ferry. | A+B | Must |
| NFR-1212 | Slow network locations shall not block the main UI thread. | A | Must |
| NFR-1213 | Failures in optional Windows/Shell extension integration shall be contained as far as practical and shall not corrupt user data. | A+B | Must |
| NFR-1214 | Corrupt/missing Ferry settings shall fall back to safe defaults and allow Ferry to start. | A | Must |
| NFR-1215 | Settings writes shall use a corruption-resistant/atomic replacement strategy where practical. | A | Must |
| NFR-1216 | Normal shutdown shall not leave unnecessary Ferry-owned temporary cache data behind. | A | Must |
| NFR-1217 | Ferry shall not maintain a large independent persistent cache. | A | Must |
| NFR-1218 | Ferry memory usage shall be bounded; result/file count shall not cause uncontrolled retained memory growth. | A | Must |
| NFR-1219 | Long-running Ferry sessions shall not exhibit material memory leaks. | A | Must |
| NFR-1220 | Exceptions shall fail safely and prioritize avoidance of user-data corruption. | A | Must |
| NFR-1221 | Closing Ferry while a file operation is active shall have a safe, explicit behavior; the operation must not be abandoned in an ambiguous/data-corrupting state. | A+B | Must |
| NFR-1222 | Ferry should degrade gracefully if Windows updates alter optional Shell integration behavior. | A+B | Should |
| NFR-1223 | Debug logging shall be available but **OFF by default**; it is for troubleshooting and shall not create an ever-growing normal-use log. | A | Should |
| NFR-1224 | Automatic crash-report upload is out of scope. | C | — |
| NFR-1225 | Telemetry is out of scope. | C | — |
| NFR-1226 | Ferry-owned caching shall be limited to the minimum justified amount. | A | Must |
| NFR-1227 | Any Ferry-owned cache shall have an explicit bounded size/lifetime policy. | A | Must |
| NFR-1228 | Windows-managed shared caches/indexes shall be reused instead of duplicated where practical. | A+B | Must |
| NFR-1229 | Cache cleanup shall not itself make startup/shutdown materially expensive. | A | Must |
| NFR-1230 | Background tasks (item count, thumbnails, metadata, direct search) shall use **bounded concurrency**; Ferry shall not start one uncontrolled worker/thread per item. Visible/needed work shall be prioritized. | A | Must |
| NFR-1231 | Ferry shall preserve valid Windows Unicode filenames, including Japanese characters and emoji, without normalization or corruption. | A+B | Must |
| NFR-1232 | Ferry UI shall operate correctly under common Windows DPI/display scaling such as 100%, 125%, 150%, and 200%. | A | Must |
| NFR-1233 | Ordinary content changes within the currently displayed folder (for example New Folder, Delete, Paste, Rename, D&D completion, or FileSystemWatcher notifications) shall be reconciled incrementally where practical instead of clearing/rebinding the entire List/Grid view, in order to minimize visible flicker and eye strain. | A | Must |

### Performance philosophy

No hard millisecond target is fixed before implementation benchmarking. The behavioral target is:

- show the shell of the view immediately;
- show available names immediately;
- load counts/thumbnails/slow metadata progressively;
- show search results as soon as they are available;
- cancel work that the user can no longer see or needs no longer;
- never trade unbounded resource consumption for marginal responsiveness.
- avoid full-view teardown/rebinding for ordinary same-folder changes when a small incremental reconciliation can preserve visual continuity.

---

# 6. v1.0 explicit Out of Scope summary

Ferry v1.0 intentionally does **not** attempt to implement:

- replacement of Windows Shell / `explorer.exe`;
- tray/service/background residency;
- automatic redirection of every Windows folder open;
- FTP/SFTP;
- dedicated cloud-provider integrations;
- `This PC` clone;
- system-file management mode;
- recursive folder-size calculation;
- preview/details pane;
- full-text/content search;
- archive-content search;
- regex search or regex rename;
- Ferry-specific search index/database;
- Everything replacement/integration;
- high-performance custom copy engine;
- copy queue/graphs;
- non-ZIP archive formats such as 7z/RAR as Ferry-owned archive workflows;
- custom compression codec/Deflate implementation from scratch;
- separate/custom Recycle Bin storage engine (Ferry may provide a virtual Recycle Bin view while Windows remains the storage/operation authority);
- custom terminal emulator;
- custom ACL editor;
- automatic case-conversion rename;
- bulk extension-editing rename mode (single-item extension editing is supported);
- type-to-select;
- per-folder view/sort/column settings;
- session/tab restoration;
- closed-tab history;
- automatic updater;
- telemetry/crash upload;
- localization/multiple UI languages in v1.0.

When such a task is needed, Ferry should provide a simple escape route to Windows Explorer or a specialist application rather than absorbing the feature itself.

---

# 7. Key user flows

## 7.1 Normal launch

`Ferry.exe` → configured Home → factory default Home = `%USERPROFILE%` → factory default view = List.

## 7.2 Open specific folder

Windows folder context → **Open with Ferry** → Ferry opens that folder.

Ferry does not globally intercept every folder open.

## 7.3 New tab

`Ctrl+T` → Home in a new tab.

Folder → middle-click or **Open in New Tab** → selected folder in a new tab.

## 7.4 Item count

View appears first → folder names/icons appear → counts populate asynchronously → Grid shows count under folder name; List shows Items column.

## 7.5 Search

Current folder → type filename text or `Ctrl+F` → Ferry searches current folder and descendants → fastest useful Windows search source first → direct traversal supplements as needed → results appear progressively → `Esc` exits search.

## 7.6 Bulk rename

Select multiple items → `F2` / Rename → Find & Replace or Numbering Template → live Current→New preview → validate conflicts/Windows names → execute safe rename → no success pop-up.

## 7.7 Advanced Shell action

Normal right click → small Ferry menu → **Show more options** at the bottom → Windows detailed Shell menu.

`Shift+Right-click` opens that detailed menu directly.

## 7.8 Folder shortcut navigation

Double-click a `.lnk` whose resolved target is a folder → Ferry resolves the shortcut → the current Ferry tab navigates to the target → Explorer is not opened for that navigation.

## 7.9 Recycle Bin

Sidebar **Recycle Bin** → current Ferry tab shows the Windows-managed Recycle Bin as a Ferry virtual view.

Recycle Bin sidebar context → **Empty Recycle Bin** → Windows confirmation/execution.

Recycle Bin item context → **Restore** / **Delete Permanently** / **Open Original Location**.

## 7.10 Search clear

Search query present → click **×**, clear the query, or press `Esc` → cancel obsolete search work → restore the pre-search current-folder snapshot immediately → schedule normal incremental reconciliation afterward. If the empty Search box still has focus, a second `Esc` returns focus to the file view.

## 7.11 Selection stabilization policy

Public v1.0 uses the native extended-selection behavior that was already stable in Ferry internal build v1.0.7.2:

- normal left click selects one item;
- `Ctrl+Click` adds/removes non-contiguous items;
- `Shift+Click` selects a contiguous range;
- `Ctrl+A` selects all;
- clicking true empty view space clears selection;
- item-originated drag remains normal file D&D; when a drag begins from any member of an existing multi-selection, the entire selected set is dragged;
- `Enter` opens all selected items; selected files launch together, while a focused selected folder uses the current tab and other selected folders use additional tabs.

Rubber-band / marquee selection is deliberately disabled for public v1.0. The Explorer behavior research performed during RC1–RC10 is retained as design research for a later version, but it is not part of the v1.0 implementation baseline.


---

# 8. Decision log

### D-001 — Ferry does not replace `explorer.exe`
Windows Shell functionality remains Windows-owned. Ferry replaces only the daily file-browsing experience.

### D-002 — Nautilus is a UX reference, not a source-code base
Ferry is a Windows-native implementation inspired by selected Nautilus behavior.

### D-003 — Windows functions are reused aggressively
File operation infrastructure, file associations, icons, thumbnails, Recycle Bin, Shell properties, clipboard, search index, permissions, and similar OS capabilities should be delegated to Windows where that gives adequate behavior.

### D-004 — Ferry-specific search index is prohibited
Ferry provides Nautilus-like recursive filename search by combining existing Windows search capabilities with direct traversal. Whole-PC specialist searching belongs to specialist tools such as Everything.

### D-005 — View configuration is global
Column widths, column order, visible columns, sort, and explicit default view are consistent across folders. Ferry does not mimic Explorer's per-folder adaptive views.

### D-006 — Home is explicit and stable
Home is user-configurable; first-run default is the current user profile folder (`%USERPROFILE%`). Normal startup opens Home rather than the previous exit location.

### D-007 — Right-click has two layers
Ferry provides a lightweight frequent-action menu. **Show more options** and `Shift+Right-click` expose Windows Shell's detailed menu.

### D-008 — Tabs are included, but kept minimal
Tabs provide strong file-browser utility at acceptable implementation cost. Tab-session restore, closed-tab history, and other browser-like tab management are intentionally omitted.

### D-009 — Ferry should be quiet on success
Routine successful file operations and renames do not generate modal completion/Undo messages. Errors are surfaced only when action is needed.

### D-010 — Resource use is deliberately bounded
Ferry does not create large private caches or indexes and does not launch unbounded background work for marginal performance gains.

### D-011 — UI language is English only for v1.0
The UI vocabulary is simple enough that internationalization infrastructure is not justified for the first version.

### D-012 — Individual rename follows Nautilus extension behavior
Single-item rename displays the complete filename including the extension, but selects only the filename stem by default. The extension remains intentionally editable when the user selects or moves into it. Bulk rename continues to protect extensions.

### D-013 — Ferry owns the ZIP workflow, not the compression codec
Ferry owns ZIP create/extract orchestration, byte-based progress, ETA, cancellation, conflict handling, and extraction safety policy. ZIP container/compression support comes from .NET `System.IO.Compression.ZipArchive`; Ferry does not implement Deflate or another compression codec from scratch and does not invoke an external archive command-line tool for its ZIP workflow.

### D-014 — Public defaults are machine-neutral
The public distribution shall not embed developer-specific absolute paths. Factory Home is `%USERPROFILE%`; terminal command is blank/Auto and resolves through Windows Terminal → Windows PowerShell → Command Prompt, while custom terminal configuration remains available.

### D-015 — Selection stability takes priority over Explorer parity

The RC1–RC10 custom Explorer-style selection engine is not part of public v1.0. Ferry rolls item selection back to the proven v1.0.7.2 native WPF extended-selection behavior and removes rubber-band selection. `Shift+Click` is the supported range-selection method for v1.0. Same-path `FileItem` reuse during refresh may remain as an independent stability optimization.


---

# 9. Specification revision history

| Revision | Scope |
|---|---|
| **1.0.0** | Initial F01–F12 requirements baseline after design audit. |
| **1.0.1** | Build-only correction; no intended requirements change. |
| **1.0.2** | Field-test clarification: `Esc` selection clearing, visible Search clear control, persistent global List widths (already required), and folder-target `.lnk` navigation inside the current Ferry tab. |
| **1.0.3** | Added same-folder drag/drop no-op, drag-to-Sidebar pinning, and Ferry-hosted virtual Recycle Bin behavior while keeping Windows as the Recycle Bin authority. |
| **1.0.4** | Added visible ascending/descending sort indicators and global indicator synchronization across tabs. |
| **1.0.5** | Added actionable D&D destination-folder highlighting and incremental same-folder refresh to reduce List/Grid flicker after New Folder, Delete, Paste, Rename, D&D, and filesystem notifications. |
| **1.0.6** | Added ZIP compression/extraction to Ferry's lightweight context menu and changed single-item rename so file extensions are visible/editable while the stem remains the default selected text. Bulk rename continues to protect extensions. |
| **1.0.7** | Promoted rubber-band multi-selection to a v1.0 Must requirement and added Explorer-style visible rectangle selection in both List and Grid views. |
| **1.0.8** | Public-release defaults: Home now defaults to `%USERPROFILE%`; external terminal defaults to Auto with Windows Terminal → Windows PowerShell → Command Prompt fallback; removed machine-specific MSYS2/UCRT64 factory settings. |
| **1.0.9** | RC1 interaction hardening: visual-whitespace rubber-band hit testing, multi-selection-preserving D&D, additive Ctrl-rubber-band, immediate Search snapshot restore, folder-aware Open in Explorer, stale Rename-Undo invalidation, configurable folder-first sorting, and hardened Ctrl+Tab handling. |
| **1.0.10** | RC2 interaction-quality hardening: delayed click-vs-rubber-band decision on item whitespace, selector-model/visual selection synchronization, explicit file-D&D arming that excludes view chrome, multi-selection Shell detailed-menu hardening, and native Windows Open With dialog invocation. |
| **1.0.11** | RC3 final interaction polish: delayed true-background click-vs-rubber-band decision, Explorer-style whitespace double-click opening, D&D highlight local-value restoration that preserves WPF selection styles, post-gesture selection/focus reconciliation, and `IShellFolder::GetUIObjectOf`-first multi-selection detailed Shell menu creation with query-failure fallback. |
| **1.0.12** | RC4 selection-engine redesign after direct Explorer behavior verification: explicitly separates Selected / Focused / Anchor state; makes row/tile whitespace gesture meaning conditional on whether the owning item was selected at mouse-down; defines selected-row whitespace drag as file D&D, unselected-row whitespace drag as rubber-band, row/tile-wide rubber-band geometry, stationary whitespace double-click open, and true-background deselection with focused-item retention. |
| **1.0.13** | RC5 Explorer-selection specification completion: adds explicit Hover / Pressed-Pending / active Selected / inactive Selected / Focused-current visual states; defines mouse-down pending behavior, background click deselection with focus-only retention and keyboard continuation, Ctrl/Shift focused-item rules, multi-item Enter semantics, same-path in-place incremental refresh for actively changing files, and logical focus transfer on rename. |
| **1.0.14** | RC11 stability rollback: supersedes the RC1–RC10 custom selection-engine requirements for public v1.0; restores v1.0.7.2 native WPF Extended selection, removes rubber-band/marquee selection, and limits range selection to `Shift+Click` while retaining public machine-neutral defaults and unrelated reliability fixes. |
| **1.0.15** | RC12 adds two narrowly scoped multi-selection interactions without reintroducing the custom selection engine: `Enter` opens all selected items, and D&D from any already-selected item preserves and drags the full selected set. |
| **1.0.16** | RC13 changes the factory default of **Sort folders before files** to ON and rebuilds multi-selection Windows detailed-context-menu PIDL construction around one parent `IShellFolder`, while retaining the RC11/RC12 native selection baseline. |
| **1.0.17** | RC14 separates ordinary background reconciliation from explicit user sorting: newly detected items remain in a stable bottom tail, same-path metadata updates do not automatically re-sort the view, and F5/Refresh/column/settings actions explicitly reapply the configured sort. |
| **1.0.18** | RC15 adds `F12` as a window-level Open Terminal Here shortcut for the current Ferry folder, independent of item selection, while leaving the RC14 interaction baseline unchanged. |
| **1.0.19** | v1.0.1 release-candidate UX refinement: inline single-item rename and New Folder focus/rename flow; full-/half-width-insensitive filename search; stable Pinned D&D reordering; lightweight List Shell icons with Grid-only thumbnails; Sidebar width range 50–480 with a thinner visual separator; and Settings About/version/creator information. |
| **1.0.20** | v1.0.1 RC2 visual polish: removes the dedicated splitter layout gap so Sidebar and file-view frame share the same boundary while retaining a transparent 5-DIP resize hit target, and increases spacing before the Settings About section. |
| **1.0.21** | v1.0.1 RC3 archive-operation feedback: adds a persistent window-level indeterminate progress bar for ZIP compression and extraction while continuing to delegate archive work to the Windows-provided archive tool. |
| **1.0.22** | v1.0.1 RC4 archive-operation polish: changes the indeterminate archive progress indicator to a neutral gray treatment and shows a short completion message after successful compression or extraction. |

| **1.0.24** | v1.0.2 ZIP baseline: replaces external Windows archive-tool delegation with Ferry-owned ZIP workflow over .NET `System.IO.Compression`; adds determinate progress/speed/ETA/Cancel, non-blocking operation, extraction safety gates/warnings, temporary-file finalization, MERGE/KEEP BOTH conflict handling, MERGE-local remembered file conflict decisions, and safe application-close cancellation. |

Release notes remain the authoritative chronological record of what changed in each application build. This specification remains the authoritative description of the **current intended v1.0 behavior**.

---

# 10. Requirements baseline status

The F01–F12 requirements review is complete and the baseline has been updated through Ferry v1.0.2 specification revision 1.0.24.

Implementation may refine technical mechanisms, but any change that alters user-visible behavior, Must/Should scope, the Windows-delegation boundary, or Ferry's resource philosophy shall be treated as a specification change and reflected in this document.

A defect fix that only restores behavior already required by this specification does not require a new functional requirement; it should still be recorded in the applicable release notes.
