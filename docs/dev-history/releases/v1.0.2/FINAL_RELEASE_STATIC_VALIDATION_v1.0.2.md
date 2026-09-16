# Ferry v1.0.2 Final Static Validation

**Baseline:** Ferry v1.0.2-rc1, accepted on Windows for release  
**Finalization rule:** application logic must not change during promotion to v1.0.2 final.

## Application source comparison

Files under `Source/Ferry` that differ from accepted RC1:

- `AssemblyInfo.cs`

Expected result: only `AssemblyInfo.cs`, changing `AssemblyInformationalVersion` from `1.0.2-rc1` to `1.0.2`.

Result: **PASS**

`app.manifest` remains `1.0.2.0`; Archive/MainWindow/Shell/Search/Selection and all other application source files are byte-identical to accepted RC1.

## Build/source coverage

- [x] C# source files under `Source/Ferry`: `29`
- [x] all 29 C# source files are referenced by `Build.cmd`
- [x] no C# source file was added or removed during final promotion

## Release metadata

- [x] AssemblyVersion = `1.0.2.0`
- [x] AssemblyFileVersion = `1.0.2.0`
- [x] AssemblyInformationalVersion = `1.0.2`
- [x] app.manifest = `1.0.2.0`
- [x] Portable README = `Ferry v1.0.2 Portable`
- [x] `Make-PortableRelease.cmd` version = `1.0.2`
- [x] Expected portable asset = `Ferry-v1.0.2-win-portable.zip`
- [x] Specification revision remains `1.0.24` because final promotion changes no requirements
- [x] Specification implementation baseline = `Ferry v1.0.2`
- [x] README / README.ja current-release and direct-download text = v1.0.2
- [x] `RELEASE_NOTES_v1.0.2.md` present
- [x] `FINAL_RELEASE_CHECKLIST_JA.md` present
- [x] `docs/GITHUB_V1.0.2_RELEASE_GUIDE_JA.md` present
- [x] public screenshot `docs/screenshot-main.png` present

## Package hygiene

- [x] v1.0.2 RC-only root documents removed
- [x] v1.0.2 RC pre-release guide removed/replaced by final release guide
- [x] no `.exe` / `.pdb` included in source tree
- [x] no user `settings.json` included
- [x] no Ferry debug log included
- [x] final source ZIP integrity verification after packaging = PASS

## Accepted Windows validation basis

Before final promotion:

- ZIP Integration Prototype 4 regression test: **PASS**
- ZIP Integration Prototype 4 safety test: **PASS**
- v1.0.2-rc1 Windows smoke test: **PASS / ACCEPTED**
- fresh portable-package launch and ZIP create/extract in RC1 smoke test: **PASS**

The observed first-invocation omission of some dynamic Windows detailed-context-menu extensions (including Open in Terminal) was reproduced in Windows Explorer after Explorer restart and is treated as an external Windows Shell behavior, not a Ferry release blocker.

## Windows-only final gate

This environment cannot run the Windows .NET Framework/WPF build toolchain. On Windows:

1. Run `Build.cmd`.
2. Confirm Settings → About Ferry = `Version 1.0.2`.
3. Run one normal ZIP compression and one normal ZIP extraction.
4. Run `Make-PortableRelease.cmd`.
5. Extract `dist\Ferry-v1.0.2-win-portable.zip` into a fresh folder and launch it.
6. Complete `FINAL_RELEASE_CHECKLIST_JA.md`.

**Static release result: PASS**
