using System;
using System.Collections.Generic;
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

            List<ShellInterop.PortableDeviceInfo> portableDevices = ShellInterop.GetPortableDevices();
            if (portableDevices.Count > 0)
            {
                AddSidebarHeading("Portable Devices");
                for (int i = 0; i < portableDevices.Count; i++)
                    AddPortableDeviceButton(portableDevices[i]);
            }

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
            button.ContextMenu = CreateSidebarPathContextMenu(path, false);
            sidebarPanel.Children.Add(button);
        }

        private void AddPortableDeviceButton(ShellInterop.PortableDeviceInfo device)
        {
            if (device == null || string.IsNullOrWhiteSpace(device.Name)) return;

            Button button = SidebarButton(device.Name);
            button.ToolTip = "Portable device — opens in Windows Explorer";
            button.Click += delegate
            {
                if (!ShellInterop.OpenPortableDevice(device.Name, device.Identity))
                    MessageBox.Show("Windows Explorer could not open this portable device.", "Ferry", MessageBoxButton.OK, MessageBoxImage.Information);
            };

            ContextMenu menu = new ContextMenu();
            menu.Items.Add(Item("Open in Explorer", delegate
            {
                if (!ShellInterop.OpenPortableDevice(device.Name, device.Identity))
                    MessageBox.Show("Windows Explorer could not open this portable device.", "Ferry", MessageBoxButton.OK, MessageBoxImage.Information);
            }));
            button.ContextMenu = menu;
            sidebarPanel.Children.Add(button);
        }

        private ContextMenu CreateSidebarPathContextMenu(string path, bool includeUnpin)
        {
            ContextMenu menu = new ContextMenu();
            menu.Items.Add(Item("Open", delegate { Navigate(path, true); }));
            menu.Items.Add(Item("Open in New Tab", delegate { OpenNewTab(path, true); }));
            menu.Items.Add(Item("Open in New Ferry Window", delegate { new MainWindow(settings, path).Show(); }));
            menu.Items.Add(new Separator());

            MenuItem terminal = Item("Open Terminal Here", delegate { ShellInterop.OpenTerminal(settings.TerminalCommand, settings.TerminalArguments, path); });
            terminal.IsEnabled = Directory.Exists(path);
            menu.Items.Add(terminal);
            menu.Items.Add(Item("Open in Explorer", delegate { ShellInterop.OpenExplorer(path, false); }));
            menu.Items.Add(Item("Properties", delegate { ShellInterop.ShowProperties(this, path); }));

            if (includeUnpin)
            {
                menu.Items.Add(new Separator());
                menu.Items.Add(Item("Unpin", delegate
                {
                    settings.PinnedFolders.RemoveAll(delegate(string p) { return string.Equals(p, path, StringComparison.OrdinalIgnoreCase); });
                    try { SettingsStore.Save(settings); } catch { }
                    BuildSidebar();
                }));
            }

            menu.Items.Add(new Separator());
            menu.Items.Add(Item("Show more options", delegate
            {
                Point point = PointToScreen(Mouse.GetPosition(this));
                List<string> paths = new List<string>(); paths.Add(path);
                if (!ShellContextMenu.Show(this, paths, point)) ShellInterop.OpenExplorer(path, false);
            }));
            return menu;
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

            pinnedListBox.PreviewMouseLeftButtonDown += PinnedListMouseLeftButtonDown;
            pinnedListBox.PreviewMouseLeftButtonUp += PinnedListMouseLeftButtonUp;
            pinnedListBox.PreviewMouseMove += PinnedListMouseMove;
            pinnedListBox.PreviewMouseRightButtonDown += PinnedListMouseRightButtonDown;
            pinnedListBox.DragOver += PinnedListDragOver;
            pinnedListBox.Drop += PinnedListDrop;
            pinnedListBox.DragLeave += delegate { ClearPinnedDropIndicator(); };

            sidebarPanel.Children.Add(pinnedListBox);
        }

        private void PinFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;
            if (!settings.PinnedFolders.Exists(delegate(string p) { return string.Equals(p, path, StringComparison.OrdinalIgnoreCase); }))
            {
                settings.PinnedFolders.Add(path);
                try { SettingsStore.Save(settings); } catch { }
            }
            BuildSidebar();
        }

        private ListBoxItem GetPinnedListItem(DependencyObject source)
        {
            DependencyObject current = source;
            while (current != null && current != pinnedListBox)
            {
                ListBoxItem item = current as ListBoxItem;
                if (item != null) return item;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private void PinnedListMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || pinnedListBox == null) return;
            ListBoxItem container = GetPinnedListItem(e.OriginalSource as DependencyObject);
            PinnedSidebarItem item = container != null ? container.DataContext as PinnedSidebarItem : null;
            pinnedDragSourceItem = item;
            pinnedDragStarted = false;
            pinnedDragStart = e.GetPosition(pinnedListBox);
        }

        private void PinnedListMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || pinnedListBox == null) return;
            PinnedSidebarItem sourceItem = pinnedDragSourceItem;
            bool wasDrag = pinnedDragStarted;
            pinnedDragSourceItem = null;
            pinnedDragStarted = false;
            if (wasDrag || sourceItem == null) return;

            ListBoxItem container = GetPinnedListItem(e.OriginalSource as DependencyObject);
            PinnedSidebarItem releasedItem = container != null ? container.DataContext as PinnedSidebarItem : null;
            if (releasedItem != null && object.ReferenceEquals(releasedItem, sourceItem))
            {
                Navigate(releasedItem.FullPath, true);
                pinnedListBox.SelectedItem = null;
                e.Handled = true;
            }
        }

        private void PinnedListMouseMove(object sender, MouseEventArgs e)
        {
            if (pinnedListBox == null || pinnedDragSourceItem == null || e.LeftButton != MouseButtonState.Pressed || pinnedDragStarted) return;
            Point current = e.GetPosition(pinnedListBox);
            if (Math.Abs(current.X - pinnedDragStart.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(current.Y - pinnedDragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;

            PinnedSidebarItem sourceItem = pinnedDragSourceItem;
            pinnedDragStarted = true;
            DataObject data = new DataObject();
            data.SetData(PinnedFolderDragFormat, sourceItem.FullPath);
            try { DragDrop.DoDragDrop(pinnedListBox, data, DragDropEffects.Move); }
            finally
            {
                pinnedDragSourceItem = null;
                pinnedDragStarted = false;
                if (pinnedListBox != null) pinnedListBox.SelectedItem = null;
                ClearPinnedDropIndicator();
            }
        }

        private void PinnedListMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (pinnedListBox == null) return;
            ListBoxItem container = GetPinnedListItem(e.OriginalSource as DependencyObject);
            PinnedSidebarItem item = container != null ? container.DataContext as PinnedSidebarItem : null;
            pinnedListBox.SelectedItem = item;
            pinnedListBox.ContextMenu = item != null ? CreateSidebarPathContextMenu(item.FullPath, true) : null;
        }

        private int GetPinnedDropSlot(Point pointer)
        {
            if (pinnedListBox == null || pinnedSidebarItems.Count == 0) return -1;
            for (int i = 0; i < pinnedSidebarItems.Count; i++)
            {
                ListBoxItem container = pinnedListBox.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                if (container == null) continue;
                Point topLeft;
                try { topLeft = container.TranslatePoint(new Point(0, 0), pinnedListBox); }
                catch { continue; }
                double midpoint = topLeft.Y + Math.Max(1.0, container.ActualHeight) / 2.0;
                if (pointer.Y < midpoint) return i;
            }
            return pinnedSidebarItems.Count;
        }

        private void PinnedListDragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(PinnedFolderDragFormat) || pinnedListBox == null) return;
            int slot = GetPinnedDropSlot(e.GetPosition(pinnedListBox));
            if (slot >= 0) ShowPinnedDropIndicator(slot); else ClearPinnedDropIndicator();
            e.Effects = slot >= 0 ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        private void PinnedListDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(PinnedFolderDragFormat) || pinnedListBox == null) return;
            string draggedPath = e.Data.GetData(PinnedFolderDragFormat) as string;
            int slot = pinnedDropSlot >= 0 ? pinnedDropSlot : GetPinnedDropSlot(e.GetPosition(pinnedListBox));
            ClearPinnedDropIndicator();
            if (!string.IsNullOrEmpty(draggedPath) && slot >= 0) ReorderPinnedFolderToSlot(draggedPath, slot);
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void ShowPinnedDropIndicator(int slot)
        {
            if (pinnedListBox == null || pinnedSidebarItems.Count == 0) { ClearPinnedDropIndicator(); return; }
            slot = Math.Max(0, Math.Min(slot, pinnedSidebarItems.Count));
            if (pinnedDropSlot == slot) return;
            ClearPinnedDropIndicator();
            pinnedDropSlot = slot;

            if (slot < pinnedSidebarItems.Count)
            {
                ListBoxItem target = pinnedListBox.ItemContainerGenerator.ContainerFromIndex(slot) as ListBoxItem;
                if (target != null)
                {
                    target.BorderBrush = SystemColors.HighlightBrush;
                    target.BorderThickness = new Thickness(0, 2, 0, 0);
                }
            }
            else
            {
                ListBoxItem target = pinnedListBox.ItemContainerGenerator.ContainerFromIndex(pinnedSidebarItems.Count - 1) as ListBoxItem;
                if (target != null)
                {
                    target.BorderBrush = SystemColors.HighlightBrush;
                    target.BorderThickness = new Thickness(0, 0, 0, 2);
                }
            }
        }

        private void ClearPinnedDropIndicator()
        {
            if (pinnedListBox != null)
            {
                for (int i = 0; i < pinnedSidebarItems.Count; i++)
                {
                    ListBoxItem container = pinnedListBox.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                    if (container == null) continue;
                    container.BorderThickness = new Thickness(0);
                    container.BorderBrush = null;
                }
            }
            pinnedDropSlot = -1;
        }

        private void SidebarDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(PinnedFolderDragFormat))
            {
                // Pinned reordering is intentionally owned by the dedicated Pinned ListBox.
                // Dropping a pin elsewhere in the sidebar does not move or unpin it.
                ClearPinnedDropIndicator();
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) { e.Effects = DragDropEffects.None; e.Handled = true; return; }
            string[] paths = e.Data.GetData(DataFormats.FileDrop) as string[]; bool hasFolder = false;
            if (paths != null) for (int i = 0; i < paths.Length; i++) if (Directory.Exists(paths[i])) { hasFolder = true; break; }
            e.Effects = hasFolder ? DragDropEffects.Link : DragDropEffects.None; e.Handled = true;
        }

        private void SidebarDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(PinnedFolderDragFormat))
                {
                    ClearPinnedDropIndicator();
                    e.Effects = DragDropEffects.None;
                    e.Handled = true;
                    return;
                }

                string[] paths = e.Data.GetData(DataFormats.FileDrop) as string[]; if (paths == null) return;
                bool changed = false;
                for (int i = 0; i < paths.Length; i++)
                {
                    string path = paths[i]; if (!Directory.Exists(path)) continue;
                    if (!settings.PinnedFolders.Exists(delegate(string p) { return string.Equals(p, path, StringComparison.OrdinalIgnoreCase); })) { settings.PinnedFolders.Add(path); changed = true; }
                }
                if (changed) { try { SettingsStore.Save(settings); } catch { } BuildSidebar(); }
                e.Handled = true;
            }
            catch { ClearPinnedDropIndicator(); }
        }

        private void ReorderPinnedFolderToSlot(string draggedPath, int originalSlot)
        {
            if (string.IsNullOrEmpty(draggedPath) || pinnedSidebarItems.Count == 0) return;
            int sourceIndex = -1;
            for (int i = 0; i < pinnedSidebarItems.Count; i++)
            {
                if (string.Equals(pinnedSidebarItems[i].FullPath, draggedPath, StringComparison.OrdinalIgnoreCase)) { sourceIndex = i; break; }
            }
            if (sourceIndex < 0) return;

            int targetIndex = Math.Max(0, Math.Min(originalSlot, pinnedSidebarItems.Count));
            if (sourceIndex < targetIndex) targetIndex--;
            targetIndex = Math.Max(0, Math.Min(targetIndex, pinnedSidebarItems.Count - 1));
            if (targetIndex != sourceIndex) pinnedSidebarItems.Move(sourceIndex, targetIndex);

            List<string> ordered = new List<string>();
            for (int i = 0; i < pinnedSidebarItems.Count; i++) ordered.Add(pinnedSidebarItems[i].FullPath);
            for (int i = 0; i < settings.PinnedFolders.Count; i++)
            {
                string path = settings.PinnedFolders[i];
                if (string.IsNullOrEmpty(path) || Directory.Exists(path)) continue;
                ordered.Add(path);
            }
            settings.PinnedFolders = ordered;
            try { SettingsStore.Save(settings); } catch { }
        }
    }
}
