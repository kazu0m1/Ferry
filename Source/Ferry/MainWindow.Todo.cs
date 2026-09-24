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
        private ItemsControl todoItemsControl;
        private ObservableCollection<TodoEntry> todoEntries;
        private DispatcherTimer todoSaveTimer;

        private bool IsTodoTabActive
        {
            get { return todoTabItem != null && tabs != null && object.ReferenceEquals(tabs.SelectedItem, todoTabItem); }
        }

        private void OpenTodoTab()
        {
            EnsureTodoLoaded();

            if (todoTabItem != null && tabs != null && tabs.Items.Contains(todoTabItem))
            {
                tabs.SelectedItem = todoTabItem;
                FocusFirstTodoEditorIfNeeded();
                return;
            }

            todoTabItem = new TabItem();
            todoTabItem.Content = BuildTodoView();
            todoTabItem.Header = BuildTodoTabHeader();

            tabs.Items.Add(todoTabItem);
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

            Grid heading = new Grid
            {
                Background = SystemColors.ControlBrush,
                Height = 32
            };
            heading.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            heading.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Border leftHeading = new Border
            {
                BorderBrush = SystemColors.ControlDarkBrush,
                BorderThickness = new Thickness(0, 0, 1, 1),
                Padding = new Thickness(10, 5, 8, 5)
            };
            leftHeading.Child = new TextBlock { Text = "To-Do", FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
            heading.Children.Add(leftHeading);
            Grid.SetColumn(leftHeading, 0);

            Border rightHeading = new Border
            {
                BorderBrush = SystemColors.ControlDarkBrush,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(10, 5, 8, 5)
            };
            rightHeading.Child = new TextBlock { Text = "Memo", FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
            heading.Children.Add(rightHeading);
            Grid.SetColumn(rightHeading, 1);

            root.Children.Add(heading);
            Grid.SetRow(heading, 0);

            todoItemsControl = new ItemsControl { ItemsSource = todoEntries };
            todoItemsControl.ItemTemplate = BuildTodoItemTemplate();

            ScrollViewer scroll = new ScrollViewer
            {
                Content = todoItemsControl,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                CanContentScroll = false,
                Background = SystemColors.WindowBrush
            };
            root.Children.Add(scroll);
            Grid.SetRow(scroll, 1);

            return root;
        }

        private DataTemplate BuildTodoItemTemplate()
        {
            DataTemplate template = new DataTemplate(typeof(TodoEntry));

            FrameworkElementFactory rowBorder = new FrameworkElementFactory(typeof(Border));
            rowBorder.SetValue(Border.BorderBrushProperty, SystemColors.ControlLightBrush);
            rowBorder.SetValue(Border.BorderThicknessProperty, new Thickness(0, 0, 0, 1));
            rowBorder.SetValue(Border.BackgroundProperty, SystemColors.WindowBrush);

            FrameworkElementFactory row = new FrameworkElementFactory(typeof(Grid));
            row.SetValue(FrameworkElement.MinHeightProperty, 38.0);
            rowBorder.AppendChild(row);

            FrameworkElementFactory leftColumn = new FrameworkElementFactory(typeof(ColumnDefinition));
            leftColumn.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            row.AppendChild(leftColumn);
            FrameworkElementFactory rightColumn = new FrameworkElementFactory(typeof(ColumnDefinition));
            rightColumn.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            row.AppendChild(rightColumn);

            FrameworkElementFactory leftBorder = new FrameworkElementFactory(typeof(Border));
            leftBorder.SetValue(Grid.ColumnProperty, 0);
            leftBorder.SetValue(Border.BorderBrushProperty, SystemColors.ControlDarkBrush);
            leftBorder.SetValue(Border.BorderThicknessProperty, new Thickness(0, 0, 1, 0));
            row.AppendChild(leftBorder);

            FrameworkElementFactory left = new FrameworkElementFactory(typeof(Grid));
            leftBorder.AppendChild(left);

            FrameworkElementFactory leftNumberColumn = new FrameworkElementFactory(typeof(ColumnDefinition));
            leftNumberColumn.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
            left.AppendChild(leftNumberColumn);
            FrameworkElementFactory leftTextColumn = new FrameworkElementFactory(typeof(ColumnDefinition));
            leftTextColumn.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            left.AppendChild(leftTextColumn);

            FrameworkElementFactory leftNumber = CreateTodoNumberFactory(0);
            left.AppendChild(leftNumber);

            FrameworkElementFactory leftEditor = CreateTodoEditorFactory("Text", TodoLeftEditorTag);
            leftEditor.SetValue(Grid.ColumnProperty, 1);
            leftEditor.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(TodoLeftPreviewKeyDown));
            left.AppendChild(leftEditor);

            FrameworkElementFactory right = new FrameworkElementFactory(typeof(Grid));
            right.SetValue(Grid.ColumnProperty, 1);
            row.AppendChild(right);

            FrameworkElementFactory rightNumberColumn = new FrameworkElementFactory(typeof(ColumnDefinition));
            rightNumberColumn.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
            right.AppendChild(rightNumberColumn);
            FrameworkElementFactory rightTextColumn = new FrameworkElementFactory(typeof(ColumnDefinition));
            rightTextColumn.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            right.AppendChild(rightTextColumn);

            FrameworkElementFactory rightNumber = CreateTodoNumberFactory(0);
            right.AppendChild(rightNumber);

            FrameworkElementFactory memoEditor = CreateTodoEditorFactory("Memo", TodoMemoEditorTag);
            memoEditor.SetValue(Grid.ColumnProperty, 1);
            right.AppendChild(memoEditor);

            template.VisualTree = rowBorder;
            return template;
        }

        private FrameworkElementFactory CreateTodoNumberFactory(int column)
        {
            FrameworkElementFactory number = new FrameworkElementFactory(typeof(TextBlock));
            Binding numberBinding = new Binding("Number") { StringFormat = "{0}." };
            number.SetBinding(TextBlock.TextProperty, numberBinding);
            number.SetValue(Grid.ColumnProperty, column);
            number.SetValue(FrameworkElement.WidthProperty, 42.0);
            number.SetValue(FrameworkElement.MarginProperty, new Thickness(6, 7, 0, 5));
            number.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Right);
            number.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Top);
            number.SetValue(FrameworkElement.ToolTipProperty, "Right-click the number to delete this item");
            number.AddHandler(UIElement.PreviewMouseRightButtonDownEvent, new MouseButtonEventHandler(TodoNumberPreviewMouseRightButtonDown));
            return number;
        }

        private FrameworkElementFactory CreateTodoEditorFactory(string propertyName, string tag)
        {
            FrameworkElementFactory editor = new FrameworkElementFactory(typeof(TextBox));
            Binding binding = new Binding(propertyName)
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            editor.SetBinding(TextBox.TextProperty, binding);
            editor.SetValue(FrameworkElement.TagProperty, tag);
            editor.SetValue(TextBox.AcceptsReturnProperty, true);
            editor.SetValue(TextBox.AcceptsTabProperty, true);
            editor.SetValue(TextBox.TextWrappingProperty, TextWrapping.Wrap);
            editor.SetValue(TextBox.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
            editor.SetValue(TextBox.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
            editor.SetValue(Control.BorderThicknessProperty, new Thickness(0));
            editor.SetValue(Control.PaddingProperty, new Thickness(8, 6, 8, 6));
            editor.SetValue(Control.BackgroundProperty, Brushes.Transparent);
            editor.SetValue(Control.VerticalContentAlignmentProperty, VerticalAlignment.Top);
            editor.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Stretch);
            editor.SetValue(FrameworkElement.MinHeightProperty, 37.0);
            editor.AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler(TodoEditorTextChanged));
            return editor;
        }

        private void EnsureTodoLoaded()
        {
            if (todoEntries != null) return;

            List<TodoEntry> loaded = TodoStore.Load();
            todoEntries = new ObservableCollection<TodoEntry>();
            for (int i = 0; i < loaded.Count; i++)
                if (loaded[i] != null) todoEntries.Add(loaded[i]);

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
        }

        private void SaveTodoNow()
        {
            if (todoEntries == null) return;
            try { TodoStore.Save(todoEntries); }
            catch (Exception ex) { Logger.Write("To-Do save failed: " + ex.Message); }
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
                string.IsNullOrEmpty(entry.Text) && string.IsNullOrEmpty(entry.Memo) &&
                todoEntries.Count > 1)
            {
                int index = todoEntries.IndexOf(entry);
                if (index < 0) return;

                TodoEntry previous = index > 0 ? todoEntries[index - 1] : todoEntries[1];
                todoEntries.RemoveAt(index);
                RenumberTodoEntries();
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
            ScheduleTodoSave();

            TodoEntry target = todoEntries[Math.Min(index, todoEntries.Count - 1)];
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(delegate
            {
                FocusTodoEditor(target, TodoLeftEditorTag, Math.Min(target.Text.Length, target.Text.Length));
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
            if (entry == null || todoItemsControl == null) return;

            todoItemsControl.UpdateLayout();
            DependencyObject container = todoItemsControl.ItemContainerGenerator.ContainerFromItem(entry);
            TextBox editor = FindTodoEditor(container, tag);
            if (editor == null)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate
                {
                    todoItemsControl.UpdateLayout();
                    DependencyObject retry = todoItemsControl.ItemContainerGenerator.ContainerFromItem(entry);
                    TextBox retryEditor = FindTodoEditor(retry, tag);
                    if (retryEditor == null) return;
                    retryEditor.Focus();
                    retryEditor.CaretIndex = Math.Max(0, Math.Min(caret, retryEditor.Text.Length));
                }));
                return;
            }

            editor.Focus();
            editor.CaretIndex = Math.Max(0, Math.Min(caret, editor.Text.Length));
        }

        private TextBox FindTodoEditor(DependencyObject root, string tag)
        {
            if (root == null) return null;

            TextBox own = root as TextBox;
            if (own != null && string.Equals(Convert.ToString(own.Tag), tag, StringComparison.Ordinal))
                return own;

            int count = 0;
            try { count = VisualTreeHelper.GetChildrenCount(root); }
            catch { return null; }

            for (int i = 0; i < count; i++)
            {
                TextBox found = FindTodoEditor(VisualTreeHelper.GetChild(root, i), tag);
                if (found != null) return found;
            }

            return null;
        }

        private void CloseTodoTab()
        {
            if (todoTabItem == null || tabs == null) return;
            SaveTodoNow();

            if (tabs.Items.Contains(todoTabItem))
                tabs.Items.Remove(todoTabItem);

            todoTabItem = null;
            todoItemsControl = null;

            if (tabs.Items.Count == 0)
                Close();
        }
    }
}
