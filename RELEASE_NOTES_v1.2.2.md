# Ferry v1.2.2

Ferry v1.2.2 makes Copy → Paste into the same folder behave like a deliberate duplication instead of failing on the source/destination name collision.

## Added

- A single same-folder COPY automatically creates a numbered sibling: `report.txt` → `report (1).txt`, then `report (2).txt` as needed. The extension remains at the end; directories use the same numbering.
- When **two or more** clipboard items would be duplicated in that same folder, Ferry asks whether to create duplicates, with **No** as the default. Choosing No performs no copy.
- Names are planned against all current files and folders and reserved across the batch. The Windows Shell performs the actual copy with explicitly mapped destinations and collision protection.
- The existing Paste-result selection highlights the newly created siblings.

## Preserved

- Cut → Paste is unchanged.
- Copy → Paste from another folder retains existing Windows Shell behavior and conflict handling.
- The clipboard remains reusable after a copy, and original items are not overwritten or deleted.
- To-Do and other v1.2.1 features are unchanged.

## Validation

- Windows GitHub Actions successfully built Ferry and executed the duplicate regression suite against the actual built Ferry.exe.
- Covered mixed file/directory batches, existing numbered copies, repeated sources, unchanged originals, source overwrite rejection, and external-source classification.
