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
        private ScrollViewer todoLayoutScrollViewer;
        private Grid todoLayoutHeading;

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
            if (e.Source != tabs || !IsTodoTabActive) return;

            EnsureTodoHeaderAlignment();
            if (!todoSessionHasEditorState) return;

            // OpenTodoTab() historically schedules a first-row focus at Input priority. Restore
            // the user's previous editor/selection afterward so Sidebar activation and direct tab
            // activation behave identically and never collapse a saved text selection.
            todoSessionRestoring = true;
            Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(RestoreTodoEditorState));
        }

        private void EnsureTodoHeaderAlignment()
        {
            if (todoTabItem == null) return;

            Grid root = todoTabItem.Content as Grid;
            if (root == null) return;

            Grid heading = null;
            ScrollViewer scroll = null;
            for (int i = 0; i < root.Children.Count; i++)
            {
                UIElement child = root.Children[i];
                if (Grid.GetRow(child) == 0 && heading == null)
                    heading = child as Grid;
                else if (Grid.GetRow(child) == 1 && scroll == null)
                    scroll = child as ScrollViewer;
            }

            if (heading == null || scroll == null) return;

            todoLayoutHeading = heading;
            if (!object.ReferenceEquals(todoLayoutScrollViewer, scroll))
            {
                todoLayoutScrollViewer = scroll;
                scroll.SizeChanged += TodoLayoutScrollViewerSizeChanged;
                scroll.ScrollChanged += TodoLayoutScrollViewerScrollChanged;
            }

            SyncTodoHeadingToViewport();
            Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(SyncTodoHeadingToViewport));
        }

        private void TodoLayoutScrollViewerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            SyncTodoHeadingToViewport();
        }

        private void TodoLayoutScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (Math.Abs(e.ViewportWidthChange) > 0.01)
                SyncTodoHeadingToViewport();
        }

        private void SyncTodoHeadingToViewport()
        {
            if (todoLayoutHeading == null || todoLayoutScrollViewer == null) return;

            double width = todoLayoutScrollViewer.ViewportWidth;
            if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0) return;

            // The rows live inside the ScrollViewer viewport, which becomes narrower whenever
            // the vertical scrollbar appears. Keep the header on that same viewport width so the
            // 50/50 To-Do/Memo boundary remains a single straight line at every window size.
            todoLayoutHeading.HorizontalAlignment = HorizontalAlignment.Left;
            todoLayoutHeading.Width = width;
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
