# Final Integration Windows Validation — Ferry v1.1.2 refactoring

## Scope
Final Windows integration gate for the behavior-preserving refactoring branch `refactor/v1.1.2` after Phase 8 completion audit.

## Branch state before this record
- Refactoring completion-audit head: `9f472389cd02c553e0939ed5bce7bd85fee74729`
- Original v1.1.2 release on `main`: `a688e59caffca1ac62af86ebc217ef293a137ad0`
- `main` remains unchanged at the original v1.1.2 release commit.

## Windows validation
Date: 2026-09-17

User result: **PASS, with one explicitly UNVERIFIED scenario.**

Validated successfully:
- `Build.cmd`
- fresh Ferry launch
- folder navigation and tabs
- List/Grid switching
- normal / Ctrl / Shift / Ctrl+Shift selection behavior
- true-background click behavior
- Arrow / Shift+Arrow behavior
- List/Grid rubber-band selection and autoscroll
- F2 rename
- batch rename
- Undo Rename responsiveness
- Copy/Cut -> Paste selection feedback
- Contains / StartsWith search
- Japanese width-insensitive search behavior
- search clear
- ZIP creation
- ZIP extraction
- archive conflict handling
- archive cancellation
- Recycle Bin listing
- Recycle Bin restore
- Recycle Bin permanent delete
- Settings save and persistence after restart
- Settings import/export
- Ferry -> Explorer same-volume Move
- Ctrl+drag Copy
- cancelled drag/drop
- Ferry -> Ferry drag/drop
- Explorer -> Ferry drag/drop
- general runtime responsiveness / no observed hang or crash during the final integration pass

## Explicitly unverified
- **Ferry -> Explorer Move across different volumes/drives** remains **UNVERIFIED**.

This was already a known v1.1.2 interoperability validation gap and was not introduced by the refactoring. The v1.1.2 safety contract remains unchanged: source cleanup after external drag/drop is allowed only when both WPF's final drop effect and Shell's Performed DropEffect indicate Move.

## Rename regression note
Phase 7-2 (`RenameEngine.Execution.cs` split) caused a reproducible severe responsiveness/stability regression in batch Rename/Undo testing despite textually unchanged method bodies. That split was fully reverted. After restoring the original `RenameEngine.cs` blob, the same two-file batch Rename and Undo returned to essentially immediate response. The Rename engine remains structurally frozen in the final refactoring state.

## Conclusion
**FINAL INTEGRATION PASS.**

The behavior-preserving refactoring branch is accepted on Windows for all tested v1.1.2 behavior. The only remaining validation gap is the pre-existing cross-volume Ferry -> Explorer Move scenario, explicitly recorded as UNVERIFIED rather than PASS or FAIL.
