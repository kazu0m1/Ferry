# Ferry v1.0.0

Ferry v1.0.0 is the first public release of a lightweight Windows 11 file manager for people who miss the simplicity of GNOME Files (Nautilus).

## Highlights

- **Folder item counts** directly in List and Grid views.
- **Recursive progressive filename search** with Contains / StartsWith / wildcard matching.
- **Bulk rename** with Find & Replace, numbering templates, start-number control, and live preview.
- **Tabs** and straightforward Back / Forward / Up / Home navigation.
- **Native multi-selection** with `Ctrl+Click`, `Shift+Click`, and `Ctrl+A`.
- **Multi-selection Enter and D&D** — open all selected items or drag the complete selected set.
- **Stable external-update behavior** — new items stay at the bottom while active metadata such as `.crdownload` size updates in place; `F5` or an explicit sort reapplies ordering.
- **Windows integration** — Recycle Bin, detailed Shell context menu including multi-selection, Properties, Open With, shortcuts, thumbnails, and Explorer handoff.
- **ZIP Compress / Extract** delegated to Windows 11.
- **F12 = Open Terminal Here** for the current Ferry folder, independent of item selection.
- **Portable JSON settings** with safe fallback when the settings file is missing or invalid.

## Factory defaults

- Home: `%USERPROFILE%`
- View: **List**
- Search: **Contains**
- Sort folders before files: **On**
- Terminal: **Auto** — Windows Terminal → Windows PowerShell → Command Prompt
- UI language: **English**
- Debug logging: **Off**

## Selection note

Ferry v1.0 uses the stable native WPF Extended-selection model. Contiguous range selection is available through `Shift+Click`; non-contiguous selection through `Ctrl+Click`.

Rubber-band/marquee selection is intentionally **not included in v1.0** and may be reconsidered in a future release.

## Privacy / scope

Ferry contains no telemetry, automatic crash upload, account system, Ferry-owned search database, custom archive codec, or always-running service/tray process.

Ferry is an independent Windows implementation inspired by GNOME Files/Nautilus. It is not a port or fork and contains no Nautilus source code or GNOME artwork.

## Validation

The final Windows smoke test on the RC15 code baseline passed **19 / 19** checks, covering startup, navigation, tabs, search, selection, multi-open, D&D, rename/undo, file operations, ZIP, sorting, active `.crdownload` updates, detailed Shell menus, Open in Explorer/Open With, F12 Terminal, Settings, Recycle Bin, and restart.

## Download

For normal use, download:

`Ferry-v1.0.0-win-portable.zip`

Extract it and run `Ferry.exe`.

The source repository can also build locally on Windows with `Build.cmd` / `Portable\Run-Ferry.cmd` without Visual Studio, NuGet, or a separate .NET SDK.
