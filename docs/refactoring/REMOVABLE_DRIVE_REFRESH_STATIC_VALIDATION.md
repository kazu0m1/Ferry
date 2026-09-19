# Removable Drive Refresh Static Validation

## Scope
Bugfix for removable-drive visibility in Ferry's Sidebar.

Observed behavior:
- A removable drive connected after Ferry startup did not appear under **Drives**.
- Restarting Ferry with the drive already connected made the drive appear and it was navigable.

## Branch
- `fix/removable-drive-refresh`
- Base: `main` at `0110ce46d9cda9a0db0e1b610901eb8f43f5ca83`

## Implementation
Added:
- `Source/Ferry/MainWindow.DriveNotifications.cs`

Build wiring only:
- `Build.cmd`
- `Source/Ferry/Ferry.csproj`

No existing source method bodies were modified.

## Behavior
The new MainWindow partial:
- hooks the WPF window message source in `OnSourceInitialized`;
- listens for `WM_DEVICECHANGE`;
- reacts only to `DBT_DEVICEARRIVAL` and `DBT_DEVICEREMOVECOMPLETE`;
- filters notifications to `DBT_DEVTYP_VOLUME`;
- debounces repeated notifications for 250 ms;
- calls the existing `BuildSidebar()` after the debounce;
- removes the hook and stops the timer when the window closes.

There is no polling loop and no always-running background worker.

## Static diff
Compared with `main`:
- `Build.cmd`: +1 source-list entry
- `Source/Ferry/Ferry.csproj`: +1 Compile entry
- `Source/Ferry/MainWindow.DriveNotifications.cs`: new file
- no other files changed

## Intended Windows validation
1. Build and launch Ferry with the removable drive disconnected.
2. Connect the removable drive while Ferry remains open.
3. Confirm the new drive appears under **Drives** without restarting Ferry.
4. Open the drive from the Sidebar and confirm normal navigation.
5. Disconnect/eject the drive and confirm it disappears from **Drives** without restarting Ferry.
6. Reconnect it and confirm it reappears.
7. Confirm existing Pinned folders and normal Sidebar navigation still work.

## Status
Static validation: PASS.
Windows runtime validation: PENDING.
