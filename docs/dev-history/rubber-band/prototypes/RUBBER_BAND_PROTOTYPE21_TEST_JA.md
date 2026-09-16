# Ferry v1.0.2 Rubber Band Integration Prototype 21 — 実機確認

Prototype 20（List表示の通常/Ctrl/Shift/Ctrl+Shift、drag途中Modifier、D&D、autoscroll上限100まで確定）を基準に、Explorerで観察した **Ctrl+Shift + true background開始** のみを追加実装した版。Grid確認不要。

## ビルド
1. `Build.cmd`
2. `Portable\Ferry.exe`
3. タイトルが **Ferry - prototype 21** であること。

## A. 最重要 — Ctrl+Shift + true background / 初期選択A
1. true backgroundで0件にする。
2. Aを通常クリックしてAを選択。
3. `Ctrl+Shift` を押したまま、行より下のtrue backgroundでMouseDown。
4. thresholdを超えてrubber-bandを開始（まだ行には触れない）。
5. 下から上へ D → C → B → A の順にrectangleへ入れる。
6. そのまま縮めて A → B → C → D の順に外す。

期待：
- [ ] active開始直後：**A**
- [ ] Dだけhit：**A/D**
- [ ] Cまでhit：**A/C/D**
- [ ] Bまでhit：**A/B/C/D**
- [ ] Aまでhit：**B/C/D**
- [ ] Aを外す：**A/B/C/D**
- [ ] Bを外す：**A/C/D**
- [ ] Cを外す：**A/D**
- [ ] Dを外して0行hit：**A**

## B. Ctrl+Shift + true background / 初期選択A/C
1. true backgroundで0件。
2. Aを通常クリック → `Ctrl+Click` でCも追加し、A/Cを選択。
3. `Ctrl+Shift` を押したままtrue backgroundからrubber-band開始。
4. D → C → B の順に入れ、B → C → D の順に外す。

期待：
- [ ] active開始直後：**A/C**
- [ ] Dへ入る：**A/C/D**
- [ ] Cへ入る：**A/D**
- [ ] Bへ入る：**A/B/D**
- [ ] Bを外す：**A/D**
- [ ] Cを外す：**A/C/D**
- [ ] Dを外す：**A/C**

## C. 最小回帰
- [ ] Ctrl+Shiftを**未選択行の行内余白**から開始したB→C / C→BはPrototype 20相当。
- [ ] 通常 / Ctrl / Shift rubber-bandはPrototype 20相当。
- [ ] 選択済み行から通常dragするとfile D&D。
- [ ] autoscrollは正常。高速側上限は**100**のまま（速度の再評価不要）。

## 今回対象外
- Grid表示
- Ctrl+Shift true background開始中のModifier途中解放/再押下
