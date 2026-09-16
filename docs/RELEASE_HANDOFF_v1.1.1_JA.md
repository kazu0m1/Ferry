# Ferry v1.1.1 — 公開前ハンドオフ

**Date:** 2026-09-16

## 凍結baseline

- v1.1.1 prototype 1のPaste result feedbackを正式仕様として凍結。
- prototype 1 Windows実機テストはPASS。
- final化ではapplication behaviorを変更せず、title/version/publication filesのみ正式版へ変更。

## Windows側で残る作業

1. `Build.cmd`。
2. `Ferry.exe`起動、Window title = `Ferry`、About/version = `1.1.1`。
3. `FINAL_RELEASE_CHECKLIST_JA.md`のPaste result spot checkとv1.1.0回帰smoke。
4. `Make-PortableRelease.cmd`。
5. `dist\Ferry-v1.1.1-win-portable.zip`をfresh folderへ展開して起動。
6. 最終ZIPのSHA-256保存。

## GitHub側

1. final sourceを既存`main`へcommit / push。
2. exact commitへ`v1.1.1` tag。
3. Release title `Ferry v1.1.1`。
4. 本文は`RELEASE_NOTES_v1.1.1.md`。
5. `Ferry-v1.1.1-win-portable.zip`をAssetとして添付。
6. Pre-release OFF。
7. 公開後、README直接Download linkから再取得しfresh launch確認。

全項目PASSでFerry v1.1.1 = RELEASED。
