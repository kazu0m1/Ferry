# Ferry Public v1.0.1 — Release Audit

**Audit date:** 2026-09-12  
**Release status:** READY FOR PUBLICATION after final Windows binary build/package verification

This document summarizes the v1.0.1 release audit and the Windows validation completed during the prototype/RC cycle. It is not a legal opinion.

## 1. Release scope

Ferry v1.0.1 is a maintenance/usability release. The principal changes are:

- single-item inline rename and improved New Folder flow
- full-width / half-width-insensitive filename search
- drag-and-drop reordering for Pinned Sidebar folders
- lightweight List-view Shell type icons; Grid thumbnails remain available
- Sidebar width range 50–480 and tighter Sidebar/File-view boundary
- About Ferry version/author/repository/license information
- persistent ZIP compression/extraction activity feedback with brief completion messages

`Open with…` default-app behavior is unchanged. Ferry still delegates archive work to Windows rather than owning an archive codec.

## 2. Dependency / source-origin audit

- Project uses Windows/.NET Framework assemblies and WPF.
- No NuGet package references are required by the build path.
- No GNOME/Nautilus source code or GNOME artwork is bundled.
- Nautilus is used as a UX reference only.
- ZIP operations remain delegated to the Windows-provided archive tool.

## 3. Privacy / data behavior

- Telemetry: none.
- Automatic crash upload: none.
- Debug logging: off by default.
- Settings: local JSON in Ferry's portable folder.
- No account system, cloud-sync subsystem, Ferry-owned search database, service, or tray process.

## 4. Windows validation status

The v1.0.1 prototype/RC cycle verified the new functionality on Windows, including:

- Inline Rename / New Folder auto-scroll + inline rename: PASS
- Full-width / half-width filename search: PASS
- Pinned D&D reorder + persistent order: PASS
- Sidebar width down to 50: PASS
- Stable List type-icon display and Grid thumbnails: PASS
- Multi-PDF `Enter` open after List-icon optimization: PASS
- Sidebar/File-view boundary polish: PASS
- About Ferry display/version: PASS
- ZIP indeterminate progress across navigation: PASS
- Neutral-gray archive progress style: PASS
- ZIP completion message (~3 seconds): PASS

Existing v1.0.0 selection, multi-item Enter/D&D, F12, detailed Shell menu, external-update stable-tail behavior, and settings recovery remain retained.

## 5. Final packaging gate

The validated RC4 application logic is promoted to v1.0.1 final with **release/version metadata and public-documentation changes only**.

Before publishing on GitHub from Windows:

1. Run `Build.cmd`.
2. Launch `Portable\Ferry.exe` and confirm Settings → About Ferry shows `Version 1.0.1`.
3. Run `Make-PortableRelease.cmd`.
4. Confirm `dist\Ferry-v1.0.1-win-portable.zip` exists.
5. Extract the ZIP into a fresh folder and launch `Ferry.exe`.
6. Perform the short final checks in `FINAL_RELEASE_CHECKLIST_JA.md`.
7. Commit/push the final source, create tag/release `v1.0.1`, and upload the portable ZIP.

**Release decision:** READY, subject only to final Windows build/package/launch verification.
