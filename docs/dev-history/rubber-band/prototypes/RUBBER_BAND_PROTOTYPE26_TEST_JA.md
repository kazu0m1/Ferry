# Ferry v1.0.2 Rubber-band Integration Prototype 26 — Keyboard Focus 可視化 実機確認

Prototype 25 を基準に、Explorer同様に **keyboard focus/current item を破線で可視化**する。Selection Anchorとは別物として扱う。rubber-band、D&D、autoscroll、List⇄Grid同期、Prototype 24/25の修正には触れない。

## A. 最重要 — Listのfocus rectangle

1. `FILE_000011.txt` を通常クリック。
2. `↑` を3回押して `FILE_000008.txt` まで移動する。

確認：
- [ ] keyboard操作で移動するたび、現在focusしている行を **1px程度の破線rectangle** が追従する。
- [ ] `FILE_000008.txt` まで移動した時点で、破線は `FILE_000008.txt` 行にある。
- [ ] 選択色そのものは従来どおりで、破線が選択表示を壊さない。

3. その状態から `FILE_000020.txt` を `Shift+左クリック`。

確認：
- [ ] 既存仕様どおり `FILE_000011.txt` ～ `FILE_000020.txt` が範囲選択される（Shift anchorは11のまま）。
- [ ] focus可視化の追加によってShift anchorが8へ変わらない。

## B. Keyboard navigation 回帰

- [ ] 行内余白をクリック → `↓` / `↑` で隣接itemへ瞬時に移動する（Prototype 25修正維持）。
- [ ] 先頭/末尾へ一瞬で飛ぶ旧症状が再発しない。
- [ ] PageUp / PageDown / Home / Endが従来どおり使える。

## C. Grid最小確認

- [ ] Gridでも矢印キーでfocus移動すると、現在tileに破線rectangleが追従する。
- [ ] 選択表示、rubber-band、D&Dを邪魔しない。

## D. 最小回帰

- [ ] itemダブルクリックで開く。
- [ ] ScrollBarを素早くクリックしても選択itemが開かない。
- [ ] 通常rubber-bandが成立する。
- [ ] `Ctrl + rubber-band` / `Shift + rubber-band` が従来どおり。
- [ ] List / Grid autoscrollが従来どおり。

## 判定

- PASS / HOLD / FAIL
