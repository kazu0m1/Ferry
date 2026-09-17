# Phase 6-3 Static Validation — SearchRequest split

## Scope
Behavior-preserving physical split of the `SearchRequest` data type from `SearchService.cs` into `SearchRequest.cs`.

## Baseline
- Branch: `refactor/v1.1.2`
- Baseline commit: `04f7aaecb1211adea8332ca3f8b0dab612130f7d`
- Implementation commit: `eb592acee2816a71e65bbbfef8817d3659ccc519`

## Diff audit
Compared baseline to implementation:
- `Build.cmd`: +1 / -0
- `Source/Ferry/Ferry.csproj`: +1 / -0
- `Source/Ferry/SearchRequest.cs`: +10 / -0
- `Source/Ferry/SearchService.cs`: +0 / -8

The `SearchService.cs` patch removes only the existing `SearchRequest` declaration. `SearchAsync`, Windows Search/index logic, direct filesystem search, Japanese width-insensitive matching, wildcard matching, cancellation, deduplication, and hidden-file handling are unchanged.

## Build-source invariant
- New source file: `Source/Ferry/SearchRequest.cs`
- Listed once in `Build.cmd`.
- Listed once in `Source/Ferry/Ferry.csproj`.
- Expected C# source count: 38.

## Protected behavior not changed
- Search result matching semantics.
- `StartsWith` and contains modes.
- Compatibility-width normalization and Japanese width-insensitive comparison.
- Wildcard handling.
- Windows Search fallback to direct traversal.
- Cancellation behavior.
- MainWindow selection, refresh, Paste feedback, D&D, keyboard, Sidebar, Archive and Recycle Bin behavior.

## Static result
**PASS.**

## Windows validation gate
1. `Build.cmd` succeeds.
2. Ferry launches normally.
3. Search for a simple filename fragment and confirm results appear.
4. Verify a `StartsWith` search.
5. Verify at least one Japanese full-width/half-width insensitive search if convenient.
6. Clear/cancel search and return to the normal folder view.
