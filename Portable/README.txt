Ferry v1.1.2 Portable

Ferry is a lightweight Windows 11 file manager inspired by the simplicity and workflow of GNOME Files (Nautilus).

Highlights in v1.1.2:
- Ferry -> Windows Explorer drag-and-drop now completes confirmed same-volume Move operations instead of leaving a duplicate source item
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
