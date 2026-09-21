# Ferry v1.1.7 正式リリースチェックリスト

v1.1.6を土台に、Backで親フォルダーへ戻った際の文脈復元と、軽量右クリックメニューからのNew Text Document作成を追加した。Windows実機で両機能PASS済み。final化では検証済みアプリ挙動を変更せず、version / documentation / packagingのみv1.1.7へ昇格する。

## A. Source / version

- [x] Window title = `Ferry`
- [x] AssemblyVersion / FileVersion = `1.1.7.0`
- [x] InformationalVersion = `1.1.7`
- [x] app.manifest = `1.1.7.0`
- [x] `Make-PortableRelease.cmd` VERSION = `1.1.7`

## B. Windows interaction validation

- [x] 項目数の多い親フォルダーで下方の子フォルダーを開き、Backで戻ると直前の子フォルダーが選択・フォーカスされる
- [x] List viewで復元対象フォルダーがviewport最上部に表示される
- [x] 背景右クリックメニューに **New Text Document** が表示される
- [x] New Text Documentで空の `.txt` が作成され、そのままinline renameへ入る
- [x] Windows GitHub Actions `Build.cmd` 成功

## C. Scope / preserved baseline

- [x] Back focus restorationは「戻り先が直前フォルダーの直接の親」の場合だけ適用
- [x] Forward / Up / unrelated history navigationは変更しない
- [x] New Text Documentは既存inline renameを再利用
- [x] v1.1.6 Portable Device recognition / Explorer hand-offを維持
- [x] v1.1.5 tab reorder / Sidebar folder context menuを維持
- [x] v1.1.4 responsive Shell Copy/Moveを維持
- [x] v1.1.3 removable-drive lifecycleを維持
- [x] v1.1.2 external D&D Move completionを維持
- [x] v1.1.1 Paste-result feedbackを維持
- [x] v1.1.0 Selection / rubber-band / Selection Anchorを維持

## D. Repository / documentation

- [x] `Portable/README.txt` = `Ferry v1.1.7 Portable`
- [x] Bug report templateのVersion例 = `v1.1.7`
- [x] `CHANGELOG.md`へv1.1.7を記録
- [x] `RELEASE_NOTES_v1.1.7.md`を作成
- [x] README / README.jaのcurrent release / direct downloadをv1.1.7へ更新
- [x] `Ferry_SPEC_v1.1.md`のrelease baselineをv1.1.7へ更新
- [x] Windows実機検証記録 = `docs/V1.1.7_BACK_TEXT_VALIDATION.md`

## E. Automated release gate

mainへのrelease merge後、Windows GitHub Actionsで以下を実行する。

- [ ] `Build.cmd` 成功
- [ ] `Make-PortableRelease.cmd` 成功
- [ ] `dist\Ferry-v1.1.7-win-portable.zip` 生成
- [ ] EXE FileVersion = `1.1.7.0`
- [ ] SHA-256算出
- [ ] exact main commitへannotated tag `v1.1.7`
- [ ] GitHub Release `Ferry v1.1.7` 作成
- [ ] Release本文 = `RELEASE_NOTES_v1.1.7.md`
- [ ] Asset = `Ferry-v1.1.7-win-portable.zip`
- [ ] 公開asset再download後のSHA-256 / FileVersion再検証
- [ ] Pre-release = OFF

A〜E完了で **Ferry v1.1.7 = RELEASED**。
