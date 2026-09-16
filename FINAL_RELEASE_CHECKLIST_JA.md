# Ferry v1.1.2 正式リリースチェックリスト

prototype 1でWindows実機テストがPASSしたFerry → Explorer D&D Move完了処理をv1.1.2仕様として凍結した。final化ではアプリケーション挙動を変更せず、version/titleと公開物だけを正式版へ切り替える。別volume D&Dはテスト環境上未実施であり、PASSとは記録しない。

## A. Source / version

- [ ] `Build.cmd` 成功
- [ ] Window title = `Ferry`
- [ ] AssemblyVersion / FileVersion = `1.1.2.0`
- [ ] InformationalVersion = `1.1.2`
- [ ] app.manifest = `1.1.2.0`
- [ ] `Make-PortableRelease.cmd` VERSION = `1.1.2`

## B. v1.1.2 External D&D final spot check

- [ ] Ferry → Explorer、同一ドライブ通常D&DでMoveになりsourceから消える
- [ ] Ferry → Explorer、`Ctrl+D&D`でCopyになりsourceが残る
- [ ] D&Dを`Esc`でCancelするとsourceが残る
- [ ] folder 1件のFerry → Explorer Moveが正常
- [ ] 複数itemのFerry → Explorer Moveが正常
- [ ] Ferry → Ferryの通常Move / Ctrl Copyが正常
- [ ] Explorer → Ferryの通常Move / Ctrl Copyが正常
- [ ] 別volume D&D: **未実施のままで可**（第二ドライブがない環境。実施した場合のみ結果を追記）

## C. v1.1.1 / v1.1.0回帰スモーク

- [ ] Copy→Pasteで今回Pasteしたitemだけが選択される
- [ ] Cut→Pasteでdestination itemが選択される
- [ ] 連続Pasteで2回目のresultだけが選択される
- [ ] List / Grid通常rubber-band
- [ ] true-background clear
- [ ] 選択済みitem D&D
- [ ] List ⇄ Gridでselection set維持

## D. Repository / documentation hygiene

- [x] `Portable/README.txt` = `Ferry v1.1.2 Portable`
- [x] Bug report templateのVersion例 = `v1.1.2`
- [x] prototype 1実装・テスト資料 = `docs/dev-history/releases/v1.1.2/`
- [x] prototype 1 Windows実機結果を記録（別volume未実施を明記）
- [x] `CHANGELOG.md`へv1.1.2を記録
- [x] `RELEASE_NOTES_v1.1.2.md`を作成
- [x] README / README.jaのcurrent release / direct downloadをv1.1.2へ更新
- [x] Source package静的監査実施
- [x] v1.1.2はcode signingなしで公開する方針

## E. Release artifact

- [ ] `Make-PortableRelease.cmd` 実行
- [ ] `dist\Ferry-v1.1.2-win-portable.zip` 生成
- [ ] fresh folderへ展開して起動
- [ ] About/version = `1.1.2`
- [ ] 最終Portable ZIPのSHA-256を保存

## F. GitHub

- [ ] final sourceをcommit
- [ ] そのexact commitへTag `v1.1.2`
- [ ] Release title: `Ferry v1.1.2`
- [ ] Release本文: `RELEASE_NOTES_v1.1.2.md`
- [ ] Asset: `Ferry-v1.1.2-win-portable.zip`
- [ ] Pre-release = OFF
- [ ] 公開後READMEの直接Download linkからassetを再取得
- [ ] fresh downloadを展開・起動して最終確認

A〜Fの必須項目PASSで **Ferry v1.1.2 = RELEASED**。
