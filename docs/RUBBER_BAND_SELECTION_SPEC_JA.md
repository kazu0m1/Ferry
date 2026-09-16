# Ferry Rubber-band / Selection Interaction Specification — Frozen Candidate

**Status:** frozen for Ferry v1.1.0 final  
**Baseline:** Prototype 27 semantics + accepted v1.1.0 UX refinements  
**Scope:** List / Grid selection, Shift anchor, D&D boundary, rubber-band, modifier semantics, autoscroll, view synchronization, keyboard-current/focus integration  
**Note:** This document defines the detailed Ferry v1.1.0 selection behavior. The historical v1.0.2 release record remains unchanged.

---

## 1. State model

Ferry treats the following as separate concepts.

- **Selected set** — items currently selected.
- **Keyboard current / keyboard focus** — item from which arrow-key navigation proceeds.
- **Selection Anchor** — logical origin used by subsequent `Shift+Click` range selection.
- **Rubber-band origin** — mouse-down point from which the active rectangle is measured.
- **Rubber-band hit set** — items whose selection geometry currently intersects the rectangle.

Selection Anchor and keyboard current are distinct concepts, but final v1.1.0 deliberately synchronizes them after an unmodified Arrow move: the newly focused/selected item becomes the new logical Selection Anchor. A true-background clear can leave selection empty while retaining a keyboard origin; the next Arrow or Shift+Arrow resumes from that origin.

The logical Selection Anchor is visualized with a 1 px dark-gray (`#707070`) dashed rectangle when its realized container is visible. Ferry v1.1.0 uses the original Prototype 27 `1,1` dash rhythm with the softer dark-gray stroke accepted during RC polish. Offscreen/virtualized anchors do not force realization or scrolling.

---

## 2. Gesture routing

### 2.1 True background

- Stationary normal click clears selection.
- Drag beyond the system drag threshold begins rubber-band selection.
- `Ctrl+Shift` from true background uses snapshot-XOR semantics described below.

### 2.2 Unselected item whitespace

- Stationary click selects that item and updates keyboard focus/current and logical Selection Anchor.
- Drag beyond threshold begins rubber-band selection.
- This applies to List row whitespace and Grid tile whitespace.

### 2.3 File hot zone

Icon/name/content hot zones are action/D&D zones, not rubber-band start zones.

### 2.4 Already-selected item

Dragging an already-selected item remains file D&D and preserves the selected set. Rubber-band must not steal this gesture.

### 2.5 View chrome

Scroll bars, thumbs, tracks, arrows, column headers and equivalent view chrome are excluded from file-open/rubber-band routing as appropriate.

---

## 3. Drag threshold and rectangle lifecycle

- Rubber-band remains pending until WPF system drag threshold is crossed.
- At activation, the rectangle starts from the original mouse-down point.
- All four drag directions are supported.
- Shrinking/reversing the rectangle updates the current selection; prior hit history is not treated as permanent selection history unless modifier semantics explicitly require it.
- Mouse capture is held by a neutral tab/container owner rather than the ListView/ListBox itself, preventing WPF built-in selection/autoscroll from competing with Ferry.
- MouseUp inside or outside the window ends the gesture and removes the rectangle immediately.
- The visual overlay is non-hit-testable.

---

## 4. Selection geometry

Ferry conceptually separates:

- **Action bounds** — icon/name/content area used for item action/D&D routing.
- **Row/tile context** — identifies which item owns whitespace under the pointer.
- **Selection bounds** — geometry used for rubber-band intersection.

### List

The selection geometry is row-wide across the file view. A minimal overlap is sufficient to select the row.

### Grid

The tile selection geometry includes the tile whitespace used by Explorer-like marquee interaction, not just icon/text pixels.

---

## 5. Normal / Ctrl / Shift rubber-band

Let:

- `I` = selection snapshot at gesture start.
- `R` = current rectangle hit set.

### Normal start

Active result is the current hit set. Conceptually:

`Selected = R`

### Ctrl start

While Ctrl remains in the initial XOR mode:

`Selected = I XOR R`

If Ctrl is released during an active Ctrl-started gesture, the current result is not recomputed immediately. Subsequent item boundary enter/leave changes continue in Ferry's transition mode. Re-pressing Ctrl does not restore the original XOR mode for that gesture.

### Shift start

A Shift-started **active rubber-band** behaves like normal rubber-band rather than `Shift+Click` union/range semantics. The Shift key matters for the stationary click path, not as a permanent union operator for the active rectangle.

Thus, with prior A/C and current rectangle B/C, active Shift rubber-band yields B/C.

### Normal start then modifier changes

If a gesture started with no modifier, pressing/releasing Ctrl or Shift after mouse-down does not convert the gesture into another selection mode. Boundary enter/leave continues with normal rubber-band behavior.

### Threshold-before/after modifier changes

The mouse-down modifier state is significant. Adding Ctrl/Shift after a normal mouse-down but before threshold does not retroactively change a normal-start gesture.

---

## 6. Ctrl+Shift rubber-band

Ctrl+Shift has two distinct start contexts.

### 6.1 Start from true background

The behavior is snapshot XOR with current hits:

`Selected = I XOR R`

This was verified with both one-item and multi-item initial selections.

### 6.2 Start from unselected item whitespace

This is direction/start-item/anchor dependent and is intentionally implemented as a dedicated policy rather than reduced to simple set algebra.

Reference three-row behavior with A as logical anchor:

#### B → C

- MouseDown/activation on B: A/B.
- Extend into C: A/B/C.
- MouseUp preserves A/B/C.

#### C → B

- MouseDown on C may show A/C.
- At active start: A/B/C.
- Enter B while moving upward: A/C (B toggled off).
- Return toward C: A/B/C.

### 6.3 Mid-drag partial release

Dedicated tested transition behavior is retained:

- B→C A/B/C, release Shift while holding Ctrl: immediate set remains A/B/C; subsequent boundary changes follow the verified transition policy.
- C→B A/C, release Ctrl while holding Shift: immediate set remains A/C; subsequent boundary changes follow the verified transition policy.

---

## 7. Stationary click, Shift anchor and keyboard navigation

Ferry-owned stationary whitespace clicks must synchronize all required state:

- selected item/set;
- keyboard focus/current item;
- WPF Extended-selection anchor where needed;
- Ferry logical Selection Anchor.

This prevents large virtualized views from jumping to the first/last item when the user presses an arrow key after clicking item whitespace.

Unmodified Arrow-key navigation moves current selection/focus and updates the logical Selection Anchor to the resulting item. Shift+Arrow keeps the applicable range origin while extending or contracting the range. After a true-background clear, Ferry retains the last keyboard origin so the first Arrow / Shift+Arrow resumes locally instead of jumping to a view extreme.

A normal click on a different item updates the logical Selection Anchor. Clicking one item within a multiple selection to collapse to that one item also updates the anchor.

---

## 8. Double-click/open guard

A double-click opens an item only when the double-click originates from an actual item container/action area appropriate for opening.

Double-click-like rapid clicks on:

- ScrollBar track;
- ScrollBar thumb;
- ScrollBar arrows;
- column header;
- true background;

must not open the currently selected item merely because the event bubbles through the Selector.

---

## 9. Autoscroll

Rubber-band autoscroll is Ferry-owned for both List and Grid.

- Edge proximity starts slow scrolling.
- Moving farther outside increases speed.
- Speed is user-configurable in Settings: **30–300**, default **100**. Value 100 preserves the validated curve; other values scale the curve and cap proportionally so the change is perceptible at the same pointer depth.
- Timer interval: 25 ms in the current implementation.
- Returning the pointer toward the central view stops autoscroll.
- Newly revealed items/tiles participate in selection.
- Offscreen virtualization alone must not clear selected items.
- Direction reversal must not cause a jump to top/bottom or focus-driven flicker.

List and Grid use their appropriate scroll-unit handling; Grid pixel offsets are not multiplied by List row-height logic.

---

## 10. List / Grid synchronization

List and Grid are separate WPF selectors even though they share the same logical file collection.

Before switching view, Ferry snapshots the active selector's exact selected item set and logical Selection Anchor. The destination selector is reconciled to that same set/anchor.

This must remain correct for large selections (validated at about 1,000 selected items in a 10,000-item folder).

---

## 11. Performance / virtualization acceptance

Validated up to 10,000 generated items without normal-use breakdown.

Known characteristics:

- 5,000-item full `Shift+Click` range: about 2–3 seconds in testing.
- 10,000-item full `Shift+Click` range: about 10 seconds after reboot in the final observed run; an earlier run was about 16 seconds.
- `Ctrl+A` remains effectively immediate.
- `Ctrl+A` followed by Ctrl-rubber-band is visibly less fluid but does not present as a frozen application; selection semantics remain stable.
- Long-distance rubber-band autoscroll and reverse direction remained stable.

The extreme full-range Shift case is accepted as a known performance characteristic rather than a release blocker.

---

## 12. Frozen invariants

The following are release-blocking regressions for the Ferry v1.1.0 behavior:

- true-background click no longer clears selection;
- Shift anchor requires a second click to update;
- item-whitespace click leaves keyboard focus/current inconsistent and arrow navigation jumps to view extremes;
- selected-item drag becomes rubber-band;
- file hot-zone drag becomes rubber-band;
- ScrollBar rapid click opens selected item;
- List/Grid switch changes the selected set;
- autoscroll jumps to top/bottom, flickers from WPF focus-follow behavior, or clears virtualized selection;
- Ctrl/Ctrl+Shift state transitions differ from the validated behavior above.

---

## 13. Release/version note

Prototype 27 established the principal rubber-band development baseline. Subsequent v1.1.0 RC validation refined keyboard navigation, Grid directional recovery, and List true-background geometry. Ferry v1.1.0 is the first public release that ships the completed behavior. Historical v1.0.0–v1.0.2 release notes remain historically accurate.
