# Documentation Status — Ferry v1.1.1 Final

**Selection baseline:** Ferry v1.1.0 final  
**Paste feedback baseline:** v1.1.1 prototype 1  
**Release baseline:** Ferry v1.1.1  
**Phase:** specification frozen / final source package prepared

## v1.1.1で追加された確定仕様

- Copy/Cut → Paste後、今回のPasteに対応するdestination直下のトップレベルitemを選択状態で残す。
- Paste result selectionはList / Grid間で同期する。
- 次のPasteでは前回結果を累積せず、今回の結果へ置き換える。
- Copy/Move本体とconflict handlingはWindows `SHFileOperation`へ委譲し続ける。
- 同一folder conflictもFerry独自のduplicate namingを導入しない。

## Current normative documents

- `Ferry_SPEC_v1.1.md` — current v1.1.1 delta/release baseline.
- `Ferry_SPEC_v1.0.md` — historical v1.0/v1.0.2 baseline.
- `docs/RUBBER_BAND_SELECTION_SPEC_JA.md` — detailed frozen v1.1.0 selection behavior inherited by v1.1.1.
- `RELEASE_NOTES_v1.1.1.md` — user-facing current release summary.
- `PUBLIC_RELEASE_AUDIT.md` — v1.1.1 release audit.
- `FINAL_RELEASE_CHECKLIST_JA.md` — Windows/publication checklist.
- `docs/GITHUB_PUBLICATION_GUIDE_JA.md` — current v1.1.1 GitHub publication procedure.
- `docs/dev-history/releases/v1.1.1/` — prototype 1 implementation / validation evidence.

Historical release records remain grouped under `docs/dev-history/releases/`. Rubber-band prototype records remain under `docs/dev-history/rubber-band/`.
