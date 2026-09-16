# Ferry v1.1.0 — Release Hygiene Audit

**Date:** 2026-09-16  
**Scope:** final source package / public repository preparation

## Completed

- RC20 Windows validation resultをfinal behavior baselineとして固定。
- Main title / informational version / portable versionをfinal `1.1.0`へ昇格。
- `Portable/README.txt`を`Ferry v1.1.0 Portable`へ更新。
- Bug report template version例 = `v1.1.0`。
- v1.1.0 RC evidenceは`docs/dev-history/releases/v1.1.0/`へ保存。
- 承認済みスクリーンショットを`docs/`へ格納しREADME / README.jaから参照。
- `docs/dev-history/`を公開repositoryへ残す判断を反映。
- v1.0.0–v1.0.2 Release Notesをrootへ残す判断を反映。
- v1.1.0 unsigned release方針を反映。
- Ubuntu日本語コミュニティ紹介は保留としてhandoffへ反映。
- Source tree build artifact scan / machine-specific path scan / credential heuristic scan / Markdown relative-link validationをfinal packageに対して再実施。

## Requires Windows / publisher action

- `Build.cmd`。
- About/version = `1.1.0`確認。
- final UI / selection smoke。
- `Make-PortableRelease.cmd`によるPortable binary作成。
- fresh-folder launch / v1.0.2 settings migration test。
- final Portable ZIP SHA-256記録。
- exact release commitへ`v1.1.0` tag、GitHub Release作成、asset upload。
- 公開後assetの再ダウンロード・展開・起動。
