# Ferry Rubber-band — Final Validation Summary

**Validated baseline:** Prototype 27  
**Platform:** Windows real-machine testing  
**Status:** PASS; incorporated into Ferry v1.1.0 final baseline

## 1. Final regression result

The comprehensive final regression checklist completed with **PASS**. Core areas verified included:

- true-background clearing;
- single/Ctrl/Shift selection and Shift anchor;
- D&D/rubber-band routing boundaries;
- normal/Ctrl/Shift/Ctrl+Shift rubber-band behavior;
- mid-drag modifier behavior;
- List and Grid autoscroll;
- List⇄Grid selection synchronization;
- 10,000-item virtualization/performance smoke testing.

Two mid-drag modifier subcases were skipped in the final all-in-one checklist because their test procedure was not restated clearly there; those behaviors had already passed dedicated Prototype 18 validation and were retained unchanged afterward.

## 2. Large-folder results

Generated test folders were used through 10,000 items.

- Normal browsing/selection did not break down in ordinary use.
- Long rubber-band autoscroll was tested approximately 1→500→250→1000 items without corruption.
- `Ctrl+A` followed by Ctrl-rubber-band remained semantically stable. UI fluidity decreased, but not to a level perceived as a frozen application.
- 5,000-item end-to-end Shift range: about 2–3 seconds.
- 10,000-item end-to-end Shift range: about 10 seconds after reboot in the final observed run; earlier observation was about 16 seconds.
- `Ctrl+A`: effectively immediate.

The full-range Shift case is retained as a known extreme-case performance characteristic, not a blocker.

## 3. Issues found during finish phase and closure

### Prototype 23 — List/Grid selection synchronization

Problem: switching from List to Grid after selecting 1,000 items could expose a stale Grid selection (1,026 items observed).

Resolution: snapshot the active view selection and reconcile the destination Selector to the exact same set; carry the logical Shift anchor as well.

Result: PASS.

### Prototype 24 — ScrollBar rapid-click double-open

Problem: rapid ScrollBar clicks bubbled as `MouseDoubleClick` through the Selector and opened the currently selected item.

Resolution: open only when the double-click originates from a valid item container/open target; exclude ScrollBar/view chrome/true background.

Result: PASS.

### Prototype 25 — keyboard navigation after Ferry-owned whitespace click

Problem: visual selection could move to an item while keyboard focus/current remained on the Selector, causing arrow-key navigation in a 10,000-item view to pause and jump to the first/last item.

Resolution: Ferry-owned stationary click commits keyboard focus/current to the clicked item container in addition to selection/anchor state.

Result: PASS.

### Prototype 26 — rejected focus visualization

Attempt: show WPF keyboard focus/current with a dashed focus visual.

Reason rejected: this moved with arrow-key current selection and did not visualize the state that determines later `Shift+Click` range origin.

### Prototype 27 — logical Selection Anchor visualization

Resolution: remove Prototype 26 focus visual and draw one non-hit-testable dashed rectangle over Ferry's logical `SelectionAnchorItem` when realized.

The Prototype 27 test established that Ferry could visualize a logical range anchor independently of WPF keyboard focus. That historical prototype initially kept the anchor fixed during ordinary Arrow navigation.

**Final v1.1.0 refinement:** Windows Explorer comparison during RC validation showed that an unmodified Arrow move should establish the newly focused/selected item as the new range anchor. RC16 adopted that behavior; RC17 extended the same navigation recovery to Grid directionality. A true-background clear can leave selection empty while retaining the last keyboard origin, so the next Arrow / Shift+Arrow resumes locally rather than jumping to a viewport end.

Result: PASS in the final RC20 baseline.

## 4. Autoscroll

- Ferry-owned neutral-container mouse capture prevents WPF ListBox/ListView autoscroll/focus-follow competition.
- List and Grid both autoscroll correctly.
- Newly revealed items are selectable.
- Reverse direction is stable.
- No top/bottom warp, focus-row flicker, or virtualization-driven mass deselection observed.
- RC3 speed is configurable in Settings: **30–300**, default **100**. Value 100 preserves the previously validated behavior; RC3 scales the curve for lower/higher values so the setting is perceptible without changing the default.

## 5. Freeze recommendation

Prototype 27 remains the principal rubber-band architecture baseline, while RC11–RC20 are part of the final v1.1.0 interaction baseline for keyboard navigation, Grid directional recovery, List true-background geometry, selection-fill clipping, and gutter polish.

RC20 passed the final Windows checks and is the frozen application-behavior baseline promoted to Ferry v1.1.0 final. The normative current behavior is documented in `RELEASE_NOTES_v1.1.0.md`, `Ferry_SPEC_v1.1.md`, and `RUBBER_BAND_SELECTION_SPEC_JA.md`.
