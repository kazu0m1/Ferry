# Ferry Public v1.1.1 — Release Audit

**Date:** 2026-09-16  
**Behavioral baseline:** v1.1.1 prototype 1 (Windows real-machine PASS)  
**Status:** final source frozen; Windows final build / Portable artifact generation remains

## Scope

v1.1.1 is a backward-compatible patch release adding Explorer-style Paste result feedback. The validated v1.1.0 selection/navigation baseline is intentionally unchanged.

## Accepted change

- After Copy/Cut → Paste, Ferry selects the destination-level items associated with the current Paste so the user can immediately identify what was pasted.
- Paste result selection is synchronized between List and Grid and remains after the final incremental refresh.
- A later Paste replaces the previous Paste result selection rather than accumulating stale operation results.
- Windows `SHFileOperation` remains authoritative for Copy/Move execution and conflict handling. Ferry adds no custom copy engine or conflict UI.

## Windows validation record

prototype 1 passed real-machine validation for:

- basic multi-item Copy → Paste result selection;
- overwrite / folder-merge destination selection;
- Cut → Paste;
- true-background selection clearing after Paste;
- List ⇄ Grid selection synchronization;
- rubber-band / D&D / Shift-selection regression;
- consecutive Paste operations selecting only the newest result set.

Same-folder Paste was observed to use the standard Windows conflict message in the tested environment, resulting in Skip/Cancel rather than a duplicate sibling name. That Windows-owned behavior is accepted and Ferry does not override it.

## Final source metadata

- Window title: `Ferry`
- app manifest identity: `1.1.1.0`
- AssemblyVersion / FileVersion: `1.1.1.0`
- AssemblyInformationalVersion: `1.1.1`
- Portable package label: `1.1.1`
- Expected asset: `Ferry-v1.1.1-win-portable.zip`

## Publisher decisions

- Keep `docs/dev-history/` in the public repository.
- Keep official historical Release Notes in repository root.
- Publish v1.1.1 unsigned.
- Existing approved screenshots remain suitable; v1.1.1 does not require a new screenshot set.

## Environment limitation

This preparation environment cannot compile or run .NET Framework 4.8 WPF. Final Windows build/launch, Portable ZIP generation, and binary SHA-256 must be completed on Windows before GitHub publication.
