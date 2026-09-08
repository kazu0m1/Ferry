# Ferry Changelog

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
