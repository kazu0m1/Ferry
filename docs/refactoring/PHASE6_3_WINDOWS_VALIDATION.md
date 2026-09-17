# Phase 6-3 Windows Validation — SearchRequest split

## Scope
Windows build/runtime validation after moving only `SearchRequest` from `SearchService.cs` to `SearchRequest.cs`.

## Baseline
- Branch: `refactor/v1.1.2`
- Phase 6-3 implementation commit: `eb592acee2816a71e65bbbfef8817d3659ccc519`
- Static validation commit: `a4f4ae6976be0fb40a38d43e88081bda0dc319a2`
- Validation date: 2026-09-17

## Result
**PASS — Windows build/runtime validation.**

Confirmed:
- `Build.cmd` succeeds.
- Ferry launches normally.
- Normal text search returns results.
- `StartsWith` search works.
- Japanese width-insensitive search remains functional.
- Clearing search returns to the normal folder view.

## Notes
Phase 6-3 moved only the `SearchRequest` data type. Search execution, Windows Search integration, direct traversal, wildcard matching, and Japanese width-insensitive comparison were not changed.
