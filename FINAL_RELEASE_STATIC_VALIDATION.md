# Ferry v1.0.1 Final Static Validation

**Baseline:** Ferry v1.0.1-rc4, accepted on Windows for release
**Finalization rule:** application logic must not change during promotion to v1.0.1 final.

## Application source comparison

Files under `Source/Ferry` that differ from RC4:

- `AssemblyInfo.cs`

Expected result: only `AssemblyInfo.cs`, changing `AssemblyInformationalVersion` from `1.0.1-rc4` to `1.0.1`.

Result: **PASS**

## Release metadata

- [x] AssemblyVersion = `1.0.1.0`
- [x] AssemblyFileVersion = `1.0.1.0`
- [x] AssemblyInformationalVersion = `1.0.1`
- [x] Portable README = `Ferry v1.0.1 Portable`
- [x] `Make-PortableRelease.cmd` version = `1.0.1`
- [x] Expected portable asset = `Ferry-v1.0.1-win-portable.zip`
- [x] Specification revision = `1.0.23`
- [x] README / README.ja current-release and direct-download text = v1.0.1
- [x] `RELEASE_NOTES_v1.0.1.md` present
- [x] public screenshot `docs/screenshot-main.png` present

## Package hygiene

- [x] v1.0.1 prototype/RC temporary root documents removed
- [x] no EXE/PDB files included in source tree
- [x] no user `settings.json` included
- [x] no Ferry debug log included
- [x] ZIP integrity check required after packaging

## Windows-only final gate

This environment cannot run the Windows .NET Framework/WPF build toolchain. On Windows:

1. Run `Build.cmd`.
2. Confirm Settings → About Ferry = `Version 1.0.1`.
3. Run `Make-PortableRelease.cmd`.
4. Extract `dist\Ferry-v1.0.1-win-portable.zip` into a fresh folder and launch it.
5. Complete the short checks in `FINAL_RELEASE_CHECKLIST_JA.md`.
