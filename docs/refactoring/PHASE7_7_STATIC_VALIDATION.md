# Phase 7-7 Static Validation — ChoiceDialog standard filename

## Scope
Pure filename rename of the generic ChoiceDialog partial:
- `Source/Ferry/ChoiceDialog.cs`
- → `Source/Ferry/ChoiceDialog.Standard.cs`

No method bodies, event wiring, dialog behavior, or class signatures were changed.

## Baseline
- Windows-validated Phase 7-6 record: `410c8d7d8b2132298d66d7be85402cdbfdf14044`

## Implementation
- Implementation head: `47eff11bebe05623331d1ee1dd2125d6a7fb1659`

## Static checks
- GitHub compare recognizes `ChoiceDialog.cs` → `ChoiceDialog.Standard.cs` as a rename with **0 additions / 0 deletions**.
- `Build.cmd`: one source-path replacement only.
- `Ferry.csproj`: one `<Compile Include>` path replacement only.
- `ChoiceDialog.ArchiveConflict.cs` unchanged.
- `ChoiceDialogResult.cs` unchanged.
- No version, title, release metadata, generated binaries, logs, or user settings changed.
- C# source count remains 47; Build.cmd and Ferry.csproj source lists remain aligned.

## Result
**PASS — static validation complete.**
