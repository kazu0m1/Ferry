# Ferry v1.1.6 正式リリースチェックリスト

v1.1.5を土台に、ドライブ文字を持たないMTP / Portable DeviceをSidebarで認識し、Windows Explorerへ開く簡易連携を追加した。Pixel 7a実機で2項目PASS済み。final化では検証済み挙動を変更せず、version / documentation / packagingのみv1.1.6へ昇格する。

## A. Source / version

- [x] Window title = `Ferry`
- [x] AssemblyVersion / FileVersion = `1.1.6.0`
- [x] InformationalVersion = `1.1.6`
- [x] app.manifest = `1.1.6.0`
- [x] `Make-PortableRelease.cmd` VERSION = `1.1.6`

## B. Windows interaction validation

- [x] Pixel 7aをUSB接続しファイル転送モードへ切り替えると、Sidebarの **Portable Devices** に端末が表示される
- [x] 端末項目をクリックするとWindows ExplorerでPixel 7aが開く
- [x] Windows GitHub Actions `Build.cmd` 成功

## C. Scope / preserved baseline

- [x] MTP内部のファイル列挙・コピー等はFerryで実装せずExplorerへ委譲
- [x] 既存の `WM_DEVICECHANGE / DBT_DEVNODES_CHANGED` Sidebar refreshを再利用
- [x] 通常のDrives列挙・リムーバブルドライブsafe-ejectを変更しない
- [x] v1.1.5 tab reorder / Sidebar folder context menuを維持
- [x] v1.1.4 responsive Shell Copy/Moveを維持
- [x] v1.1.3 removable-drive lifecycleを維持
- [x] v1.1.2 external D&D Move completionを維持
- [x] v1.1.1 Paste-result feedbackを維持
- [x] v1.1.0 Selection / rubber-band / Selection Anchorを維持

## D. Repository / documentation

- [x] `Portable/README.txt` = `Ferry v1.1.6 Portable`
- [x] Bug report templateのVersion例 = `v1.1.6`
- [x] `CHANGELOG.md`へv1.1.6を記録
- [x] `RELEASE_NOTES_v1.1.6.md`を作成
- [x] README / README.jaのcurrent release / direct downloadをv1.1.6へ更新
- [x] `Ferry_SPEC_v1.1.md`のrelease baselineをv1.1.6へ更新
- [x] Windows実機検証記録 = `docs/V1.1.6_PORTABLE_DEVICE_VALIDATION.md`

## E. Automated release gate

mainへのrelease merge後、Windows GitHub Actionsで以下を実行する。

- [ ] `Build.cmd` 成功
- [ ] `Make-PortableRelease.cmd` 成功
- [ ] `dist\Ferry-v1.1.6-win-portable.zip` 生成
- [ ] EXE FileVersion = `1.1.6.0`
- [ ] SHA-256算出
- [ ] exact main commitへannotated tag `v1.1.6`
- [ ] GitHub Release `Ferry v1.1.6` 作成
- [ ] Release本文 = `RELEASE_NOTES_v1.1.6.md`
- [ ] Asset = `Ferry-v1.1.6-win-portable.zip`
- [ ] 公開asset再download後のSHA-256 / FileVersion再検証
- [ ] Pre-release = OFF

A〜E完了で **Ferry v1.1.6 = RELEASED**。
