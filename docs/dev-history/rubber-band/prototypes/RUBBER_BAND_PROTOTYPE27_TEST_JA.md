# Ferry v1.0.2 Rubber-band Integration Prototype 27 — Shift Anchor 可視化 実機確認

Prototype 26 の破線は WPF keyboard focus/current を表示していたため不採用。本版は Prototype 25 の安定動作を維持しつつ、**Shift+Click の範囲起点となる Ferry logical Selection Anchor** を破線で表示する。

## A. 最重要 — keyboard移動とanchorの分離

1. `FILE_000011.txt` を通常クリック。
2. ↑キーを3回押し、選択を `FILE_000008.txt` まで移動。

期待：
- [ ] 選択色は `FILE_000008.txt` へ移動する。
- [ ] 破線は **`FILE_000011.txt` に残る**。
- [ ] `FILE_000020.txt` を `Shift+Click` → **FILE_000011.txt ～ FILE_000020.txt** が範囲選択される。
- [ ] 破線は範囲起点の `FILE_000011.txt` を示し続ける。

## B. anchor更新

- [ ] 別itemを修飾キーなしで通常クリック → 破線がそのitemへ移る。
- [ ] 行内余白を通常クリックした場合も破線がそのitemへ移る。
- [ ] 複数選択内の1itemを通常クリックして1件へcollapse → 破線もそのitemへ移る。
- [ ] View切替 List → Grid → List 後もanchorが不自然な旧位置へ戻らない。

## C. スクロール / 仮想化

- [ ] anchorが画面内なら破線がitemに追従する。
- [ ] anchorを画面外へスクロールすると破線は消える。
- [ ] anchorを再び画面内へ戻すと破線が正しいitemへ再表示される。
- [ ] 10,000 itemフォルダーでも破線表示のためにスクロールやキー操作が重くならない。

## D. Prototype 25 / rubber-band 最小回帰

- [ ] 行内余白クリック → ↑/↓で隣接itemへ瞬時に移動する。
- [ ] 通常 / Ctrl / Shift / Ctrl+Shift rubber-band が従来どおり。
- [ ] List / Grid autoscroll が従来どおり。
- [ ] 選択済みitemからのdragは file D&D。
- [ ] ScrollBar高速クリックで選択itemが誤って開かない。
