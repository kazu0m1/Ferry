# Ferry Public v1.1.0 — Release Audit

**Date:** 2026-09-16  
**Behavioral baseline:** v1.1.0 RC20  
**Status:** final source frozen; Windows final build / Portable artifact generation remains

## Scope

v1.1.0 is a backward-compatible minor release centered on Explorer-style selection, keyboard navigation, and navigation/UI polish. The historical v1.0.2 audit remains under `docs/dev-history/releases/v1.0.2/`.

## Major accepted changes

- Rubber-band selection for List and Grid.
- Ctrl / Shift / Ctrl+Shift semantics, D&D routing, outside-window release, and edge autoscroll.
- Visible Selection Anchor and synchronized keyboard-current handling.
- Grid four-direction keyboard navigation after true-background clear.
- Explorer-like List interaction geometry: 10px left true-background gutter and right-of-final-column true background.
- List selection fill / Selection Anchor clipped to the final visible data column.
- Exact List/Grid selection synchronization.
- Ctrl+L Location Box dismissal/restoration behavior.
- Configurable rubber-band autoscroll speed 30–300, default 100.
- Sidebar divider double-click auto-fit.
- Fixed toolbar/path-row layout, 30×30 icon buttons, and 10px Breadcrumb horizontal scrollbar with always-visible end buttons.
- Scrollbar double-click routing fix preventing selected-item accidental open.

## Validation record

- Prototype 27 established the main rubber-band selection baseline.
- Final rubber-band regression: PASS, including 10,000-item smoke testing.
- RC1–RC10 completed Location Box / toolbar / Breadcrumb polish.
- RC11–RC17 resolved true-background keyboard-current recovery, Shift+Arrow behavior, Selection Anchor synchronization, and Grid directional navigation.
- RC18–RC20 completed final List true-background geometry, selection-fill clipping, and gutter header visual consistency.
- RC20 Windows validation: **all requested checks PASS**. This is the frozen application-behavior baseline for final v1.1.0.

## Known characteristic

Extreme Shift-range selection across several thousand items can take several seconds. This remains accepted because ordinary operation, marquee selection, `Ctrl+A`, virtualization, and selection correctness were stable in testing.

## Final source metadata

- Window title: `Ferry`
- AssemblyVersion / FileVersion: `1.1.0.0`
- AssemblyInformationalVersion: `1.1.0`
- Portable package label: `1.1.0`
- Breadcrumb scrollbar height: `10px`
- Breadcrumb thumb/end-button CornerRadius: `0`

## Publisher decisions

- Keep `docs/dev-history/` in the public repository.
- Keep v1.0.0–v1.0.2 official Release Notes in repository root.
- Publish v1.1.0 unsigned.
- Keep the approved current screenshot set; do not retake for the small final List-geometry visual difference.
- Ubuntu Japanese community announcement is intentionally deferred.

## Environment limitation

This preparation environment cannot compile or run .NET Framework 4.8 WPF. Final Windows build/launch, Portable ZIP generation, and binary SHA-256 must be completed on Windows before GitHub publication.
