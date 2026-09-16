# Ferry v1.1.2 — Final Static Validation

**Date:** 2026-09-16  
**Candidate:** `1.1.2` final  
**Behavioral baseline:** v1.1.2 prototype 1 (Windows real-machine PASS; cross-volume D&D not run)

## Final application delta from prototype 1

No external D&D behavior was changed after the prototype 1 Windows PASS.

Files under `Source/Ferry` that differ from the accepted prototype 1:

1. `MainWindow.cs`: `Ferry - prototype 1` → `Ferry`.
2. `AssemblyInfo.cs`: informational version `1.1.2-prototype1` → `1.1.2`.
3. `app.manifest`: assembly identity `1.1.1.0` → `1.1.2.0`.

`ClipboardHelper.cs` and `ShellFileOperations.cs` are byte-identical to prototype 1. The external D&D completion logic in `MainWindow.cs` is unchanged apart from the window-title line.

## Frozen v1.1.2 D&D behavior

- Ferry captures the final result of `DragDrop.DoDragDrop` for external drags.
- Ferry reads Windows Shell `Performed DropEffect` from the same drag DataObject.
- Remaining source paths are cleaned up only when both signals indicate Move.
- Source paths already removed by an optimized Move are not deleted again.
- Copy / Link / None / Cancel, or an absent/non-Move `Performed DropEffect`, do not trigger source deletion.
- Ferry→Ferry internal D&D and Explorer→Ferry D&D remain unchanged.

## Accepted Windows validation

prototype 1 passed real-machine testing for:

- same-drive Ferry → Explorer normal Move;
- `Ctrl+D&D` Copy safety;
- Cancel / invalid-drop safety;
- folder and multi-item external D&D;
- Ferry → Ferry D&D regression;
- Explorer → Ferry D&D regression;
- v1.1.1 Paste-result feedback regression;
- List/Grid selection and rubber-band smoke regression.

Cross-volume D&D was not executed because the test machine had no second drive. It remains explicitly unverified rather than recorded as PASS.

## Static source checks

- C# files under `Source/Ferry`: **29**.
- C# files referenced by `Build.cmd`: **29**.
- Build source coverage: **PASS** (sets match exactly).
- Lexical `() / {} / []` delimiter balance across all 29 C# files after excluding comments/string/char literals: **PASS**.
- `CompleteExternalMoveIfRequired` call retained: **PASS**.
- `Performed DropEffect` reader retained: **PASS**.
- guarded `DeleteAfterExternalMove` path retained: **PASS**.
- no `Ferry.exe`, `.pdb`, `dist/`, `bin/`, `obj/`, `.vs/`, user `settings.json`, or `Ferry.log` in the final source tree: **PASS**.

## Version / publication metadata

- Main window title: `Ferry`.
- app manifest identity: `1.1.2.0`.
- AssemblyVersion: `1.1.2.0`.
- AssemblyFileVersion: `1.1.2.0`.
- AssemblyInformationalVersion: `1.1.2`.
- `Make-PortableRelease.cmd`: `VERSION=1.1.2`.
- `Portable/README.txt`: `Ferry v1.1.2 Portable`.
- Bug report template example: `v1.1.2`.
- README / README.ja current release and direct download references: `v1.1.2`.
- Expected binary asset: `dist\Ferry-v1.1.2-win-portable.zip`.

## Documentation / repository

- `RELEASE_NOTES_v1.1.2.md` created.
- `Ferry_SPEC_v1.1.md` updated to the v1.1.2 release baseline and external D&D completion semantics.
- Prototype 1 implementation, static validation, test plan, and Windows test result are retained under `docs/dev-history/releases/v1.1.2/`.
- Previous generic v1.1.1 release/publication documents were archived under `docs/dev-history/releases/v1.1.1/` before replacement.

## Build limitation

This environment does not provide the Windows .NET Framework 4.8 WPF build/runtime environment. Compilation, launch validation, Portable binary creation, final functional spot checks, and final binary SHA-256 must be completed on Windows using `FINAL_RELEASE_CHECKLIST_JA.md`.
