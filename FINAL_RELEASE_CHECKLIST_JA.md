# Ferry v1.0.1 正式リリースチェックリスト

RC4の実機確認済みロジックを、機能変更なしでv1.0.1へ昇格する。

## A. Source / metadata

- [ ] `AssemblyInformationalVersion` が `1.0.1`
- [ ] Settings → About Ferry が `Version 1.0.1` を表示
- [ ] `Portable/README.txt` が `Ferry v1.0.1 Portable`
- [ ] `Make-PortableRelease.cmd` が `VERSION=1.0.1`
- [ ] README / README.ja のCurrent releaseとDownloadリンクがv1.0.1
- [ ] `RELEASE_NOTES_v1.0.1.md`を確認

## B. Windows build

1. `Build.cmd` を実行
2. `Portable\Ferry.exe` を起動
3. 最小確認:
   - [ ] Home / Navigation
   - [ ] Inline Rename / New Folder
   - [ ] Pinned D&D reorder
   - [ ] List icon / Grid thumbnail
   - [ ] 全角/半角検索
   - [ ] F12 Open Terminal Here
   - [ ] ZIP圧縮/展開ProgressBarと3秒完了表示
   - [ ] Settings → About Ferry = Version 1.0.1

## C. Portable ZIP

- [ ] `Make-PortableRelease.cmd` を実行
- [ ] `dist\Ferry-v1.0.1-win-portable.zip` が生成
- [ ] ZIPを別フォルダーへ展開して`Ferry.exe`を起動
- [ ] `config`がローカルに生成/保存される

## D. GitHub

- [ ] 正式版Source内容をGit管理フォルダーへ反映
- [ ] prototype/RC一時文書を公開ルートから削除
- [ ] Commit / Push
- [ ] Tag: `v1.0.1`
- [ ] Release title: `Ferry v1.0.1`
- [ ] Release本文: `RELEASE_NOTES_v1.0.1.md`
- [ ] Asset: `Ferry-v1.0.1-win-portable.zip`
- [ ] READMEの直接DownloadリンクからAssetを取得できることを確認
