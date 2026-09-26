# Ferry v1.2.0

Ferry v1.2.0 adds a deliberately simple **To-Do / Memo scratchpad** to the Sidebar and includes a small keyboard-focus fix for inline rename.

## Highlights

### To-Do / Memo scratchpad

- **To-Do** is always available from the Sidebar and opens in a dedicated tab.
- The To-Do tab is pinned to the **leftmost tab position**.
- Each numbered To-Do entry on the left is paired with the same numbered Memo on the right.
- Left and right cells share one physical row height, so related notes remain horizontally aligned even when one side wraps to multiple lines.
- **Enter** in To-Do creates the next numbered item.
- **Shift+Enter** inserts a newline within the current To-Do item.
- **Arrow keys** support moving between rows and between To-Do / Memo while preserving normal multiline text editing.
- An empty To-Do row can be removed with **Backspace** at the start of the To-Do field, even when its Memo still contains text.
- A row can also be removed from the number's context menu.
- To-Do content is automatically saved with Ferry's other portable settings in `config\settings.json`.
- No separate To-Do database, cloud service, reminders, priorities, tags, or rich-text format are used.

The feature is intended equally well for a short task list, a command/reference scratchpad, or other transient working notes.

### Inline rename keyboard focus

After finishing a single-item inline rename with either **Enter** or **Esc**, keyboard focus is restored to the renamed item itself. Up/Down navigation therefore continues immediately without requiring an extra Escape key press.

## Compatibility

- Windows 11
- .NET Framework 4.8 / WPF
- Portable; no installer required
- Existing v1.1.x settings remain valid. The new To-Do collection is added to the same `settings.json` file.

## Download

Download `Ferry-v1.2.0-win-portable.zip` from the GitHub Release assets, extract it, and run `Ferry.exe`.

Ferry is currently unsigned, so Windows SmartScreen may show a warning on first launch.
