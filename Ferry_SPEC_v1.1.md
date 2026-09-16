# Ferry Specification v1.1

**Release baseline:** Ferry v1.1.1  
**Platform:** Windows 11 / .NET Framework 4.8 / WPF  
**Status:** Final release baseline

This document defines the v1.1 additions and behavioral changes relative to the historical v1.0/v1.0.2 baseline in `Ferry_SPEC_v1.0.md`. Requirements not changed here remain governed by that v1.0 baseline. Detailed selection semantics are frozen in `docs/RUBBER_BAND_SELECTION_SPEC_JA.md`.

## 1. Explorer-style selection

- List and Grid support rubber-band / marquee selection.
- Rubber-band activation begins only after the system drag threshold is crossed.
- Selection geometry is distinct from file action/D&D hot zones.
- Normal, Ctrl, Shift and Ctrl+Shift behavior follows the validated Prototype 27 interaction baseline.
- Mouse capture permits stable dragging and MouseUp outside the window.
- List and Grid selection state remains synchronized when switching views.
- Selection Anchor is a Ferry-owned logical range origin visualized with a 1px dark-gray (`#707070`) dashed rectangle; an unmodified Arrow move establishes the newly focused/selected item as the new anchor, matching the validated Explorer behavior.

## 2. Rubber-band autoscroll

- Edge autoscroll is supported in both List and Grid.
- Speed accelerates with pointer distance from the view edge.
- Settings exposes **Rubber-band autoscroll speed** from **30–300**, default **100**.
- Virtualized/offscreen items must not be dropped from selection merely because they are unrealized.

## 3. Keyboard-current and Shift anchor integration

- Ferry-owned stationary clicks synchronize keyboard focus/current with the clicked item.
- A true-background click may clear the selection while retaining the last keyboard origin; the next Arrow / Shift+Arrow continues from that origin instead of jumping to a viewport end.
- Unmodified Arrow navigation establishes the newly selected/focused item as the new Selection Anchor.
- Shift+Arrow extends/contracts from the validated keyboard range origin.
- Grid keyboard recovery respects visual Left / Right / Up / Down direction rather than treating vertical movement as simple index ±1.

## 4. Location Box and Breadcrumb

- `Ctrl+L` switches the path area to Location Box mode.
- `Esc` or pressing `Ctrl+L` again returns to Breadcrumb mode.
- Mouse interaction with the file view dismisses Location Box mode and restores Breadcrumb.
- The path/toolbar row uses a fixed layout height so switching modes does not resize the toolbar.
- Breadcrumb horizontal overflow uses a dedicated **10px** horizontal scrollbar.
- Its line-left / line-right buttons are always visible and use a darker gray than the thumb for discoverability.
- Final v1.1.0 uses rectangular scrollbar thumb and end-button chrome to remain visually consistent with the square toolbar controls.

## 5. Toolbar and Sidebar polish

- Primary icon toolbar buttons are fixed at **30×30**.
- Double-clicking the Sidebar/file-view divider auto-fits the Sidebar to its currently displayed labels, subject to the existing 50–480 width bounds.
- The resulting Sidebar width remains persisted through the existing settings mechanism.

## 6. Large-folder behavior

- Final regression covered normal use and marquee interaction with up to **10,000 items**.
- `Ctrl+A` remains effectively immediate in the tested environment.
- Very large Shift-range selection is a known performance characteristic rather than a correctness failure; approximately 10 seconds for a 10,000-item end-to-end Shift range was observed after restart in testing.

## 7. Paste result feedback

- After a successful Copy/Cut → Paste, Ferry selects the destination-level items associated with the current Paste so the user can immediately identify what was pasted.
- Selection targets are limited to the destination folder's top-level items; Ferry does not recursively select descendants.
- Paste result selection is synchronized between List and Grid and remains after the final incremental refresh.
- Each Paste operation owns a bounded per-tab feedback session. A later Paste replaces the previous operation result selection rather than accumulating stale items.
- Windows `SHFileOperation` remains authoritative for Copy/Move execution and conflict UI. Ferry does not add a custom copy engine or custom same-folder duplicate naming policy.
- If Windows skips or cancels a conflict and no destination item is produced/updated by that operation, Ferry must not invent a pasted result.

## 8. Release relationship

- `Ferry_SPEC_v1.0.md` remains the historical v1.0/v1.0.2 baseline and is not rewritten.
- This v1.1 specification plus `docs/RUBBER_BAND_SELECTION_SPEC_JA.md` defines the current v1.1.1 behavior.
- Historical Prototype and RC records are retained as development evidence and are not normative for later releases.
