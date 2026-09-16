# Ferry v1.1.2 Release Handoff

## Accepted behavioral baseline

- v1.1.2 prototype 1 Windows real-machine PASS.
- Ferry → Explorer same-drive Move completion fixed.
- `Ctrl+D&D` Copy and Cancel safety PASS.
- folder / multi-item / Ferry→Ferry / Explorer→Ferry regression PASS.
- v1.1.1 Paste feedback and selection smoke PASS.
- Cross-volume D&D is explicitly unverified because no second drive was available.

## Final promotion rule

Do not change external D&D logic after prototype acceptance. Final promotion is limited to release metadata and public documentation.

## Publisher next steps

1. Run `Build.cmd` on Windows.
2. Complete `FINAL_RELEASE_CHECKLIST_JA.md`.
3. Run `Make-PortableRelease.cmd`.
4. Record the SHA-256 of `dist\Ferry-v1.1.2-win-portable.zip`.
5. Commit final source as `Release Ferry v1.1.2`.
6. Tag exact commit `v1.1.2`.
7. Publish GitHub Release with `RELEASE_NOTES_v1.1.2.md` and the portable ZIP.
8. Re-download through README and launch from a fresh folder.
