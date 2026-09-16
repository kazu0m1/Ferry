# Ferry v1.0.2 Rubber Band Integration Prototype 23 — View切替選択同期 実機確認

Prototype 22 は List/Grid の rubber-band・autoscroll までPASS。大量フォルダー試験で、Listで1000件を選択してGridへ切り替えると1026件へ増えるケースが判明した。本版は **List ⇔ Grid 切替時の選択集合同期だけ**を追加する。rubber-band / D&D / autoscroll上限100は変更しない。

## A. 最重要 — List → Grid 1000件
1. 5,000件または10,000件テストフォルダーをList表示で開く。
2. 任意の連続1000件をShift範囲選択する。
3. 選択件数と先頭/末尾を確認する。
4. Grid表示へ切り替える。

期待：
- [ ] 選択件数が **1000件のまま**。
- [ ] 選択されている先頭/末尾がList時と同じ。
- [ ] 余計なtileが追加選択されない／選択が欠けない。
- [ ] View切替で「固まった」と感じる長時間停止がない。

## B. Grid → List の逆方向
1. Grid側で選択を少し変更する（数件追加/解除、または別の連続範囲を選ぶ）。
2. 現在の選択件数と分かりやすい先頭/末尾を確認。
3. Listへ戻す。

期待：
- [ ] Gridで確定した選択集合がListへそのまま引き継がれる。
- [ ] Gridで解除した項目がListで復活しない。
- [ ] Gridで追加した項目がListで消えない。

## C. Shift anchor の最小確認
1. 少数項目でBを通常クリックしてanchorにする。
2. List ⇔ Gridを1回切り替える。
3. DをShift+Clickする。

期待：
- [ ] Bを起点にB～D相当が範囲選択される。切替前の古いanchorへ戻らない。

## D. 最小回帰
- [ ] 切替後も通常rubber-bandが開始できる。
- [ ] 選択済みitemからのdragはfile D&Dのまま。
- [ ] List/Grid autoscrollは従来どおり。

## 今回不要
- 20,000件試験
- Shiftで10,000件全件を選ぶ性能評価
- autoscroll速度の再評価
