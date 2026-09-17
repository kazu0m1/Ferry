# Phase 7-3 Static Validation — SettingsWindow persistence partial

## Scope
Move SettingsWindow persistence/state-transfer responsibilities from `SettingsWindow.cs` into `SettingsWindow.Persistence.cs` without changing behavior.

## Baseline
- Branch: `refactor/v1.1.2`
- Windows-validated Phase 7-2 baseline: `31bede2be1b7588cfce88029648073dc0df409c8`
- Phase 7-3 implementation head: `b942132449e17d9b0f93dc3b306fa032392b7182`

## Final diff from validated baseline
- `Build.cmd`: +1 / -0
- `Source/Ferry/Ferry.csproj`: +1 / -0
- `Source/Ferry/SettingsWindow.Persistence.cs`: +27 / -0
- `Source/Ferry/SettingsWindow.cs`: +1 / -13

## Boundary validation
Moved unchanged into `SettingsWindow.Persistence.cs`:
- `SaveSettings`
- `ExportSettings`
- `ImportSettings`
- `ResetSettings`
- `ApplyFields`
- `LoadFields`
- `Clone`
- `CopyInto`

Kept in `SettingsWindow.cs`:
- constructor
- `BuildUi`
- About/version display
- UI helper methods
- `BrowseHome`

`SettingsWindow` is now a partial class. No settings defaults, validation bounds, import/export format, UI layout, event wiring, or persistence behavior was intentionally changed.

## Build wiring
`SettingsWindow.Persistence.cs` is listed once in both:
- `Build.cmd`
- `Source/Ferry/Ferry.csproj`

## Rename performance observation isolation
Phase 7-2's reported temporary rename/Undo slowdown remains documented separately. Phase 7-3 does not touch Rename code, so it does not confound that observation.

## Result
**PASS — static validation.**

## Windows gate
1. `Build.cmd` succeeds.
2. Ferry launches normally.
3. Open Settings and change one simple setting; Save succeeds.
4. Close/relaunch Ferry and confirm the saved setting persists.
5. Export settings to JSON.
6. Import the exported JSON and confirm fields reload normally.
7. Reset settings and confirm the UI fields return to defaults before cancelling or saving as desired.
8. Home-folder Browse still opens normally.
