# Ferry v1.0.2 Rubber Band Integration Prototype 6 — 実機確認

Prototype 3R を基準線とし、**Shift anchor の更新だけ**を局所修正した版。Gesture routing、true-background clear、Ctrl/Shift rubber-band、D&D は Prototype 3R のまま。

## ビルド
1. `Build.cmd`
2. `Portable\Ferry.exe`
3. タイトルが **Ferry - prototype 6** であること。

## A. 最重要 — List の Shift anchor
- [ ] true background を通常クリックして0件選択にする。
- [ ] 任意の未選択行 E の **行内余白を1回だけ**通常クリックする。
- [ ] 別行 H を `Shift+Click` → **E～Hだけ**が連続選択される。
- [ ] E以外の2行でも同じ操作を繰り返し、毎回1回目のクリックが新しいShift起点になる。
- [ ] 同じ行を2回クリックしないと直らない旧症状が消えている。

## B. Prototype 3R 回帰 — true background
- [ ] 1件選択 → true background単クリック → 0件選択。
- [ ] `Ctrl+Click`複数選択 → true background単クリック → 0件選択。
- [ ] `Shift+Click`範囲選択 → true background単クリック → 0件選択。
- [ ] rubber-band複数選択 → true background単クリック → 0件選択。

## C. Prototype 3R 回帰 — rubber-band / D&D
- [ ] 通常 rubber-band が成立する。
- [ ] `Ctrl + rubber-band` が XOR のまま。
- [ ] `Shift + rubber-band` が UNION のまま。
- [ ] 選択済み行から通常ドラッグすると file D&D になり、rubber-bandにならない。
- [ ] 通常の `Ctrl+Click` 複数選択が従来どおり。

## D. Grid 最小確認
- [ ] tile余白を1回クリック → 別tileを `Shift+Click` したとき、1回目のtileを起点に範囲選択される。
- [ ] true background単クリックで選択解除できる。
- [ ] 通常 / Ctrl / Shift rubber-band が Prototype 3R 相当で動く。

## 今回まだ対象外
- `Ctrl+Shift + rubber-band`
- 自動スクロール
- Modifierをdrag途中で押す/離す場合のExplorer完全一致
