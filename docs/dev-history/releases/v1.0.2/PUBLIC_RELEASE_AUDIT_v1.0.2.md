# Ferry Public v1.0.2 — Release Audit

**Audit date:** 2026-09-13  
**Release status:** READY FOR PUBLICATION after final Windows build/package/launch verification

This document summarizes the v1.0.2 scope and validation completed through the accepted RC1. It is not a legal opinion.

## 1. Release scope

v1.0.2 changes one main product area: ZIP creation/extraction.

- Ferry owns ZIP workflow/control using .NET `System.IO.Compression`.
- Determinate progress, speed, ETA, Cancel, and non-blocking Ferry operation.
- Ferry-owned extraction conflict handling including MERGE and KEEP BOTH.
- Extraction safety validation, resource warnings, temporary-file finalization, and cancellation cleanup.
- Existing v1.0.1 browsing/search/rename/selection/Shell behavior is retained.
- Rubber-band/marquee selection remains out of scope for v1.0.2.

## 2. Dependency / source-origin audit

- Build remains C# / WPF / .NET Framework 4.8.
- No NuGet package is required by the supported build path.
- ZIP support uses framework assemblies `System.IO.Compression.dll` and `System.IO.Compression.FileSystem.dll`.
- No 7-Zip/WinRAR/external archive command-line dependency is required for normal Ferry ZIP work.
- Ferry does not implement Deflate from scratch.
- No GNOME/Nautilus source code or GNOME artwork is bundled.

## 3. Privacy / data behavior

Unchanged from v1.0.1:

- no telemetry;
- no automatic crash upload;
- no account system;
- settings stored in local portable JSON;
- no Ferry-owned search database;
- no always-running service/tray process.

## 4. Windows validation status

The accepted ZIP Integration Prototype 4 passed practical Windows regression and safety testing for normal compression/extraction, non-blocking Ferry operation, conflict handling, cancellation/cleanup, unsafe archive blocking, resource warnings, and safe close behavior.

The promoted v1.0.2-rc1 subsequently passed the complete Windows smoke test, including build, representative existing features, ZIP create/extract, conflict handling, Cancel, safety spot checks, safe close, Portable ZIP generation, fresh-folder launch, and restart.

A detailed-context-menu extension initialization issue observed with **Open in Terminal** was reproduced in Windows Explorer after Explorer restart and classified as external to Ferry.

## 5. Final freeze rule

The accepted v1.0.2-rc1 application logic is promoted to v1.0.2 final unchanged.

Finalization changes are limited to:

- `AssemblyInformationalVersion`;
- release/packaging metadata;
- README / specification release-baseline wording / release notes;
- final release checklist, validation, and publication documentation.

Any application-logic change requires a new validation cycle rather than silent inclusion in final.

## 6. Final Windows packaging gate

Before publishing on GitHub from Windows:

1. Run `Build.cmd`.
2. Confirm Settings → About Ferry shows `Version 1.0.2`.
3. Perform one normal ZIP create and one normal ZIP extract.
4. Run `Make-PortableRelease.cmd`.
5. Confirm `dist\Ferry-v1.0.2-win-portable.zip` exists.
6. Extract the ZIP into a fresh folder and launch `Ferry.exe`.
7. Complete `FINAL_RELEASE_CHECKLIST_JA.md`.
8. Publish tag/release `v1.0.2` with `RELEASE_NOTES_v1.0.2.md` and the portable ZIP asset.

**Release decision:** READY, subject only to final Windows build/package/launch verification.
