# Ferry Changelog

## v1.0.2 — 2026-09-13

Ferry v1.0.2 replaces the previous Windows-delegated ZIP workflow with a Ferry-owned ZIP workflow built on .NET `System.IO.Compression`.

### ZIP progress / control

- Determinate overall progress in the bottom status area.
- Processed/total data, file count, speed, ETA when available, and Cancel.
- Setup window closes after Start; Ferry remains usable during archive work.
- One archive job per Ferry window at a time.

### Extraction conflicts

- Folder conflicts: MERGE / KEEP BOTH / SKIP / CANCEL.
- File conflicts: REPLACE / KEEP BOTH / SKIP / CANCEL.
- KEEP BOTH uses `name(1)` / `name(1).ext`, then incrementing suffixes.
- After MERGE, a file decision can optionally be remembered for the remaining file conflicts under that merged folder only.

### ZIP safety

- Blocks unsafe destination escape/path traversal, rooted paths, ADS-style names, reserved Windows device-name forms, and unsafe reparse-point merge/replace cases.
- Warns for expanded data >20 GiB, >50,000 files, or compression ratio >100×; the user chooses YES/NO.
- Uses temporary output files before finalizing extracted files.
- Safe cancellation/partial-result reporting and safe Ferry close while archive work is active.

### Validation

- Integrated ZIP Prototype 4 passed Windows regression and safety tests before release packaging.
- The accepted RC1 application logic is promoted unchanged to v1.0.2 final; finalization changes release metadata/documentation only.

## v1.0.1 — 2026-09-12

Ferry v1.0.1 focuses on small daily-workflow improvements and lighter List-view behavior.

### Rename / creation

- Single-item `F2` / Rename now edits the item name inline instead of opening a separate rename window.
- Files initially select only the filename stem while leaving the extension visible and editable.
- **New Folder** creates the folder in the stable bottom tail, scrolls it into view, selects it, and immediately enters inline rename.

### Search

- Filename search now ignores full-width / half-width differences while preserving kana-type distinctions.
- Examples: `カタカナ` matches `ｶﾀｶﾅ`, `ガ` matches `ｶﾞ`, and `ABC` matches `ＡＢＣ`.

### Sidebar

- Pinned folders can be reordered by drag and drop and retain their order after restart.
- The Pinned section now uses a dedicated ordered list model for stable reordering.
- Sidebar width is configurable from **50–480** and remains persisted.
- Sidebar and file view now meet at one visual boundary with no dedicated splitter gap; the transparent resize hit target remains easy to grab.

### List / Grid

- List view uses lightweight Windows Shell type icons rather than content thumbnails.
- Grid view continues to load thumbnails when visual previews are useful.
- List icon/type queries avoid unnecessary access to file content/thumbnail providers.

### Settings / About

- Settings now includes a visually separated **About Ferry** section showing the assembly-derived version, `Created by kazu0m1`, the GitHub repository, and MIT license information.

### Archive feedback

- ZIP compression and extraction now show a persistent neutral-gray indeterminate progress indicator in the bottom status bar.
- The archive progress indicator remains visible across folder/tab navigation until the operation finishes.
- Successful completion briefly shows `ZIP compression complete.` / `ZIP extraction complete.` before returning to the normal status.
- Archive work continues to use the Windows-provided archive tool rather than a Ferry-owned codec.

### Compatibility

- Existing v1.0.0 selection behavior, multi-item Enter/D&D, F12 terminal shortcut, external-update stable-tail behavior, and Windows detailed context menu are retained.
- **Open with…** behavior is unchanged in v1.0.1.

## v1.0.0 — First public release

Ferry v1.0.0 is the first public release of the lightweight Windows 11 file manager inspired by the simplicity and workflow of GNOME Files (Nautilus).

### Navigation / views

- Home defaults to `%USERPROFILE%`.
- List and Grid views with global sort/view behavior.
- Folder direct-child item counts.
- Tabs with `Ctrl+T`, `Ctrl+W`, `Ctrl+Tab`, and `Ctrl+Shift+Tab`.
- Natural filename sorting and visible ascending/descending indicators.
- **Sort folders before files** enabled by default.
- New items detected from external filesystem activity remain in a stable tail at the bottom until an explicit user sort/refresh.
- Same-path metadata such as active download size/time updates in place without automatic resorting.

### Selection / open / drag

- Native WPF Extended selection: normal click, `Ctrl+Click`, `Shift+Click`, and `Ctrl+A`.
- Clicking true empty List/Grid space clears selection.
- Multiple selected items can be opened with `Enter`.
- Dragging one already-selected item preserves and drags the complete selected set.
- Rubber-band/marquee selection is intentionally deferred from v1.0 in favor of the stable native selection model.

### Search

- Recursive progressive filename search in the current folder and descendants.
- Contains / StartsWith modes, `*` and `?` wildcards, and multiple-term AND matching.
- Windows Search is used when useful, with direct traversal as fallback.
- Fast exit back to the cached normal folder view.

### Rename

- Single-file `F2` rename with the complete filename visible and stem initially selected.
- Bulk Find & Replace and numbering templates with configurable start number and live preview.
- Collision-safe two-phase execution and extension preservation for bulk rename.
- Session-scoped `Ctrl+Z` for the last Ferry rename, invalidated after unrelated file mutations.

### Windows integration / file operations

- Copy / Cut / Paste, Recycle Bin delete, permanent delete, Properties, Open With, shortcuts, icons, thumbnails, and drag & drop.
- In-app Recycle Bin virtual view with Restore, Delete Permanently, Open Original Location, and Empty Recycle Bin.
- Detailed Windows Shell context menu, including multiple-selection support through both Show more options and `Shift+Right-click`.
- Open in Explorer with folder/file-aware targeting.
- Optional per-user **Open with Ferry** Explorer registration.
- ZIP Compress / Extract commands delegated to Windows 11.

### Terminal

- Machine-neutral Terminal **Auto** mode: Windows Terminal → Windows PowerShell → Command Prompt.
- Custom terminal command/arguments remain configurable.
- `F12` opens a terminal in the current Ferry folder regardless of item selection.

### Settings / privacy

- Portable JSON settings.
- Missing or invalid `settings.json` falls back safely to factory defaults; saving regenerates valid JSON.
- No telemetry, automatic crash upload, account system, Ferry-owned search database, or always-running service.

### Release validation

- Final Windows smoke test: **19 / 19 PASS** on the v1.0.0 RC15 code baseline.
- The final v1.0.0 source changes only release/version metadata and public documentation from that validated code baseline.
