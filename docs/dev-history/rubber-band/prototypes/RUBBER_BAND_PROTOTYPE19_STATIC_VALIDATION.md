# Ferry v1.0.2 Rubber Band Integration Prototype 19 — Static Validation

## Baseline
- Direct baseline: Prototype 18.
- Prototype 18 selection/modifier tests were reported PASS.

## Intended source changes
- `Source/Ferry/MainWindow.cs`
  - window title: `Ferry - prototype 19`
  - rubber-band autoscroll speed curve only
  - max speed remains 80 lines/sec
  - 0–30px edge-depth behavior remains the Prototype 18 curve
  - beyond 30px, acceleration slope is increased so the ceiling becomes reachable at ordinary pointer distances
- `Source/Ferry/AssemblyInfo.cs`
  - informational version only

## Explicitly unchanged
- rubber-band gesture routing
- Normal / Ctrl / Shift / Ctrl+Shift selection policies
- mid-drag modifier state logic
- Shift anchor handling
- D&D routing
- mouse capture strategy
- autoscroll timer interval (25 ms)
- edge zone (30 px)
- per-tick maximum steps (3)
- F5/refresh

## Curve sanity values
- depth 0px: 2.5 lines/sec
- depth 30px: 8.5 lines/sec
- depth 60px: 22.5 lines/sec
- depth 120px: 50.5 lines/sec
- depth 180px: 78.5 lines/sec
- ceiling 80 lines/sec at about 183px

No Windows runtime is available in this environment; final behavior requires the user's Windows real-machine test.
