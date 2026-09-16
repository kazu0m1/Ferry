# Ferry v1.1.0 — Final Static Validation

**Date:** 2026-09-16  
**Candidate:** `1.1.0` final  
**Behavioral baseline:** v1.1.0 RC20 (Windows real-machine PASS)

## Final application delta from RC20

No application behavior was changed after the RC20 Windows PASS.

Finalization changes are limited to:

1. Window title: `Ferry - RC 20` → `Ferry`.
2. `AssemblyInformationalVersion`: `1.1.0-rc20` → `1.1.0`.
3. `Make-PortableRelease.cmd`: `VERSION=1.1.0-rc20` → `VERSION=1.1.0`.
4. Release documentation / screenshot gallery / final audit records.

## Version metadata

- `AssemblyVersion`: `1.1.0.0`
- `AssemblyFileVersion`: `1.1.0.0`
- `AssemblyInformationalVersion`: `1.1.0`
- Main window title: `Ferry`
- `Make-PortableRelease.cmd`: `VERSION=1.1.0`
- Expected binary asset: `dist\Ferry-v1.1.0-win-portable.zip`

## Frozen v1.1.0 interaction baseline

- Explorer-style rubber-band selection in List / Grid.
- Normal / Ctrl / Shift / Ctrl+Shift marquee semantics and D&D coexistence.
- Edge autoscroll, Settings range 30–300, default 100.
- Selection Anchor visualization.
- List true-background geometry: left 10px gutter plus right-of-final-column tail.
- List selection fill and Selection Anchor stop at the final visible data column.
- List / Grid keyboard navigation after true-background clear, including Shift+Arrow recovery and Grid four-direction navigation.
- List ⇄ Grid selection synchronization.
- `Ctrl+L` / Breadcrumb interaction, 10px Breadcrumb horizontal scrollbar, 30×30 toolbar buttons.
- Sidebar divider double-click auto-fit.

## Documentation / repository

- README / README.ja current release and direct download references = v1.1.0.
- Approved screenshot set copied under `docs/` and referenced by README / README.ja.
- v1.0.x official release notes remain in repository root.
- Prototype / RC evidence remains under `docs/dev-history/`.
- Ubuntu Japanese community announcement remains prepared but publication is intentionally deferred.

## Build limitation

This environment does not provide the Windows .NET Framework 4.8 WPF build/runtime environment. Compilation, launch validation, Portable binary creation, and final binary SHA-256 must be completed on Windows using `FINAL_RELEASE_CHECKLIST_JA.md`.
