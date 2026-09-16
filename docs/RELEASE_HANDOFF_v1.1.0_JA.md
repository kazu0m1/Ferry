# Ferry v1.1.0 — 公開前ハンドオフ

**更新:** 2026-09-16  
**状態:** RC20 Windows実機PASS → final source凍結済み

## 完了済み

- RC20をv1.1.0の最終挙動baselineとして凍結。
- finalではアプリ挙動を変更せず、title/version/package metadataのみ正式版へ昇格。
- README / README.jaへ承認済みスクリーンショットを掲載。
- `docs/dev-history/`をPublic repositoryへ残す方針で確定。
- v1.0.0〜v1.0.2 Release Notesをrootへ残す方針で確定。
- v1.1.0はAuthenticode未署名で公開する方針で確定。
- Ubuntu日本語コミュニティへの紹介は今回は保留。
- Source packageのrelease hygiene / static auditを再実施。

## 次にWindowsで実施する項目

1. final sourceを展開し`Build.cmd`を実行。
2. `Ferry.exe`を起動し、window title = `Ferry`、About/version = `1.1.0`を確認。
3. `FINAL_RELEASE_CHECKLIST_JA.md`のUI / selection smokeを実施。
4. v1.0.2系の既存`settings.json`で起動し、欠損autoscroll値が100になることを確認。
5. `Make-PortableRelease.cmd`を実行。
6. `dist\Ferry-v1.1.0-win-portable.zip`をfresh folderへ展開し起動。
7. Portable ZIPのSHA-256を記録。
8. final sourceをcommitし、そのexact commitへ`v1.1.0` tagを付与。
9. GitHub Releaseを作成しPortable ZIPをassetとして添付。
10. 公開後、READMEの直接Download linkからassetを再ダウンロードし、展開・起動を1回確認。

## Release判定

上記Windows / GitHub項目がPASSすれば、Ferry v1.1.0の公開作業は完了。
