using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Ferry
{
    internal sealed class CreateZipSetupWindow : Window
    {
        private readonly TextBox destination;
        private readonly IList<string> sourcePaths;

        public string DestinationZipPath { get { return destination.Text.Trim(); } }

        public CreateZipSetupWindow(Window owner, IList<string> paths, string initialDestinationZipPath)
        {
            sourcePaths = new List<string>();
            if (paths != null)
            {
                for (int i = 0; i < paths.Count; i++) sourcePaths.Add(paths[i]);
            }

            Owner = owner;
            Title = "Create ZIP";
            Width = 700;
            Height = 330;
            MinWidth = 580;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            Grid root = new Grid { Margin = new Thickness(18) };
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(125) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            AddLabel(root, 0, "Selected items");
            ListBox sources = new ListBox { MinHeight = 105, Margin = new Thickness(0, 4, 0, 8) };
            for (int i = 0; i < sourcePaths.Count; i++)
            {
                string path = sourcePaths[i];
                ListBoxItem item = new ListBoxItem { Content = path, ToolTip = path, IsHitTestVisible = false };
                sources.Items.Add(item);
            }
            Grid.SetRow(sources, 0);
            Grid.SetColumn(sources, 1);
            Grid.SetColumnSpan(sources, 2);
            root.Children.Add(sources);

            AddLabel(root, 1, "Destination ZIP");
            destination = AddTextBox(root, 1);
            destination.Text = initialDestinationZipPath ?? string.Empty;
            Button browse = AddBrowseButton(root, 1);
            browse.Click += delegate
            {
                SaveFileDialog dialog = new SaveFileDialog
                {
                    Title = "Choose ZIP destination",
                    Filter = "ZIP archive (*.zip)|*.zip|All files (*.*)|*.*",
                    AddExtension = true,
                    DefaultExt = ".zip",
                    OverwritePrompt = false
                };
                if (!string.IsNullOrWhiteSpace(destination.Text))
                {
                    try
                    {
                        dialog.InitialDirectory = Path.GetDirectoryName(destination.Text);
                        dialog.FileName = Path.GetFileName(destination.Text);
                    }
                    catch { }
                }
                if (dialog.ShowDialog(this) == true) destination.Text = dialog.FileName;
            };

            TextBlock hint = new TextBlock
            {
                Text = "After Start, this window closes. Compression continues in the Ferry status bar while you keep using Ferry.",
                Foreground = System.Windows.Media.Brushes.DimGray,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 10, 0, 10)
            };
            Grid.SetRow(hint, 2);
            Grid.SetColumn(hint, 1);
            Grid.SetColumnSpan(hint, 2);
            root.Children.Add(hint);

            StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            Button cancel = new Button { Content = "Cancel", MinWidth = 88, Padding = new Thickness(12, 5, 12, 5), Margin = new Thickness(0, 0, 8, 0), IsCancel = true };
            Button start = new Button { Content = "Start", MinWidth = 88, Padding = new Thickness(12, 5, 12, 5), IsDefault = true };
            start.Click += delegate
            {
                if (sourcePaths.Count == 0)
                {
                    MessageBox.Show(this, "No items are selected.", "Create ZIP", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                for (int i = 0; i < sourcePaths.Count; i++)
                {
                    if (!File.Exists(sourcePaths[i]) && !Directory.Exists(sourcePaths[i]))
                    {
                        MessageBox.Show(this, "A selected item no longer exists.\n\n" + sourcePaths[i], "Create ZIP", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                if (string.IsNullOrWhiteSpace(DestinationZipPath))
                {
                    MessageBox.Show(this, "Choose a destination ZIP file.", "Create ZIP", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!string.Equals(Path.GetExtension(DestinationZipPath), ".zip", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(this, "The destination file must use the .zip extension.", "Create ZIP", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                DialogResult = true;
            };
            buttons.Children.Add(cancel);
            buttons.Children.Add(start);
            Grid.SetRow(buttons, 3);
            Grid.SetColumn(buttons, 1);
            Grid.SetColumnSpan(buttons, 2);
            root.Children.Add(buttons);

            Content = root;
        }

        private static void AddLabel(Grid grid, int row, string text)
        {
            TextBlock label = new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 8, 10, 5) };
            Grid.SetRow(label, row);
            Grid.SetColumn(label, 0);
            grid.Children.Add(label);
        }

        private static TextBox AddTextBox(Grid grid, int row)
        {
            TextBox box = new TextBox { MinWidth = 300, Margin = new Thickness(0, 4, 8, 4), VerticalContentAlignment = VerticalAlignment.Center };
            Grid.SetRow(box, row);
            Grid.SetColumn(box, 1);
            grid.Children.Add(box);
            return box;
        }

        private static Button AddBrowseButton(Grid grid, int row)
        {
            Button button = new Button { Content = "Browse...", MinWidth = 82, Padding = new Thickness(10, 3, 10, 3), Margin = new Thickness(0, 4, 0, 4) };
            Grid.SetRow(button, row);
            Grid.SetColumn(button, 2);
            grid.Children.Add(button);
            return button;
        }
    }

    internal sealed class ExtractZipSetupWindow : Window
    {
        private readonly TextBox source;
        private readonly TextBox destination;

        public string SourceZipPath { get { return source.Text.Trim(); } }
        public string DestinationDirectory { get { return destination.Text.Trim(); } }

        public ExtractZipSetupWindow(Window owner, string zipPath, string initialDestinationDirectory)
        {
            Owner = owner;
            Title = "Extract ZIP";
            Width = 700;
            Height = 250;
            MinWidth = 580;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            Grid root = new Grid { Margin = new Thickness(18) };
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(125) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            AddLabel(root, 0, "ZIP file");
            source = AddTextBox(root, 0);
            source.Text = zipPath ?? string.Empty;
            source.IsReadOnly = true;
            source.Background = System.Windows.SystemColors.ControlBrush;

            AddLabel(root, 1, "Destination folder");
            destination = AddTextBox(root, 1);
            destination.Text = initialDestinationDirectory ?? string.Empty;
            Button browse = AddBrowseButton(root, 1);
            browse.Click += delegate
            {
                string selected;
                if (ShellFolderPicker.TryPickFolder(this, "Choose extraction destination", out selected)) destination.Text = selected;
            };

            TextBlock hint = new TextBlock
            {
                Text = "After Start, this window closes. Extraction continues in the Ferry status bar while you keep using Ferry.",
                Foreground = System.Windows.Media.Brushes.DimGray,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 10, 0, 8)
            };
            Grid.SetRow(hint, 2);
            Grid.SetColumn(hint, 1);
            Grid.SetColumnSpan(hint, 2);
            root.Children.Add(hint);

            StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            Button cancel = new Button { Content = "Cancel", MinWidth = 88, Padding = new Thickness(12, 5, 12, 5), Margin = new Thickness(0, 0, 8, 0), IsCancel = true };
            Button start = new Button { Content = "Start", MinWidth = 88, Padding = new Thickness(12, 5, 12, 5), IsDefault = true };
            start.Click += delegate
            {
                if (!ArchiveHelper.IsZip(SourceZipPath))
                {
                    MessageBox.Show(this, "The selected ZIP file no longer exists.", "Extract ZIP", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(DestinationDirectory))
                {
                    MessageBox.Show(this, "Choose a destination folder.", "Extract ZIP", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                DialogResult = true;
            };
            buttons.Children.Add(cancel);
            buttons.Children.Add(start);
            Grid.SetRow(buttons, 3);
            Grid.SetColumn(buttons, 1);
            Grid.SetColumnSpan(buttons, 2);
            root.Children.Add(buttons);

            Content = root;
        }

        private static void AddLabel(Grid grid, int row, string text)
        {
            TextBlock label = new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 5, 10, 5) };
            Grid.SetRow(label, row);
            Grid.SetColumn(label, 0);
            grid.Children.Add(label);
        }

        private static TextBox AddTextBox(Grid grid, int row)
        {
            TextBox box = new TextBox { MinWidth = 300, Margin = new Thickness(0, 4, 8, 4), VerticalContentAlignment = VerticalAlignment.Center };
            Grid.SetRow(box, row);
            Grid.SetColumn(box, 1);
            grid.Children.Add(box);
            return box;
        }

        private static Button AddBrowseButton(Grid grid, int row)
        {
            Button button = new Button { Content = "Browse...", MinWidth = 82, Padding = new Thickness(10, 3, 10, 3), Margin = new Thickness(0, 4, 0, 4) };
            Grid.SetRow(button, row);
            Grid.SetColumn(button, 2);
            grid.Children.Add(button);
            return button;
        }
    }
}
