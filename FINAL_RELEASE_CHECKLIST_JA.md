# Ferry v1.0.0 正式公開チェックリスト

RC15のWindows最終スモークテストは19/19 PASS済みです。ここでは**正式版Binaryを作ってGitHubへ載せるための最終作業だけ**を行います。

## A. Source確認

- [ ] `Source\Ferry\AssemblyInfo.cs` の InformationalVersion が `1.0.0`
- [ ] `Make-PortableRelease.cmd` の VERSION が `1.0.0`
- [ ] READMEが `v1.0.0` 正式版表記
- [ ] Source treeに個人用`settings.json`や`Ferry.log`がない

## B. Windows build

Repository rootで:

```text
Build.cmd
```

- [ ] `Portable\Ferry.exe`が生成される
- [ ] `Ferry.exe`を1回起動できる
- [ ] Homeが表示される

## C. Portable Release asset生成

```text
Make-PortableRelease.cmd
```

- [ ] `dist\Ferry-v1.0.0-win-portable.zip`が生成される
- [ ] コマンドが表示したSHA-256を控える

## D. Binary ZIP最終確認

ZIPを新しい空フォルダーへ展開して:

- [ ] `Ferry.exe`が起動する
- [ ] Homeが開く
- [ ] F12で現在フォルダーにTerminalが開く
- [ ] Settingsが開く
- [ ] ZIP内に個人用`config\settings.json`が入っていない

## E. GitHub

- [ ] Repository `kazu0m1/Ferry` をPublicで作成 / push
- [ ] Tag `v1.0.0`
- [ ] Release title `Ferry v1.0.0`
- [ ] Release本文に`RELEASE_NOTES_v1.0.0.md`を使用
- [ ] `Ferry-v1.0.0-win-portable.zip`を添付
- [ ] Release本文末尾へSHA-256を追記
- [ ] Pre-releaseをOFF

## F. 公開直後

- [ ] READMEが正常表示
- [ ] 日本語READMEリンクが正常
- [ ] Release assetをダウンロード可能
- [ ] ダウンロードしたZIPを新規フォルダーで起動可能
- [ ] Issuesが利用可能

ここまで完了したらFerry v1.0.0正式公開完了です。
