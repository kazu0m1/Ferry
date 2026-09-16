# Ferry v1.1.1 GitHub正式リリース手順

対象Repository: `kazu0m1/Ferry`  
正式版: `v1.1.1`

## 1. 最終Sourceをmainへ反映

GitHub Desktopで差分を確認し、次を確認する。

- Window title = `Ferry`
- `AssemblyInformationalVersion` = `1.1.1`
- `Make-PortableRelease.cmd` = `VERSION=1.1.1`
- `Portable/README.txt`先頭 = `Ferry v1.1.1 Portable`
- Bug report templateのVersion例 = `v1.1.1`
- v1.1.1 prototype資料 = `docs/dev-history/releases/v1.1.1/`

Commit例:

```text
Release Ferry v1.1.1
```

commit後、`main`へpushする。

## 2. Windowsで最終ビルド

Repository rootで、

```text
Build.cmd
```

を実行し、`Portable\Ferry.exe`生成、Window title = `Ferry`、About/version = `1.1.1`を確認する。詳細は`FINAL_RELEASE_CHECKLIST_JA.md`に従う。

## 3. Portable ZIP生成

```text
Make-PortableRelease.cmd
```

生成物:

```text
dist\Ferry-v1.1.1-win-portable.zip
```

## 4. fresh folder確認

生成ZIPを別の新しいfolderへ展開して起動し、最低限以下を確認する。

- About = `1.1.1`
- Copy/Cut → Paste結果が選択状態で残る
- 連続2回Pasteで2回目の結果だけが選択される
- List / Grid切替後もPaste結果selectionを維持する
- rubber-band / D&D / keyboard selectionに重大回帰がない

## 5. SHA-256保存

`Make-PortableRelease.cmd`が表示したSHA-256を保存する。必要ならPowerShellでも確認できる。

```powershell
Get-FileHash -Algorithm SHA256 .\dist\Ferry-v1.1.1-win-portable.zip
```

## 6. Tag / Release

- Tag: `v1.1.1`
- Target: final sourceのexact commit
- Release title: `Ferry v1.1.1`
- Description: `RELEASE_NOTES_v1.1.1.md`
- Asset: `Ferry-v1.1.1-win-portable.zip`
- Pre-release: **OFF**

## 7. 公開直後

- README Current release = `v1.1.1`
- README直接Download linkからassetを取得できる
- fresh downloadを展開・起動できる
- About = `1.1.1`
- IssuesのBug report templateに`v1.1.1`が表示される

以上がPASSしたらFerry v1.1.1公開完了。
