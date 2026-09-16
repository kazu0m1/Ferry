# Ferry Rubber-band — Development History (Condensed)

This is a concise maintenance history. Detailed per-prototype test/static documents are archived under `docs/dev-history/rubber-band/prototypes/`.

| Prototype | Main result |
|---|---|
| 1 | Basic normal rubber-band proved viable; true-background clear regression discovered. |
| 2 | True-background clear restored. |
| 3 / 3R | Ctrl XOR and Shift behavior added; native Shift-anchor mismatch isolated. |
| 4–5 | Alternative routing experiments rejected; regressions increased. |
| 6 | WPF internal selection click path improved Shift anchor but rare miss remained. |
| 7–8 | Alternative anchor timing/order experiments rejected; worse responsiveness. |
| 9 | Directly synchronized Extended-selection anchor; Shift-anchor miss eliminated. |
| 10 | First autoscroll implementation; WPF autoscroll/focus competition exposed. |
| 11 | Mouse capture moved to neutral container; autoscroll stabilized. |
| 12–14 | Ctrl+Shift semantics iterated; Prototype 14 reached all-clear for core selection behavior. |
| 15–20 | Mid-drag modifier semantics and autoscroll speed refined; max speed ultimately set to 100. |
| 21 | Ctrl+Shift true-background semantics completed and passed. |
| 22 | Grid autoscroll completed and passed. |
| 23 | List⇄Grid exact selection/anchor synchronization completed and passed. |
| 24 | ScrollBar rapid-click double-open guard completed and passed. |
| 25 | Keyboard focus/current synchronized after Ferry-owned stationary whitespace click; arrow-key extreme-jump bug fixed. |
| 26 | WPF focus dashed visualization attempted and rejected. |
| 27 | Logical Shift Selection Anchor dashed visualization implemented and passed. |

## Final baseline

Prototype 27 is the frozen development baseline before release promotion/documentation version assignment.
