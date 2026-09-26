# Ferry v1.2 Specification Delta

This document describes the user-visible delta from the Ferry v1.1.7 baseline to **Ferry v1.2.0**.

## 1. To-Do virtual tab

Ferry adds a Sidebar item named **To-Do**. Activating it opens or reuses a dedicated virtual tab.

- The To-Do tab is not a filesystem `TabState`.
- It is pinned at tab index 0 while open.
- It cannot be moved by tab drag and ordinary filesystem tabs cannot be dropped before it.
- `Ctrl+W` closes the To-Do tab.
- Closing Ferry flushes pending To-Do changes.

## 2. Row model and alignment

Each item is one numbered row containing:

- left: **To-Do** plain-text editor
- right: **Memo** plain-text editor

Both editors are hosted by the same physical two-column row container. The row height is therefore the larger of the two editor contents, and the next To-Do/Memo pair always begins at the same vertical position.

Display numbers are derived from the current row order and are not separately persisted.

## 3. Editing behavior

### To-Do column

- `Enter`: split/create the next numbered row.
- `Shift+Enter`: newline inside the current row.
- `Backspace` at caret position 0 when the To-Do text is empty: remove that row, including its Memo content.
- If only one row exists, deletion leaves one empty row rather than zero rows.

### Memo column

- Plain multiline text.
- `Enter` inserts a normal newline.

### Arrow navigation

- Up/Down move between rows when the caret is at the first/last visual text line.
- Normal Up/Down movement inside multiline text takes precedence.
- Right moves from the end of To-Do to the Memo in the same row.
- Left moves from the start of Memo to the To-Do in the same row.

### Deletion menu

Right-clicking a displayed number offers **Delete item**.

## 4. Persistence

To-Do entries are stored in the existing portable `config\settings.json` file as part of `AppSettings`.

- Autosave is debounced.
- Settings export/import includes To-Do data.
- Resetting general Ferry settings does not erase To-Do data.
- There is no separate `todo.json`, database, account, sync service, or background service.

## 5. Inline rename focus repair

After a single-item inline rename ends with either Enter (commit) or Esc (cancel), Ferry restores:

- the selected item
- the logical selection anchor
- keyboard navigation origin
- actual item-container keyboard focus

This allows immediate Up/Down navigation after leaving rename mode.

## 6. Scope

The v1.2 To-Do feature intentionally does not add:

- checkboxes or completion states
- due dates or reminders
- priorities or tags
- projects
- Markdown or rich text
- cloud synchronization

It remains a small working scratchpad integrated into the file-manager window.
