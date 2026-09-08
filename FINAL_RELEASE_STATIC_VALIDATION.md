# Ferry v1.0.0 Final Static Validation

**Date:** 2026-09-08  
**Baseline:** Ferry v1.0.0-rc15, Windows final smoke test 19/19 PASS

## Release-promotion rule

The validated RC15 application logic must not change during promotion to v1.0.0 final.

## Source comparison

- `Source/Ferry` compared with RC15: byte-identical for every source/project/manifest file except `AssemblyInfo.cs`.
- `AssemblyInfo.cs` change: `AssemblyInformationalVersion("1.0.0-rc15")` → `AssemblyInformationalVersion("1.0.0")` only.
- AssemblyVersion and AssemblyFileVersion remain `1.0.0.0`.

**Result:** PASS — no application-logic change introduced during final promotion.

## Release metadata

- Assembly informational version: `1.0.0`
- `Make-PortableRelease.cmd` version: `1.0.0`
- Portable README: `Ferry v1.0.0 Portable`
- README / README.ja: final `v1.0.0` release wording
- Specification header: revision `1.0.18`

**Result:** PASS.

## Package hygiene

Static scan of the final source tree found no:

- `settings.json`
- Ferry log file
- `Ferry.exe`
- `.pdb`
- user-specific runtime configuration

Historical RC quick-check/static-validation files were removed from the public root package. The Explorer-selection research document is retained under `docs/` as development research, while the public README/spec clearly state that rubber-band selection is not included in v1.0.

**Result:** PASS.

## Public documentation

Updated for final v1.0.0:

- `README.md`
- `README.ja.md`
- `CHANGELOG.md`
- `RELEASE_NOTES_v1.0.0.md`
- `PUBLIC_RELEASE_AUDIT.md`
- `TEST_CHECKLIST.md`
- `FINAL_RELEASE_CHECKLIST_JA.md`
- `docs/GITHUB_PUBLICATION_GUIDE_JA.md`
- `docs/UBUNTU_JP_ANNOUNCEMENT_DRAFT.md`
- `docs/FINAL_SMOKE_TEST_RESULT_JA.md`

The old community-announcement claim that v1.0 includes rubber-band selection was removed.

**Result:** PASS.

## Environment limitation / final binary gate

This Linux environment does not contain the Windows .NET Framework/WPF compiler used by `Build.cmd`, so the final binary cannot be truthfully built or runtime-tested here.

Required Windows packaging steps:

1. `Build.cmd`
2. launch `Portable\Ferry.exe` once
3. `Make-PortableRelease.cmd`
4. extract `dist\Ferry-v1.0.0-win-portable.zip` into a fresh directory
5. launch `Ferry.exe` once
6. record the SHA-256 printed by the packaging script

**Source release readiness:** PASS.  
**Binary release readiness:** pending only the Windows build/package/launch gate above.
