# Ferry v1.1.3 正式リリースチェックリスト

v1.1.2を土台に、リムーバブルドライブの接続反映とsafe ejectを修正した。アプリケーション挙動はWindows実機で単一タブ・複数タブまでPASS済み。final化ではその検証済み挙動を変更せず、version / documentation / packagingのみv1.1.3へ昇格する。

## A. Source / version

- [x] Window title = `Ferry`
- [x] AssemblyVersion / FileVersion = `1.1.3.0`
- [x] InformationalVersion = `1.1.3`
- [x] app.manifest = `1.1.3.0`
- [x] `Make-PortableRelease.cmd` VERSION = `1.1.3`

## B. Removable drive Windows validation

- [x] 起動後にリムーバブルドライブを接続するとDrivesへ自動表示
- [x] SidebarからドライブをOpen可能
- [x] 取り外し後にDrivesから消える
- [x] 再接続すると再表示され、再度Open可能
- [x] 1タブで対象ドライブを開いた状態からsafe eject成功
- [x] 2タブで対象ドライブを開いた状態からsafe eject成功
- [x] safe eject時、対象タブはHome等のローカルfallbackへ退避

## C. Preserved baseline

- [x] v1.1.2 External D&D Move completionは変更なし
- [x] v1.1.1 Paste-result feedbackは変更なし
- [x] v1.1.0 Selection / rubber-band / Selection Anchorは変更なし
- [x] 今回の変更にpolling loop / background serviceは追加しない

## D. Repository / documentation

- [x] `Portable/README.txt` = `Ferry v1.1.3 Portable`
- [x] Bug report templateのVersion例 = `v1.1.3`
- [x] `CHANGELOG.md`へv1.1.3を記録
- [x] `RELEASE_NOTES_v1.1.3.md`を作成
- [x] README / README.jaのcurrent release / direct downloadをv1.1.3へ更新
- [x] `Ferry_SPEC_v1.1.md`のrelease baselineをv1.1.3へ更新
- [x] Windows検証記録 = `docs/refactoring/REMOVABLE_DRIVE_REFRESH_STATIC_VALIDATION.md`

## E. Automated release gate

mainへのrelease merge後、Windows GitHub Actionsで以下を実行する。

- [ ] `Build.cmd` 成功
- [ ] `Make-PortableRelease.cmd` 成功
- [ ] `dist\\Ferry-v1.1.3-win-portable.zip` 生成
- [ ] EXE FileVersion = `1.1.3.0`
- [ ] SHA-256算出
- [ ] exact main commitへannotated tag `v1.1.3`
- [ ] GitHub Release `Ferry v1.1.3` 作成
- [ ] Release本文 = `RELEASE_NOTES_v1.1.3.md`
- [ ] Asset = `Ferry-v1.1.3-win-portable.zip`
- [ ] Pre-release = OFF

A〜E完了で **Ferry v1.1.3 = RELEASED**。
