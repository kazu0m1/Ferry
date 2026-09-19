# Ferry v1.1.3

Ferry v1.1.3 is a small removable-drive lifecycle bugfix release built on the validated v1.1.2 baseline.

## Fixed

- Removable drives connected after Ferry has already started now appear automatically under **Drives** in the Sidebar.
- Drive removal/reconnection updates the Sidebar without restarting Ferry.
- Fixed Windows **Safely Remove Hardware** failing with “This volume is currently in use” when Ferry had the removable drive open.
- When Windows requests safe removal, Ferry releases its registered drive handle, active `FileSystemWatcher`, and related tab background work for that drive before the eject decision completes.
- Every Ferry tab currently on the removable drive automatically moves to Home (or another non-affected local fallback) before removal.

## Windows validation

The final implementation passed real-machine checks for:

- connecting a removable drive after Ferry startup and seeing it appear automatically;
- opening the newly connected drive from the Sidebar;
- removing the drive and seeing the Sidebar update;
- reconnecting the drive and opening it again;
- safe eject while one Ferry tab is on the removable drive; and
- safe eject while two Ferry tabs are on the removable drive, with both tabs leaving the drive before Windows completes removal.

## Implementation scope

Ferry uses Windows device-change notifications and registered drive handles for the safe-eject handshake. It does not add a polling loop, always-running service, or background tray process.

## Unchanged

- v1.1.2 Ferry → Explorer external D&D Move completion behavior.
- v1.1.1 Paste-result selection behavior.
- v1.1.0 rubber-band selection, Selection Anchor, keyboard navigation, autoscroll, Breadcrumb/Location Box, and toolbar behavior.
- Windows remains authoritative for the final device-removal decision.

Release status: the removable-drive implementation passed Windows validation before final promotion; v1.1.3 finalization changes version/release metadata and packaging only.
