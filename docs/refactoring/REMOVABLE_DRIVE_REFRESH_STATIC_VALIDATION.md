# Removable Drive Refresh / Safe Eject Validation

## Scope
Bugfix for removable-drive lifecycle behavior in Ferry's Sidebar and open tabs.

Originally observed:
- A removable drive connected after Ferry startup did not appear under **Drives**.
- Restarting Ferry with the drive already connected made the drive appear and it was navigable.

Follow-up observation:
- When Ferry had the removable drive open, Windows **Safely Remove Hardware** reported that the volume was in use.
- Windows Explorer leaves the removable volume before ejecting, while Ferry previously kept its folder watcher active.

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

## Drive arrival / removal behavior
The new MainWindow partial:
- hooks the WPF window message source in `OnSourceInitialized`;
- listens for `WM_DEVICECHANGE`;
- handles volume arrival/removal notifications;
- debounces repeated Sidebar refresh notifications for 250 ms;
- calls the existing `BuildSidebar()`;
- removes the hook and stops the timer when the window closes.

There is no polling loop and no always-running background worker.

## Safe-eject behavior

### Prototype 1
The first safe-eject attempt listened only for broadcast volume `DBT_DEVICEQUERYREMOVE` notifications.

Windows result:
- drive arrival/removal refresh: PASS;
- safe eject while the drive was open: FAIL;
- Ferry did not leave the drive before Windows reported the volume as in use.

Conclusion:
- generic volume broadcasts were sufficient for arrival/removal UI refresh;
- they were not sufficient to obtain the pre-removal notification needed to release Ferry's open watcher in the tested environment.

### Prototype 2
Ferry now follows the Win32 handle-notification pattern:
- enumerate ready non-system Fixed/Removable drive roots;
- open a zero-access, fully shared directory handle for each candidate drive root;
- register the handle with `RegisterDeviceNotification` using `DBT_DEVTYP_HANDLE`;
- on `DBT_DEVICEQUERYREMOVE`, map the notification back to the affected drive;
- unregister/close Ferry's notification handle;
- find all Ferry tabs whose current path is on that drive;
- stop refresh timers, search draining, Grid thumbnail work, and tab background work;
- synchronously disable/dispose each tab's `FileSystemWatcher`;
- move affected tabs to a non-affected Home/UserProfile/System-drive fallback;
- return TRUE to Windows for Ferry's registered removal query.

On normal arrival/removal events, Ferry still refreshes the Sidebar and updates drive-notification registrations.

The message is never proactively denied; Windows remains authoritative for whether the device can actually be removed.

## Static diff
Compared with `main`:
- `Build.cmd`: +1 source-list entry
- `Source/Ferry/Ferry.csproj`: +1 Compile entry
- `Source/Ferry/MainWindow.DriveNotifications.cs`: new partial
- this validation document
- no existing source method bodies changed

## Windows validation

### Drive refresh
User result: **PASS**.

Validated:
- start Ferry with removable drive disconnected;
- connect drive while Ferry remains open;
- drive appears under **Drives** without restart;
- drive opens normally from Sidebar;
- safely disconnect/eject and drive disappears from **Drives**;
- reconnect and drive reappears;
- existing Pinned folders / normal Sidebar navigation remain functional.

### Safe eject Prototype 1
User result: **FAIL**.

Observed:
- Windows still reported the volume as currently in use;
- therefore Ferry did not move the open tab to Home and reconnection flow could not be completed.

### Safe eject Prototype 2
Single-tab user result: **PASS**.

Validated:
- Ferry had the removable drive open in one tab;
- Windows **Safely Remove Hardware** was invoked without navigating away manually;
- Ferry automatically moved the affected tab to Home/a local fallback;
- Windows allowed the drive to be ejected without the previous "volume is currently in use" failure.

Remaining:
1. Reconnect the removable drive and confirm it reappears under **Drives** and opens normally.
2. Open the removable drive in two Ferry tabs.
3. Invoke **Safely Remove Hardware**.
4. Confirm both affected tabs leave the removable drive.
5. Confirm Windows eject succeeds.

## Status
Static validation: PASS.
Drive arrival/removal Windows validation: PASS.
Safe-eject Prototype 1: FAIL / superseded.
Safe-eject Prototype 2 single-tab: PASS.
Reconnect / two-tab safe-eject: PENDING.
