# Ferry v1.1.0 GitHub正式リリース手順

対象Repository: `kazu0m1/Ferry`  
正式版: `v1.1.0`

この文書は**現行版の公開手順**です。v1.0.xの公開手順は履歴資料として別文書に残します。

## 1. 最終Sourceをmainへ反映

GitHub DesktopまたはGitで差分を確認し、次を確認します。

- Window titleが`Ferry`
- `AssemblyInformationalVersion`が`1.1.0`
- `Make-PortableRelease.cmd`の`VERSION=1.1.0`
- RC用文書がRepository rootではなく`docs/dev-history/releases/v1.1.0/`へ整理されている
- `Portable/README.txt`の先頭が`Ferry v1.1.0 Portable`
- Bug report templateのVersion例が`v1.1.0`

Commit例:

```text
Release Ferry v1.1.0
```

その後、`main`へpushします。

## 2. Windowsで最終ビルド

Repository rootで、

```text
Build.cmd
```

を実行します。

最低限、次を確認します。

- `Portable\Ferry.exe`が生成される
- 起動できる
- Window titleが`Ferry`
- Settings / AboutのVersionが`1.1.0`

詳細はRepository rootの`FINAL_RELEASE_CHECKLIST_JA.md`に従います。

## 3. GitHub Release用Portable ZIPを生成

```text
Make-PortableRelease.cmd
```

成功すると、

```text
dist\Ferry-v1.1.0-win-portable.zip
```

が生成され、SHA-256がコンソールに表示されます。

## 4. 生成ZIPをfresh folderで確認

生成したZIPを、ビルド元とは別の新しいフォルダーへ展開して`Ferry.exe`を起動します。

最低限、次を確認します。

- Ferryが起動する
- Homeが開く
- Aboutが`1.1.0`
- List / Gridが切り替わる
- Rubber-band selectionが動く
- `Ctrl+L` → `Esc`でBreadcrumbへ戻る
- Settingsが開く
- 既存v1.0.2系`settings.json`を使っても起動でき、Rubber-band autoscroll speedの欠損値が100になる

## 5. 最終SHA-256を保存

`Make-PortableRelease.cmd`が表示したSHA-256を、Release本文またはRelease作業メモへ保存します。

必要ならPowerShellでも再確認できます。

```powershell
Get-FileHash -Algorithm SHA256 .\dist\Ferry-v1.1.0-win-portable.zip
```

## 6. Tag / Releaseを作成

GitHub Repository → **Releases** → **Create a new release**。

- Tag: `v1.1.0`
- Target: 最終Sourceを含む`main`のcommit
- Release title: `Ferry v1.1.0`
- Description: `RELEASE_NOTES_v1.1.0.md`を使用
- Asset: `Ferry-v1.1.0-win-portable.zip`
- Pre-release: **OFF**
- Latest release: **ON / 通常の正式Releaseとして公開**

Release本文末尾にSHA-256を追記しても構いません。

## 7. 公開直後の確認

- READMEトップのCurrent releaseが`v1.1.0`
- READMEの直接Downloadリンクが開く
- `Ferry-v1.1.0-win-portable.zip`を取得できる
- Tag / Release title / Aboutがすべて`v1.1.0`
- Release ZIPをfresh folderへ展開して起動できる
- IssuesのBug report templateに`v1.1.0`が表示される
- Source treeのrootにRC資料が散らばっていない

## 8. 任意の公開品質項目

以下はv1.1.0公開の必須条件ではありません。

- Windows code signing certificateで`Ferry.exe`へ署名するか
- READMEのメインスクリーンショットをv1.1.0完成画面へ差し替えるか
- Ubuntu日本語コミュニティ等へ紹介するか
- winget等のpackage manager登録を後続Versionで行うか

署名しない場合でもFerryは公開できますが、未知のpublisherとしてWindows側の警告が出る可能性があります。
