# Ferry v1.0.2 Rubber Band Integration Prototype 2 — Static Validation

**Baseline:** Rubber Band Integration Prototype 1 (Windows test: all Prototype 1 checklist items PASS)

## Scope
Prototype 2 fixes one regression discovered outside the Prototype 1 checklist: a stationary click on true view background could leave an existing selection committed, regardless of whether that selection had been made by rubber-band, Ctrl+Click, or Shift+Click.

The fix is intentionally narrow:
- true-background MouseDown clears all native Selector selection facets (`UnselectAll`, `SelectedItems`, `SelectedItem`, `SelectedIndex`) before focus/capture changes;
- a stationary true-background MouseUp reasserts the same empty-selection state;
- unselected row/tile whitespace click semantics and selected-item D&D remain unchanged.

## Source-diff guard
Relative to Prototype 1, functional source changes are limited to `Source/Ferry/MainWindow.cs`:
- true-background clear path in `HandleLeftMouseDown`;
- stationary true-background branch in `HandleLeftMouseUp`;
- new `ClearSelectionFromTrueBackground` helper.

`Source/Ferry/AssemblyInfo.cs` changes informational version only.

## Structural checks
- C# files under `Source/Ferry`: **29**
- All 29 C# files are referenced by `Build.cmd`: **PASS**
- `MainWindow.cs` raw delimiter balance:
  - `{` / `}` = **723 / 723**
  - `(` / `)` = **2457 / 2457**
  - `[` / `]` = **113 / 113**
- Exactly one definition each: `ClearSelectionFromTrueBackground`, `BeginRubberBandGesture`, `HandleRubberBandMouseMove`, `EndRubberBandGesture`: **PASS**
- AssemblyInformationalVersion: `1.0.2-rubberband-prototype2`

## Windows gate
Windows/.NET Framework/WPF runtime behavior cannot be executed in this environment. `Build.cmd` plus `RUBBER_BAND_PROTOTYPE2_TEST_JA.md` is the acceptance gate.
