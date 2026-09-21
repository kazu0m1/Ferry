Ferry v1.1.7 Portable

Ferry is a lightweight Windows 11 file manager inspired by the simplicity and workflow of GNOME Files (Nautilus).

Highlights in v1.1.7:
- Back navigation to a parent folder restores selection/focus to the folder that was just left
- In List view, that restored folder is aligned to the top of the viewport for immediate context
- Ferry's lightweight background context menu includes New Text Document
- New text files use collision-safe names and immediately enter inline rename

Also includes v1.1.6:
- Connected MTP/portable devices such as Android phones are recognized in the Sidebar
- Portable devices appear under a dedicated Portable Devices section
- Clicking a portable device opens it in Windows Explorer for file transfer
- Ferry does not attempt native MTP file operations; Windows Explorer remains authoritative

Also includes v1.1.5:
- Open tabs can be reordered by dragging them left or right
- Sidebar folders provide an Explorer-style right-click menu with Open, new-tab/window, Explorer, Properties, and Windows detailed options
- Pinned folders keep Unpin in the expanded sidebar context menu
- Tab reordering uses a Ferry-only drag format so normal file drag-and-drop remains independent

Also includes v1.1.4:
- Long Windows Shell Copy/Move operations no longer block Ferry's window or normal browsing UI
- Ferry remains usable during large cross-volume Copy/Move operations, including minimize/restore, tab switching, and folder navigation
- Ferry-to-Ferry drag-and-drop releases both the sender and receiver UI while the target-side Shell transfer continues
- Ferry refuses to close while a Ferry-owned Copy/Move transfer is still active, preventing the worker operation from being abandoned

Also includes v1.1.3:
- Removable drives connected after Ferry starts now appear automatically under Drives
- Safely Remove Hardware releases Ferry's drive handles/watchers and moves affected tabs to Home before eject
- Multiple Ferry tabs on the removable drive are released together before eject

Also includes v1.1.2:
- Ferry -> Windows Explorer drag-and-drop completes confirmed same-volume Move operations instead of leaving a duplicate source item
- Source cleanup is guarded by both the final WPF Move result and Windows Shell Performed DropEffect = Move
- Ctrl+D&D Copy, Cancel, Ferry->Ferry, Explorer->Ferry, and v1.1.1 Paste-result behavior remain unchanged in Windows validation

Also includes v1.1.1:
- Explorer-style Paste result feedback: top-level items created/updated by the current Paste become selected
- Selection is mirrored across List/Grid and remains after Paste completes
- Windows continues to own the actual Copy/Cut/Paste operation and conflict UI

Also includes the validated v1.1.0 baseline:
- Explorer-style rubber-band selection in List and Grid views
- Ctrl / Shift / Ctrl+Shift selection behavior with a visible Selection Anchor
- Edge autoscroll for rubber-band selection, configurable from 30 to 300
- Stable List/Grid selection synchronization
- Improved Ctrl+L Location Box / Breadcrumb switching
- Sidebar double-click auto-fit
- Fixed-height toolbar with 30x30 icon buttons and a dedicated 10px Breadcrumb scrollbar

Factory defaults:
- Home: current Windows user profile (%USERPROFILE%)
- View: List
- Search: Contains
- Sort folders before files: On
- Rubber-band autoscroll speed: 100
- Terminal: Auto (Windows Terminal -> Windows PowerShell -> Command Prompt)
- F12: Open Terminal Here for the current Ferry folder

ZIP archive workflow:
- Ferry-owned ZIP create/extract workflow using .NET System.IO.Compression
- Determinate progress, transfer speed, ETA, and Cancel in the bottom status area
- Ferry remains usable while ZIP work runs
- MERGE / KEEP BOTH / REPLACE / SKIP conflict handling as applicable

Binary GitHub Release package:
- Run Ferry.exe directly.
- Settings are stored under the local config folder beside Ferry.exe.

Source/self-building package:
- Run Portable\Run-Ferry.cmd. If Ferry.exe is absent, Ferry is built locally with the .NET Framework compiler included with Windows.

Ferry contains no telemetry or automatic crash upload.

Project: https://github.com/kazu0m1/Ferry
License: MIT
