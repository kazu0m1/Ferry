# Ferry Public v1.0.0 — Release Audit

**Audit date:** 2026-09-08  
**Release status:** READY FOR PUBLICATION after final Windows binary build/package verification

This document summarizes the public-release audit and the Windows validation completed for Ferry v1.0.0. It is not a legal opinion.

## 1. Personal-environment dependency audit

| Item | Public v1.0.0 state | Status |
|---|---|---|
| Default Home | `%USERPROFILE%` | PASS |
| Terminal command | Blank = Auto | PASS |
| Terminal fallback | Windows Terminal → Windows PowerShell → Command Prompt | PASS |
| Custom terminal | Configurable command/arguments | PASS |
| User-specific paths/names | No private machine-specific path retained in public defaults | PASS |
| Settings | Portable JSON; fresh package contains no user settings | PASS |

## 2. Dependency / source-origin audit

- Project uses Windows/.NET Framework assemblies and WPF.
- No NuGet package references are present in `Ferry.csproj` or `Build.cmd`.
- No GNOME/Nautilus source code or GNOME artwork is bundled.
- Nautilus is used as a UX reference only.
- ZIP operations are delegated to the Windows 11 archive tool.

**Result:** no third-party package/library blocker was identified in the supplied Ferry source tree.

## 3. Branding / attribution audit

- Product name is **Ferry**.
- README states that Ferry is an independent Windows implementation, not a port/fork of Nautilus.
- README includes a GNOME non-affiliation statement.
- MIT License copyright holder is `kazu0m1`.

## 4. Privacy / data behavior audit

- Telemetry: none.
- Automatic crash upload: none.
- Debug logging: off by default.
- Settings: local JSON in Ferry's portable folder.
- No account system, cloud-sync subsystem, Ferry-owned search database, service, or tray process.

## 5. Selection / interaction release decision

The RC1–RC10 custom Explorer-style Selection Engine was removed before release because Windows testing found regressions not present in the internally proven native selection baseline.

Public v1.0 therefore uses native WPF `SelectionMode.Extended` behavior:

- normal click
- `Ctrl+Click`
- `Shift+Click`
- `Ctrl+A`
- empty-space deselect
- multi-selection Enter
- multi-selection-preserving D&D

Rubber-band/marquee selection is out of scope for v1.0.

## 6. Final Windows validation

The final RC15 code baseline passed the **19 / 19 public-release smoke test** on Windows.

Additional targeted validations completed during the RC cycle include:

- Multiple-selection Windows detailed Shell menu: PASS through both `Shift+Right-click` and Ferry → Show more options.
- Corrupted `settings.json`: PASS; Ferry starts with defaults and writes valid JSON again.
- Missing `settings.json`: PASS; Ferry starts and creates a new file after Settings → Save.
- Active `.crdownload`: PASS; metadata updates in place, new item remains at the bottom, clicks no longer intermittently miss, and `F5` reapplies sort.
- `F12` current-folder Open Terminal Here: PASS.

## 7. Final packaging gate

The source/package metadata has been promoted from `1.0.0-rc15` to `1.0.0` without changing the validated application logic.

Before creating the GitHub Release on Windows:

1. Run `Build.cmd`.
2. Launch the resulting `Portable\Ferry.exe` once and confirm it opens.
3. Run `Make-PortableRelease.cmd`.
4. Confirm `dist\Ferry-v1.0.0-win-portable.zip` exists.
5. Extract that ZIP into a new folder and launch `Ferry.exe` once.
6. Record the SHA-256 printed by `Make-PortableRelease.cmd`.
7. Create GitHub tag/release `v1.0.0` and upload the portable ZIP.

**Release decision:** READY, subject only to the final binary packaging/launch verification above.
