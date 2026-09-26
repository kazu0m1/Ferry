using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Ferry
{
    internal sealed partial class MainWindow : Window
    {
        private const string TodoLeftEditorTag = "Ferry.Todo.Left";
        private const string TodoMemoEditorTag = "Ferry.Todo.Memo";

        private TabItem todoTabItem;
        private StackPanel todoRowsPanel;
        private ObservableCollection<TodoEntry> todoEntries;
        private DispatcherTimer todoSaveTimer;
        private readonly Dictionary<TodoEntry, TextBox> todoLeftEditors = new Dictionary<TodoEntry, TextBox>();
        private readonly Dictionary<TodoEntry, TextBox> todoMemoEditors = new Dictionary<TodoEntry, TextBox>();

        private bool IsTodoTabActive
        {
            get { return todoTabItem != null && tabs != null && object.ReferenceEquals(tabs.SelectedItem, todoTabItem); }
        }

        private void OpenTodoTab()
        {
            EnsureTodoLoaded();

            if (todoTabItem != null && tabs != null && tabs.Items.Contains(todoTabItem))
            {
                int currentIndex = tabs.Items.IndexOf(todoTabItem);
                if (currentIndex > 0)
                {
                    tabs.Items.Remove(todoTabItem);
                    tabs.Items.Insert(0, todoTabItem);
                }
                tabs.SelectedItem = todoTabItem;
                FocusFirstTodoEditorIfNeeded();
                return;
            }

            todoTabItem = new TabItem
            {
                Content = BuildTodoView(),
                Header = BuildTodoTabHeader()
            };

            tabs.Items.Insert(0, todoTabItem);
            tabs.SelectedItem = todoTabItem;
            FocusFirstTodoEditorIfNeeded();
        }

        private object BuildTodoTabHeader()
        {
            StackPanel header = new StackPanel { Orientation = Orientation.Horizontal };
            TextBlock title = new TextBlock
            {
                Text = "To-Do",
                VerticalAlignment = VerticalAlignment.Center,
                MaxWidth = 180,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Button close = new Button
            {
                Content = "×",
                Padding = new Thickness(4, 0, 4, 0),
                Margin = new Thickness(7, 0, 0, 0),
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                ToolTip = "Close tab"
            };
            close.Click += delegate(object sender, RoutedEventArgs e)
            {
                e.Handled = true;
                CloseTodoTab();
            };

            header.Children.Add(title);
            header.Children.Add(close);
            return header;
        }

        private UIElement BuildTodoView()
        {
            Grid root = new Grid { Background = SystemColors.WindowBrush };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid heading = CreateTodoTwoColumnGrid();
            heading.Height = 32;
            heading.Background = SystemColors.ControlBrush;

            Border leftHeading = new Border
            {
                BorderBrush = SystemColors.ControlDarkBrush,
                BorderThickness = new Thickness(0, 0, 1, 1),
                Padding = new Thickness(10, 5, 8, 5),
                Child = new TextBlock
                {
                    Text = "To-Do",
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            heading.Children.Add(leftHeading);
            Grid.SetColumn(leftHeading, 0);

            Border rightHeading = new Border
            {
                BorderBrush = SystemColors.ControlDarkBrush,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(10, 5, 8, 5),
                Child = new TextBlock
                {
                    Text = "Memo",
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            heading.Children.Add(rightHeading);
            Grid.SetColumn(rightHeading, 1);

            root.Children.Add(heading);
            Grid.SetRow(heading, 0);

            todoRowsPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Background = SystemColors.WindowBrush
            };
            RebuildTodoRows();

            ScrollViewer scroll = new ScrollViewer
            {
                Content = todoRowsPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                CanContentScroll = false,
                Background = SystemColors.WindowBrush
            };
            root.Children.Add(scroll);
            Grid.SetRow(scroll, 1);

            return root;
        }

        private Grid CreateTodoTwoColumnGrid()
        {
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            return grid;
        }

        private void RebuildTodoRows()
        {
            if (todoRowsPanel == null || todoEntries == null) return;

            todoRowsPanel.Children.Clear();
            todoLeftEditors.Clear();
            todoMemoEditors.Clear();

            for (int i = 0; i < todoEntries.Count; i++)
            {
                TodoEntry entry = todoEntries[i];
                if (entry != null) todoRowsPanel.Children.Add(CreateTodoRow(entry));
            }
        }

        private UIElement CreateTodoRow(TodoEntry entry)
        {
            Border rowBorder = new Border
            {
                BorderBrush = SystemColors.ControlLightBrush,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Background = SystemColors.WindowBrush,
                MinHeight = 38
            };

            // One physical Grid owns both editors. Its row height is therefore the larger of the
            // left and right content heights, so the next numbered pair always begins at the same Y.
            Grid row = CreateTodoTwoColumnGrid();
            rowBorder.Child = row;

            Border leftBorder = new Border
            {
                BorderBrush = SystemColors.ControlDarkBrush,
                BorderThickness = new Thickness(0, 0, 1, 0),
                VerticalAlignment = VerticalAlignment.Stretch
            };
            row.Children.Add(leftBorder);
            Grid.SetColumn(leftBorder, 0);

            Grid left = CreateTodoCellGrid();
            leftBorder.Child = left;

            TextBlock leftNumber = CreateTodoNumber(entry);
            left.Children.Add(leftNumber);
            Grid.SetColumn(leftNumber, 0);

            TextBox leftEditor = CreateTodoEditor(entry, "Text", TodoLeftEditorTag);
            leftEditor.PreviewKeyDown += TodoLeftPreviewKeyDown;
            left.Children.Add(leftEditor);
            Grid.SetColumn(leftEditor, 1);
            todoLeftEditors[entry] = leftEditor;

            Grid right = CreateTodoCellGrid();
            row.Children.Add(right);
            Grid.SetColumn(right, 1);

            TextBlock rightNumber = CreateTodoNumber(entry);
            right.Children.Add(rightNumber);
            Grid.SetColumn(rightNumber, 0);

            TextBox memoEditor = CreateTodoEditor(entry, "Memo", TodoMemoEditorTag);
            right.Children.Add(memoEditor);
            Grid.SetColumn(memoEditor, 1);
            todoMemoEditors[entry] = memoEditor;

            return rowBorder;
        }

        private Grid CreateTodoCellGrid()
        {
            Grid cell = new Grid { VerticalAlignment = VerticalAlignment.Stretch };
            cell.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            cell.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            return cell;
        }

        private TextBlock CreateTodoNumber(TodoEntry entry)
        {
            TextBlock number = new TextBlock
            {
                DataContext = entry,
                Width = 28,
                Margin = new Thickness(10, 7, 0, 5),
                TextAlignment = TextAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                ToolTip = "Right-click the number to delete this item"
            };
            number.SetBinding(TextBlock.TextProperty, new Binding("Number") { StringFormat = "{0}." });
            number.PreviewMouseRightButtonDown += TodoNumberPreviewMouseRightButtonDown;
            return number;
        }

        private TextBox CreateTodoEditor(TodoEntry entry, string propertyName, string tag)
        {
            TextBox editor = new TextBox
            {
                DataContext = entry,
                Tag = tag,
                AcceptsReturn = true,
                AcceptsTab = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(4, 6, 8, 6),
                Background = Brushes.Transparent,
                VerticalContentAlignment = VerticalAlignment.Top,
                VerticalAlignment = VerticalAlignment.Stretch,
                MinHeight = 37
            };
            editor.SetBinding(TextBox.TextProperty, new Binding(propertyName)
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            editor.TextChanged += TodoEditorTextChanged;
            return editor;
        }

        private void EnsureTodoLoaded()
        {
            if (todoEntries != null) return;

            List<TodoEntry> loaded = settings == null || settings.TodoEntries == null
                ? new List<TodoEntry>()
                : settings.TodoEntries;
            todoEntries = new ObservableCollection<TodoEntry>();
            for (int i = 0; i < loaded.Count; i++)
            {
                TodoEntry source = loaded[i];
                if (source != null)
                    todoEntries.Add(new TodoEntry { Text = source.Text, Memo = source.Memo });
            }

            if (todoEntries.Count == 0)
                todoEntries.Add(new TodoEntry());

            RenumberTodoEntries();

            todoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(450) };
            todoSaveTimer.Tick += delegate
            {
                todoSaveTimer.Stop();
                SaveTodoNow();
            };
        }

        private void RenumberTodoEntries()
        {
            if (todoEntries == null) return;
            for (int i = 0; i < todoEntries.Count; i++)
                todoEntries[i].Number = i + 1;
        }

        private void TodoEditorTextChanged(object sender, TextChangedEventArgs e)
        {
            ScheduleTodoSave();
        }

        private void ScheduleTodoSave()
        {
            if (todoSaveTimer == null) return;
            todoSaveTimer.Stop();
            todoSaveTimer.Start();
            UpdateStatus();
        }

        private void SaveTodoNow()
        {
            if (todoEntries == null || settings == null) return;
            try
            {
                List<TodoEntry> snapshot = new List<TodoEntry>();
                for (int i = 0; i < todoEntries.Count; i++)
                {
                    TodoEntry source = todoEntries[i];
                    if (source == null) continue;
                    snapshot.Add(new TodoEntry { Text = source.Text, Memo = source.Memo });
                }
                settings.TodoEntries = snapshot;
                SettingsStore.Save(settings);
            }
            catch (Exception ex) { Logger.Write("To-Do save failed: " + ex.Message); }
        }

        private void ReloadTodoEntriesFromSettings()
        {
            if (todoEntries == null || settings == null) return;

            todoEntries.Clear();
            List<TodoEntry> sourceEntries = settings.TodoEntries ?? new List<TodoEntry>();
            for (int i = 0; i < sourceEntries.Count; i++)
            {
                TodoEntry source = sourceEntries[i];
                if (source != null)
                    todoEntries.Add(new TodoEntry { Text = source.Text, Memo = source.Memo });
            }

            if (todoEntries.Count == 0)
                todoEntries.Add(new TodoEntry());

            RenumberTodoEntries();
            RebuildTodoRows();
            UpdateStatus();
        }

        private void TodoLeftPreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox editor = sender as TextBox;
            TodoEntry entry = editor == null ? null : editor.DataContext as TodoEntry;
            if (editor == null || entry == null || todoEntries == null) return;

            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                // Shift+Enter is an ordinary newline inside the current numbered item.
                return;
            }

            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                int index = todoEntries.IndexOf(entry);
                if (index < 0) return;

                string current = editor.Text ?? string.Empty;
                int start = Math.Max(0, Math.Min(editor.SelectionStart, current.Length));
                int end = Math.Max(start, Math.Min(start + editor.SelectionLength, current.Length));

                entry.Text = current.Substring(0, start);
                TodoEntry next = new TodoEntry
                {
                    Text = current.Substring(end),
                    Memo = string.Empty
                };
                todoEntries.Insert(index + 1, next);
                RenumberTodoEntries();
                RebuildTodoRows();
                ScheduleTodoSave();

                e.Handled = true;
                Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(delegate
                {
                    FocusTodoEditor(next, TodoLeftEditorTag, 0);
                }));
                return;
            }

            if (e.Key == Key.Back && Keyboard.Modifiers == ModifierKeys.None &&
                editor.SelectionLength == 0 && editor.CaretIndex == 0 &&
                string.IsNullOrEmpty(entry.Text))
            {
                int index = todoEntries.IndexOf(entry);
                if (index < 0) return;

                // The left To-Do text is the authority for whether a numbered row still exists.
                // If it is empty, Backspace removes the whole row even when Memo contains text.
                if (todoEntries.Count == 1)
                {
                    entry.Memo = string.Empty;
                    ScheduleTodoSave();
                    e.Handled = true;
                    FocusTodoEditor(entry, TodoLeftEditorTag, 0);
                    return;
                }

                TodoEntry previous = index > 0 ? todoEntries[index - 1] : todoEntries[1];
                todoEntries.RemoveAt(index);
                RenumberTodoEntries();
                RebuildTodoRows();
                ScheduleTodoSave();

                e.Handled = true;
                Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(delegate
                {
                    FocusTodoEditor(previous, TodoLeftEditorTag, previous.Text.Length);
                }));
            }
        }

        private void TodoNumberPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            TodoEntry entry = element == null ? null : element.DataContext as TodoEntry;
            if (entry == null) return;

            ContextMenu menu = new ContextMenu();
            MenuItem delete = new MenuItem { Header = "Delete item" };
            delete.Click += delegate { DeleteTodoEntry(entry); };
            menu.Items.Add(delete);
            element.ContextMenu = menu;
            menu.PlacementTarget = element;
            menu.IsOpen = true;
            e.Handled = true;
        }

        private void DeleteTodoEntry(TodoEntry entry)
        {
            if (entry == null || todoEntries == null) return;
            int index = todoEntries.IndexOf(entry);
            if (index < 0) return;

            if (todoEntries.Count == 1)
            {
                entry.Text = string.Empty;
                entry.Memo = string.Empty;
                ScheduleTodoSave();
                FocusTodoEditor(entry, TodoLeftEditorTag, 0);
                return;
            }

            todoEntries.RemoveAt(index);
            RenumberTodoEntries();
            RebuildTodoRows();
            ScheduleTodoSave();

            TodoEntry target = todoEntries[Math.Min(index, todoEntries.Count - 1)];
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(delegate
            {
                FocusTodoEditor(target, TodoLeftEditorTag, target.Text.Length);
            }));
        }

        private void FocusFirstTodoEditorIfNeeded()
        {
            if (todoEntries == null || todoEntries.Count == 0) return;
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(delegate
            {
                if (IsTodoTabActive)
                    FocusTodoEditor(todoEntries[0], TodoLeftEditorTag, todoEntries[0].Text.Length);
            }));
        }

        private void FocusTodoEditor(TodoEntry entry, string tag, int caret)
        {
            if (entry == null) return;

            TextBox editor = null;
            if (string.Equals(tag, TodoMemoEditorTag, StringComparison.Ordinal))
                todoMemoEditors.TryGetValue(entry, out editor);
            else
                todoLeftEditors.TryGetValue(entry, out editor);

            if (editor == null) return;
            editor.Focus();
            editor.CaretIndex = Math.Max(0, Math.Min(caret, editor.Text.Length));
        }

        private void CloseTodoTab()
        {
            if (todoTabItem == null || tabs == null) return;
            SaveTodoNow();

            if (tabs.Items.Contains(todoTabItem))
                tabs.Items.Remove(todoTabItem);

            todoTabItem = null;
            todoRowsPanel = null;
            todoLeftEditors.Clear();
            todoMemoEditors.Clear();

            if (tabs.Items.Count == 0)
                Close();
        }
    }
}
