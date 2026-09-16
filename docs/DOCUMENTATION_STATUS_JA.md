# Documentation Status — Ferry v1.1.2 Final

**Selection baseline:** Ferry v1.1.0 final  
**Paste feedback baseline:** Ferry v1.1.1 final  
**External D&D baseline:** v1.1.2 prototype 1  
**Release baseline:** Ferry v1.1.2  
**Phase:** specification frozen / final source package prepared

## v1.1.2で追加された確定仕様

- Ferry → Windows Explorer外向きD&Dで、Windowsがunoptimized Moveを確定した場合にsource cleanupを完了する。
- source削除には`DoDragDrop` final effect = Moveと`Performed DropEffect` = Moveの二重条件を要求する。
- optimized Moveですでにsourceが消えている場合は二重削除しない。
- Copy / Cancelではsourceを削除しない。
- Ferry→Ferry / Explorer→Ferryの既存経路は変更しない。
- 別volume D&Dは実機環境上未実施であり、検証済みとは扱わない。

## Current normative documents

- `Ferry_SPEC_v1.1.md` — current v1.1.2 delta/release baseline.
- `Ferry_SPEC_v1.0.md` — historical v1.0/v1.0.2 baseline.
- `docs/RUBBER_BAND_SELECTION_SPEC_JA.md` — detailed frozen v1.1.0 selection behavior inherited by v1.1.2.
- `RELEASE_NOTES_v1.1.2.md` — user-facing current release summary.
- `PUBLIC_RELEASE_AUDIT.md` — v1.1.2 release audit.
- `FINAL_RELEASE_CHECKLIST_JA.md` — Windows/publication checklist.
- `docs/GITHUB_PUBLICATION_GUIDE_JA.md` — current v1.1.2 GitHub publication procedure.
- `docs/dev-history/releases/v1.1.2/` — prototype 1 implementation / validation evidence.

Historical release records remain grouped under `docs/dev-history/releases/`. Rubber-band prototype records remain under `docs/dev-history/rubber-band/`.
