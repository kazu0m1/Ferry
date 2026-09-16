# Ferry v1.1.1

Ferry v1.1.1 adds Explorer-style **Paste result feedback** while preserving the validated v1.1.0 selection and navigation behavior.

## What's new

- After **Copy/Cut → Paste**, the top-level destination items associated with the current Paste remain selected, making it immediately clear what was pasted.
- Paste result selection is synchronized between **List** and **Grid** and survives the final incremental refresh.
- A second Paste replaces the previous Paste result selection instead of accumulating stale items.
- Overwrite / folder-merge results are reflected at the destination-item level when Windows completes the operation.

## Windows-owned file operations remain Windows-owned

Ferry still delegates Copy/Move execution and conflict handling to Windows `SHFileOperation`. v1.1.1 does **not** add a custom copy engine, queue, conflict dialog, or same-folder duplicate naming policy. In the tested environment, same-folder Paste displayed the standard Windows conflict dialog and was skipped/cancelled rather than creating a duplicate name.

## Preserved from v1.1.0

- Explorer-style rubber-band selection in List / Grid
- Ctrl / Shift / Ctrl+Shift marquee behavior
- Selection Anchor and keyboard range behavior
- True-background selection clearing
- List / Grid selection synchronization
- File D&D coexistence and edge autoscroll
- Breadcrumb / Location Box and toolbar behavior

## Platform / distribution

- Windows 11
- .NET Framework 4.8 / WPF
- Portable ZIP
- No installer required
- No telemetry
- Code signing is not included in this release
