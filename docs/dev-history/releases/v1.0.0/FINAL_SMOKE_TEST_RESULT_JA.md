# Ferry v1.0.0 最終スモークテスト結果

**実施日:** 2026-09-08  
**対象コードベース:** Ferry v1.0.0-rc15（正式版v1.0.0へロジック変更なしで昇格）  
**結果:** **19 / 19 PASS**

| # | 項目 | 結果 |
|---:|---|:---:|
| 1 | 起動 | PASS |
| 2 | フォルダー移動 / Back / Forward | PASS |
| 3 | Tabs / Ctrl+Tab / Ctrl+Shift+Tab | PASS |
| 4 | Search / Backspace / Esc / × | PASS |
| 5 | 通常選択 / 完全空白click解除 | PASS |
| 6 | Ctrl / Shift / Ctrl+A複数選択 | PASS |
| 7 | 複数選択 + Enter一括Open | PASS |
| 8 | 単一 / 複数選択D&D | PASS |
| 9 | F2 Rename / Ctrl+Z | PASS |
| 10 | Copy / Cut / Paste / Delete | PASS |
| 11 | ZIP Compress / Extract | PASS |
| 12 | Sort / folders-first | PASS |
| 13 | `.crdownload`外部更新 / 末尾保持 / Size更新 / F5再ソート | PASS |
| 14 | Windows詳細メニュー（2経路） | PASS |
| 15 | Open in Explorer / Open With | PASS |
| 16 | F12 Open Terminal Here | PASS |
| 17 | Settings保存 / 再起動保持 | PASS |
| 18 | Recycle Bin | PASS |
| 19 | 終了 → 再起動 | PASS |

## 追加で完了したtargeted test

- 壊れた`settings.json`からfactory defaultで正常起動し、正常なJSONへ再生成: PASS
- `settings.json`が存在しない状態から正常起動し、Settings保存で新規生成: PASS
- ダウンロード中の`.crdownload`でも通常click / Ctrl-click / F2 / D&D / F5: PASS
- 複数選択のWindows詳細メニュー: `Shift+Right-click` / Show more optionsの両方でPASS

## Release decision

RC15のapplication logicをFerry v1.0.0 finalへ昇格する。正式版化ではversion metadataと公開ドキュメントのみ更新し、新機能・interaction logicの変更は行わない。
