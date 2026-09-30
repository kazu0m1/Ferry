# Ferry v1.2.1

Ferry v1.2.1 is a focused To-Do usability update.

## Improvements

- Added **Open To-Do tab at startup** to Ferry Settings. The default is Off, preserving the existing startup behavior.
- When enabled, Ferry prepares the pinned To-Do tab at startup while keeping the normal startup folder tab selected.
- Returning to the To-Do tab now restores the previous editor context: row, To-Do/Memo side, caret position, and text selection.
- Fixed To-Do/Memo column-boundary drift when the vertical scrollbar appears in a smaller window. The header now follows the ScrollViewer viewport width so the 50/50 divider stays aligned with the item rows.

## Notes

- To-Do data remains stored in the existing portable `config\settings.json`.
- No new background service, database, or sync subsystem is introduced.
