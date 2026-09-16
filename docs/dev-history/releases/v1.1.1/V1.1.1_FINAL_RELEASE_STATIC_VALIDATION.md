# Ferry v1.1.1 — Final Static Validation

**Date:** 2026-09-16  
**Candidate:** `1.1.1` final  
**Behavioral baseline:** v1.1.1 prototype 1 (Windows real-machine PASS)

## Final application delta from prototype 1

No Paste/selection behavior was changed after the prototype 1 Windows PASS.

Application-source differences are limited to release metadata:

1. `MainWindow.cs`: `Ferry - prototype 1` → `Ferry`.
2. `AssemblyInfo.cs`: informational version `1.1.1-prototype1` → `1.1.1`.
3. `app.manifest`: assembly identity `1.1.0.0` → `1.1.1.0`.

The Paste feedback implementation in `ClipboardHelper.cs` / `MainWindow.cs` is otherwise unchanged from the tested prototype.

## Static source checks

- C# files under `Source/Ferry`: **29**.
- C# files referenced by `Build.cmd`: **29**.
- Build source coverage: **PASS** (sets match exactly).
- Lexical `() / {} / []` delimiter balance across all 29 C# files after excluding comments/string/char literals: **PASS**.
- No `Ferry.exe`, `dist/`, `bin/`, or `obj/` build output is included in the final source tree.

## Version / publication metadata

- Main window title: `Ferry`.
- app manifest identity: `1.1.1.0`.
- AssemblyVersion: `1.1.1.0`.
- AssemblyFileVersion: `1.1.1.0`.
- AssemblyInformationalVersion: `1.1.1`.
- `Make-PortableRelease.cmd`: `VERSION=1.1.1`.
- `Portable/README.txt`: `Ferry v1.1.1 Portable`.
- Bug report template example: `v1.1.1`.
- README / README.ja current release and direct download references: `v1.1.1`.
- Expected binary asset: `dist\Ferry-v1.1.1-win-portable.zip`.

## Frozen v1.1.1 behavior

- Copy/Cut → Paste leaves the current operation's destination-level result items selected.
- Paste result selection is mirrored across List/Grid and survives final incremental refresh.
- Consecutive Paste operations replace the previous result selection rather than accumulating it.
- Windows remains authoritative for Copy/Move/conflict handling; Ferry does not introduce a custom copy engine or conflict UI.
- v1.1.0 rubber-band, Selection Anchor, true-background, keyboard navigation, D&D, autoscroll, Breadcrumb/Location Box and toolbar behavior remain inherited unchanged.

## Documentation / repository

- `RELEASE_NOTES_v1.1.1.md` created.
- `Ferry_SPEC_v1.1.md` updated to the v1.1.1 release baseline and Paste-result semantics.
- Prototype 1 implementation and Windows validation records retained under `docs/dev-history/releases/v1.1.1/`.
- v1.1.0 generic final audit/publication documents were archived under `docs/dev-history/releases/v1.1.0/` before the current documents were replaced.

## Build limitation

This environment does not provide the Windows .NET Framework 4.8 WPF build/runtime environment. Compilation, launch validation, Portable binary creation, final functional spot checks, and final binary SHA-256 must be completed on Windows using `FINAL_RELEASE_CHECKLIST_JA.md`.
