# Ferry v1.0.2 Rubber Band Integration Prototype 9 — 実機確認

Prototype 6 を基準線に戻し、Shift anchor 更新だけを変更した版。Prototype 7/8 の変更は含めない。Grid確認は不要。

## ビルド
1. `Build.cmd`
2. `Portable\Ferry.exe`
3. タイトルが **Ferry - prototype 9** であること。

## A. 最重要 — Shift anchor の応答
- [ ] true background をクリックして0件選択 → Eの行内余白を1回だけ通常クリック → Hを `Shift+Click` → **E～Hだけ**が選択される。
- [ ] 上記を異なる起点・終点で10回程度、やや速めに繰り返しても、古いanchorへ戻る「すっぽ抜け」が出ない。
- [ ] 同じ行を2回クリックしないとanchorが更新されない旧症状が出ない。
- [ ] 体感レスポンスが Prototype 6 と同等以上である。

## B. Prototype 6 回帰
- [ ] true background単クリックで選択解除できる。
- [ ] 通常 rubber-band が成立する。
- [ ] `Ctrl + rubber-band` が XOR のまま。
- [ ] `Shift + rubber-band` が UNION のまま。
- [ ] 選択済み行から通常ドラッグすると file D&D になる。
- [ ] 通常の `Ctrl+Click` が従来どおり。

## 今回まだ対象外
- Grid表示
- `Ctrl+Shift + rubber-band`
- 自動スクロール
- Modifierをdrag途中で押す/離す場合のExplorer完全一致
