# Phase 1 Windows Validation

**Branch:** `refactor/v1.1.2`  
**Phase 1 implementation commit:** `1b0a03d7caf7562bfd4c5831f9746be1498b44fd`  
**Validation date:** 2026-09-16

## Result

**PASS**

The Windows 11 runtime gate for Phase 1 was completed on the user's actual environment after updating to the refactoring branch.

Confirmed by the user:

- `Build.cmd` completed without errors.
- The generated Ferry application launched successfully.
- Ferry operated normally in the smoke test after the nested-type partial-class split.

No runtime regression was reported.

## Scope of this gate

Phase 1 changed structure only:

- `MainWindow` became a partial class.
- The five nested helper/state types were moved unchanged to `MainWindow.Types.cs`.
- `Build.cmd` and `Ferry.csproj` were updated to compile the new source file.

No executable method bodies, event registration, selection semantics, D&D semantics, refresh logic, Paste feedback logic, or Shell boundaries were changed.

## Decision

Phase 1 is accepted as the validated refactoring baseline. Phase 2 may proceed from this branch head using the same rule: physical relocation first, no behavior cleanup in the same batch.
