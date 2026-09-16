# Ferry v1.1.0 RC15

RC15 is a focused keyboard-navigation correction candidate based on direct Windows Explorer comparison.

## Fixed

- Ordinary Up/Down now synchronizes Ferry's visible/logical Selection Anchor to the item reached by keyboard navigation, so a later Shift+Arrow range starts from the current keyboard row rather than an older mouse-click row.
- After clearing selection by clicking true background, Shift+Up/Shift+Down now resumes from the preserved keyboard origin instead of entering the WPF Selector edge-jump path.
- The Explorer-observed sequence `A -> clear on true background -> Shift+Down` now targets the A/B range.

## Unchanged by design

- Rubber-band selection state machine.
- D&D routing.
- List/Grid synchronization architecture.
- Autoscroll behavior and speed setting.
- Breadcrumb / Location Box / toolbar / Sidebar behavior.

## Candidate metadata

- Window title: `Ferry - RC 15`
- InformationalVersion: `1.1.0-rc15`
- Portable package label: `1.1.0-rc15`
