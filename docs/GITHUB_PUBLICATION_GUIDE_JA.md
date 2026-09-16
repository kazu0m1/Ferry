# Ferry v1.1.2 GitHub正式リリース手順

対象Repository: `kazu0m1/Ferry`  
正式版: `v1.1.2`

## 1. 最終Sourceをmainへ反映

GitHub Desktopで差分を確認し、次を確認する。

- Window title = `Ferry`
- `AssemblyInformationalVersion` = `1.1.2`
- `Make-PortableRelease.cmd` = `VERSION=1.1.2`
- `Portable/README.txt`先頭 = `Ferry v1.1.2 Portable`
- Bug report templateのVersion例 = `v1.1.2`
- v1.1.2 prototype資料 = `docs/dev-history/releases/v1.1.2/`
- repositoryへ`dist/`、`bin/`、`obj/`、`.vs/`等を入れない

Commit例:

```text
Release Ferry v1.1.2
```

commit後、`main`へpushする。

## 2. Windowsで最終ビルド

Repository rootで、

```text
Build.cmd
```

を実行し、Window title = `Ferry`、About/version = `1.1.2`を確認する。

## 3. Portable ZIP作成

```text
Make-PortableRelease.cmd
```

生成物:

```text
dist\Ferry-v1.1.2-win-portable.zip
```

fresh folderへ展開して起動確認後、最終ZIPのSHA-256を保存する。

## 4. Tag

GitHub DesktopのHistoryでexact `Release Ferry v1.1.2` commitへ、

```text
v1.1.2
```

のtagを作成してpushする。

## 5. GitHub Release

- Tag: `v1.1.2`
- Release title: `Ferry v1.1.2`
- 本文: `RELEASE_NOTES_v1.1.2.md`
- Asset: `Ferry-v1.1.2-win-portable.zip`
- Pre-release: OFF

## 6. 公開後確認

READMEの直接DownloadリンクからZIPを再取得し、fresh folderへ展開して起動する。

- Window title = `Ferry`
- About/version = `1.1.2`
- 可能なら公開ZIPのSHA-256が公開前に固定した値と一致することも確認する。
