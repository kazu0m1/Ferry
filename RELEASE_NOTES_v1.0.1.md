# Ferry v1.0.1

Ferry v1.0.1 is a small usability and responsiveness release focused on making everyday file operations feel more direct and predictable.

## Highlights

### Inline rename and New Folder workflow

- Single-item `F2` / Rename now edits the item name **inline** instead of opening a separate rename window.
- For files, the filename stem is selected initially while the extension remains visible and editable.
- **New Folder** is created in Ferry's stable unsorted tail, automatically scrolled into view, selected, and immediately placed into inline rename.
- Rename/new-folder operations do not unexpectedly re-sort the list; explicit `F5` or a column sort reapplies ordering.

### More natural Japanese filename search

- Search now ignores full-width / half-width differences.
- Examples: `カタカナ` matches `ｶﾀｶﾅ`, `ガ` matches `ｶﾞ`, and `ABC` matches `ＡＢＣ`.
- Hiragana / katakana type remains distinct.

### Reorder Pinned folders

- Pinned Sidebar folders can now be reordered by drag & drop.
- An insertion bar shows the target position.
- The resulting order is saved and restored after restart.
- The Pinned section now uses an ordered list model rather than ad-hoc button positioning.

### Faster List view

- List view now uses lightweight Windows Shell **type icons** rather than content thumbnails.
- Grid view continues to load content thumbnails when previews are useful.
- Shell metadata/icon queries were adjusted to avoid unnecessary access to file content/thumbnail providers, improving responsiveness in folders with many PDFs/images.

### Sidebar and Settings polish

- Sidebar width can be configured from **50–480** and remains persisted.
- Sidebar and file view now meet at a single visual boundary while keeping a usable invisible resize hit target.
- Settings now includes **About Ferry** with the running version, `Created by kazu0m1`, GitHub repository information, and MIT license notice.

### ZIP activity feedback

- ZIP compression and extraction now show a persistent neutral-gray **indeterminate progress bar** in Ferry's bottom status area.
- The indicator remains visible even if you navigate to another folder/tab while the archive operation continues.
- Successful completion briefly displays `ZIP compression complete.` or `ZIP extraction complete.` for about three seconds.
- Ferry still delegates archive work to the Windows-provided archive tool; v1.0.1 does not add a custom archive codec or estimated-time calculation.

## Compatibility / retained behavior

- Existing v1.0.0 selection behavior is retained: normal click, `Ctrl+Click`, `Shift+Click`, `Ctrl+A`, and blank-area deselection.
- Multi-item `Enter`, multi-item drag & drop, F12 **Open Terminal Here**, external-update stable-tail behavior, Windows detailed context menu, Settings recovery, and portable configuration are retained.
- **Open with…** behavior is unchanged in v1.0.1.

## Notes

Ferry remains a portable Windows 11 application built on .NET Framework 4.8 / WPF. No installer, account, telemetry, or always-running background service is required.
