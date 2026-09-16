# Ferry v1.1.0 RC11

RC11 is a focused regression-fix candidate discovered during final screenshot preparation after the v1.1.0 release candidate had otherwise been accepted.

## Symptom

After selecting an item, a plain click on true background clears the visible selection. In one remaining route, Ferry then left keyboard focus on the ListView/ListBox itself. The next arrow-key navigation could start from the Selector route instead of the previously focused item and jump to a distant list edge.

## Change

- On plain true-background MouseDown, Ferry records the currently focused item if it belongs to the active selector.
- The pending gesture still focuses the Selector exactly as before so real rubber-band drags keep the validated behavior.
- On MouseUp, **only when the gesture stayed stationary**, Ferry restores keyboard focus/current to that previously focused realized item.
- Visible selection remains empty after the background click.
- No `ScrollIntoView` is used, so the fix does not deliberately move the viewport or realize distant containers.
- Rubber-band selection semantics, Selection Anchor, List/Grid synchronization, D&D, autoscroll, Breadcrumb/Location Box, sidebar, and toolbar layout are unchanged.

## Version

- Window title: `Ferry - RC 11`
- AssemblyVersion/FileVersion: `1.1.0.0`
- InformationalVersion: `1.1.0-rc11`

Use `V1.1.0_RC11_TEST_JA.md` for Windows validation.
