# Ferry v1.0.2 正式リリースチェックリスト

WindowsでACCEPT済みのv1.0.2-rc1ロジックを、機能変更なしでv1.0.2へ昇格する。

## A. Source / metadata

- [ ] `AssemblyVersion` = `1.0.2.0`
- [ ] `AssemblyFileVersion` = `1.0.2.0`
- [ ] `AssemblyInformationalVersion` = `1.0.2`
- [ ] Settings → About Ferry = `Version 1.0.2`
- [ ] `Portable/README.txt` = `Ferry v1.0.2 Portable`
- [ ] `Make-PortableRelease.cmd` = `VERSION=1.0.2`
- [ ] README / README.ja のCurrent releaseと直接Downloadリンクがv1.0.2
- [ ] `RELEASE_NOTES_v1.0.2.md`を確認

## B. Windows final gate

1. `Build.cmd` を実行する。
2. `Portable\Ferry.exe` を起動する。
3. 最小確認:
   - [ ] 正常起動する
   - [ ] About = `Version 1.0.2`
   - [ ] 通常のZIP圧縮を1回実行できる
   - [ ] 通常のZIP展開を1回実行できる
   - [ ] 圧縮/展開後も通常のフォルダー移動ができる

RC1で回帰・安全・競合・Cancel・PortableスモークまでPASS済みのため、Finalでは同じ網羅試験を繰り返さない。

## C. Portable ZIP

- [ ] `Make-PortableRelease.cmd` を実行
- [ ] `dist\Ferry-v1.0.2-win-portable.zip` が生成
- [ ] 表示されたSHA-256を記録
- [ ] ZIPを新しい別フォルダーへ展開
- [ ] 展開先の`Ferry.exe`が正常起動
- [ ] About = `Version 1.0.2`
- [ ] `config`がローカルに生成/保存される
- [ ] ZIP圧縮/展開を各1回実行できる

## D. GitHub

- [ ] 正式版Source内容をGit管理フォルダーへ反映
- [ ] RC用一時文書が公開ルートから削除されている
- [ ] Commit / Push
- [ ] Tag: `v1.0.2`
- [ ] Release title: `Ferry v1.0.2`
- [ ] Release本文: `RELEASE_NOTES_v1.0.2.md`
- [ ] Asset: `Ferry-v1.0.2-win-portable.zip`
- [ ] Pre-release: OFF
- [ ] READMEの直接DownloadリンクからAssetを取得できる

全項目PASSで **Ferry v1.0.2 = RELEASED**。
