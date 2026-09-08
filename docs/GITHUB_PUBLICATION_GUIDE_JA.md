# Ferry v1.0.0をGitHubで初公開する手順

対象アカウント: `kazu0m1`  
Repository: `Ferry`

## 1. Repositoryを作成

GitHubで **New repository** を選びます。

推奨値:

- Repository name: `Ferry`
- Description: `A lightweight Windows file manager for people who miss the simplicity of Nautilus.`
- Visibility: **Public**
- README / .gitignore / license の自動追加: **しない**（このSource一式に含まれています）

## 2. 正式版Sourceをpush

Gitを使う場合の例:

```text
git init
git add .
git commit -m "Ferry v1.0.0 public release"
git branch -M main
git remote add origin https://github.com/kazu0m1/Ferry.git
git push -u origin main
```

GitHub Desktopを使う場合は、このフォルダーを既存ローカルRepositoryとして追加し、`kazu0m1/Ferry`へPublishしても構いません。

## 3. Windowsで正式版をビルド

Repository rootで、

```text
Build.cmd
```

を実行します。

`Portable\Ferry.exe`が生成されたら、一度起動してHomeが正常に表示されることを確認します。

RC15では最終スモークテスト19/19を完了済みです。正式版ではロジック変更をしていないため、ここでは長い再テストは不要です。

## 4. GitHub Release用Portable ZIPを生成

```text
Make-PortableRelease.cmd
```

成功すると、

```text
dist\Ferry-v1.0.0-win-portable.zip
```

が生成され、SHA-256が画面に表示されます。

## 5. Binary ZIPを最後に1回だけ確認

生成されたZIPを**別の新しいフォルダー**へ展開し、`Ferry.exe`を起動します。

最低限、次だけ確認します。

- Ferryが起動する
- Homeが開く
- F12で現在フォルダーにTerminalが開く
- Settingsを開ける

問題なければ、そのZIPをGitHub Release assetとして使用します。

## 6. Tag / Releaseを作成

GitHub Repository → **Releases** → **Create a new release**。

- Tag: `v1.0.0`
- Release title: `Ferry v1.0.0`
- Description: `RELEASE_NOTES_v1.0.0.md`を使用
- Asset: `dist\Ferry-v1.0.0-win-portable.zip`
- SHA-256: `Make-PortableRelease.cmd`が表示した値をRelease本文の末尾へ追記

**Pre-release**にはチェックを入れません。

## 7. 公開直後の確認

- READMEトップが意図どおり表示される
- `README.ja.md`へのリンクが動く
- Release ZIPをダウンロードできる
- Release本文とTagが`v1.0.0`になっている
- 新規フォルダーへ展開して`Ferry.exe`を起動できる
- Issuesが利用できる
- GNOME公式プロジェクトと誤認させる表現・ロゴがない

## 8. 公開後

まずGitHub Releasesで利用者・Issue・実機環境差の情報を集めます。wingetやMicrosoft Storeはv1.0.0公開後の別フェーズで検討すれば十分です。
