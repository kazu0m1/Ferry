# Ferry

**A lightweight Windows file manager for people who miss the simplicity of Nautilus.**

Ferry brings a focused, GNOME Files (Nautilus)-inspired file-management workflow to Windows 11 without trying to replace the Windows Shell.

If you move between Linux and Windows and find yourself missing Nautilus — its direct navigation, useful folder item counts, quick filename search, and comfortable bulk rename — Ferry is built for that gap.

> **Current release:** v1.0.0  
> **Platform:** Windows 11  
> **Runtime:** .NET Framework 4.8 / WPF  
> **License:** MIT

[日本語 README](README.ja.md)

## Why Ferry?

Ferry is deliberately small in scope. It does not try to become an all-in-one dual-pane power tool or replace `explorer.exe`. Instead, it focuses on everyday browsing and a few high-value workflows:

- **Nautilus-inspired simplicity** — a familiar, uncluttered file-browsing model
- **Folder item counts** — visible directly in Grid and List views
- **Fast recursive filename search** — progressive results, Windows Search when useful, direct traversal as fallback
- **Capable bulk rename** — find/replace and numbering templates with live preview
- **Tabs** — lightweight tabbed browsing without session-management bloat
- **Windows integration** — Recycle Bin, Shell context menu, Properties, shortcuts, thumbnails, drag & drop
- **ZIP workflow** — Compress / Extract commands delegated to Windows 11
- **Portable settings** — small human-readable JSON, no private database, no telemetry

## A Windows app, inspired by Nautilus

Ferry is an independent Windows implementation. It is **not** a port, fork, or modified build of GNOME Files/Nautilus, and it contains no Nautilus source code or GNOME artwork.

GNOME® is a registered trademark of the GNOME Foundation. Ferry is not affiliated with, endorsed by, or supported by the GNOME Foundation.

## Quick start

### Option A — prebuilt portable release

1. Download `Ferry-v1.0.0-win-portable.zip` from GitHub Releases.
2. Extract it to a folder of your choice.
3. Run `Ferry.exe`.

Ferry stores its settings in the local `config` folder beside the executable.

### Option B — source / self-building package

1. Download or clone the source repository.
2. Run `Portable\Run-Ferry.cmd`.
3. If `Ferry.exe` is not present, the launcher calls `Build.cmd` and builds it locally with the .NET Framework compiler included with Windows.
4. Ferry starts in your Windows **Home profile folder** (`%USERPROFILE%`) using **List** view.

No Visual Studio, NuGet, separate .NET SDK, or Internet connection is required for this build path.

## Factory defaults

- Sort folders before files: **On** (can be disabled in Settings)
- Home: `%USERPROFILE%`
- View: **List**
- Search mode: **Contains**
- External terminal: **Auto**
- UI language: **English**
- Debug logging: **Off**
- Telemetry / crash upload: **None**

Terminal **Auto** mode tries, in order:

1. Windows Terminal (`wt.exe`)
2. Windows PowerShell (`powershell.exe`)
3. Command Prompt (`cmd.exe`)

A custom terminal command and arguments can be set in **Settings**.

## Main features

### Navigation and views

- Sidebar with Home, standard user folders, pinned folders, drives, and Recycle Bin
- Breadcrumb navigation
- Back / Forward / Up / Home
- Tabs (`Ctrl+T`, `Ctrl+W`, `Ctrl+Tab`, `Ctrl+Shift+Tab`)
- List and Grid views
- Global List columns and sort configuration
- Natural filename sorting (`file2` before `file10`)
- **Sort folders before files** enabled by default
- Visible ascending / descending sort indicator
- Native multi-selection: `Ctrl+Click`, `Shift+Click`, and `Ctrl+A`
- Multi-selection actions: `Enter` opens all selected items; dragging any already-selected item preserves and drags the full selected set
- New items detected from outside Ferry stay at the bottom until the user explicitly refreshes or sorts; metadata such as an active download's size can continue updating in place
- `F12` opens a terminal in the current Ferry folder regardless of item selection

> Ferry v1.0 intentionally does not implement rubber-band/marquee selection. Use `Shift+Click` for contiguous range selection.

### Search

Ferry search is intentionally a **file-browser filename search**, not an Everything replacement.

- current folder + descendants
- files and folders
- progressive asynchronous results
- **Contains** and **StartsWith** modes
- `*` and `?` wildcards
- multiple terms use AND behavior in Contains mode
- hidden items included only when **Show hidden items** is enabled
- junction/symbolic-link targets are not recursively followed
- Windows Search Index is used when useful, with direct traversal as fallback
- no Ferry-owned search index or search database

### Rename

**Single selection + `F2`** edits the complete filename. Ferry initially selects only the stem, so the extension remains visible and intentionally editable.

**Multiple selection + `F2`** opens the integrated bulk-renaming dialog with:

- Find & Replace
- numbering templates
- configurable start number
- live Current → New preview
- deterministic order based on the current visible Ferry sort
- collision-safe two-phase rename
- extension preservation for bulk rename

Supported numbering tokens include:

```text
[1, 2, 3]
[01, 02, 03]
[001, 002, 003]
```

### Recycle Bin

Recycle Bin opens **inside Ferry** as a virtual view over the current Windows user's Recycle Bin.

- Restore
- Delete Permanently
- Open Original Location
- Empty Recycle Bin from the Sidebar context menu

Windows remains the authority for Recycle Bin storage and operations.

### File operations and drag & drop

Ferry delegates Windows-owned behavior where practical:

- Copy / Cut / Paste
- delete to Recycle Bin / permanent delete
- drag & drop
- conflict handling
- Properties
- Open With
- Windows shortcuts
- Shell detailed context menu, including multi-selection
- icons and thumbnails

Dragging files back into the same folder is treated as a no-op. Actionable destination folders are highlighted during drag-over.

### ZIP

The lightweight context menu provides:

- **Compress to ZIP** for selected files/folders
- **Extract Here** for a selected ZIP
- **Extract to `<archive-name>\`**

Archive work is asynchronous and delegated to the Windows 11 archive tool. Ferry contains no custom archive codec.

## Open with Ferry in Explorer

After Ferry has been built, run:

```text
ShellIntegration\Register.cmd
```

This adds **Open with Ferry** to the folder context menu for the current Windows user. Administrator rights are not required.

Remove it with:

```text
ShellIntegration\Unregister.cmd
```

Ferry does not globally replace or hijack Explorer folder opening.

## Keyboard shortcuts

| Shortcut | Action |
|---|---|
| `Ctrl+T` | New Home tab |
| `Ctrl+W` | Close tab |
| `Ctrl+Tab` | Next tab |
| `Ctrl+Shift+Tab` | Previous tab |
| `Alt+Left` | Back |
| `Alt+Right` | Forward |
| `Alt+Up` | Parent folder |
| `Ctrl+L` | Direct path entry |
| `Ctrl+F` | Search |
| `F5` | Refresh and reapply the current sort |
| `F12` | Open Terminal Here in the current folder |
| `F2` | Rename / bulk rename |
| `Ctrl+C/X/V` | Copy / Cut / Paste |
| `Ctrl+A` | Select all |
| `Delete` | Move to Recycle Bin |
| `Shift+Delete` | Permanent delete |
| `Enter` | Open all selected items |
| `Alt+Enter` | Properties |
| `Esc` | Exit search / clear selection |
| `Shift+Right-click` | Windows detailed context menu |

Normal typing while the file view has focus starts Ferry search rather than Explorer-style type-to-select.

## Settings and privacy

Settings are stored in `Portable\config\settings.json` in the source/self-building layout, or `config\settings.json` beside `Ferry.exe` in the binary portable package.

If the settings file is missing or invalid, Ferry starts with safe factory defaults. Saving Settings creates/replaces a valid JSON file.

Ferry has:

- no telemetry
- no automatic crash upload
- no account system
- no cloud sync subsystem
- no Ferry-specific search database
- no always-running service or tray process

Debug logging is **Off** by default and bounded when enabled.

## Build

Ferry targets **.NET Framework 4.8 / WPF** and uses Windows/.NET Framework assemblies only. There are no NuGet dependencies.

Run:

```text
Build.cmd
```

The script uses the .NET Framework C# compiler under `%WINDIR%\Microsoft.NET\Framework[64]\v4.0.30319\`.

To create the binary-only GitHub Release asset on Windows:

```text
Make-PortableRelease.cmd
```

This creates:

```text
dist\Ferry-v1.0.0-win-portable.zip
```

## Repository layout

```text
Ferry/
├─ Source/Ferry/                 C# / WPF source
├─ Portable/                     launcher and local runtime folder
├─ ShellIntegration/             optional Explorer context-menu registration
├─ docs/                         publication / design documentation
├─ Build.cmd                     local build
├─ Make-PortableRelease.cmd      binary release ZIP builder
├─ Ferry_SPEC_v1.0.md            functional specification
├─ TEST_CHECKLIST.md             regression checklist
├─ RELEASE_NOTES_v1.0.0.md       GitHub Release notes
├─ CHANGELOG.md
├─ LICENSE.txt
├─ README.md
└─ README.ja.md
```

## Scope philosophy

Ferry intentionally does **not** implement its own Windows Shell, high-performance copy engine, terminal emulator, archive codec, full-text search engine, cloud client, or search database. When Windows already owns a capability well, Ferry tries to reuse it rather than duplicate it.

See `Ferry_SPEC_v1.0.md` for the full v1.0 requirements baseline.

## License

MIT License — see `LICENSE.txt`.

Copyright © 2026 **kazu0m1**.
