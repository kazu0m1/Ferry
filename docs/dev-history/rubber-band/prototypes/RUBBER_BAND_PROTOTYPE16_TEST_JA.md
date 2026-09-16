# Ferry v1.0.2 Rubber Band Integration Prototype 16 — 実機確認

Prototype 15（高速autoscroll 30、通常/Ctrl/Shift/Ctrl+Shift、Shift anchor、D&DまでPASS）を基準に、Explorerで追加観察した **drag途中Modifier** を反映した版。Grid確認は不要。

## ビルド
1. `Build.cmd`
2. `Portable\Ferry.exe`
3. タイトルが **Ferry - prototype 16** であること。

## A. 最小回帰
- [ ] true background単クリックで0件選択。
- [ ] 通常rubber-bandが成立する。
- [ ] `Ctrl + rubber-band` は、Ctrlを押したままなら従来どおりXOR。
- [ ] `Ctrl+Shift + rubber-band` のB→C / C→BはPrototype 15相当。
- [ ] 選択済み行から通常dragするとfile D&D。
- [ ] autoscrollは正常、高速側上限は30のまま。

## B. Explorer再観察反映 — Shift開始のactive rubber-band
事前選択をA/C、rubber-bandの矩形をB/Cにする。

1. A/Cを選択。
2. `Shift` を押したまま **true background** でMouseDown。
3. drag thresholdを超えてrubber-bandを開始し、B/Cを囲む。

期待：
- [ ] activeになった時点で **B/Cだけ**。Aは残らない。
- [ ] Shiftを離しても **B/Cのまま**。
- [ ] B/Cの範囲を変えない1～2px移動でも **B/Cのまま**。

※ Stationaryな通常の `Shift+Click` 範囲選択は従来どおりであること。

## C. Normal開始後のModifier追加は無視
事前選択A/C → Modifierなしtrue backgroundからrubber-band開始 → B/Cを囲む。

- [ ] この時点で **B/C**。
- [ ] Ctrlを押す → **B/Cのまま**。
- [ ] Ctrlを離す → **B/Cのまま**。
- [ ] Shiftを押す → **B/Cのまま**。
- [ ] Shiftを離す → **B/Cのまま**。
- [ ] Modifierを押した状態でもDへ広げるとD選択、Dを外すと解除、再びDへ入ると選択。つまりNormal bandのまま。

## D. Ctrl開始 → Active中にCtrl解放
1. A/Cを選択。
2. Ctrlを押したままtrue backgroundからrubber-band開始。
3. B/Cを囲む。

期待：
- [ ] Ctrlを押したままなら **A/B**（XOR）。
- [ ] その位置でCtrlを離しても **A/Bのまま**。
- [ ] B/Cの範囲を変えない1～2px移動でも **A/Bのまま**。
- [ ] その後Cをrectangle外へ出してもA/Bのまま、Cへ再侵入すると **A/B/C** になる。

## E. Ctrl MouseDown → threshold前にCtrl解放
1. A/Cを選択。
2. Ctrlを押したままtrue backgroundでMouseDown。
3. thresholdを超える前にCtrlを離す。
4. Modifierなしでdrag開始し、B/Cを囲む。

期待：
- [ ] **A/B/C**。
- [ ] Cをrectangle外へ出すと **A/B**。
- [ ] Cへ戻すと **A/B/C**。

## F. Shift MouseDown → active中にShift解放
1. A/Cを選択。
2. Shiftを押したままtrue backgroundからrubber-band開始しB/Cを囲む。
3. Shiftを離す。

期待：
- [ ] 開始時から **B/C**。
- [ ] Shift解放後も **B/C**。
- [ ] 範囲を変えない小移動でも **B/C**。

## 今回対象外
- Grid表示
- Ctrl+Shiftをdrag途中で押す / 離す場合
- Ctrlを一度離した後に再度押し直す場合のExplorer完全一致
- autoscroll速度の再評価
