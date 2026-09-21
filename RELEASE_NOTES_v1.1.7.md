# Ferry v1.1.7

Ferry v1.1.7 is a small navigation and file-creation usability release built on the validated v1.1.6 baseline.

## Added

- Ferry's lightweight background context menu now includes **New Text Document**.
- The command creates an empty `.txt` file with a collision-safe name and immediately enters Ferry's existing inline rename flow.

## Changed

- When **Back** returns from a child folder to its direct parent, Ferry restores selection and keyboard focus to the folder that was just left.
- In List view, the restored folder is aligned to the top of the viewport instead of merely being made visible at the bottom.

## Windows validation

The final implementation passed real-machine checks for:

- opening a folder low in a long parent list and returning with **Back**;
- restoring selection/focus to that folder;
- aligning the restored folder to the top of List view;
- creating a text file through **New Text Document**; and
- entering inline rename immediately after creation.

Windows GitHub Actions `Build.cmd` also passed for the final candidate.

## Preserved

- v1.1.6 Portable Device recognition and Windows Explorer hand-off.
- v1.1.5 tab reordering and Sidebar folder context menus.
- v1.1.4 responsive long Windows Shell Copy/Move and Ferry-to-Ferry D&D behavior.
- v1.1.3 removable-drive arrival/removal and safe-eject behavior.
- v1.1.2 Ferry → Explorer external D&D Move completion behavior.
- v1.1.1 Paste-result selection behavior.
- v1.1.0 selection/rubber-band/Selection Anchor behavior.

Release status: Back context restoration/top alignment and New Text Document creation passed Windows real-machine validation before final promotion; v1.1.7 finalization changes version/release metadata and packaging only.
