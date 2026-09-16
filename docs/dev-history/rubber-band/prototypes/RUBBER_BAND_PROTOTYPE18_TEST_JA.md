# Ferry v1.0.2 Rubber Band Integration Prototype 18 — 実機確認

Prototype 17（Prototype 16 PASS + autoscroll上限50）を基準に、Explorerで追加観察した **Ctrl再押下 / Ctrl+Shift途中解放** を反映し、autoscroll高速側上限を **80 lines/sec** に変更した版。Grid確認不要。

## ビルド
1. `Build.cmd`
2. `Portable\Ferry.exe`
3. タイトルが **Ferry - prototype 18** であること。

## A. 最小回帰
- [ ] true background単クリックで0件選択。
- [ ] 通常 / Ctrl / Shift rubber-band がPrototype 16相当。
- [ ] Ctrl+Shift B→C / C→B が、Modifierを途中解放しなければPrototype 16相当。
- [ ] 選択済み行から通常dragするとfile D&D。

## B. Ctrl開始 → Ctrl解放 → Ctrl再押下
1. A/Cを選択。
2. Ctrlを押したままtrue backgroundからrubber-band開始し、B/Cを囲む → **A/B**。
3. Ctrlを離す → **A/Bのまま**。
4. Cを矩形外へ出す→戻す → **A/B → A/B/C**。
5. Ctrlを再度押す。
6. Cを再び矩形外へ出す→戻す。

期待：
- [ ] Ctrl再押下直後 **A/B/C**。
- [ ] Cを外すと **A/B**。
- [ ] Cへ再侵入すると **A/B/C**。
- [ ] Ctrl再押下でXORモードへ戻らない。

## C. Ctrl+Shift B→C 中に Shiftだけ離す（Ctrl保持）
1. true backgroundで0件 → Aを通常クリック。
2. Ctrl+Shiftを押したまま未選択B行の行内余白からrubber-band開始。
3. Cへ入れて **A/B/C**。
4. 左ボタンとCtrlは保持し、Shiftだけ離す。
5. Cを矩形外へ出す → Cへ再侵入。

期待：
- [ ] Shift解放直後 **A/B/C**。
- [ ] Cを外すと **B/C**。
- [ ] Cへ再侵入すると **A/B/C**。

## D. Ctrl+Shift C→B 中に Ctrlだけ離す（Shift保持）
1. true backgroundで0件 → Aを通常クリック。
2. Ctrl+Shiftを押したまま未選択C行の行内余白からrubber-band開始。
3. Bへ入れて **A/C**。
4. 左ボタンとShiftは保持し、Ctrlだけ離す。
5. BからC側へ戻す → 再びBへ入る。

期待：
- [ ] Ctrl解放直後 **A/C**。
- [ ] C側へ戻すと **A/B/C**。
- [ ] 再びBへ入ると **A/C**。

## E. autoscroll
高速側上限は **80 lines/sec**。速度値はユーザー指定なので、選択ロジック確認と切り離して扱う。

## 今回対象外
- Grid表示
- Ctrl+Shiftをtrue backgroundから開始する完全一致
- Ctrl+Shift途中解放の未観察パターン（両キー同時解放・解放後の再押下など）
