# Ferry v1.0.2 GitHub更新リリース手順

## 1. ローカルリポジトリを更新

正式版Sourceの中身を、既存のGit管理フォルダー `Ferry` 直下へ上書きします。既存の `.git` と `.gitattributes` は削除しません。

GitHub Desktop等で差分を確認し、v1.0.2-rc1から`Source/Ferry`のapplication logicが変わっていないことを確認します。Finalで許可されるSource差分は`AssemblyInfo.cs`の`AssemblyInformationalVersion`のみです。

Commit例:

```text
Release Ferry v1.0.2
```

その後 **Push origin**。

## 2. WindowsでFinal確認 / Portable ZIP生成

```text
Build.cmd
Make-PortableRelease.cmd
```

生成物:

```text
dist\Ferry-v1.0.2-win-portable.zip
```

別フォルダーへ展開して起動し、SettingsのAboutが`Version 1.0.2`であることを確認します。`FINAL_RELEASE_CHECKLIST_JA.md`の短いFinal gateを完了します。

## 3. GitHub Release

Repository → **Releases** → **Create a new release**

- Tag: `v1.0.2`（Target: `main`）
- Release title: `Ferry v1.0.2`
- Description: `RELEASE_NOTES_v1.0.2.md` の全文
- Asset: `Ferry-v1.0.2-win-portable.zip`
- Pre-release: OFF

公開後、READMEの直接DownloadリンクからPortable ZIPが取得できることを確認します。
