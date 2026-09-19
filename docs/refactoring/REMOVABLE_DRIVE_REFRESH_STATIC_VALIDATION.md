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
For a volume `DBT_DEVICEQUERYREMOVE` / `DBT_DEVICEREMOVEPENDING` notification:
- identify the affected drive(s) from `DEV_BROADCAST_VOLUME.dbcv_unitmask`;
- find all Ferry tabs whose current path is on the affected volume;
- stop the tab refresh timer;
- stop search draining and Grid thumbnail work;
- cancel tab background work;
- synchronously disable/dispose the tab's `FileSystemWatcher`;
- move the affected tab to a non-affected Home/UserProfile/System-drive fallback;
- do not add the disappearing path to navigation history.

The message is not denied; Windows remains authoritative for whether the device can be removed.

## Static diff
Compared with `main`:
- `Build.cmd`: +1 source-list entry
- `Source/Ferry/Ferry.csproj`: +1 Compile entry
- `Source/Ferry/MainWindow.DriveNotifications.cs`: new partial
- this validation document
- no other files changed

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

### Safe eject while drive is open
Status: **PENDING after follow-up fix**.

Required:
1. Start Ferry and connect the removable drive.
2. Open the removable drive in Ferry.
3. Use Windows **Safely Remove Hardware** without navigating away manually.
4. Confirm Ferry moves the affected tab to Home (or another local fallback).
5. Confirm Windows allows the drive to be ejected without reporting Ferry as the process using the volume.
6. Reconnect and confirm the drive appears again and opens normally.
7. If more than one Ferry tab is open on the removable drive, repeat once with two such tabs and confirm both leave the drive before eject.

## Status
Static validation: PASS.
Drive arrival/removal Windows validation: PASS.
Safe-eject Windows validation: PENDING.
