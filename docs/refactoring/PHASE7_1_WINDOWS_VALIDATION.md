# Phase 7-1 Windows Validation — ChoiceDialog archive conflict partial

## Scope
Windows build/runtime validation after moving only `ShowArchiveConflict` from `ChoiceDialog.cs` to `ChoiceDialog.ArchiveConflict.cs` and making `ChoiceDialog` partial.

## Baseline
- Branch: `refactor/v1.1.2`
- Phase 7-1 implementation head: `5c31b2b015dd20dbf6e29b4376772928c4b1bd26`
- Static validation commit: `5c467feba6542e98cfdaed40501806bbc0d5d4b3`
- Validation date: 2026-09-17

## Result
**PASS — Windows build/runtime validation.**

Confirmed:
- `Build.cmd` succeeds.
- Ferry launches normally.
- Normal Yes/No-style confirmation dialogs still behave normally, including Enter/Esc behavior.
- ZIP extraction conflict UI still opens normally.
- Archive conflict choice handling remains functional.

## Notes
Phase 7-1 was a behavior-preserving responsibility split only. Dialog semantics, button behavior, archive conflict decisions, and call sites were not changed.