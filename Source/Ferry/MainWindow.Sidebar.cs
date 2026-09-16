using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Ferry
{
    internal sealed partial class MainWindow : Window
    {
        private sealed class PinnedSidebarItem
        {
            public PinnedSidebarItem(string name, string fullPath) { Name = name; FullPath = fullPath; }
            public string Name { get; private set; }
            public string FullPath { get; private set; }
        }

        private void SidebarSplitterPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2 || settings == null || !settings.SidebarVisible) return;
            e.Handled = true;
            FitSidebarWidthToContent();
        }

        private void FitSidebarWidthToContent()
        {
            if (mainGrid == null || mainGrid.ColumnDefinitions.Count == 0 || sidebarPanel == null) return;

            sidebarPanel.UpdateLayout();
            double desired = 50.0;
            foreach (UIElement child in sidebarPanel.Children)
            {
                TextBlock heading = child as TextBlock;
                if (heading != null)
                {
                    double textWidth = MeasureSidebarText(heading.Text, heading.FontFamily, heading.FontSize, heading.FontStyle, heading.FontWeight, heading.FontStretch);
                    desired = Math.Max(desired, textWidth + heading.Margin.Left + heading.Margin.Right);
                    continue;
                }

                Button button = child as Button;
                string label = button == null ? null : button.Content as string;
                if (button != null && label != null)
                {
                    double textWidth = MeasureSidebarText(label, button.FontFamily, button.FontSize, button.FontStyle, button.FontWeight, button.FontStretch);
                    double chrome = button.Margin.Left + button.Margin.Right + button.Padding.Left + button.Padding.Right + button.BorderThickness.Left + button.BorderThickness.Right;
                    desired = Math.Max(desired, textWidth + chrome);
                }
            }

            if (pinnedListBox != null && pinnedSidebarItems.Count > 0)
            {
                const double pinnedChrome = 28.0; // item margin 4+4 and padding 12+8
                for (int i = 0; i < pinnedSidebarItems.Count; i++)
                {
                    PinnedSidebarItem item = pinnedSidebarItems[i];
                    if (item == null) continue;
                    double textWidth = MeasureSidebarText(item.Name, pinnedListBox.FontFamily, pinnedListBox.FontSize, pinnedListBox.FontStyle, pinnedListBox.FontWeight, pinnedListBox.FontStretch);
                    desired = Math.Max(desired, textWidth + pinnedChrome);
                }
            }

            if (sidebarScrollViewer != null && sidebarScrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible)
                desired += SystemParameters.VerticalScrollBarWidth;

            desired = Math.Max(50.0, Math.Min(480.0, Math.Ceiling(desired + 2.0)));
            ColumnDefinition sidebarColumn = mainGrid.ColumnDefinitions[0];
            sidebarColumn.Width = new GridLength(desired);
            settings.SidebarWidth = desired;
            try { SettingsStore.Save(settings); } catch { }
        }

        private static double MeasureSidebarText(string text, FontFamily fontFamily, double fontSize, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch)
        {
            TextBlock probe = new TextBlock
            {
                Text = text ?? string.Empty,
                FontFamily = fontFamily,
                FontSize = fontSize,
                FontStyle = fontStyle,
                FontWeight = fontWeight,
                FontStretch = fontStretch
            };
            probe.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return probe.DesiredSize.Width;
        }

        private void BuildSidebar()
        {
            if (sidebarPanel == null) return;
            sidebarPanel.Children.Clear();
            pinnedSidebarItems.Clear();
            pinnedListBox = null;
            AddSidebarHeading("Places");
            AddSidebarButton("Home", settings.HomePath);
            AddKnownFolder("Desktop", Environment.SpecialFolder.DesktopDirectory);
            AddKnownFolder("Documents", Environment.SpecialFolder.MyDocuments);
            string downloads = KnownFolders.Downloads; if (Directory.Exists(downloads)) AddSidebarButton("Downloads", downloads);
            AddKnownFolder("Pictures", Environment.SpecialFolder.MyPictures);
            AddKnownFolder("Music", Environment.SpecialFolder.MyMusic);
            AddKnownFolder("Videos", Environment.SpecialFolder.MyVideos);

            if (settings.PinnedFolders.Count > 0)
            {
                AddSidebarHeading("Pinned");
                BuildPinnedSidebarList();
            }

            AddSidebarHeading("Drives");
            try
            {
                DriveInfo[] drives = DriveInfo.GetDrives();
                for (int i = 0; i < drives.Length; i++)
                {
                    string label = drives[i].Name;
                    try { if (drives[i].IsReady && !string.IsNullOrEmpty(drives[i].VolumeLabel)) label = drives[i].VolumeLabel + " (" + drives[i].Name.TrimEnd('\\') + ")"; } catch { }
                    AddSidebarButton(label, drives[i].RootDirectory.FullName);
                }
            }
            catch { }
            AddSidebarHeading("System");
            Button recycle = SidebarButton("Recycle Bin");
            recycle.Click += delegate { Navigate(TabState.RecycleBinPath, true); };
            ContextMenu recycleMenu = new ContextMenu();
            MenuItem emptyRecycle = new MenuItem { Header = "Empty Recycle Bin" };
            emptyRecycle.Click += delegate { EmptyRecycleBin(); };
            recycleMenu.Items.Add(emptyRecycle);
            recycle.ContextMenu = recycleMenu;
            sidebarPanel.Children.Add(recycle);
        }

        private void AddKnownFolder(string name, Environment.SpecialFolder folder)
        {
            string path = Environment.GetFolderPath(folder); if (Directory.Exists(path)) AddSidebarButton(name, path);
        }

        private void AddSidebarHeading(string text)
        {
            sidebarPanel.Children.Add(new TextBlock { Text = text, FontWeight = FontWeights.SemiBold, Margin = new Thickness(12, 14, 8, 5) });
        }

        private Button SidebarButton(string text)
        {
            return new Button { Content = text, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(12, 6, 8, 6), Margin = new Thickness(4, 1, 4, 1), Background = Brushes.Transparent, BorderThickness = new Thickness(0) };
        }

        private void AddSidebarButton(string text, string path)
        {
            Button button = SidebarButton(text); button.Tag = path; button.ToolTip = path; button.Click += delegate { Navigate(path, true); };
            sidebarPanel.Children.Add(button);
        }

        private void BuildPinnedSidebarList()
        {
            pinnedSidebarItems.Clear();
            for (int i = 0; i < settings.PinnedFolders.Count; i++)
            {
                string path = settings.PinnedFolders[i];
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) continue;
                string name = Path.GetFileName(path.TrimEnd('\\'));
                if (string.IsNullOrEmpty(name)) name = path;
                pinnedSidebarItems.Add(new PinnedSidebarItem(name, path));
            }
            if (pinnedSidebarItems.Count == 0) return;

            pinnedListBox = new ListBox
            {
                ItemsSource = pinnedSidebarItems,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                SelectionMode = SelectionMode.Single,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                AllowDrop = true
            };
            ScrollViewer.SetVerticalScrollBarVisibility(pinnedListBox, ScrollBarVisibility.Disabled);
            ScrollViewer.SetHorizontalScrollBarVisibility(pinnedListBox, ScrollBarVisibility.Disabled);

            DataTemplate template = new DataTemplate(typeof(PinnedSidebarItem));
            FrameworkElementFactory text = new FrameworkElementFactory(typeof(TextBlock));
            text.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            text.SetBinding(FrameworkElement.ToolTipProperty, new Binding("FullPath"));
            text.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
            template.VisualTree = text;
            pinnedListBox.ItemTemplate = template;

            Style itemStyle = new Style(typeof(ListBoxItem));
            itemStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
            itemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 6, 8, 6)));
            itemStyle.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(4, 1, 4, 1)));
            itemStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            itemStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
            pinnedListBox.ItemContainerStyle = itemStyle;

            ContextMenu menu = new ContextMenu();
            MenuItem unpin = new MenuItem { Header = "Unpin" };
            unpin.Click += delegate
            {
                PinnedSidebarItem item = pinnedListBox != null ? pinnedListBox.SelectedItem as PinnedSidebarItem : null;
                if (item == null) return;
                settings.PinnedFolders.RemoveAll(delegate(string p) { return string.Equals(p, item.FullPath, StringComparison.OrdinalIgnoreCase); });
                try { SettingsStore.Save(settings); } catch { }
                BuildSidebar();
            };
            menu.Items.Add(unpin);
            pinnedListBox.ContextMenu = menu;

            pinnedListBox.PreviewMouseLeftButtonDown += PinnedListMouseLeftButtonDown;
            pinnedListBox.PreviewMouseLeftButtonUp += PinnedListMouseLeftButtonUp;
            pinnedListBox.PreviewMouseMove += PinnedListMouseMove;
            pinnedListBox.PreviewMouseRightButtonDown += PinnedListMouseRightButtonDown;
            pinnedListBox.DragOver += PinnedListDragOver;
            pinnedListBox.Drop += PinnedListDrop;
            pinnedListBox.DragLeave += delegate { ClearPinnedDropIndicator(); };

            sidebarPanel.Children.Add(pinnedListBox);
        }
    }
}
