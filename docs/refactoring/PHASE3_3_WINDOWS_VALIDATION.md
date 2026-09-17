# Ferry v1.1.2 Refactoring — Phase 3-3 Windows Validation

## Result

**PASS**

Validated on Windows after pulling `refactor/v1.1.2` through implementation commit `cdc6ed9938e5923d59508faf1a55550982f1fe26` and static-validation commit `595b5b066bc46313020a7a7c2b7bcda4e03e81d0`.

Confirmed:

- `Build.cmd` completed successfully.
- Ferry launched normally from `Portable\Ferry.exe`.
- Ordinary Sidebar navigation worked.
- `Pin to Sidebar` added a folder successfully.
- Pinned folder navigation worked.
- Pinned folders could be reordered by drag-and-drop.
- Dragging an external folder onto the Sidebar pinned it successfully.
- `Unpin` removed a pinned folder successfully.

This closes the Phase 3-3 Windows gate and permits completion of the Sidebar extraction.
