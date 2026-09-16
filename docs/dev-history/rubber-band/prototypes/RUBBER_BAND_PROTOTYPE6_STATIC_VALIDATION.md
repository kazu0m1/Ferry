# Ferry v1.0.2 Rubber Band Integration Prototype 6 — Static Validation

**Baseline:** Prototype 3R (Prototype 3 application logic)

## Scope
Prototype 6 intentionally changes only the stationary unselected row/tile-whitespace click commit path needed to refresh WPF Extended-selection's Shift anchor.

Application-source differences from Prototype 3R:
- `Source/Ferry/MainWindow.cs`
  - title → `Ferry - prototype 6`
  - adds `CommitNativeExtendedSelectionClick(...)`
  - stationary pending item click uses that helper, with existing `SetSingleSelection(...)` as fallback
- `Source/Ferry/AssemblyInfo.cs`
  - informational version → `1.0.2-rubberband-prototype6`

No gesture-candidate rules, true-background clear rules, rubber-band Ctrl/Shift set algebra, D&D arming, or selection geometry were intentionally changed.

## Static checks
- Prototype 3R → Prototype 6 changed application source files exactly: `MainWindow.cs`, `AssemblyInfo.cs`: **PASS**
- Window title marker: **PASS**
- Native WPF Extended-selection click helper marker: **PASS**
- Existing fallback single-selection path retained: **PASS**
- true-background clear marker retained: **PASS**
- Ctrl XOR marker retained: **PASS**
- Shift UNION marker retained: **PASS**

## Runtime gate
This environment cannot execute Ferry's Windows/.NET Framework 4.8 WPF build. `Build.cmd` and the Windows test checklist remain the runtime acceptance gate.
