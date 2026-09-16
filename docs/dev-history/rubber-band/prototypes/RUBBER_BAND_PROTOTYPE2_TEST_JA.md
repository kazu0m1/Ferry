# Ferry v1.0.2 Rubber Band Integration Prototype 2 — 実機確認

Prototype 1 の全項目は PASS 済み。今回は空白クリックによる選択解除の回帰修正だけを重点確認する。

## ビルド
1. `Build.cmd`
2. `Portable\Ferry.exe`

## 必須確認
- [ ] List: rubber-band で複数選択 → アイテム群より下の true background を単クリック → 選択が 0 件になる。
- [ ] List: `Ctrl+Click` で複数選択 → true background を単クリック → 選択が 0 件になる。
- [ ] List: `Shift+Click` で範囲選択 → true background を単クリック → 選択が 0 件になる。
- [ ] Grid: 複数選択後に true background を単クリック → 選択が 0 件になる。
- [ ] true background をドラッグすると、Prototype 1 同様に rubber-band が開始する。
- [ ] 未選択行/タイルの余白を単クリックすると、その1項目が選択される。
- [ ] 選択済み行/タイルの余白からの D&D は従来どおり機能する。

上記がすべて通れば Prototype 2 は PASS とする。
