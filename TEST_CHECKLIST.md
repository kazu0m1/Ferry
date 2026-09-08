# Ferry v1.0.0 Regression Checklist

Use this checklist for future changes after v1.0.0. The v1.0.0 final candidate itself passed the separate 19-item Windows smoke test recorded in `docs/FINAL_SMOKE_TEST_RESULT_JA.md`.

## Startup / navigation

- [ ] `Portable\Run-Ferry.cmd` builds and starts Ferry when `Ferry.exe` is absent.
- [ ] Fresh Ferry opens `%USERPROFILE%` in List view.
- [ ] Back / Forward / Up / Home work.
- [ ] `Ctrl+L` direct path entry works.
- [ ] `F5` refreshes and reapplies the current sort.

## Tabs

- [ ] `Ctrl+T`, `Ctrl+W`, `Ctrl+Tab`, `Ctrl+Shift+Tab` work.
- [ ] Open in New Tab works.

## Views / selection / sort

- [ ] List and Grid views work.
- [ ] Folder Items count appears asynchronously.
- [ ] Hidden toggle changes visibility and counts consistently.
- [ ] Single click selects one item.
- [ ] `Ctrl+Click` builds a non-contiguous multi-selection.
- [ ] `Shift+Click` selects a contiguous range.
- [ ] `Ctrl+A` selects all visible items.
- [ ] Clicking true empty List/Grid space clears selection.
- [ ] Empty-space drag does not display a rubber-band rectangle.
- [ ] Sort folders before files defaults to ON for fresh settings.
- [ ] Name / Modified / Created / Type / Items / Size sorting behaves as specified.

## Search

- [ ] Typing in file view starts search.
- [ ] `Ctrl+F` focuses search.
- [ ] Contains / StartsWith / wildcard search work.
- [ ] Results appear progressively and include Location.
- [ ] Backspace from an empty query, `Esc`, and `×` return promptly to the normal folder view.

## Rename

- [ ] Single `F2` shows the complete filename and initially selects the stem.
- [ ] Multi-selection `F2` opens bulk rename.
- [ ] Find/Replace and numbering templates work.
- [ ] Duplicate/invalid targets are blocked.
- [ ] `Ctrl+Z` restores the last valid Ferry rename in the current session.

## File operations / Windows integration

- [ ] Copy / Cut / Paste work.
- [ ] Delete sends to Recycle Bin.
- [ ] `Shift+Delete` performs permanent delete with Windows behavior.
- [ ] Single-item D&D works.
- [ ] Multi-selection D&D preserves and drags the complete selected set.
- [ ] Multiple selected files + `Enter` opens all selected files.
- [ ] Multiple selected folders + `Enter` opens the focused/current folder in the current tab and the remaining folders in additional tabs.
- [ ] Properties works.
- [ ] Open With works.
- [ ] Open in Explorer works.
- [ ] Show more options opens the Windows detailed Shell menu for single and multiple selection.
- [ ] `Shift+Right-click` opens the Windows detailed Shell menu for single and multiple selection.
- [ ] `F12` opens a terminal in the current Ferry folder regardless of item selection.

## Recycle Bin

- [ ] Sidebar Recycle Bin opens in Ferry.
- [ ] Restore works.
- [ ] Delete Permanently works.
- [ ] Open Original Location works.
- [ ] Empty Recycle Bin works.

## ZIP

- [ ] Compress to ZIP works for one and multiple selected items.
- [ ] Extract Here works.
- [ ] Extract to `<name>\` works.
- [ ] Archive work does not freeze Ferry.

## External updates

- [ ] A newly detected file/folder appears at the bottom of the view.
- [ ] Active `.crdownload` Size/Modified values update in place.
- [ ] Repeated clicks do not intermittently miss during active metadata updates.
- [ ] The new item does not auto-move while its metadata changes.
- [ ] `F5`, Refresh, or a sort-column click reapplies the current sort.

## Settings

- [ ] Settings Save persists after restart.
- [ ] Missing `settings.json` falls back safely.
- [ ] Invalid `settings.json` falls back safely and can be regenerated.
- [ ] Settings Export / Import works.

## Shell integration

- [ ] `ShellIntegration\Register.cmd` adds Open with Ferry.
- [ ] Open with Ferry opens the selected folder.
- [ ] `ShellIntegration\Unregister.cmd` removes the registration.
