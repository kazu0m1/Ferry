# Ferry v1.0.2

Ferry v1.0.2 replaces the previous Windows-delegated ZIP workflow with a Ferry-owned ZIP create/extract workflow built on .NET `System.IO.Compression`.

The application logic in this final release is unchanged from the Windows-accepted v1.0.2-rc1. Final promotion changes release/version metadata, public documentation, and packaging metadata only.

## Highlights

### Accurate ZIP progress inside Ferry

- ZIP compression and extraction run asynchronously without blocking normal Ferry browsing.
- The setup window closes after **Start**; progress moves to Ferry's bottom status area.
- A single neutral-gray overall progress bar is shown.
- Ferry reports processed/total data, file count, transfer speed, and estimated remaining time when available.
- **Cancel** is available while an archive job is running.
- One archive job may run per Ferry window at a time; normal folder/tab/search operations remain usable.

### Ferry-owned conflict handling

Folder conflicts offer:

- **MERGE** — extract into the existing folder.
- **KEEP BOTH** — create a collision-safe sibling such as `Photos(1)`.
- **SKIP** — skip the folder and everything below it.
- **CANCEL** — stop extraction.

File conflicts offer:

- **REPLACE** — replace the existing file.
- **KEEP BOTH** — extract as a collision-safe name such as `photo(1).jpg`.
- **SKIP** — skip the file.
- **CANCEL** — stop extraction.

When a folder is **MERGE**d, the first file conflict below that merged folder can optionally apply the selected file decision to all remaining file conflicts under that merged folder only. A different merged folder asks again.

### Archive safety

Ferry validates ZIP entries before/during extraction and blocks unsafe path behavior, including attempts to escape the selected destination. It also protects against Windows-specific unsafe names/path forms covered by the archive safety tests.

Resource-heavy archives trigger a warning rather than an unconditional block. The current warning defaults are:

- expanded data greater than **20 GiB**;
- more than **50,000 files**;
- compression ratio greater than **100×**.

The warning shows the measured values and lets the user choose **YES** to continue or **NO** to cancel.

Extraction writes file contents through temporary files before finalizing them. Cancellation or failure does not leave an incomplete file under its final filename. Completed files from a partially completed extraction may remain and are reported to the user.

### No external archive-tool dependency for ZIP work

Ferry no longer invokes the Windows archive command-line tool for its ZIP workflow. ZIP container/Deflate implementation is provided by .NET `System.IO.Compression`; Ferry owns the workflow, progress measurement, ETA, cancellation, conflict handling, and safety policy. Ferry does **not** implement a compression codec from scratch.

## Scope retained from v1.0.1

Existing v1.0.1 browsing, search, inline rename, bulk rename, Pinned reordering, List/Grid behavior, Recycle Bin, Shell integration, multi-selection, multi-open/D&D, F12 terminal behavior, and portable settings remain unchanged by the ZIP feature work.

Rubber-band/marquee selection remains out of scope for v1.0.2.

## Validation

The ZIP Integration Prototype 4 passed Windows regression and safety testing. The promoted v1.0.2-rc1 then passed the final Windows smoke test, including fresh portable-package launch and ZIP create/extract checks.

A Windows Shell quirk observed during testing—some dynamic detailed-context-menu extensions such as **Open in Terminal** appearing only from the second invocation after Explorer restart—was reproduced in Windows Explorer itself and is therefore not treated as a Ferry release blocker.

**Release status:** accepted for v1.0.2 final promotion with no application-logic changes after RC1 acceptance.
