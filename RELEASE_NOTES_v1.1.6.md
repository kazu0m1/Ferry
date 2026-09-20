# Ferry v1.1.6

Ferry v1.1.6 is a small Windows integration release built on the validated v1.1.5 baseline.

## Added

- Ferry now recognizes connected MTP / Portable Devices exposed under Windows **This PC**.
- Connected Android phones and similar devices appear in a dedicated **Portable Devices** section in the Sidebar.
- Clicking a Portable Device opens that device in Windows Explorer for file transfer.

## Scope

This is intentionally a lightweight integration.

- Ferry does not browse MTP storage internally.
- MTP file enumeration, copy, move, rename, delete, and folder creation remain owned by Windows Explorer.
- Existing Windows device-change notifications are reused so connect/disconnect changes can refresh the Sidebar without a polling loop.
- Normal drive-letter-backed storage continues to use Ferry's existing **Drives** behavior.

## Windows validation

The candidate implementation passed:

- Windows GitHub Actions `Build.cmd`;
- Pixel 7a recognition after USB connection and switching Android to **File transfer** mode; and
- opening the Pixel 7a in Windows Explorer by clicking its Ferry Sidebar entry.

## Preserved

- v1.1.5 tab reordering and Sidebar folder context menus.
- v1.1.4 responsive long Windows Shell Copy/Move and Ferry-to-Ferry D&D behavior.
- v1.1.3 removable-drive arrival/removal and safe-eject behavior.
- v1.1.2 Ferry → Explorer external D&D Move completion behavior.
- v1.1.1 Paste-result selection behavior.
- v1.1.0 selection/rubber-band/Selection Anchor behavior.

Release status: Portable Device recognition and Explorer hand-off passed Windows real-machine validation before final promotion; v1.1.6 finalization changes version/release metadata and packaging only.
