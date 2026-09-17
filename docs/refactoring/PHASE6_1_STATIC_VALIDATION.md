# Phase 6-1 Static Validation — split SettingsStore from AppSettings

## Scope
Physically split the settings DTO from settings persistence without changing behavior.

Moved unchanged from `AppSettings.cs` to new `SettingsStore.cs`:
- `SettingsStore.SettingsDirectory`
- `SettingsStore.SettingsPath`
- `SettingsStore.Load()`
- `SettingsStore.Save()`
- `SettingsStore.Export()`
- `SettingsStore.Import()`
- `SettingsStore.Normalize()`
- `SettingsStore.PrettyJson()`

`AppSettings` itself, its constructor defaults, comments, and original using directives remain unchanged.

## Implementation
- Implementation commit: `41a0b3df0b18cdd783f41e8fd97b43511ec26fd9`
- `Build.cmd`: +1 source entry for `SettingsStore.cs`
- `Ferry.csproj`: +1 `<Compile>` entry for `SettingsStore.cs`
- `AppSettings.cs`: +0 / -118
- `SettingsStore.cs`: new file, +126

## Static result
**PASS**

Confirmed:
- No behavior edits or cleanup were combined with the move.
- UTF-8 BOM on `AppSettings.cs` is preserved.
- Existing `AppSettings.cs` using directives are preserved even where now redundant.
- `SettingsStore` method bodies and comments are unchanged.
- Build/project source lists both include `SettingsStore.cs` exactly once.
- `main` and the v1.1.2 tag were not touched.

## Windows gate
1. `Build.cmd` succeeds.
2. Ferry launches normally from `Portable\Ferry.exe`.
3. Settings dialog opens and saves a changed setting.
4. Close/relaunch Ferry and confirm the setting persists.
5. If convenient, Export/Import settings still work.
