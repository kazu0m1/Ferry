# Ferry v1.0.2 Rubber Band Integration Prototype 3 — 実機確認

Prototype 2 は PASS 済み。今回は **Ctrl / Shift + rubber-band** の集合演算と既存操作の回帰だけを確認する。

## ビルド
1. `Build.cmd`
2. `Portable\Ferry.exe`

## 必須確認 — List
- [ ] **Ctrl XOR**: A と C を選択済みにする → `Ctrl` を押したまま true background から rubber-band で B と C に交差させる → 最終的に **A と B が選択、C は解除**される。
- [ ] 上記 Ctrl rubber-band を縮めたり広げたりしても、同じ位置では選択結果が安定し、チラつくような ON/OFF 反転を繰り返さない。
- [ ] **Shift UNION**: A を選択済みにする → `Shift` を押したまま true background から rubber-band で B と C に交差させる → **A/B/C が選択**される。
- [ ] Shift rubber-band を縮めて B が矩形外になった場合 → **A は維持、B は解除、C は選択**となる。
- [ ] `Ctrl+Click` の通常の複数選択が従来どおり使える。
- [ ] `Shift+Click` の通常の範囲選択が従来どおり使える。
- [ ] Ctrl/Shift で作った複数選択も、修飾キーなしで true background を単クリックすると 0 件選択になる（Prototype 2 回帰確認）。
- [ ] 選択済み行の余白から通常ドラッグすると、rubber-band ではなく従来どおりファイル D&D になる。

## 必須確認 — Grid
- [ ] Ctrl + rubber-band で既存選択との XOR が成立する。
- [ ] Shift + rubber-band で既存選択との UNION が成立する。

## 今回まだ対象外
- `Ctrl+Shift + rubber-band`
- 自動スクロール
- Modifier をドラッグ途中で押す / 離す場合の Explorer 完全一致

上記がすべて通れば Prototype 3 は PASS とする。
