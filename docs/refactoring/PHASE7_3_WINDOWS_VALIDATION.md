# Phase 7-3 Windows Validation — SettingsWindow persistence partial

## Scope
Windows build/runtime validation after moving Settings save/import/export/reset and AppSettings field transfer helpers into `SettingsWindow.Persistence.cs`.

## Baseline
- Branch: `refactor/v1.1.2`
- Phase 7-3 implementation head: `b942132449e17d9b0f93dc3b306fa032392b7182`
- Static validation commit: `7f084133c963670bb3636c3c65e8e6e9c957380a`
- Validation date: 2026-09-17

## Result
**PASS — Phase 7-3 SettingsWindow behavior.**

Confirmed:
- `Build.cmd` succeeds.
- Ferry launches normally.
- Settings window opens normally.
- Saving a changed setting works and persists after restart.
- Settings export/import works.
- Reset works.
- Home folder Browse works.

## Separate observation requiring revalidation
During subsequent Rename testing, a two-file batch rename/Undo showed severe latency (roughly 20+ seconds) and may have hung/crashed/restarted Ferry. This is outside Phase 7-3 scope and is being treated as a possible regression associated with the earlier Phase 7-2 RenameEngine structural split until isolated.
