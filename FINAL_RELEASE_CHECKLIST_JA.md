# Ferry v1.1.4 正式リリースチェックリスト

v1.1.3を土台に、長時間のWindows Shell Copy/MoveとFerry→Ferry D&DでUIが拘束される問題を修正した。アプリケーション挙動はWindows実機でPaste / Copy / Move / Ferry→Ferry D&DまでPASS済み。final化ではその検証済み挙動を変更せず、version / documentation / packagingのみv1.1.4へ昇格する。

## A. Source / version

- [x] Window title = `Ferry`
- [x] AssemblyVersion / FileVersion = `1.1.4.0`
- [x] InformationalVersion = `1.1.4`
- [x] app.manifest = `1.1.4.0`
- [x] `Make-PortableRelease.cmd` VERSION = `1.1.4`

## B. Responsive Shell transfer Windows validation

- [x] 大容量C:→D: Cut→Paste Moveが正常完了
- [x] 大容量D:→C: Cut→Paste Moveが正常完了
- [x] active Move中にFerryを最小化→復元可能
- [x] active Move中にtab追加 / tab切替 / folder navigation可能
- [x] 大容量Copy中も最小化→復元 / tab操作が可能で正常完了
- [x] active Copy/Move中のCloseで警告しFerryを終了しない
- [x] Ferry→Ferry D&D中、受信側が最小化→復元可能
- [x] Ferry→Ferry D&D中、送信側が最小化→復元可能
- [x] Ferry→Ferry D&D中、送信側で複数tab open / folder navigation可能
- [x] 同一drive内Ferry→Ferry D&D Moveが正常完了しsource重複なし

## C. Preserved baseline

- [x] Windows ShellがCopy/Move progress / conflict / cancelのauthorityのまま
- [x] v1.1.1 Paste-result feedbackとPaste completion contractを維持
- [x] Delete処理は今回のbugfixでは変更なし
- [x] v1.1.3 removable-drive lifecycleは変更なし
- [x] v1.1.2 external D&D Move completionは変更なし
- [x] v1.1.0 Selection / rubber-band / Selection Anchorは変更なし

## D. Repository / documentation

- [x] `Portable/README.txt` = `Ferry v1.1.4 Portable`
- [x] Bug report templateのVersion例 = `v1.1.4`
- [x] `CHANGELOG.md`へv1.1.4を記録
- [x] `RELEASE_NOTES_v1.1.4.md`を作成
- [x] README / README.jaのcurrent release / direct downloadをv1.1.4へ更新
- [x] `Ferry_SPEC_v1.1.md`のrelease baselineをv1.1.4へ更新
- [x] Windows検証記録 = `docs/RESPONSIVE_SHELL_TRANSFER_VALIDATION.md`

## E. Automated release gate

mainへのrelease merge後、Windows GitHub Actionsで以下を実行する。

- [ ] `Build.cmd` 成功
- [ ] `Make-PortableRelease.cmd` 成功
- [ ] `dist\Ferry-v1.1.4-win-portable.zip` 生成
- [ ] EXE FileVersion = `1.1.4.0`
- [ ] SHA-256算出
- [ ] exact main commitへannotated tag `v1.1.4`
- [ ] GitHub Release `Ferry v1.1.4` 作成
- [ ] Release本文 = `RELEASE_NOTES_v1.1.4.md`
- [ ] Asset = `Ferry-v1.1.4-win-portable.zip`
- [ ] 公開asset再download後のSHA-256 / FileVersion再検証
- [ ] Pre-release = OFF

A〜E完了で **Ferry v1.1.4 = RELEASED**。
