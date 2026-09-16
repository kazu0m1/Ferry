# Ferry v1.1.1 正式リリースチェックリスト

prototype 1でWindows実機テストがPASSしたPaste result feedbackをv1.1.1仕様として凍結した。final化ではアプリケーション挙動を変更せず、version/titleと公開物だけを正式版へ切り替える。

## A. Source / version

- [ ] `Build.cmd` 成功
- [ ] Window title = `Ferry`
- [ ] AssemblyVersion / FileVersion = `1.1.1.0`
- [ ] InformationalVersion = `1.1.1`
- [ ] `Make-PortableRelease.cmd` VERSION = `1.1.1`

## B. v1.1.1 Paste result final spot check

- [ ] 複数itemをCopy → 別folderへPasteし、今回Pasteしたトップレベルitemだけが選択される
- [ ] 既存itemへの上書き／folder merge後も、今回のdestination itemが選択される
- [ ] `Ctrl+X` → `Ctrl+V`でも移動先itemが選択される
- [ ] Paste後のtrue-background clickでselectionを解除できる
- [ ] Paste後にList ⇄ Gridを切り替えても同じselection setを維持する
- [ ] 連続2回Pasteすると2回目の結果だけが選択される
- [ ] 同一folder conflictはWindows標準UIへ委譲され、Skip/Cancel時にFerry独自のduplicateを生成しない

## C. v1.1.0回帰スモーク

- [ ] List / Grid通常rubber-band
- [ ] Ctrl / Shift / Ctrl+Shift selection
- [ ] true-background clear後のArrow / Shift+Arrow
- [ ] Selection Anchor表示
- [ ] 選択済みitem D&D
- [ ] List / Grid autoscroll
- [ ] List ⇄ Gridでselection set維持
- [ ] Breadcrumb / `Ctrl+L` / toolbarの主要操作

## D. Repository / documentation hygiene

- [x] `Portable/README.txt` = `Ferry v1.1.1 Portable`
- [x] Bug report templateのVersion例 = `v1.1.1`
- [x] prototype 1実装・テスト資料 = `docs/dev-history/releases/v1.1.1/`
- [x] `CHANGELOG.md`へv1.1.1を記録
- [x] `RELEASE_NOTES_v1.1.1.md`を作成
- [x] README / README.jaのcurrent release / direct downloadをv1.1.1へ更新
- [x] Source package静的監査実施
- [x] v1.1.1はcode signingなしで公開する方針

## E. Release artifact

- [ ] `Make-PortableRelease.cmd` 実行
- [ ] `dist\Ferry-v1.1.1-win-portable.zip` 生成
- [ ] fresh folderへ展開して起動
- [ ] About/version = `1.1.1`
- [ ] 最終Portable ZIPのSHA-256を保存

## F. GitHub

- [ ] final sourceをcommit
- [ ] そのexact commitへTag `v1.1.1`
- [ ] Release title: `Ferry v1.1.1`
- [ ] Release本文: `RELEASE_NOTES_v1.1.1.md`
- [ ] Asset: `Ferry-v1.1.1-win-portable.zip`
- [ ] Pre-release = OFF
- [ ] 公開後READMEの直接Download linkからassetを再取得
- [ ] fresh downloadを展開・起動して最終確認

A〜Fの必須項目PASSで **Ferry v1.1.1 = RELEASED**。
