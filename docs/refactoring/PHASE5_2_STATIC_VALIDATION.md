# Phase 5-2 Static Validation — MainWindow converter isolation

## Scope
Physical relocation of `InverseBooleanToVisibilityConverter` from `MainWindow.Types.cs` into a dedicated `MainWindow.Converters.cs` partial.

## Baseline
- Branch: `refactor/v1.1.2`
- Parent: `029b1bd2b7e84472304bbad47de7f9a87acca2dd`
- Implementation commit: `a58118a9cce231dccf0f1fc2eb1b80ffadef998a`
- Validation date: 2026-09-17

## Static checks
- Only four files changed.
- `Build.cmd`: +1 source entry for `MainWindow.Converters.cs`.
- `Ferry.csproj`: +1 Compile entry for `MainWindow.Converters.cs`.
- `MainWindow.Converters.cs`: added with the converter body unchanged.
- `MainWindow.Types.cs`: removed only the same converter definition.
- `MainWindow.cs` unchanged.
- No selection, rubber-band, keyboard, D&D, paste, archive, navigation, or settings behavior changed.

## Result
**PASS — static structural validation.**

Windows build/runtime smoke validation remains required.
