# Ferry v1.1.0 正式リリースチェックリスト

RC20でWindows実機テストがオールクリアとなった挙動をv1.1.0仕様として凍結した。final化ではアプリケーション挙動を変更せず、version/titleと公開物だけを正式版へ切り替える。

## A. Source / version

- [ ] `Build.cmd` 成功
- [ ] Window title = `Ferry`
- [ ] AssemblyVersion / FileVersion = `1.1.0.0`
- [ ] InformationalVersion = `1.1.0`
- [ ] `Make-PortableRelease.cmd` VERSION = `1.1.0`

## B. Final UI spot check

- [ ] List左10px gutterが他headerと同色
- [ ] List選択色 / Selection Anchorが最右data columnで止まる
- [ ] List左gutter / 右側tailがtrue backgroundとして機能
- [ ] Breadcrumb horizontal scrollbarは10px、thumb / end buttonは直角
- [ ] Back / Forward / Up / Home等は30×30
- [ ] `Ctrl+L` → `Esc` / 再`Ctrl+L` / file-view clickでBreadcrumbへ戻る

## C. Selection / keyboard smoke

- [ ] List通常rubber-band
- [ ] Grid通常rubber-band
- [ ] List: `A → true background → ↓ = B`
- [ ] List: `A → true background → Shift+↓ = A/B`
- [ ] Grid: 4方向Arrowが視覚方向どおり移動
- [ ] Grid: true-background clear後も4方向Arrowが正常
- [ ] Selection Anchor表示
- [ ] 選択済みitem D&D
- [ ] List / Grid autoscroll
- [ ] List ⇄ Gridで選択集合維持

## D. Settings compatibility

- [ ] Rubber-band autoscroll speed = 30–300、default 100
- [ ] 設定保存・再起動後も値維持
- [ ] v1.0.2系の既存`settings.json`でも起動し、欠損autoscroll値は100

## E. Repository / documentation hygiene

- [x] `Portable/README.txt` = `Ferry v1.1.0 Portable`
- [x] Bug report templateのVersion例 = `v1.1.0`
- [x] v1.1.0 RC資料 = `docs/dev-history/releases/v1.1.0/`
- [x] `docs/dev-history/`を公開repositoryへ残す方針
- [x] v1.0.0〜v1.0.2 Release Notesをrootへ残す方針
- [x] v1.1.0はcode signingなしで公開する方針
- [x] Ubuntu日本語コミュニティへの紹介は今回保留
- [x] 現在の承認済みスクリーンショットをREADMEへ採用
- [x] Source package静的監査実施

## F. Release artifact

- [ ] `Make-PortableRelease.cmd` 実行
- [ ] `dist\Ferry-v1.1.0-win-portable.zip` 生成
- [ ] fresh folderへ展開して起動
- [ ] About/version = `1.1.0`
- [ ] 最終Portable ZIPのSHA-256を保存

## G. GitHub

- [ ] final sourceをcommit
- [ ] そのexact commitへTag `v1.1.0`
- [ ] Release title: `Ferry v1.1.0`
- [ ] Release本文: `RELEASE_NOTES_v1.1.0.md`
- [ ] Asset: `Ferry-v1.1.0-win-portable.zip`
- [ ] Pre-release = OFF
- [ ] 公開後READMEの直接Download linkからassetを再取得
- [ ] fresh downloadを展開・起動して最終確認

A〜Gの必須項目PASSで **Ferry v1.1.0 = RELEASED**。
