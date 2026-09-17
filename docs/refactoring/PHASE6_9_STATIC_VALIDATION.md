# Phase 6-9 Static Validation — Create ZIP setup window split

## Scope
Move only `CreateZipSetupWindow` out of `ArchiveSetupWindows.cs` into `CreateZipSetupWindow.cs`.

## Baseline
- Branch: `refactor/v1.1.2`
- Windows-validated Phase 6-8 baseline: `194c78b4ba648c927bd677c61f126bbbacba3e54`
- Phase 6-9 final implementation head: `ab40b7c4f504a9f83f24cfc66b24e842cf974a00`

## Final diff from validated baseline
- `Build.cmd`: +1 / -0
- `Source/Ferry/ArchiveSetupWindows.cs`: +0 / -150
- `Source/Ferry/CreateZipSetupWindow.cs`: +159 / -0
- `Source/Ferry/Ferry.csproj`: +1 / -0

## Boundary validation
Moved unchanged:
- `CreateZipSetupWindow`
- destination ZIP browsing setup
- selected-source validation
- destination extension validation
- Create ZIP setup UI helpers local to the class

Kept in `ArchiveSetupWindows.cs` unchanged:
- `ExtractZipSetupWindow`
- extraction destination browsing
- ZIP existence validation
- extraction setup UI helpers local to the class

No archive execution, compression/extraction implementation, progress, cancellation, safety thresholds, conflict handling, result handling, Selection, D&D, Paste feedback, Search, Settings, Recycle Bin, or Shell behavior was changed.

## Build wiring
`CreateZipSetupWindow.cs` is listed once in both:
- `Build.cmd`
- `Source/Ferry/Ferry.csproj`

Expected C# source count after Phase 6-9: 43.

## Result
**PASS — static validation.**

## Windows gate
1. `Build.cmd` succeeds.
2. Ferry launches normally.
3. Select one or more items and open Create ZIP.
4. Confirm the selected-item list, destination field, Browse button, Cancel, and Start behave normally.
5. Create a small ZIP successfully.
6. Open Extract ZIP once and confirm its setup window still opens and behaves normally.