# Ferry v1.0.2 Rubber-band Integration Prototype 25 — Keyboard focus 回帰確認

基準線：Prototype 24（scrollbar double-click 修正 PASS）。  
本版は、Ferry が所有する stationary row/tile-whitespace click 後に **選択itemへ keyboard focus/current を同期する**局所修正のみ。

## A. 最重要 — 10,000 item / List / 行内余白

Folders First を有効にし、DIR群の直後に `FILE_000001.txt` がある状態。

- [ ] `FILE_000001.txt` の**行内余白**を通常クリック → `↓`
  - 期待：`FILE_000002.txt` へ移る。
  - 長時間停止・末尾へのワープがない。
- [ ] `FILE_009999.txt` の**行内余白**を通常クリック → `↑`
  - 期待：直前itemへ移る。
  - 長時間停止・先頭へのワープがない。

## B. native click 基準線

- [ ] `FILE_000001.txt` の**名前部分**を通常クリック → `↓`
  - 期待：従来どおり `FILE_000002.txt`。

## C. Shift range endpoint

- [ ] 数item手前を通常クリック → `FILE_000001.txt` を `Shift+Click` で範囲終点にする。
- [ ] Shiftを離して `↓`
  - 期待：keyboard navigation が `FILE_000001.txt` を起点に続き、末尾へワープしない。
  - 長時間停止しない。

## D. Ctrl stationary click

- [ ] 行内余白を `Ctrl+Click` してそのitemを最後に操作した状態にする → `↑` / `↓`
  - 期待：そのitem近傍からkeyboard navigationが始まる。
  - 先頭/末尾へワープしない。

## E. 最小回帰

- [ ] 行内余白1回クリック → 別行 `Shift+Click` のShift anchorが従来どおり。
- [ ] 通常 / Ctrl / Shift / Ctrl+Shift rubber-band が開始できる。
- [ ] 選択済みitemからのdragはfile D&D。
- [ ] scrollbarを素早くクリックしても選択itemが開かない。
- [ ] item本体のダブルクリックは従来どおり開く。

## 判定

**PASS / HOLD / FAIL**
