# Ferry v1.1.5 正式リリースチェックリスト

v1.1.4を土台に、タブ並べ替えとSidebarフォルダーの右クリック操作を追加した。アプリケーション挙動はWindows実機で4項目PASS済み。final化ではその検証済み挙動を変更せず、version / documentation / packagingのみv1.1.5へ昇格する。

## A. Source / version

- [x] Window title = `Ferry`
- [x] AssemblyVersion / FileVersion = `1.1.5.0`
- [x] InformationalVersion = `1.1.5`
- [x] app.manifest = `1.1.5.0`
- [x] `Make-PortableRelease.cmd` VERSION = `1.1.5`

## B. Windows interaction validation

- [x] 3タブ程度を開き、タブを左右へドラッグして任意順へ並べ替え可能
- [x] タブの `×` は従来どおりCloseとして機能し、誤ってドラッグにならない
- [x] SidebarのHome / Documents / Pinned等で右クリックメニューが表示され、**Open** で現在タブに開ける
- [x] Pinned folderの右クリックメニューに **Unpin** が残っている
- [x] Windows GitHub Actions `Build.cmd` 成功

## C. Preserved baseline

- [x] タブD&Dは `Ferry.TabItem` 専用formatでファイルD&Dと分離
- [x] Pinned folder D&D並べ替えを維持
- [x] v1.1.4 responsive Shell Copy/Move / Ferry→Ferry D&D behaviorを維持
- [x] v1.1.3 removable-drive lifecycleを維持
- [x] v1.1.2 external D&D Move completionを維持
- [x] v1.1.1 Paste-result feedbackを維持
- [x] v1.1.0 Selection / rubber-band / Selection Anchorを維持

## D. Repository / documentation

- [x] `Portable/README.txt` = `Ferry v1.1.5 Portable`
- [x] Bug report templateのVersion例 = `v1.1.5`
- [x] `CHANGELOG.md`へv1.1.5を記録
- [x] `RELEASE_NOTES_v1.1.5.md`を作成
- [x] README / README.jaのcurrent release / direct downloadをv1.1.5へ更新
- [x] `Ferry_SPEC_v1.1.md`のrelease baselineをv1.1.5へ更新
- [x] Windows検証記録 = `docs/V1.1.5_TAB_SIDEBAR_VALIDATION.md`

## E. Automated release gate

mainへのrelease merge後、Windows GitHub Actionsで以下を実行する。

- [ ] `Build.cmd` 成功
- [ ] `Make-PortableRelease.cmd` 成功
- [ ] `dist\Ferry-v1.1.5-win-portable.zip` 生成
- [ ] EXE FileVersion = `1.1.5.0`
- [ ] SHA-256算出
- [ ] exact main commitへannotated tag `v1.1.5`
- [ ] GitHub Release `Ferry v1.1.5` 作成
- [ ] Release本文 = `RELEASE_NOTES_v1.1.5.md`
- [ ] Asset = `Ferry-v1.1.5-win-portable.zip`
- [ ] 公開asset再download後のSHA-256 / FileVersion再検証
- [ ] Pre-release = OFF

A〜E完了で **Ferry v1.1.5 = RELEASED**。
