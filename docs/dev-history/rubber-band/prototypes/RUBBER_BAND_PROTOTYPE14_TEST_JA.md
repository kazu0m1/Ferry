# Ferry v1.0.2 Rubber Band Integration Prototype 14 — 実機確認

Prototype 13 を直接基準にした修正版。F5/Refresh 経路は Prototype 13 から変更しない。Grid確認は不要。

## ビルド
1. `Build.cmd`
2. `Portable\Ferry.exe`
3. タイトルが **Ferry - prototype 14** であること。

## A. Shift anchor 回帰
- [ ] Aを通常クリック → Eを `Shift+Click` → A～E。
- [ ] その選択範囲内のCを修飾キーなしで1回クリック → Cだけ選択 → Gを `Shift+Click` → **C～Gだけ**。
- [ ] true backgroundで0件 → Eの行内余白を1回クリック → Hを `Shift+Click` → **E～Hだけ**。

## B. Ctrl + rubber-band
- [ ] A/C等を既存選択 → **true background**から `Ctrl + rubber-band` → XORになる。
- [ ] 同じ条件で **未選択行の行内余白**から開始しても `Ctrl + rubber-band` が開始し、XORになる。
- [ ] 矩形を縮めたり戻したりしても同じ位置で選択状態が安定する。

## C. Shift + rubber-band
- [ ] Aを既存選択 → **true background**から `Shift + rubber-band` → A + 現在矩形の UNION。
- [ ] Aを既存選択 → **未選択行の行内余白**から開始しても Shift の通常範囲選択へ化けず、rubber-bandが開始して UNIONになる。
- [ ] 矩形を縮めると、矩形から外れた新規追加分だけが解除され、開始前のAは維持される。

## D. Ctrl+Shift — B → C
連続した3行を A / B / C とする。
1. true backgroundで選択解除。
2. Aを通常クリック。
3. `Ctrl+Shift` を押したまま未選択B行の行内余白でMouseDown。
4. B行内で横へ数px動かしてrubber-band開始。
5. そのままC行へ入る。

- [ ] MouseDown直後は A/B。
- [ ] rubber-band開始時も A/B。
- [ ] Cへ入ると **A/B/C**。
- [ ] MouseUp後も A/B/C。

## E. Ctrl+Shift — C → B
1. true backgroundで選択解除。
2. Aを通常クリック。
3. `Ctrl+Shift` を押したまま未選択C行の行内余白でMouseDown。
4. C行内で横へ数px動かしてrubber-band開始。
5. その後B行へ入る。

- [ ] MouseDown直後は **A/C**。
- [ ] rubber-band開始時点で **A/B/C**。
- [ ] B行へ入ると **A/C**（Bだけ解除）。
- [ ] C行内へ戻すと **A/B/C**。

## F. 最小回帰
- [ ] true background単クリックで0件選択。
- [ ] 選択済み行から通常dragするとfile D&Dで、rubber-bandにならない。
- [ ] 未選択行のicon/name等file hot zoneからdragしてもrubber-bandにならない。
- [ ] autoscrollはPrototype 11相当で動作し、高速側上限は25のまま。

## 今回対象外
- Grid表示
- Ctrl+Shiftをtrue backgroundから開始した場合の完全一致
- drag途中でModifierを押す/離す場合のExplorer完全一致
- autoscroll速度の追加評価
