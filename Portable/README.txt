Ferry v1.0.2 Portable

Ferry is a lightweight Windows 11 file manager inspired by the simplicity and workflow of GNOME Files (Nautilus).

Factory defaults:
- Home: current Windows user profile (%USERPROFILE%)
- View: List
- Search: Contains
- Sort folders before files: On
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
