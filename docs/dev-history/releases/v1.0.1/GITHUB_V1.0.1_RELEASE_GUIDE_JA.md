# Ferry v1.0.1 GitHub更新リリース手順

## 1. ローカルリポジトリを更新

正式版Sourceの中身を、既存のGit管理フォルダー `Ferry` 直下へ上書きします。既存の `.git` と `.gitattributes` は削除しません。

GitHub Desktopで変更内容を確認し、prototype/RC用の一時文書が削除対象になっていることも確認します。

Commit例:

```text
Release Ferry v1.0.1
```

その後 **Push origin**。

## 2. WindowsでPortable ZIPを生成

```text
Build.cmd
Make-PortableRelease.cmd
```

生成物:

```text
dist\Ferry-v1.0.1-win-portable.zip
```

別フォルダーへ展開して起動し、SettingsのAboutが`Version 1.0.1`であることを確認します。

## 3. GitHub Release

Repository → **Releases** → **Create a new release**

- Tag: `v1.0.1`（Target: `main`）
- Release title: `Ferry v1.0.1`
- Description: `RELEASE_NOTES_v1.0.1.md` の全文
- Asset: `Ferry-v1.0.1-win-portable.zip`
- Pre-release: OFF

公開後、READMEの直接DownloadリンクからPortable ZIPが取得できることを確認します。
