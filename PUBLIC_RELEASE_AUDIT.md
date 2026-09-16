# Ferry Public v1.1.2 — Release Audit

**Date:** 2026-09-16  
**Behavioral baseline:** v1.1.2 prototype 1 (Windows real-machine PASS; cross-volume D&D not run)  
**Status:** final source frozen; Windows final build / Portable artifact generation remains

## Scope

v1.1.2 is a backward-compatible patch release fixing Ferry → Windows Explorer Move completion. The validated v1.1.1 Paste feedback and v1.1.0 selection/navigation baselines are intentionally unchanged.

## Accepted change

- Capture the final WPF external drag/drop result.
- Read Windows Shell `Performed DropEffect` from the drag DataObject.
- Delete remaining source paths only when both signals confirm Move.
- Do not double-delete paths already removed by an optimized target-side Move.
- Leave source data untouched for Copy, Link, None, Cancel, or an absent/non-Move `Performed DropEffect`.

## Windows validation record

prototype 1 passed real-machine validation for:

- same-drive Ferry → Explorer normal Move;
- `Ctrl+D&D` Copy safety;
- Cancel / invalid drop safety;
- folder and multi-item external D&D;
- Ferry → Ferry D&D regression;
- Explorer → Ferry D&D regression;
- v1.1.1 Paste feedback regression;
- selection/rubber-band smoke regression.

Cross-volume D&D was not executed because no second drive was available. This is recorded as unverified rather than PASS.

## Final promotion rule

No external D&D application logic changes after prototype 1 acceptance. Final promotion changes title/version/publication metadata only.

## Publication metadata

- Final window title: `Ferry`
- AssemblyVersion / FileVersion: `1.1.2.0`
- AssemblyInformationalVersion: `1.1.2`
- app.manifest identity: `1.1.2.0`
- Portable version: `1.1.2`
- Expected asset: `Ferry-v1.1.2-win-portable.zip`
- Release title: `Ferry v1.1.2`
- Tag: `v1.1.2`

## Publisher decisions

- Publish v1.1.2 unsigned.
- Existing approved screenshots remain suitable; v1.1.2 does not require a new screenshot set.
- Preserve prototype evidence under `docs/dev-history/releases/v1.1.2/`.
