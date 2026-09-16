using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace Ferry
{
    internal sealed partial class MainWindow : Window
    {
        private UIElement BuildUi()
        {
            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Border toolbar = new Border { BorderBrush = SystemColors.ControlDarkBrush, BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(8, 6, 8, 6), Background = SystemColors.ControlBrush };
            DockPanel top = new DockPanel { LastChildFill = true };
            StackPanel nav = new StackPanel { Orientation = Orientation.Horizontal };
            backButton = ToolbarButton("←", "Back"); backButton.Click += delegate { GoBack(); };
            forwardButton = ToolbarButton("→", "Forward"); forwardButton.Click += delegate { GoForward(); };
            Button up = ToolbarButton("↑", "Up"); up.Click += delegate { GoUp(); };
            Button home = ToolbarButton("⌂", "Home"); home.Click += delegate { Navigate(settings.HomePath, true); };
            Button newTab = ToolbarButton("+", "New tab (Ctrl+T)"); newTab.Click += delegate { OpenNewTab(settings.HomePath, true); };
            MakeSquareToolbarButton(backButton); MakeSquareToolbarButton(forwardButton); MakeSquareToolbarButton(up); MakeSquareToolbarButton(home); MakeSquareToolbarButton(newTab);
            nav.Children.Add(backButton); nav.Children.Add(forwardButton); nav.Children.Add(up); nav.Children.Add(home); nav.Children.Add(newTab);
            DockPanel.SetDock(nav, Dock.Left); top.Children.Add(nav);

            StackPanel right = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8, 0, 0, 0) };
            Button list = ToolbarButton("☷", "List view"); list.Click += delegate { SetTemporaryView("List"); };
            Button grid = ToolbarButton("▦", "Grid view"); grid.Click += delegate { SetTemporaryView("Grid"); };
            Button columns = ToolbarButton("Columns", "Show/hide list columns"); columns.Click += ShowColumnsMenu;
            Button settingsButton = ToolbarButton("⚙", "Settings"); settingsButton.Click += ShowSettings;
            MakeSquareToolbarButton(list); MakeSquareToolbarButton(grid); MakeSquareToolbarButton(settingsButton);
            searchModeBox = new ComboBox { Width = 102, Height = 30, Margin = new Thickness(6, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center };
            searchModeBox.Items.Add("Contains"); searchModeBox.Items.Add("StartsWith"); searchModeBox.SelectedItem = settings.SearchMode;
            searchModeBox.SelectionChanged += delegate { if (searchModeBox.SelectedItem != null) { settings.SearchMode = Convert.ToString(searchModeBox.SelectedItem); if (!suppressSearchTextEvent && searchBox != null && searchBox.Text.Length > 0) StartSearch(); } };
            searchBox = new TextBox { Width = 200, Height = 30, Padding = new Thickness(7, 4, 7, 4), VerticalAlignment = VerticalAlignment.Center, ToolTip = "Search current folder and subfolders (Ctrl+F)" };
            searchBox.TextChanged += SearchBoxChanged;
            searchClearButton = ToolbarButton("×", "Clear search");
            MakeSquareToolbarButton(searchClearButton);
            searchClearButton.Padding = new Thickness(5, 4, 5, 4);
            searchClearButton.Visibility = Visibility.Collapsed;
            searchClearButton.Click += delegate { ClearSearchText(); };
            searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };
            searchDebounceTimer.Tick += delegate { searchDebounceTimer.Stop(); StartSearch(); };
            right.Children.Add(list); right.Children.Add(grid); right.Children.Add(columns); right.Children.Add(searchModeBox); right.Children.Add(searchBox); right.Children.Add(searchClearButton); right.Children.Add(settingsButton);
            DockPanel.SetDock(right, Dock.Right); top.Children.Add(right);

            Grid pathHost = new Grid { Margin = new Thickness(10, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center };
            breadcrumbPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            ScrollViewer crumbScroll = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = breadcrumbPanel };
            // Breadcrumb keeps a visible horizontal scrollbar when needed, but only this ScrollViewer
            // uses a 10px horizontal bar.  List/Grid scrollbars remain on the normal Windows size.
            Style crumbScrollBarStyle = new Style(typeof(ScrollBar));
            Trigger horizontalCrumbBar = new Trigger { Property = ScrollBar.OrientationProperty, Value = Orientation.Horizontal };
            horizontalCrumbBar.Setters.Add(new Setter(FrameworkElement.HeightProperty, 10.0));
            horizontalCrumbBar.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, 10.0));
            horizontalCrumbBar.Setters.Add(new Setter(FrameworkElement.MaxHeightProperty, 10.0));
            crumbScrollBarStyle.Triggers.Add(horizontalCrumbBar);
            crumbScroll.Resources[typeof(ScrollBar)] = crumbScrollBarStyle;
            crumbScroll.Loaded += delegate
            {
                // The Windows theme hides the horizontal end-button chrome until hover at this
                // very small height. Ferry keeps those controls visible so horizontal navigation
                // is discoverable even before the pointer reaches the scrollbar.
                ApplyBreadcrumbScrollButtonChrome(crumbScroll);
                crumbScroll.Dispatcher.BeginInvoke(
                    new Action(delegate { ApplyBreadcrumbScrollButtonChrome(crumbScroll); }),
                    DispatcherPriority.Loaded);
            };

            // Location Box and Breadcrumb share one fixed host height.  Reserve exactly the custom
            // 10px breadcrumb scrollbar row rather than the full Windows system scrollbar height.
            locationBox = new TextBox { Visibility = Visibility.Hidden, Height = 30, Padding = new Thickness(7, 4, 7, 4), VerticalAlignment = VerticalAlignment.Center };
            locationBox.KeyDown += LocationBoxKeyDown;
            pathHost.Children.Add(crumbScroll); pathHost.Children.Add(locationBox);
            pathHost.Loaded += delegate
            {
                const double breadcrumbScrollBarHeight = 10.0;
                double editorHeight = locationBox.ActualHeight > 0 ? locationBox.ActualHeight : locationBox.DesiredSize.Height;
                if (editorHeight > 0)
                {
                    double h = editorHeight + breadcrumbScrollBarHeight;
                    pathHost.Height = h;
                    pathHost.MinHeight = h;
                    pathHost.MaxHeight = h;
                }
            };
            top.Children.Add(pathHost);
            toolbar.Child = top; root.Children.Add(toolbar); Grid.SetRow(toolbar, 0);

            mainGrid = new Grid();
            // Sidebar and file view meet at the same visual boundary.  The resize hit target is
            // overlaid on the sidebar edge instead of consuming its own layout column, so there is
            // no empty strip between the sidebar and the TabControl/file-view frame.
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = settings.SidebarVisible ? new GridLength(settings.SidebarWidth) : new GridLength(0), MinWidth = settings.SidebarVisible ? 50 : 0, MaxWidth = settings.SidebarVisible ? 480 : double.PositiveInfinity });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            sidebarPanel = new StackPanel();
            sidebarScrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = sidebarPanel };
            sidebarBorder = new Border { BorderThickness = new Thickness(0), Child = sidebarScrollViewer, Background = SystemColors.ControlLightBrush, AllowDrop = true };
            sidebarBorder.DragOver += SidebarDragOver;
            sidebarBorder.Drop += SidebarDrop;
            sidebarBorder.DragLeave += delegate { ClearPinnedDropIndicator(); };
            mainGrid.Children.Add(sidebarBorder); Grid.SetColumn(sidebarBorder, 0);

            // The TabControl/file-view frame supplies the visible separator.  Keep only a
            // transparent 5-DIP splitter hit target overlaid on the sidebar's right edge.
            sidebarSplitter = new GridSplitter
            {
                Width = 5,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Stretch,
                ResizeDirection = GridResizeDirection.Columns,
                ResizeBehavior = GridResizeBehavior.CurrentAndNext,
                Background = Brushes.Transparent
            };
            sidebarSplitter.PreviewMouseLeftButtonDown += SidebarSplitterPreviewMouseLeftButtonDown;
            Panel.SetZIndex(sidebarSplitter, 20);
            mainGrid.Children.Add(sidebarSplitter); Grid.SetColumn(sidebarSplitter, 0);
            BuildSidebar();

            tabs = new TabControl { Padding = new Thickness(0), Margin = new Thickness(0) };
            tabs.SelectionChanged += TabsSelectionChanged;
            mainGrid.Children.Add(tabs); Grid.SetColumn(tabs, 1);
            root.Children.Add(mainGrid); Grid.SetRow(mainGrid, 1);

            Border status = new Border { BorderBrush = SystemColors.ControlDarkBrush, BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(10, 4, 10, 4) };
            Grid statusGrid = new Grid();
            statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            statusText = new TextBlock { Text = "Ready", VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
            statusProgress = new ProgressBar
            {
                Width = 160,
                Height = 12,
                Margin = new Thickness(12, 0, 0, 0),
                Minimum = 0,
                Maximum = 100,
                IsIndeterminate = false,
                Visibility = Visibility.Collapsed,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.Gray,
                Background = SystemColors.ControlLightBrush
            };
            archiveCancelButton = new Button
            {
                Content = "Cancel",
                MinWidth = 62,
                Padding = new Thickness(8, 1, 8, 1),
                Margin = new Thickness(8, 0, 0, 0),
                Visibility = Visibility.Collapsed,
                VerticalAlignment = VerticalAlignment.Center
            };
            archiveCancelButton.Click += delegate { CancelArchiveOperation(); };
            statusGrid.Children.Add(statusText); Grid.SetColumn(statusText, 0);
            statusGrid.Children.Add(statusProgress); Grid.SetColumn(statusProgress, 1);
            statusGrid.Children.Add(archiveCancelButton); Grid.SetColumn(archiveCancelButton, 2);
            status.Child = statusGrid; root.Children.Add(status); Grid.SetRow(status, 2);
            return root;
        }

        private Button ToolbarButton(string content, string tooltip)
        {
            return new Button
            {
                Content = content,
                ToolTip = tooltip,
                Height = 30,
                MinWidth = 30,
                Padding = new Thickness(7, 4, 7, 4),
                Margin = new Thickness(2, 0, 2, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private static void MakeSquareToolbarButton(Button button)
        {
            if (button == null) return;
            button.Width = 30;
            button.MinWidth = 30;
            button.MaxWidth = 30;
            button.Height = 30;
        }

        private void ApplyBreadcrumbScrollButtonChrome(ScrollViewer scrollViewer)
        {
            if (scrollViewer == null) return;
            scrollViewer.ApplyTemplate();
            ApplyBreadcrumbScrollButtonChromeRecursive(scrollViewer);
        }

        private void ApplyBreadcrumbScrollButtonChromeRecursive(DependencyObject root)
        {
            if (root == null) return;

            ScrollBar bar = root as ScrollBar;
            if (bar != null && bar.Orientation == Orientation.Horizontal)
            {
                bar.ApplyTemplate();
                StyleBreadcrumbLineButtonsRecursive(bar);
                StyleBreadcrumbThumbRecursive(bar);
            }

            int count = 0;
            try { count = VisualTreeHelper.GetChildrenCount(root); } catch { return; }
            for (int i = 0; i < count; i++)
                ApplyBreadcrumbScrollButtonChromeRecursive(VisualTreeHelper.GetChild(root, i));
        }

        private void StyleBreadcrumbLineButtonsRecursive(DependencyObject root)
        {
            if (root == null) return;

            RepeatButton button = root as RepeatButton;
            if (button != null)
            {
                bool isLeft = object.ReferenceEquals(button.Command, ScrollBar.LineLeftCommand);
                bool isRight = object.ReferenceEquals(button.Command, ScrollBar.LineRightCommand);
                if (isLeft || isRight)
                {
                    button.Opacity = 1.0;
                    button.Visibility = Visibility.Visible;
                    button.Focusable = false;
                    button.IsTabStop = false;
                    button.Template = CreateBreadcrumbLineButtonTemplate(isLeft);
                }
            }

            int count = 0;
            try { count = VisualTreeHelper.GetChildrenCount(root); } catch { return; }
            for (int i = 0; i < count; i++)
                StyleBreadcrumbLineButtonsRecursive(VisualTreeHelper.GetChild(root, i));
        }

        private void StyleBreadcrumbThumbRecursive(DependencyObject root)
        {
            if (root == null) return;

            Thumb thumb = root as Thumb;
            if (thumb != null)
            {
                thumb.Background = new SolidColorBrush(Color.FromRgb(0xC8, 0xC8, 0xC8));
                thumb.Template = CreateBreadcrumbThumbTemplate();
            }

            int count = 0;
            try { count = VisualTreeHelper.GetChildrenCount(root); } catch { return; }
            for (int i = 0; i < count; i++)
                StyleBreadcrumbThumbRecursive(VisualTreeHelper.GetChild(root, i));
        }

        private static ControlTemplate CreateBreadcrumbThumbTemplate()
        {
            Brush normalBrush = new SolidColorBrush(Color.FromRgb(0xC8, 0xC8, 0xC8));
            Brush hoverBrush = new SolidColorBrush(Color.FromRgb(0xB8, 0xB8, 0xB8));
            Brush dragBrush = new SolidColorBrush(Color.FromRgb(0xA8, 0xA8, 0xA8));

            ControlTemplate template = new ControlTemplate(typeof(Thumb));
            FrameworkElementFactory chrome = new FrameworkElementFactory(typeof(Border));
            chrome.Name = "Chrome";
            chrome.SetValue(Border.BackgroundProperty, normalBrush);
            chrome.SetValue(Border.CornerRadiusProperty, new CornerRadius(0.0));
            chrome.SetValue(Border.SnapsToDevicePixelsProperty, true);
            template.VisualTree = chrome;

            Trigger hover = new Trigger { Property = Thumb.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(Border.BackgroundProperty, hoverBrush, "Chrome"));
            template.Triggers.Add(hover);

            Trigger drag = new Trigger { Property = Thumb.IsDraggingProperty, Value = true };
            drag.Setters.Add(new Setter(Border.BackgroundProperty, dragBrush, "Chrome"));
            template.Triggers.Add(drag);

            return template;
        }

        private static ControlTemplate CreateBreadcrumbLineButtonTemplate(bool pointsLeft)
        {
            Brush normalBrush = new SolidColorBrush(Color.FromRgb(0xB8, 0xB8, 0xB8));
            Brush hoverBrush = new SolidColorBrush(Color.FromRgb(0xA2, 0xA2, 0xA2));
            Brush pressedBrush = new SolidColorBrush(Color.FromRgb(0x8E, 0x8E, 0x8E));
            Brush glyphBrush = new SolidColorBrush(Color.FromRgb(0x62, 0x62, 0x62));

            ControlTemplate template = new ControlTemplate(typeof(RepeatButton));

            FrameworkElementFactory chrome = new FrameworkElementFactory(typeof(Border));
            chrome.Name = "Chrome";
            chrome.SetValue(Border.BackgroundProperty, normalBrush);
            chrome.SetValue(Border.CornerRadiusProperty, new CornerRadius(0.0));
            chrome.SetValue(Border.SnapsToDevicePixelsProperty, true);

            FrameworkElementFactory glyph = new FrameworkElementFactory(typeof(System.Windows.Shapes.Path));
            glyph.SetValue(System.Windows.Shapes.Path.FillProperty, glyphBrush);
            glyph.SetValue(FrameworkElement.WidthProperty, 3.0);
            glyph.SetValue(FrameworkElement.HeightProperty, 5.0);
            glyph.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            glyph.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            glyph.SetValue(System.Windows.Shapes.Path.StretchProperty, Stretch.Fill);
            glyph.SetValue(
                System.Windows.Shapes.Path.DataProperty,
                Geometry.Parse(pointsLeft ? "M 3,0 L 0,2.5 L 3,5 Z" : "M 0,0 L 3,2.5 L 0,5 Z"));

            chrome.AppendChild(glyph);
            template.VisualTree = chrome;

            Trigger hover = new Trigger { Property = RepeatButton.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(Border.BackgroundProperty, hoverBrush, "Chrome"));
            template.Triggers.Add(hover);

            Trigger pressed = new Trigger { Property = RepeatButton.IsPressedProperty, Value = true };
            pressed.Setters.Add(new Setter(Border.BackgroundProperty, pressedBrush, "Chrome"));
            template.Triggers.Add(pressed);

            return template;
        }
    }
}
