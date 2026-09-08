using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ferry
{
    internal sealed class RenameDialog : Window
    {
        private readonly List<string> sourcePaths;
        private readonly ObservableCollection<RenameEntry> entries = new ObservableCollection<RenameEntry>();
        private RadioButton templateMode;
        private RadioButton replaceMode;
        private TextBox templateBox;
        private TextBox startNumberBox;
        private TextBox findBox;
        private TextBox replaceBox;
        private Grid templatePanel;
        private Grid replacePanel;
        private DataGrid previewGrid;
        private Button renameButton;
        private TextBlock statusText;

        public List<RenameUndoRecord> UndoRecords { get; private set; }

        public RenameDialog(Window owner, IList<string> paths)
        {
            Owner = owner;
            Title = paths.Count == 1 ? "Rename" : "Rename " + paths.Count + " items";
            Width = 860; Height = 620; MinWidth = 680; MinHeight = 460;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            sourcePaths = new List<string>(paths);
            Content = BuildUi();
            PreviewKeyDown += OnPreviewKeyDown;
            Loaded += delegate { RefreshPreview(); };
        }

        private UIElement BuildUi()
        {
            Grid root = new Grid { Margin = new Thickness(18) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            StackPanel mode = new StackPanel { Orientation = Orientation.Horizontal };
            templateMode = new RadioButton { Content = "Rename using a template", IsChecked = true, Margin = new Thickness(0, 0, 22, 0) };
            replaceMode = new RadioButton { Content = "Find and replace text" };
            templateMode.Checked += ModeChanged; replaceMode.Checked += ModeChanged;
            mode.Children.Add(templateMode); mode.Children.Add(replaceMode);
            root.Children.Add(mode); Grid.SetRow(mode, 0);

            templatePanel = BuildTemplatePanel(); templatePanel.Margin = new Thickness(0, 16, 0, 12);
            root.Children.Add(templatePanel); Grid.SetRow(templatePanel, 1);
            replacePanel = BuildReplacePanel(); replacePanel.Margin = new Thickness(0, 16, 0, 12); replacePanel.Visibility = Visibility.Collapsed;
            root.Children.Add(replacePanel); Grid.SetRow(replacePanel, 1);

            statusText = new TextBlock { Margin = new Thickness(0, 0, 0, 8) };
            root.Children.Add(statusText); Grid.SetRow(statusText, 2);

            previewGrid = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true, CanUserAddRows = false, CanUserDeleteRows = false, HeadersVisibility = DataGridHeadersVisibility.Column, ItemsSource = entries };
            previewGrid.Columns.Add(new DataGridTextColumn { Header = "Current Name", Binding = new System.Windows.Data.Binding("CurrentName"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            previewGrid.Columns.Add(new DataGridTextColumn { Header = "New Name", Binding = new System.Windows.Data.Binding("NewName"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            previewGrid.Columns.Add(new DataGridTextColumn { Header = "Status", Binding = new System.Windows.Data.Binding("Status"), Width = new DataGridLength(220) });
            root.Children.Add(previewGrid); Grid.SetRow(previewGrid, 3);

            StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
            Button cancel = new Button { Content = "Cancel", MinWidth = 90, Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(0, 0, 8, 0) };
            cancel.Click += delegate { DialogResult = false; };
            renameButton = new Button { Content = "Rename", MinWidth = 90, Padding = new Thickness(12, 6, 12, 6), IsDefault = true };
            renameButton.Click += ExecuteRename;
            buttons.Children.Add(cancel); buttons.Children.Add(renameButton);
            root.Children.Add(buttons); Grid.SetRow(buttons, 4);
            return root;
        }

        private Grid BuildTemplatePanel()
        {
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(115) });

            StackPanel tpl = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
            tpl.Children.Add(new TextBlock { Text = "Template", Margin = new Thickness(0, 0, 0, 5) });
            templateBox = new TextBox { Text = RenameEngine.OriginalToken, Padding = new Thickness(8, 6, 8, 6) };
            templateBox.TextChanged += delegate { RefreshPreview(); };
            tpl.Children.Add(templateBox); grid.Children.Add(tpl); Grid.SetColumn(tpl, 0);

            Button add = new Button { Content = "+ Add", Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 21, 10, 0) };
            ContextMenu menu = new ContextMenu();
            MenuItem orig = new MenuItem { Header = "Original filename", Tag = RenameEngine.OriginalToken }; orig.Click += InsertToken; menu.Items.Add(orig);
            menu.Items.Add(new Separator());
            for (int w = 1; w <= 6; w++) { MenuItem mi = new MenuItem { Header = "Number: " + RenameEngine.NumberToken(w), Tag = RenameEngine.NumberToken(w) }; mi.Click += InsertToken; menu.Items.Add(mi); }
            add.ContextMenu = menu; add.Click += delegate { add.ContextMenu.IsOpen = true; };
            grid.Children.Add(add); Grid.SetColumn(add, 1);

            StackPanel start = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
            start.Children.Add(new TextBlock { Text = "Start number", Margin = new Thickness(0, 0, 0, 5) });
            startNumberBox = new TextBox { Text = "1", Padding = new Thickness(8, 6, 8, 6) }; startNumberBox.TextChanged += delegate { RefreshPreview(); };
            start.Children.Add(startNumberBox); grid.Children.Add(start); Grid.SetColumn(start, 2);

            return grid;
        }

        private Grid BuildReplacePanel()
        {
            Grid grid = new Grid(); grid.ColumnDefinitions.Add(new ColumnDefinition()); grid.ColumnDefinitions.Add(new ColumnDefinition());
            StackPanel find = new StackPanel { Margin = new Thickness(0, 0, 8, 0) }; find.Children.Add(new TextBlock { Text = "Existing Text", Margin = new Thickness(0, 0, 0, 5) });
            findBox = new TextBox { Padding = new Thickness(8, 6, 8, 6) }; findBox.TextChanged += delegate { RefreshPreview(); }; find.Children.Add(findBox); grid.Children.Add(find); Grid.SetColumn(find, 0);
            StackPanel repl = new StackPanel { Margin = new Thickness(8, 0, 0, 0) }; repl.Children.Add(new TextBlock { Text = "Replace With", Margin = new Thickness(0, 0, 0, 5) });
            replaceBox = new TextBox { Padding = new Thickness(8, 6, 8, 6) }; replaceBox.TextChanged += delegate { RefreshPreview(); }; repl.Children.Add(replaceBox); grid.Children.Add(repl); Grid.SetColumn(repl, 1);
            return grid;
        }

        private void InsertToken(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem; if (item == null) return; string token = item.Tag as string; if (token == null) return;
            int pos = templateBox.SelectionStart; templateBox.Text = templateBox.Text.Insert(pos, token); templateBox.SelectionStart = pos + token.Length; templateBox.Focus();
        }

        private void ModeChanged(object sender, RoutedEventArgs e)
        {
            if (templatePanel == null || replacePanel == null) return;
            bool template = templateMode.IsChecked == true; templatePanel.Visibility = template ? Visibility.Visible : Visibility.Collapsed; replacePanel.Visibility = template ? Visibility.Collapsed : Visibility.Visible; RefreshPreview();
        }

        private void RefreshPreview()
        {
            if (entries == null || renameButton == null) return;
            entries.Clear();
            int start; bool startOk = int.TryParse(startNumberBox == null ? "1" : startNumberBox.Text, out start) && start >= 0; if (!startOk) start = 1;
            bool template = templateMode == null || templateMode.IsChecked == true;
            string find = findBox == null ? string.Empty : findBox.Text; string repl = replaceBox == null ? string.Empty : replaceBox.Text;
            for (int i = 0; i < sourcePaths.Count; i++)
            {
                string path = sourcePaths[i]; string original = RenameEngine.GetBaseName(path); string newBase;
                if (template) newBase = RenameEngine.BuildTemplateName(templateBox == null ? RenameEngine.OriginalToken : templateBox.Text, original, start + i);
                else newBase = string.IsNullOrEmpty(find) ? original : original.Replace(find, repl);
                string target = RenameEngine.BuildTargetPath(path, newBase);
                entries.Add(new RenameEntry { SourcePath = path, CurrentName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar)), NewBaseName = newBase, TargetPath = target, NewName = Path.GetFileName(target), IsValid = true, Status = string.Empty });
            }
            string error = RenameEngine.ValidateBatch(entries);
            if (template && !startOk) error = "Start number must be zero or greater.";
            if (statusText != null) statusText.Text = error == null ? "Preview is valid." : error;
            renameButton.IsEnabled = error == null && entries.Count > 0;
        }

        private void ExecuteRename(object sender, RoutedEventArgs e)
        {
            try
            {
                RefreshPreview(); if (!renameButton.IsEnabled) return;
                UndoRecords = RenameEngine.ExecuteRename(new List<RenameEntry>(entries));
                DialogResult = true;
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Ferry Rename", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) { DialogResult = false; e.Handled = true; }
        }
    }
}
