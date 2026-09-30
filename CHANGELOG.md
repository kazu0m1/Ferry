# Changelog

## v1.2.1 — 2026-09-30

- Added **Open To-Do tab at startup** to Settings, default Off.
- Returning to the To-Do tab now restores the previous row/cell, caret position, and text selection.
- Fixed To-Do/Memo header and row column-boundary drift when a vertical scrollbar appears in smaller windows.
- Promoted release metadata and portable packaging to v1.2.1.

## v1.2.0 — 2026-09-26

- Added the Sidebar **To-Do / Memo scratchpad** with numbered, vertically aligned paired rows.
- Added To-Do keyboard editing/navigation: Enter, Shift+Enter, arrow-key row/column movement, and empty-To-Do Backspace row deletion.
- Pinned the To-Do tab to the leftmost tab position while open.
- Saved To-Do data in the existing portable `config\settings.json`; no separate To-Do store is created.
- Included To-Do data in Settings export/import while keeping general Settings reset from erasing it.
- Fixed single-item inline rename so Up/Down navigation works immediately after both Enter commit and Esc cancel.

﻿# Ferry Changelog

## [1.1.7] - 2026-09-21

### Added
- Ferryの軽量な背景右クリックメニューに **New Text Document** を追加した。
- 空の `.txt` をcollision-safeな名前で作成し、そのまま既存のinline renameへ入るようにした。

### Changed
- 子フォルダーを開いた後に **Back** で親フォルダーへ戻ると、直前に開いていたフォルダーを再選択・フォーカスするようにした。
- List viewでは、その復元対象フォルダーをviewportの最上部へ揃えて表示し、戻った位置をすぐ確認できるようにした。

### Validated
- Windows GitHub Actions `Build.cmd`: PASS。
- 項目数の多い親フォルダーで下方の子フォルダーを開き、Backで戻った際に対象フォルダーが選択・フォーカスされること: PASS。
- 対象フォルダーがList view最上部へ表示されること: PASS。
- 背景右クリック → **New Text Document** で `.txt` を作成し、そのままファイル名編集状態になること: PASS。
