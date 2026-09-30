using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace Ferry
{
    internal sealed partial class MainWindow : Window
    {
        private bool todoSessionInitialized;
        private bool todoStartupHandled;
        private bool todoSessionHasEditorState;
        private bool todoSessionRestoring;
        private int todoSessionRowIndex = -1;
        private string todoSessionEditorTag;
        private int todoSessionSelectionStart;
        private int todoSessionSelectionLength;

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            if (!todoSessionInitialized)
            {
                todoSessionInitialized = true;

                if (tabs != null)
                    tabs.SelectionChanged += TodoSessionTabsSelectionChanged;

                AddHandler(
                    TextBoxBase.SelectionChangedEvent,
                    new RoutedEventHandler(TodoSessionEditorSelectionChanged),
                    true);
                AddHandler(
                    Keyboard.GotKeyboardFocusEvent,
                    new KeyboardFocusChangedEventHandler(TodoSessionEditorGotKeyboardFocus),
                    true);
                AddHandler(
                    Keyboard.LostKeyboardFocusEvent,
                    new KeyboardFocusChangedEventHandler(TodoSessionEditorLostKeyboardFocus),
                    true);
            }

            if (!todoStartupHandled)
            {
                todoStartupHandled = true;
                OpenTodoAtStartupIfRequested();
            }
        }

        private void OpenTodoAtStartupIfRequested()
        {
            if (settings == null || !settings.OpenTodoOnStartup || tabs == null) return;

            // Keep Ferry's normal startup folder selected. The preference controls whether the
            // pinned To-Do tab is already available, not which workspace Ferry opens into.
            object selected = tabs.SelectedItem;
            OpenTodoTab();
            if (selected != null && tabs.Items.Contains(selected))
                tabs.SelectedItem = selected;
        }

        private void TodoSessionEditorSelectionChanged(object sender, RoutedEventArgs e)
        {
            RememberTodoEditorState(e.OriginalSource as TextBox);
        }

        private void TodoSessionEditorGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            RememberTodoEditorState(e.NewFocus as TextBox);
        }

        private void TodoSessionEditorLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            RememberTodoEditorState(e.OldFocus as TextBox);
        }

        private void RememberTodoEditorState(TextBox editor)
        {
            if (todoSessionRestoring || editor == null || todoEntries == null) return;

            string tag = Convert.ToString(editor.Tag);
            if (!string.Equals(tag, TodoLeftEditorTag, StringComparison.Ordinal) &&
                !string.Equals(tag, TodoMemoEditorTag, StringComparison.Ordinal)) return;

            TodoEntry entry = editor.DataContext as TodoEntry;
            if (entry == null) return;

            int rowIndex = todoEntries.IndexOf(entry);
            if (rowIndex < 0) return;

            todoSessionRowIndex = rowIndex;
            todoSessionEditorTag = tag;
            todoSessionSelectionStart = editor.SelectionStart;
            todoSessionSelectionLength = editor.SelectionLength;
            todoSessionHasEditorState = true;
        }

        private void TodoSessionTabsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source != tabs || !IsTodoTabActive || !todoSessionHasEditorState) return;

            // OpenTodoTab() historically schedules a first-row focus at Input priority. Restore
            // the user's previous editor/selection afterward so Sidebar activation and direct tab
            // activation behave identically and never collapse a saved text selection.
            todoSessionRestoring = true;
            Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(RestoreTodoEditorState));
        }

        private void RestoreTodoEditorState()
        {
            try
            {
                if (!IsTodoTabActive || !todoSessionHasEditorState || todoEntries == null ||
                    todoSessionRowIndex < 0 || todoSessionRowIndex >= todoEntries.Count)
                {
                    if (todoSessionRowIndex < 0 || todoEntries == null || todoSessionRowIndex >= (todoEntries == null ? 0 : todoEntries.Count))
                        todoSessionHasEditorState = false;
                    return;
                }

                TodoEntry entry = todoEntries[todoSessionRowIndex];
                TextBox editor = null;
                if (string.Equals(todoSessionEditorTag, TodoMemoEditorTag, StringComparison.Ordinal))
                    todoMemoEditors.TryGetValue(entry, out editor);
                else
                    todoLeftEditors.TryGetValue(entry, out editor);

                if (editor == null)
                {
                    todoSessionHasEditorState = false;
                    return;
                }

                int start = Math.Max(0, Math.Min(todoSessionSelectionStart, editor.Text.Length));
                int length = Math.Max(0, Math.Min(todoSessionSelectionLength, editor.Text.Length - start));

                editor.Focus();
                editor.BringIntoView();
                editor.Select(start, length);
            }
            finally
            {
                todoSessionRestoring = false;
            }
        }
    }
}
