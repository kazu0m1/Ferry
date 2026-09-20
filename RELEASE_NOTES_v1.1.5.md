# Ferry v1.1.5

Ferry v1.1.5 is a small usability release built on the validated v1.1.4 baseline.

## Added

- Open tabs can now be reordered by dragging them left or right.
- Sidebar folder entries now have an Explorer-style right-click menu.
- The Sidebar menu includes **Open**, Open in New Tab, Open in New Ferry Window, Open Terminal Here, Open in Explorer, Properties, and Show more options.
- Pinned folders use the same expanded menu while retaining **Unpin**.

## Interaction details

- The tab close button remains dedicated to closing the tab and does not start a tab drag.
- Tab reordering uses a Ferry-private drag format, separate from file drag-and-drop.
- Existing Pinned-folder drag reordering remains unchanged.
- Show more options continues to delegate the detailed context menu to Windows Shell.

## Windows validation

The final implementation passed real-machine checks for:

- reordering three open tabs by dragging them left/right;
- normal tab close-button behavior without accidental drag initiation;
- opening Sidebar folders through right-click → **Open**; and
- retaining **Unpin** for Pinned folders.

Windows GitHub Actions `Build.cmd` also passed for the candidate implementation.

## Preserved

- v1.1.4 responsive long Windows Shell Copy/Move and Ferry-to-Ferry D&D behavior.
- v1.1.3 removable-drive arrival/removal and safe-eject behavior.
- v1.1.2 Ferry → Explorer external D&D Move completion behavior.
- v1.1.1 Paste-result selection behavior.
- v1.1.0 selection/rubber-band/Selection Anchor behavior.

Release status: application behavior passed Windows validation before final promotion; v1.1.5 finalization changes version/release metadata and packaging only.
