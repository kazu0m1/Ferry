# Ferry v1.0.2 Rubber Band Integration Prototype 3 — Static Validation

**Overall: PASS**

## Checks
- [x] Ctrl XOR policy present: **PASS**
- [x] Shift UNION policy present: **PASS**
- [x] Selection snapshot present: **PASS**
- [x] Pass-through pending present: **PASS**
- [x] System drag threshold retained: **PASS**
- [x] Mouse capture on active band: **PASS**
- [x] True background regression fix retained: **PASS**
- [x] Ctrl+Shift intentionally excluded: **PASS**
- [x] Brace count balanced (coarse): **PASS**
- [x] Paren count balanced (coarse): **PASS**
- [x] Prototype3 informational version: **PASS**

## Scope
- Baseline: accepted Rubber Band Prototype 2.
- Added: Ctrl rubber-band = initial-selection XOR current hit set.
- Added: Shift rubber-band = initial-selection UNION current hit set.
- Stationary Ctrl/Shift clicks remain on native WPF Extended-selection path until the drag threshold is crossed.
- Ctrl+Shift and auto-scroll remain intentionally out of scope.

## Environment limitation
This environment cannot execute the Windows .NET Framework 4.8 / WPF build. `Build.cmd` plus the Windows checklist remains the runtime gate.
