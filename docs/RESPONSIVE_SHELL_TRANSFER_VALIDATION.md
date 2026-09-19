# Responsive Shell Transfer Validation

## Scope
Bugfix candidate for Ferry remaining responsive during long Windows Shell Copy/Move operations.

Observed behavior on v1.1.3:
- A large cross-volume move (for example C: -> D:) can run for a long time.
- If Ferry is minimized while the move is running, the window cannot be restored until the file operation completes.
- As soon as the move finishes, Ferry becomes visible again.

## Root cause
Ferry invokes Windows `SHFileOperation` synchronously from the WPF UI thread for Copy/Move operations. Long cross-volume transfers therefore block the Dispatcher even though the Windows Shell transfer itself continues.

## Branch
- `fix/responsive-shell-transfer`
- Base: current `main` / Ferry v1.1.3

## Implementation
Modified:
- `Source/Ferry/ShellFileOperations.cs`
- `Source/Ferry/MainWindow.cs`

### ShellFileOperations
- Copy and Move still use the existing Windows `SHFileOperation` implementation.
- The expensive Shell call runs on a dedicated STA worker thread.
- The UI-thread caller keeps the existing synchronous completion contract by waiting with a nested WPF `DispatcherFrame` instead of blocking the Dispatcher.
- Window messages, painting, minimize/restore, and normal Dispatcher work therefore continue while the transfer runs.
- Only one Ferry Copy/Move transfer can run at a time.
- Delete operations are intentionally unchanged in this bugfix.

### Window close safety
- Ferry refuses to close while a Copy/Move transfer is active.
- This prevents the worker transfer from being lost by closing the application mid-operation.
- The Windows Shell remains responsible for transfer progress, conflicts, and cancellation.

## Preserved behavior
- Paste feedback keeps its existing synchronous operation-completion semantics.
- Ferry internal/external drag/drop callers still observe Copy/Move as completing before the ShellFileOperations call returns.
- v1.1.3 removable-drive lifecycle behavior is unchanged.
- No custom copy engine or transfer queue is introduced.

## Automated validation
A temporary Windows GitHub Actions workflow was used only on the bugfix branch:
- `Build.cmd`: PASS.
- The temporary workflow was removed after validation and is not part of the intended product diff.

## Required Windows validation
1. Start Ferry from the bugfix branch.
2. Use Cut -> Paste to move a sufficiently large file from C: to D: so the Windows transfer remains active long enough to test.
3. While the transfer is running:
   - minimize Ferry;
   - restore Ferry from the taskbar;
   - confirm the window restores promptly rather than waiting for transfer completion;
   - confirm Ferry repaints normally.
4. While the same transfer is still running, switch tabs or browse another folder and confirm the UI remains responsive.
5. Let the transfer complete and confirm the file move completes normally.
6. Repeat with a large Copy if convenient.
7. Optional but useful: start a Ferry-internal D&D Copy/Move that takes long enough to observe and confirm minimize/restore remains responsive.
8. During an active transfer, click Ferry's Close button once and confirm Ferry refuses to close until the transfer is finished/cancelled.

## Windows interactive runtime validation

### Large cross-volume Move: minimize / restore
User result: **PASS**.

Validated:
- a large C: -> D: Cut/Paste move was still in progress;
- Ferry was minimized during the active transfer;
- Ferry restored from the taskbar before the transfer finished;
- the original v1.1.3 symptom (window not returning until transfer completion) did not reproduce.

Additional user result: **PASS**.

Validated while the same large cross-volume Move was still active:
- opening another Ferry tab worked;
- switching between tabs worked;
- navigating to other folders and back worked;
- Ferry remained interactively usable during the Shell transfer.

Additional user result: **PASS**.

Validated:
- large C: -> D: Move completed normally;
- source item was removed from C: and present on D:;
- large D: -> C: Move also completed normally;
- cross-volume Move semantics remained correct in both directions.

Additional user result: **PASS**.

Validated:
- clicking Ferry's Close button during an active Copy/Move shows the transfer-in-progress warning;
- Ferry remains open and the active transfer is not interrupted;
- after the transfer finishes, Ferry remains open. This is intentional: the close request was cancelled at the time it was made, so Ferry does not auto-close later without a new explicit user close action.

Additional user result: **PASS**.

Validated:
- large cross-volume Copy via Ferry Paste remained responsive during minimize/restore and tab switching;
- the Copy completed normally.

### Ferry -> Ferry long-running D&D: first candidate

User result: **PARTIAL / FAIL on sender responsiveness**.

Observed with two Ferry windows during a long-running Ferry -> Ferry transfer:
- receiving Ferry window: minimize / restore PASS;
- sending Ferry window: minimize / restore FAIL until the target Drop operation returns.

A screen recording of the sending side was reviewed and is consistent with the reported sender-side blocking.

Root cause:
- the receiving Ferry no longer blocks its own Dispatcher while SHFileOperation runs;
- however, the sending Ferry still remains inside synchronous WPF `DragDrop.DoDragDrop(...)`;
- `DoDragDrop` does not return until the receiving Ferry's Drop handler returns;
- therefore keeping the receiving `ViewDrop` handler synchronously open for the entire transfer still blocks the sending Ferry's D&D call.

Required follow-up:
- Ferry's target-side `ViewDrop` must accept the drop and return promptly;
- the actual Shell Copy/Move must then continue on the existing STA transfer worker;
- Paste behavior and Shell transfer semantics must remain unchanged.

Remaining interactive validation:
- re-test long-running Ferry -> Ferry D&D after the target-side asynchronous handoff fix.

## Status
Static/code review: PASS.
Windows automated build: PASS.
Windows minimize/restore runtime validation: PASS.
Windows tab/navigation responsiveness validation: PASS.
Windows cross-volume Move completion validation (C: <-> D:): PASS.
Windows active-transfer close guard validation: PASS.
Windows large Copy responsiveness/completion validation: PASS.
Ferry -> Ferry D&D receiver responsiveness: PASS.
Ferry -> Ferry D&D sender responsiveness: FAIL on first candidate; follow-up fix required.
Remaining Windows interactive validation: PENDING.
