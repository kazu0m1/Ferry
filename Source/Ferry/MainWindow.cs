using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Ferry
{
    internal sealed partial class MainWindow : Window
    {
        private AppSettings settings;
        private readonly Dictionary<Guid, TabViewContext> contexts = new Dictionary<Guid, TabViewContext>();
        private TabControl tabs;
        private Grid mainGrid;
        private Border sidebarBorder;
        private GridSplitter sidebarSplitter;
        private ScrollViewer sidebarScrollViewer;
        private StackPanel sidebarPanel;
        private StackPanel breadcrumbPanel;
        private TextBox locationBox;
        private IInputElement locationReturnFocus;
        private TextBox searchBox;
        private Button searchClearButton;
        private DispatcherTimer searchDebounceTimer;
        private ComboBox searchModeBox;
        private TextBlock statusText;
        private ProgressBar statusProgress;
        private Button archiveCancelButton;
        private readonly ArchiveService archiveService = new ArchiveService();
        private readonly ArchiveThresholds archiveThresholds = new ArchiveThresholds();
        private CancellationTokenSource archiveCancellation;
        private ArchiveProgressInfo archiveProgressInfo;
        private bool archiveOperationActive;
        private string archiveOperationLabel;
        private bool closeAfterArchiveCancellation;
        private DispatcherTimer transientStatusTimer;
        private string transientStatusMessage;
        private DateTime transientStatusUntilUtc;
        private Button backButton;
        private Button forwardButton;
        private string currentViewMode;
        private Point dragStart;
        private List<RenameUndoRecord> lastRenameUndo;
        private bool suppressSearchTextEvent;
        private readonly NaturalStringComparer natural = new NaturalStringComparer();
        private TabViewContext lastSelectedContext;
        private readonly ObservableCollection<PinnedSidebarItem> pinnedSidebarItems = new ObservableCollection<PinnedSidebarItem>();
        private ListBox pinnedListBox;
        private Point pinnedDragStart;
        private PinnedSidebarItem pinnedDragSourceItem;
        private bool pinnedDragStarted;
        private int pinnedDropSlot = -1;
        private const string PinnedFolderDragFormat = "Ferry.PinnedFolder";
        private const string InlineRenameEditorTag = "Ferry.InlineRenameEditor";
        private const string ListGutterColumnTag = "__FerryListGutter";
        private const double ListTrueBackgroundGutterWidth = 10.0;

        public MainWindow(AppSettings appSettings, string initialPath)
        {
            settings = appSettings ?? new AppSettings();
            currentViewMode = settings.DefaultView;
            Title = "Ferry";
            Width = settings.WindowWidth; Height = settings.WindowHeight; MinWidth = 760; MinHeight = 500;
            if (settings.WindowLeft >= 0) Left = settings.WindowLeft;
            if (settings.WindowTop >= 0) Top = settings.WindowTop;
            if (settings.WindowMaximized) WindowState = WindowState.Maximized;
            WindowStartupLocation = (settings.WindowLeft < 0 || settings.WindowTop < 0) ? WindowStartupLocation.CenterScreen : WindowStartupLocation.Manual;
            Content = BuildUi();
            AddHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);
            // Bubble-phase observer: WPF ListView/ListBox has already completed its native
            // unmodified Arrow navigation by the time this reaches the Window.  Using the
            // bubbling KeyDown event avoids the RC15 dispatcher race that left Ferry's dashed
            // Selection Anchor one item behind the actual keyboard focus/selection.
            AddHandler(Keyboard.KeyDownEvent, new KeyEventHandler(OnKeyDownAfterControls), true);
            PreviewTextInput += OnPreviewTextInput;
            Closing += OnClosing;
            Loaded += delegate
            {
                string path = Directory.Exists(initialPath) ? initialPath : settings.HomePath;
                if (!Directory.Exists(path)) path = KnownFolders.Home;
                if (!Directory.Exists(path)) path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                OpenNewTab(path, true);
            };
        }

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

        private void OpenNewTab(string path, bool select)
        {
            if (!Directory.Exists(path) && !TabState.IsRecycleBinPath(path)) return;
            TabState state = new TabState(path);
            TabViewContext ctx = CreateTabContext(state);
            contexts.Add(state.Id, ctx);
            tabs.Items.Add(ctx.TabItem);
            if (select) tabs.SelectedItem = ctx.TabItem;
            LoadFolder(state, path, false);
        }

        private TabViewContext CreateTabContext(TabState state)
        {
            TabViewContext ctx = new TabViewContext(); ctx.State = state;
            ctx.Container = new Grid();
            ctx.ListView = new ListView { SelectionMode = SelectionMode.Extended, ItemsSource = state.Items, AllowDrop = true };
            VirtualizingStackPanel.SetIsVirtualizing(ctx.ListView, true); VirtualizingStackPanel.SetVirtualizationMode(ctx.ListView, VirtualizationMode.Recycling);
            ctx.GridView = new ListBox { SelectionMode = SelectionMode.Extended, ItemsSource = state.Items, AllowDrop = true, Visibility = Visibility.Collapsed, HorizontalContentAlignment = HorizontalAlignment.Stretch };

            // Prototype 27 / RC16: do not use WPF FocusVisualStyle for the Explorer-style dashed
            // rectangle. Ferry visualizes its logical Shift range anchor. Explorer observation in
            // RC16 preserves the Explorer rule that an unmodified Arrow establishes the newly focused item as the
            // next keyboard Shift+Arrow origin, while an active Shift range keeps its original
            // anchor. Ferry synchronizes this logical anchor after ordinary keyboard navigation.
            FrameworkElementFactory wrap = new FrameworkElementFactory(typeof(VirtualizingWrapPanel)); wrap.SetValue(VirtualizingWrapPanel.IsItemsHostProperty, true); wrap.SetValue(VirtualizingWrapPanel.ItemWidthProperty, 164.0); wrap.SetValue(VirtualizingWrapPanel.ItemHeightProperty, settings.GridIconSize + 78.0);
            ctx.GridView.ItemsPanel = new ItemsPanelTemplate(wrap); ScrollViewer.SetCanContentScroll(ctx.GridView, true); VirtualizingPanel.SetIsVirtualizing(ctx.GridView, true); ctx.GridView.ItemTemplate = BuildGridTemplate();
            BuildListColumns(ctx, false);
            AttachViewEvents(ctx.ListView, state); AttachViewEvents(ctx.GridView, state);
            ctx.Container.Children.Add(ctx.ListView); ctx.Container.Children.Add(ctx.GridView);

            // Rubber-band overlay: visual only. Mouse input remains owned by ListView/ListBox.
            ctx.SelectionOverlay = new Canvas { IsHitTestVisible = false, ClipToBounds = true };
            Color selectionColor = SystemColors.HighlightColor;
            ctx.RubberBandRectangle = new System.Windows.Shapes.Rectangle
            {
                Stroke = SystemColors.HighlightBrush,
                StrokeThickness = 1,
                Fill = new SolidColorBrush(Color.FromArgb(38, selectionColor.R, selectionColor.G, selectionColor.B)),
                Visibility = Visibility.Collapsed
            };
            ctx.SelectionAnchorRectangle = new System.Windows.Shapes.Rectangle
            {
                // RC3: restore the Prototype 27 dash rhythm, but retain RC2's softer dark-gray
                // stroke so the logical Shift anchor is visible without dominating the selection.
                Stroke = new SolidColorBrush(Color.FromRgb(112, 112, 112)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection(new double[] { 1.0, 1.0 }),
                Fill = Brushes.Transparent,
                SnapsToDevicePixels = true,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false
            };
            ctx.SelectionOverlay.Children.Add(ctx.SelectionAnchorRectangle);
            ctx.SelectionOverlay.Children.Add(ctx.RubberBandRectangle);
            Panel.SetZIndex(ctx.SelectionOverlay, 100);
            ctx.Container.Children.Add(ctx.SelectionOverlay);
            ctx.Container.LayoutUpdated += delegate { UpdateListItemVisualBounds(ctx); UpdateSelectionAnchorVisual(ctx); };

            ctx.TabItem = new TabItem { Tag = state, Content = ctx.Container };
            SetTabHeader(ctx);
            ctx.RefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) }; ctx.RefreshTimer.Tick += delegate { ctx.RefreshTimer.Stop(); if (!state.IsSearching) SafeRefreshFolderIncremental(state, false); };
            ctx.RubberBandAutoScrollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(25) };
            ctx.RubberBandAutoScrollTimer.Tick += delegate { HandleRubberBandAutoScrollTick(ctx); };

            // Prototype 11: active rubber-band capture belongs to the neutral tab container,
            // not to ListView/ListBox. WPF ListBox starts its own private auto-scroll timer
            // whenever the ListBox itself owns mouse capture; that timer navigates/focuses
            // rows and fights Ferry's rubber-band auto-scroll. The container receives captured
            // move/up events without activating ListBox's built-in auto-scroll machinery.
            ctx.Container.AddHandler(UIElement.PreviewMouseMoveEvent, new MouseEventHandler(delegate(object sender, MouseEventArgs e)
            {
                if (Mouse.Captured == ctx.Container && (ctx.RubberBandPending || ctx.RubberBandActive) && ctx.RubberBandView != null)
                    HandleRubberBandMouseMove(ctx, ctx.RubberBandView, e);
            }), true);
            ctx.Container.AddHandler(UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(delegate(object sender, MouseButtonEventArgs e)
            {
                if (Mouse.Captured == ctx.Container && (ctx.RubberBandPending || ctx.RubberBandActive) && ctx.RubberBandView != null)
                    HandleLeftMouseUp(ctx.RubberBandView, state, e);
            }), true);
            ctx.Container.LostMouseCapture += delegate(object sender, MouseEventArgs e)
            {
                if ((ctx.RubberBandPending || ctx.RubberBandActive) && Mouse.Captured != ctx.Container)
                    EndRubberBandGesture(ctx, false);
            };
            return ctx;
        }

        private void SetTabHeader(TabViewContext ctx)
        {
            StackPanel header = new StackPanel { Orientation = Orientation.Horizontal };
            TextBlock title = new TextBlock { Text = ctx.State.Title, VerticalAlignment = VerticalAlignment.Center, MaxWidth = 180, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = ctx.State.CurrentPath };
            Button close = new Button { Content = "×", Padding = new Thickness(4, 0, 4, 0), Margin = new Thickness(7, 0, 0, 0), BorderThickness = new Thickness(0), Background = Brushes.Transparent, ToolTip = "Close tab" };
            close.Click += delegate(object sender, RoutedEventArgs e) { e.Handled = true; CloseTab(ctx.State); };
            header.Children.Add(title); header.Children.Add(close); ctx.TabItem.Header = header;
        }

        private void UpdateListItemVisualBounds(TabViewContext ctx)
        {
            if (ctx == null || ctx.ListView == null || ctx.ListView.Visibility != Visibility.Visible) return;
            if (ctx.ListGrid == null) return;

            double gutter = GetListGutterWidth(ctx);
            double dataWidth = GetListDataColumnsWidth(ctx);
            if (dataWidth <= 0) return;

            // RC19: WPF's default ListViewItem selection background paints the entire row,
            // even though RC18 deliberately treats only the visible data-column span as the
            // logical item surface.  Clip only the realized ListViewItem visuals to that same
            // span.  This preserves the native/theme selection brush and text rendering while
            // keeping the left gutter and the area right of the last column visually true
            // background.  Hit testing / rubber-band geometry remain governed by the existing
            // RC18 interaction bounds.
            UpdateListItemVisualBoundsRecursive(ctx.ListView, gutter, dataWidth);
        }

        private void UpdateListItemVisualBoundsRecursive(DependencyObject node, double gutter, double dataWidth)
        {
            if (node == null) return;

            ListViewItem item = node as ListViewItem;
            if (item != null)
            {
                double height = Math.Max(0, item.ActualHeight);
                double available = Math.Max(0, item.ActualWidth - gutter);
                double width = Math.Max(0, Math.Min(dataWidth, available));
                Rect rect = new Rect(gutter, 0, width, height);

                RectangleGeometry current = item.Clip as RectangleGeometry;
                if (current == null || current.Rect != rect)
                    item.Clip = new RectangleGeometry(rect);
                return;
            }

            int count = 0;
            try { count = VisualTreeHelper.GetChildrenCount(node); }
            catch { return; }
            for (int i = 0; i < count; i++)
                UpdateListItemVisualBoundsRecursive(VisualTreeHelper.GetChild(node, i), gutter, dataWidth);
        }

        private void UpdateSelectionAnchorVisual(TabViewContext ctx)
        {
            if (ctx == null || ctx.SelectionAnchorRectangle == null || ctx.Container == null) return;

            FileItem anchor = ctx.SelectionAnchorItem;
            Selector selector = null;
            if (ctx.ListView != null && ctx.ListView.Visibility == Visibility.Visible) selector = ctx.ListView;
            else if (ctx.GridView != null && ctx.GridView.Visibility == Visibility.Visible) selector = ctx.GridView;

            if (anchor == null || selector == null)
            {
                ctx.SelectionAnchorRectangle.Visibility = Visibility.Collapsed;
                return;
            }

            FrameworkElement container = selector.ItemContainerGenerator.ContainerFromItem(anchor) as FrameworkElement;
            if (container == null || !container.IsVisible || container.ActualWidth <= 0 || container.ActualHeight <= 0)
            {
                ctx.SelectionAnchorRectangle.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                Point p = container.TransformToAncestor(ctx.Container).Transform(new Point(0, 0));
                Rect bounds;
                if (selector is ListView)
                {
                    // RC18: the logical List item span is the data columns only.  The left gutter
                    // and any space to the right of the final visible column are true background.
                    double gutter = GetListGutterWidth(ctx);
                    double dataWidth = GetListDataColumnsWidth(ctx);
                    double available = Math.Max(0, container.ActualWidth - gutter);
                    double width = dataWidth > 0 ? Math.Min(dataWidth, available) : available;
                    bounds = new Rect(p.X + gutter + 1, p.Y + 1, Math.Max(0, width - 2), Math.Max(0, container.ActualHeight - 2));
                }
                else
                {
                    bounds = new Rect(p.X + 1, p.Y + 1, Math.Max(0, container.ActualWidth - 2), Math.Max(0, container.ActualHeight - 2));
                }
                Rect visible = new Rect(0, 0, Math.Max(0, ctx.Container.ActualWidth), Math.Max(0, ctx.Container.ActualHeight));
                bounds.Intersect(visible);
                if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0)
                {
                    ctx.SelectionAnchorRectangle.Visibility = Visibility.Collapsed;
                    return;
                }

                Canvas.SetLeft(ctx.SelectionAnchorRectangle, bounds.Left);
                Canvas.SetTop(ctx.SelectionAnchorRectangle, bounds.Top);
                ctx.SelectionAnchorRectangle.Width = bounds.Width;
                ctx.SelectionAnchorRectangle.Height = bounds.Height;
                ctx.SelectionAnchorRectangle.Visibility = Visibility.Visible;
            }
            catch
            {
                ctx.SelectionAnchorRectangle.Visibility = Visibility.Collapsed;
            }
        }

        private DataTemplate BuildGridTemplate()
        {
            FrameworkElementFactory panel = new FrameworkElementFactory(typeof(StackPanel)); panel.SetValue(StackPanel.WidthProperty, 150.0); panel.SetValue(StackPanel.MarginProperty, new Thickness(7)); panel.SetValue(StackPanel.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            FrameworkElementFactory image = new FrameworkElementFactory(typeof(Image)); image.SetBinding(Image.SourceProperty, new Binding("Icon")); image.SetValue(Image.WidthProperty, settings.GridIconSize); image.SetValue(Image.HeightProperty, settings.GridIconSize); image.SetValue(Image.StretchProperty, Stretch.Uniform); image.SetValue(Image.HorizontalAlignmentProperty, HorizontalAlignment.Center); panel.AppendChild(image);

            FrameworkElementFactory nameHost = new FrameworkElementFactory(typeof(Grid)); nameHost.SetValue(Grid.MarginProperty, new Thickness(2, 5, 2, 0));
            FrameworkElementFactory name = new FrameworkElementFactory(typeof(TextBlock)); name.SetBinding(TextBlock.TextProperty, new Binding("Name")); name.SetBinding(UIElement.VisibilityProperty, new Binding("IsRenaming") { Converter = new InverseBooleanToVisibilityConverter() }); name.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center); name.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap); name.SetValue(TextBlock.MaxHeightProperty, 42.0); nameHost.AppendChild(name);
            FrameworkElementFactory editor = BuildInlineRenameEditor(true); nameHost.AppendChild(editor);
            panel.AppendChild(nameHost);

            FrameworkElementFactory count = new FrameworkElementFactory(typeof(TextBlock)); count.SetBinding(TextBlock.TextProperty, new Binding("DisplayCount")); count.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center); count.SetValue(TextBlock.ForegroundProperty, SystemColors.GrayTextBrush); panel.AppendChild(count);
            DataTemplate template = new DataTemplate(typeof(FileItem)); template.VisualTree = panel; return template;
        }

        private DataTemplate BuildNameTemplate()
        {
            FrameworkElementFactory panel = new FrameworkElementFactory(typeof(StackPanel)); panel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            FrameworkElementFactory image = new FrameworkElementFactory(typeof(Image)); image.SetBinding(Image.SourceProperty, new Binding("ListIcon")); image.SetValue(Image.WidthProperty, 18.0); image.SetValue(Image.HeightProperty, 18.0); image.SetValue(Image.MarginProperty, new Thickness(0, 0, 7, 0)); panel.AppendChild(image);

            FrameworkElementFactory nameHost = new FrameworkElementFactory(typeof(Grid));
            FrameworkElementFactory text = new FrameworkElementFactory(typeof(TextBlock)); text.SetBinding(TextBlock.TextProperty, new Binding("Name")); text.SetBinding(UIElement.VisibilityProperty, new Binding("IsRenaming") { Converter = new InverseBooleanToVisibilityConverter() }); text.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center); nameHost.AppendChild(text);
            FrameworkElementFactory editor = BuildInlineRenameEditor(false); nameHost.AppendChild(editor);
            panel.AppendChild(nameHost);
            DataTemplate t = new DataTemplate(); t.VisualTree = panel; return t;
        }

        private FrameworkElementFactory BuildInlineRenameEditor(bool centered)
        {
            FrameworkElementFactory editor = new FrameworkElementFactory(typeof(TextBox));
            Binding textBinding = new Binding("RenameText") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
            editor.SetBinding(TextBox.TextProperty, textBinding);
            editor.SetBinding(UIElement.VisibilityProperty, new Binding("IsRenaming") { Converter = new BooleanToVisibilityConverter() });
            editor.SetValue(FrameworkElement.TagProperty, InlineRenameEditorTag);
            editor.SetValue(Control.PaddingProperty, new Thickness(2, 0, 2, 0));
            editor.SetValue(FrameworkElement.MinWidthProperty, centered ? 120.0 : 90.0);
            if (centered) editor.SetValue(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Center);
            editor.AddHandler(Keyboard.LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(InlineRenameEditorLostKeyboardFocus));
            return editor;
        }

        private void BuildListColumns(TabViewContext ctx, bool searchResults)
        {
            GridView grid = new GridView(); ctx.ListGrid = grid;

            // RC18: Explorer-style Details view keeps a narrow true-background gutter between
            // the ListView frame and the first data column.  This structural spacer keeps header
            // and row geometry aligned; Ferry's hit testing deliberately excludes it from item
            // interaction so it can start/clear rubber-band selection like any other true background.
            GridViewColumnHeader gutterHeader = new GridViewColumnHeader
            {
                Content = string.Empty,
                Tag = ListGutterColumnTag,
                Focusable = false,
                IsHitTestVisible = false,
                Padding = new Thickness(0),
                BorderThickness = new Thickness(0)
            };
            grid.Columns.Add(new GridViewColumn
            {
                Header = gutterHeader,
                CellTemplate = BuildListGutterTemplate(),
                Width = ListTrueBackgroundGutterWidth
            });

            bool recycleView = ctx != null && ctx.State != null && ctx.State.IsRecycleBin;
            List<string> order = settings.ColumnOrder == null ? new List<string>(new string[] { "Name", "Items", "Type", "Size", "Modified", "Created" }) : settings.ColumnOrder;
            for (int i = 0; i < order.Count; i++)
            {
                string name = order[i]; if (settings.HiddenColumns != null && settings.HiddenColumns.Contains(name)) continue;
                GridViewColumn column = MakeColumn(name, recycleView); if (column != null) grid.Columns.Add(column);
            }
            if (searchResults)
            {
                GridViewColumnHeader h = new GridViewColumnHeader { Content = "Location", Tag = "Location" }; h.Click += ColumnHeaderClicked;
                grid.Columns.Add(new GridViewColumn { Header = h, DisplayMemberBinding = new Binding("RelativeLocation"), Width = 260 });
            }
            else if (recycleView)
            {
                GridViewColumnHeader h = new GridViewColumnHeader { Content = "Original Location", Tag = "OriginalLocation" };
                grid.Columns.Add(new GridViewColumn { Header = h, DisplayMemberBinding = new Binding("RelativeLocation"), Width = 300 });
            }
            ctx.ListView.View = grid;
        }

        private DataTemplate BuildListGutterTemplate()
        {
            FrameworkElementFactory spacer = new FrameworkElementFactory(typeof(Border));
            spacer.SetValue(Border.BackgroundProperty, SystemColors.WindowBrush);
            spacer.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            spacer.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Stretch);
            spacer.SetValue(UIElement.IsHitTestVisibleProperty, false);
            DataTemplate template = new DataTemplate();
            template.VisualTree = spacer;
            return template;
        }

        private GridViewColumn MakeColumn(string name, bool recycleView)
        {
            double width = settings.ColumnWidths.ContainsKey(name) ? settings.ColumnWidths[name] : 130;
            string headerText = recycleView && name == "Modified" ? "Date Deleted" : name;
            GridViewColumnHeader h = new GridViewColumnHeader { Content = BuildColumnHeaderContent(headerText, name), Tag = name }; h.Click += ColumnHeaderClicked;
            GridViewColumn c = new GridViewColumn { Header = h, Width = width };
            INotifyPropertyChanged notifyColumn = c as INotifyPropertyChanged;
            if (notifyColumn != null) notifyColumn.PropertyChanged += delegate(object sender, PropertyChangedEventArgs args)
            {
                if ((args.PropertyName == "ActualWidth" || args.PropertyName == "Width") && c.ActualWidth > 0 && !double.IsInfinity(c.ActualWidth)) settings.ColumnWidths[name] = c.ActualWidth;
            };
            if (name == "Name") c.CellTemplate = BuildNameTemplate();
            else if (name == "Items") c.DisplayMemberBinding = new Binding("DisplayCount");
            else if (name == "Type") c.DisplayMemberBinding = new Binding("TypeName");
            else if (name == "Size") c.DisplayMemberBinding = new Binding("SizeText");
            else if (name == "Modified") c.DisplayMemberBinding = new Binding("ModifiedText");
            else if (name == "Created") c.DisplayMemberBinding = new Binding("CreatedText");
            else return null;
            return c;
        }

        private void ColumnHeaderClicked(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader header = sender as GridViewColumnHeader; if (header == null || header.Tag == null) return;
            string key = Convert.ToString(header.Tag); if (key == "Location") key = "Name";
            if (string.Equals(settings.SortKey, key, StringComparison.OrdinalIgnoreCase)) settings.SortDescending = !settings.SortDescending; else { settings.SortKey = key; settings.SortDescending = false; }
            foreach (TabViewContext ctx in contexts.Values) { ctx.State.ClearUnsortedTail(); ApplySort(ctx.State, true); }
            UpdateSortIndicators();
            UpdateStatus();
        }

        private object BuildColumnHeaderContent(string label, string key)
        {
            StackPanel panel = new StackPanel { Orientation = Orientation.Horizontal };
            TextBlock text = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center };
            TextBlock indicator = new TextBlock { Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, FontSize = 10, Text = GetSortIndicator(key) };
            panel.Children.Add(text); panel.Children.Add(indicator);
            return panel;
        }

        private string GetSortIndicator(string key)
        {
            if (!string.Equals(settings.SortKey, key, StringComparison.OrdinalIgnoreCase)) return string.Empty;
            return settings.SortDescending ? "\u25BC" : "\u25B2";
        }

        private void UpdateSortIndicators()
        {
            foreach (TabViewContext ctx in contexts.Values)
            {
                if (ctx == null || ctx.ListGrid == null) continue;
                for (int i = 0; i < ctx.ListGrid.Columns.Count; i++)
                {
                    GridViewColumnHeader header = ctx.ListGrid.Columns[i].Header as GridViewColumnHeader;
                    if (header == null || header.Tag == null) continue;
                    string key = Convert.ToString(header.Tag);
                    StackPanel panel = header.Content as StackPanel;
                    if (panel == null || panel.Children.Count < 2) continue;
                    TextBlock indicator = panel.Children[1] as TextBlock;
                    if (indicator != null) indicator.Text = GetSortIndicator(key);
                }
            }
        }

        private void AttachViewEvents(Selector view, TabState state)
        {
            UIElement element = (UIElement)view;
            ((FrameworkElement)view).ContextMenu = new ContextMenu();
            view.MouseDoubleClick += delegate(object sender, MouseButtonEventArgs e) { HandleViewMouseDoubleClick((Selector)view, state, e); };
            element.PreviewMouseRightButtonDown += delegate(object sender, MouseButtonEventArgs e) { HandleRightMouseDown((ItemsControl)view, state, e); };
            ((FrameworkElement)view).ContextMenuOpening += delegate(object sender, ContextMenuEventArgs e) { BuildAndAssignContextMenu((FrameworkElement)view, state, e); };
            // handledEventsToo is intentional: WPF item/scroll templates may class-handle these.
            element.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(delegate(object sender, MouseButtonEventArgs e) { HandleLeftMouseDown((ItemsControl)view, state, e); }), true);
            element.AddHandler(UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(delegate(object sender, MouseButtonEventArgs e) { HandleLeftMouseUp((Selector)view, state, e); }), true);
            element.AddHandler(UIElement.PreviewMouseMoveEvent, new MouseEventHandler(delegate(object sender, MouseEventArgs e) { HandleMouseMoveDrag((Selector)view, state, e); }), true);
            element.AddHandler(Keyboard.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(delegate(object sender, KeyboardFocusChangedEventArgs e)
            {
                TabViewContext c;
                if (!contexts.TryGetValue(state.Id, out c)) return;
                FileItem focusedItem = GetSelectorItemFromElement((Selector)view, e.NewFocus as DependencyObject);
                if (focusedItem != null) c.KeyboardNavigationItem = focusedItem;
            }), true);
            element.LostMouseCapture += delegate(object sender, MouseEventArgs e)
            {
                TabViewContext c;
                if (contexts.TryGetValue(state.Id, out c) && (c.RubberBandPending || c.RubberBandActive))
                    EndRubberBandGesture(c, false);
            };
            ((UIElement)view).DragOver += delegate(object sender, DragEventArgs e) { ViewDragOver((ItemsControl)view, state, e); };
            ((UIElement)view).DragLeave += delegate(object sender, DragEventArgs e) { if (contexts.ContainsKey(state.Id)) ClearDropTargetHighlight(contexts[state.Id]); };
            ((UIElement)view).Drop += delegate(object sender, DragEventArgs e) { ViewDrop((ItemsControl)view, state, e); };
            ((UIElement)view).MouseDown += delegate(object sender, MouseButtonEventArgs e)
            {
                if (e.ChangedButton == MouseButton.Middle)
                {
                    if (IsLocationBoxVisible()) HideLocationBox(false);
                    bool hitItem = SelectUnderMouse((ItemsControl)view, e.GetPosition((IInputElement)view));
                    if (!hitItem) { ClearSelection(state); e.Handled = true; return; }
                    FileItem item = GetSingleSelected(state);
                    if (item != null && item.IsDirectory) OpenNewTab(item.FullPath, true);
                    e.Handled = true;
                }
            };
            ((Selector)view).SelectionChanged += delegate { TabViewContext c; if (contexts.TryGetValue(state.Id, out c) && c.IsReconciling) return; UpdateStatus(); };
        }

        private void HandleViewMouseDoubleClick(Selector view, TabState state, MouseButtonEventArgs e)
        {
            if (view == null || state == null || e == null || e.ChangedButton != MouseButton.Left) return;

            DependencyObject source = e.OriginalSource as DependencyObject;
            if (source == null || IsInlineRenameEditorSource(source)) return;

            ItemsControl itemsView = view as ItemsControl;
            if (itemsView == null || IsViewChrome(source, itemsView)) return;

            // MouseDoubleClick bubbles from the whole Selector, including its ScrollBar and
            // true background. Only an actual Ferry interaction-bound item double click may open
            // items.  In List mode RC18 excludes the left gutter and the right-of-columns tail.
            Control container = GetItemContainerUnderPoint(itemsView, e.GetPosition(itemsView));
            if (container == null || !(container.DataContext is FileItem)) return;

            OpenSelected(state);
        }

        private void HandleLeftMouseDown(ItemsControl view, TabState state, MouseButtonEventArgs e)
        {
            // RC2: interacting with the file view dismisses the temporary Ctrl+L editor.
            // Do not restore the pre-Ctrl+L focus here: the mouse gesture that follows owns focus.
            if (IsLocationBoxVisible()) HideLocationBox(false);
            dragStart = e.GetPosition(this);
            if (e.ChangedButton != MouseButton.Left || state == null || !contexts.ContainsKey(state.Id)) return;

            TabViewContext ctx = contexts[state.Id];
            Selector selector = (Selector)view;
            Point viewPoint = e.GetPosition(view);
            Control container = GetItemContainerUnderPoint(view, viewPoint);
            bool chrome = IsViewChrome(e.OriginalSource as DependencyObject, view);
            ctx.ItemDragArmed = false;

            if (IsInlineRenameEditorSource(e.OriginalSource as DependencyObject)) return;
            if (chrome)
            {
                CancelPendingMultiSelectionGesture(ctx);
                EndRubberBandGesture(ctx, false);
                return;
            }

            FileItem item = container == null ? null : container.DataContext as FileItem;
            if (item != null) ctx.KeyboardNavigationItem = item;
            bool selectedAtMouseDown = container != null && IsContainerSelected(container);
            bool fileHotZone = container != null && IsFileGestureHotZone(e.OriginalSource as DependencyObject, container);
            ModifierKeys rubberModifiers = Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift);
            bool supportedRubberModifier = rubberModifiers == ModifierKeys.None ||
                                           rubberModifiers == ModifierKeys.Control ||
                                           rubberModifiers == ModifierKeys.Shift;
            bool rubberCandidate = container == null || (!selectedAtMouseDown && !fileHotZone);

            // Prototype 21: Ctrl+Shift rubber-band is owned by Ferry both from an unselected
            // row/tile whitespace candidate and from true background. Explorer distinguishes the
            // two starts: row-whitespace keeps the validated anchor-to-start state machine, while
            // true-background behaves as a plain Ctrl/XOR band against the MouseDown snapshot.
            // Selected rows and file hot zones stay on the established D&D/native-selection routes.
            if (rubberModifiers == (ModifierKeys.Control | ModifierKeys.Shift) &&
                (container == null || (!selectedAtMouseDown && !fileHotZone)))
            {
                CancelPendingMultiSelectionGesture(ctx);
                HashSet<FileItem> initial = SnapshotSelection(selector);
                FileItem selectionAnchor = GetLogicalSelectionAnchor(ctx, selector);
                BeginRubberBandGesture(ctx, selector, e.GetPosition(ctx.Container), item,
                                       rubberModifiers, initial, false);

                if (container == null)
                {
                    // Explorer observation: Ctrl+Shift started on true background is simply
                    // InitialSelection XOR CurrentHitSet. Keep the current selection untouched
                    // until the band becomes active; no anchor-to-start range is involved.
                    ctx.RubberBandCtrlShiftTrueBackgroundMode = true;
                    ((Control)view).Focus();
                }
                else
                {
                    ctx.RubberBandSelectionAnchor = selectionAnchor;

                    // Match the observed pre-drag state: preserve the existing set and add the
                    // row on which Ctrl+Shift MouseDown occurred (A+C in the C->B observation).
                    ApplyAdditivePendingSelection(ctx, selector, item);
                }

                Mouse.Capture(ctx.Container);
                e.Handled = true;
                return;
            }

            // Prototype 14: Ferry owns every rubber-band candidate from MouseDown onward,
            // including Ctrl/Shift row-whitespace starts. Passing those MouseDown events into
            // WPF mutates its Extended-selection anchor before the drag threshold is known and
            // caused both stale Shift anchors and modified-band failures. Stationary clicks are
            // committed explicitly on MouseUp; an actual drag never enters WPF's click path.
            if (supportedRubberModifier && rubberCandidate)
            {
                CancelPendingMultiSelectionGesture(ctx);
                BeginRubberBandGesture(ctx, selector, e.GetPosition(ctx.Container), item,
                                       rubberModifiers, SnapshotSelection(selector), false);

                // A stationary plain click on true background must clear selection without
                // losing the item that owned keyboard focus before MouseDown.  Ferry still focuses
                // the Selector during the pending gesture so actual rubber-band drags keep the
                // previously validated route; MouseUp restores the item only when no drag occurred.
                if (container == null && rubberModifiers == ModifierKeys.None)
                {
                    ctx.RubberBandBackgroundFocusItem = GetFocusedSelectorItem(selector);
                    ClearSelectionFromTrueBackground(ctx);
                }

                ((Control)view).Focus();
                Mouse.Capture(ctx.Container);
                e.Handled = true;
                return;
            }

            // Existing stable Ferry D&D/native-selection path is retained for icon/text hot zones,
            // selected row/tile whitespace, view chrome, and non-rubber-band clicks. Track the
            // logical Shift anchor for ordinary unmodified native clicks as well, so Ctrl+Shift
            // rubber-band never depends solely on WPF's private anchor state.
            ctx.ItemDragArmed = container != null;
            if (container != null && Keyboard.Modifiers == ModifierKeys.None && item != null)
                ctx.SelectionAnchorItem = item;

            if (container != null && Keyboard.Modifiers == ModifierKeys.None &&
                IsContainerSelected(container) && GetSelectedCount(selector) > 1)
            {
                ctx.PendingMultiSelectionClick = true;
                ctx.PendingMultiSelectionView = selector;
                ctx.PendingMultiSelectionItem = item;
                ctx.PendingMultiSelectionDragStarted = false;
                e.Handled = true;
            }
            else
            {
                CancelPendingMultiSelectionGesture(ctx);
            }
        }

        private void HandleLeftMouseUp(Selector view, TabState state, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || state == null || !contexts.ContainsKey(state.Id)) return;
            TabViewContext ctx = contexts[state.Id];

            if ((ctx.RubberBandPending || ctx.RubberBandActive) && ctx.RubberBandView == view)
            {
                FileItem pendingItem = ctx.RubberBandStartItem;
                FileItem backgroundFocusItem = ctx.RubberBandBackgroundFocusItem;
                bool wasActive = ctx.RubberBandActive;
                ModifierKeys completedModifiers = ctx.RubberBandModifiers;
                EndRubberBandGesture(ctx, true);

                if (!wasActive && completedModifiers == (ModifierKeys.Control | ModifierKeys.Shift))
                {
                    // Ctrl+Shift row-whitespace MouseDown already established the observed A+B/A+C
                    // pre-drag state. A stationary click keeps that set and advances the logical
                    // anchor to the clicked row without invoking WPF's modifier-dependent click path.
                    SetExtendedSelectionAnchorOnly(view, pendingItem);
                    if (pendingItem != null) ctx.SelectionAnchorItem = pendingItem;
                    FocusSelectorItem(view, pendingItem);
                    ctx.ItemDragArmed = false;
                    UpdateStatus();
                    e.Handled = true;
                    return;
                }

                if (!wasActive)
                {
                    CommitOwnedRubberBandClick(ctx, view, pendingItem, completedModifiers);

                    // True-background clear can leave keyboard focus on
                    // the ListView/ListBox itself.  The next Up/Down could then use WPF's Selector
                    // navigation path and jump to a distant edge.  Restore the pre-click item
                    // focus/current while keeping the visible selection empty.  Actual rubber-band
                    // drags (wasActive == true) deliberately retain the established behavior.
                    if (pendingItem == null && completedModifiers == ModifierKeys.None &&
                        backgroundFocusItem != null)
                    {
                        RestoreSelectorItemFocusWithoutSelection(ctx, view, backgroundFocusItem);
                    }
                }
                UpdateStatus();
                e.Handled = true;
                return;
            }

            if (!ctx.PendingMultiSelectionClick || ctx.PendingMultiSelectionView != view)
            {
                ctx.ItemDragArmed = false;
                return;
            }

            if (!ctx.PendingMultiSelectionDragStarted && ctx.PendingMultiSelectionItem != null)
            {
                // A normal click on one member of a multi-selection still collapses to
                // that one item, matching normal Extended-selection behavior. The collapse
                // is delayed until mouse-up so a drag can keep the full selection intact.
                // Prototype 13: collapsing a multi-selection must also move WPF's private
                // Extended-selection anchor. Otherwise a later Shift+Click can unexpectedly
                // reuse the range anchor that existed before this ordinary click.
                CommitSingleSelectionWithExplicitShiftAnchor(ctx, view, ctx.PendingMultiSelectionItem);
            }
            CancelPendingMultiSelectionGesture(ctx);
            ctx.ItemDragArmed = false;
            UpdateStatus();
            e.Handled = true;
        }

        private void CommitSingleSelectionWithExplicitShiftAnchor(TabViewContext ctx, Selector selector, FileItem item)
        {
            // Prototype 9 starts from the responsive Prototype 6 baseline, but avoids invoking
            // WPF's full private NotifyListItemClicked path. That path also performs mouse-capture,
            // focus and modifier-dependent work. For this already-resolved stationary whitespace
            // click we only need two effects: commit the single selection and move WPF's private
            // Extended-selection anchor to the clicked item.
            SetSingleSelection(selector, item);
            if (ctx != null && item != null) ctx.SelectionAnchorItem = item;
            FocusSelectorItem(selector, item);

            ListBox listBox = selector as ListBox;
            if (listBox == null || item == null) return;
            ListBoxItem container = selector.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;

            try
            {
                System.Reflection.BindingFlags flags =
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public;

                System.Reflection.PropertyInfo anchor = typeof(ListBox).GetProperty("AnchorItem", flags);
                if (anchor != null && anchor.CanWrite)
                    anchor.SetValue(listBox, item, null);

                // Keep WPF's internal invariant in sync when the property is available.
                System.Reflection.PropertyInfo lastAction = typeof(ListBox).GetProperty("LastActionItem", flags);
                if (lastAction != null && lastAction.CanWrite && container != null)
                    lastAction.SetValue(listBox, container, null);
            }
            catch
            {
                // Visible selection has already been committed. If a future framework build hides
                // these properties, Ferry falls back to the pre-Prototype-6 anchor behavior rather
                // than disturbing background-clear, rubber-band or D&D routing.
            }
        }

        private void CommitOwnedRubberBandClick(TabViewContext ctx, Selector selector, FileItem item, ModifierKeys modifiers)
        {
            if (ctx == null || selector == null) return;

            if (item == null)
            {
                if (modifiers == ModifierKeys.None)
                    ClearSelectionFromTrueBackground(ctx);
                // Modified stationary clicks on true background preserve the existing set.
                return;
            }

            if (modifiers == ModifierKeys.None)
            {
                CommitSingleSelectionWithExplicitShiftAnchor(ctx, selector, item);
                return;
            }

            if (modifiers == ModifierKeys.Control)
            {
                IList selected = GetSelectedItemsList(selector);
                if (selected == null) return;
                ctx.IsReconciling = true;
                try
                {
                    if (selected.Contains(item)) selected.Remove(item);
                    else selected.Add(item);
                }
                finally { ctx.IsReconciling = false; }
                ctx.SelectionAnchorItem = item;
                SetExtendedSelectionAnchorOnly(selector, item);
                FocusSelectorItem(selector, item);
                UpdateStatus();
                return;
            }

            if (modifiers == ModifierKeys.Shift)
            {
                CommitShiftRangeSelection(ctx, selector, item, false);
                return;
            }
        }

        private void CommitShiftRangeSelection(TabViewContext ctx, Selector selector, FileItem endpoint, bool additive)
        {
            if (ctx == null || selector == null || endpoint == null) return;
            FileItem anchor = GetLogicalSelectionAnchor(ctx, selector);
            if (anchor == null)
            {
                CommitSingleSelectionWithExplicitShiftAnchor(ctx, selector, endpoint);
                return;
            }

            int anchorIndex = selector.Items.IndexOf(anchor);
            int endpointIndex = selector.Items.IndexOf(endpoint);
            if (anchorIndex < 0 || endpointIndex < 0)
            {
                CommitSingleSelectionWithExplicitShiftAnchor(ctx, selector, endpoint);
                return;
            }

            int first = Math.Min(anchorIndex, endpointIndex);
            int last = Math.Max(anchorIndex, endpointIndex);
            HashSet<FileItem> desired = additive ? SnapshotSelection(selector) : new HashSet<FileItem>();
            for (int i = first; i <= last; i++)
            {
                FileItem candidate = selector.Items[i] as FileItem;
                if (candidate != null) desired.Add(candidate);
            }

            IList selected = GetSelectedItemsList(selector);
            if (selected == null) return;
            ctx.IsReconciling = true;
            try
            {
                List<FileItem> remove = new List<FileItem>();
                for (int i = 0; i < selected.Count; i++)
                {
                    FileItem candidate = selected[i] as FileItem;
                    if (candidate != null && !desired.Contains(candidate)) remove.Add(candidate);
                }
                for (int i = 0; i < remove.Count; i++) selected.Remove(remove[i]);
                foreach (FileItem candidate in desired)
                    if (!selected.Contains(candidate)) selected.Add(candidate);
            }
            finally { ctx.IsReconciling = false; }

            // Shift range keeps the original anchor, but keyboard focus/current must move
            // to the clicked endpoint just like a native WPF Shift+Click. Without this, a
            // subsequent Up/Down starts from the Selector itself and can jump to the first/last
            // item, which is especially expensive with large virtualized folders.
            ctx.SelectionAnchorItem = anchor;
            SetExtendedSelectionAnchorOnly(selector, anchor);
            FocusSelectorItem(selector, endpoint);
            UpdateStatus();
        }

        private FileItem GetLogicalSelectionAnchor(TabViewContext ctx, Selector selector)
        {
            if (ctx != null && ctx.SelectionAnchorItem != null && selector != null &&
                selector.Items.IndexOf(ctx.SelectionAnchorItem) >= 0)
                return ctx.SelectionAnchorItem;
            return GetExtendedSelectionAnchor(selector);
        }

        private void FocusSelectorItem(Selector selector, FileItem item)
        {
            if (selector == null || item == null) return;

            // Stationary row/tile-whitespace clicks are owned by Ferry so WPF never receives
            // the native ListBoxItem MouseDown that would normally move keyboard focus/current.
            // The clicked item is necessarily realized, so focus its existing container only;
            // do not ScrollIntoView, which could introduce large-folder jumps or extra work.
            ListBoxItem container = selector.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
            if (container != null)
                container.Focus();
        }

        private FileItem GetSelectorItemFromElement(Selector selector, DependencyObject current)
        {
            if (selector == null || current == null) return null;
            while (current != null && current != selector)
            {
                ListBoxItem container = current as ListBoxItem;
                if (container != null)
                {
                    FileItem item = container.DataContext as FileItem;
                    if (item != null && selector.Items.IndexOf(item) >= 0) return item;
                }
                try { current = VisualTreeHelper.GetParent(current); }
                catch { return null; }
            }
            return null;
        }

        private FileItem GetFocusedSelectorItem(Selector selector)
        {
            return GetSelectorItemFromElement(selector, Keyboard.FocusedElement as DependencyObject);
        }

        private int GetGridItemsPerRow(TabViewContext ctx)
        {
            if (ctx == null || ctx.GridView == null) return 1;
            VirtualizingWrapPanel panel = FindVisualChild<VirtualizingWrapPanel>(ctx.GridView);
            if (panel == null) return 1;

            double itemWidth = Math.Max(1.0, panel.ItemWidth);
            double viewportWidth = panel.ViewportWidth > 0 ? panel.ViewportWidth : ctx.GridView.ActualWidth;
            return Math.Max(1, (int)Math.Floor(Math.Max(itemWidth, viewportWidth) / itemWidth));
        }

        private bool TryHandleEmptySelectionArrowNavigation(TabViewContext ctx, Selector selector, Key key, bool extendRange)
        {
            if (ctx == null || selector == null) return false;

            bool gridMode = ReferenceEquals(selector, ctx.GridView);
            bool supportedKey = key == Key.Down || key == Key.Up ||
                                (gridMode && (key == Key.Left || key == Key.Right));
            if (!supportedKey) return false;
            if (GetSelectedCount(selector) != 0 || !selector.IsKeyboardFocused || selector.Items.Count == 0) return false;

            FileItem origin = ctx.KeyboardNavigationItem;
            int originIndex = origin == null ? -1 : selector.Items.IndexOf(origin);
            if (originIndex < 0)
            {
                origin = ctx.SelectionAnchorItem;
                originIndex = origin == null ? -1 : selector.Items.IndexOf(origin);
            }
            if (originIndex < 0) return false;

            int targetIndex;
            if (gridMode)
            {
                // Grid recovery must follow the visual WrapPanel geometry, not List's +/-1 row
                // semantics.  Left/Right move within the current visual row; Up/Down keep the
                // same visual column.  At an outer edge the current item is retained, matching
                // directional navigation rather than wrapping to an unrelated row.
                int perRow = GetGridItemsPerRow(ctx);
                int row = originIndex / perRow;
                int column = originIndex % perRow;
                targetIndex = originIndex;

                if (key == Key.Left && column > 0)
                    targetIndex = originIndex - 1;
                else if (key == Key.Right && column < perRow - 1 && originIndex + 1 < selector.Items.Count)
                    targetIndex = originIndex + 1;
                else if (key == Key.Up && row > 0)
                    targetIndex = originIndex - perRow;
                else if (key == Key.Down && originIndex + perRow < selector.Items.Count)
                    targetIndex = originIndex + perRow;
            }
            else
            {
                targetIndex = key == Key.Down
                    ? Math.Min(selector.Items.Count - 1, originIndex + 1)
                    : Math.Max(0, originIndex - 1);
            }

            FileItem target = selector.Items[targetIndex] as FileItem;
            if (target == null) return false;

            ctx.KeyboardNavigationItem = target;

            if (extendRange)
            {
                // Explorer: after a true-background clear, Shift+Arrow resumes from the item
                // that owned keyboard navigation before the clear.  For example, A -> background
                // clear -> Shift+Down selects A+B.  Preserve A as the range anchor while moving
                // keyboard focus/current to B.
                ctx.SelectionAnchorItem = origin;
                SetExtendedSelectionAnchorOnly(selector, origin);
                CommitShiftRangeSelection(ctx, selector, target, false);
            }
            else
            {
                ctx.SelectionAnchorItem = target;
                SetSingleSelection(selector, target);
            }

            ListBox listBox = selector as ListBox;
            if (listBox != null)
            {
                listBox.ScrollIntoView(target);
                listBox.UpdateLayout();
            }

            // CommitShiftRangeSelection already focuses the endpoint when realized.  Re-assert
            // the private WPF anchor after scrolling/focus so repeated Shift+Arrow continues from
            // the preserved origin rather than the newly focused endpoint.
            SetExtendedSelectionAnchorOnly(selector, extendRange ? origin : target);
            FocusSelectorItem(selector, target);

            // If virtualization delayed container realization, finish the focus/current repair
            // after layout.  The selection and viewport target are already deterministic here.
            if (Keyboard.FocusedElement == selector)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(delegate
                {
                    if (selector.Items.IndexOf(target) < 0) return;
                    SetExtendedSelectionAnchorOnly(selector, extendRange ? origin : target);
                    FocusSelectorItem(selector, target);
                }));
            }

            UpdateStatus();
            return true;
        }

        private void SyncPlainArrowAnchorAfterControlNavigation(TabViewContext ctx, Selector selector)
        {
            if (ctx == null || selector == null) return;
            if (GetSelectedCount(selector) != 1) return;

            // This method is called from the Window's bubbling KeyDown handler.  At that point
            // ListView/ListBox has already executed its native unmodified Up/Down navigation, so
            // the focused/selected item is the actual destination rather than RC15's one-step-old
            // PreviewKeyDown state.  Explorer uses that destination as the next Shift range origin.
            // With an unmodified Arrow, WPF updates selection as part of navigation.  Use the
            // single selected item as the authoritative destination; keyboard focus can be
            // committed slightly later by the framework on some paths.
            FileItem current = selector.SelectedItem as FileItem;
            if (current == null) current = GetFocusedSelectorItem(selector);
            if (current == null || selector.Items.IndexOf(current) < 0) return;

            ctx.KeyboardNavigationItem = current;
            ctx.SelectionAnchorItem = current;
            SetExtendedSelectionAnchorOnly(selector, current);
            UpdateSelectionAnchorVisual(ctx);
        }

        private void RestoreSelectorItemFocusWithoutSelection(TabViewContext ctx, Selector selector, FileItem item)
        {
            if (ctx == null || selector == null || item == null || selector.Items.IndexOf(item) < 0) return;

            // Do not realize or scroll to an off-screen container just to repair focus.  The
            // regression path is a stationary background click while the previously focused item
            // is still realized; avoiding ScrollIntoView preserves the user's viewport.
            ListBoxItem container = selector.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
            if (container == null) return;

            container.Focus();

            // Programmatic focus normally does not change selection, but keep the background-click
            // contract explicit in case a framework/theme path couples focus and selection.
            if (GetSelectedCount(selector) != 0)
                ClearSelectionFromTrueBackground(ctx);
        }

        private void SetSingleSelection(Selector selector, FileItem item)
        {
            ListView lv = selector as ListView;
            if (lv != null)
            {
                lv.UnselectAll();
                lv.SelectedItem = item;
                return;
            }
            ListBox lb = selector as ListBox;
            if (lb != null)
            {
                lb.UnselectAll();
                lb.SelectedItem = item;
            }
        }

        private bool IsContainerSelected(Control container)
        {
            ListViewItem lvi = container as ListViewItem;
            if (lvi != null) return lvi.IsSelected;
            ListBoxItem lbi = container as ListBoxItem;
            return lbi != null && lbi.IsSelected;
        }

        private int GetSelectedCount(Selector selector)
        {
            ListView lv = selector as ListView;
            if (lv != null) return lv.SelectedItems.Count;
            ListBox lb = selector as ListBox;
            return lb == null ? 0 : lb.SelectedItems.Count;
        }

        private void CancelPendingMultiSelectionGesture(TabViewContext ctx)
        {
            if (ctx == null) return;
            ctx.PendingMultiSelectionClick = false;
            ctx.PendingMultiSelectionView = null;
            ctx.PendingMultiSelectionItem = null;
            ctx.PendingMultiSelectionDragStarted = false;
        }

        private void HandleRightMouseDown(ItemsControl view, TabState state, MouseButtonEventArgs e)
        {
            if (IsLocationBoxVisible()) HideLocationBox(false);
            bool hitItem = SelectUnderMouse(view, e.GetPosition(view));
            if (!hitItem && !IsViewChrome(e.OriginalSource as DependencyObject, view)) ClearSelection(state);
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                e.Handled = true;
                List<string> paths = GetSelectedPaths(state);
                if (paths.Count == 0) paths.Add(state.CurrentPath);
                Point point = PointToScreen(e.GetPosition(this));
                Dispatcher.BeginInvoke(new Action(delegate { if (!ShellContextMenu.Show(this, paths, point)) MessageBox.Show(this, "Windows detailed context menu is not available for this selection.", "Ferry", MessageBoxButton.OK, MessageBoxImage.Information); }));
            }
        }

        private bool SelectUnderMouse(ItemsControl view, Point position)
        {
            Control hit = GetItemContainerUnderPoint(view, position);
            if (hit is ListViewItem)
            {
                ListViewItem item = (ListViewItem)hit; if (!item.IsSelected) { ((ListView)view).SelectedItems.Clear(); item.IsSelected = true; }
                return true;
            }
            if (hit is ListBoxItem)
            {
                ListBoxItem item = (ListBoxItem)hit; if (!item.IsSelected) { ((ListBox)view).SelectedItems.Clear(); item.IsSelected = true; }
                return true;
            }
            return false;
        }

        private bool IsItemUnderMouse(ItemsControl view, Point position)
        {
            return GetItemContainerUnderPoint(view, position) != null;
        }

        private bool IsFileGestureHotZone(DependencyObject source, Control itemContainer)
        {
            DependencyObject current = source;
            while (current != null && current != itemContainer)
            {
                if (current is Image || current is TextBlock) return true;
                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }

        private bool IsViewChrome(DependencyObject source, ItemsControl view)
        {
            DependencyObject current = source;
            while (current != null && current != view)
            {
                if (current is ScrollBar || current is Thumb || current is Track || current is RepeatButton || current is GridViewColumnHeader) return true;
                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }

        private bool IsInlineRenameEditorSource(DependencyObject source)
        {
            DependencyObject current = source;
            while (current != null)
            {
                TextBox editor = current as TextBox;
                if (editor != null && string.Equals(Convert.ToString(editor.Tag), InlineRenameEditorTag, StringComparison.Ordinal)) return true;
                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }

        private void ClearSelectionFromTrueBackground(TabViewContext ctx)
        {
            if (ctx == null) return;
            bool previousReconciling = ctx.IsReconciling;
            ctx.IsReconciling = true;
            try
            {
                ctx.ListView.UnselectAll();
                ctx.GridView.UnselectAll();
                ctx.ListView.SelectedItems.Clear();
                ctx.GridView.SelectedItems.Clear();
                ctx.ListView.SelectedItem = null;
                ctx.GridView.SelectedItem = null;
                ctx.ListView.SelectedIndex = -1;
                ctx.GridView.SelectedIndex = -1;
            }
            finally
            {
                ctx.IsReconciling = previousReconciling;
            }
            UpdateStatus();
        }

        private void ClearSelection(TabState state)
        {
            if (state == null || !contexts.ContainsKey(state.Id)) return;
            TabViewContext ctx = contexts[state.Id];
            ctx.ListView.UnselectAll();
            ctx.GridView.UnselectAll();
            UpdateStatus();
        }

        private void OpenTerminalHere(TabState state)
        {
            if (state == null || state.IsRecycleBin || string.IsNullOrWhiteSpace(state.CurrentPath) || !Directory.Exists(state.CurrentPath)) return;
            ShellInterop.OpenTerminal(settings.TerminalCommand, settings.TerminalArguments, state.CurrentPath);
        }

        private void BuildAndAssignContextMenu(FrameworkElement view, TabState state, ContextMenuEventArgs e)
        {
            List<string> paths = GetSelectedPaths(state); ContextMenu menu = new ContextMenu();
            if (state != null && state.IsRecycleBin)
            {
                List<FileItem> recycleItems = GetSelectedItems(state);
                if (recycleItems.Count > 0)
                {
                    menu.Items.Add(Item("Restore", delegate { RestoreRecycleItems(recycleItems); }));
                    menu.Items.Add(Item("Delete Permanently", delegate { DeleteRecycleItemsPermanently(recycleItems); }));
                    FileItem one = recycleItems.Count == 1 ? recycleItems[0] : null;
                    if (one != null && !string.IsNullOrEmpty(one.OriginalPath)) menu.Items.Add(Item("Open Original Location", delegate { string parent = Path.GetDirectoryName(one.OriginalPath); if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent)) Navigate(parent, true); }));
                }
                else
                {
                    menu.Items.Add(Item("Refresh", delegate { LoadFolder(state, TabState.RecycleBinPath, false); }));
                    menu.Items.Add(Item("Empty Recycle Bin", delegate { EmptyRecycleBin(); }));
                }
                view.ContextMenu = menu;
                return;
            }
            if (paths.Count > 0)
            {
                FileItem single = paths.Count == 1 ? GetSingleSelected(state) : null;
                MenuItem open = Item("Open", delegate { OpenSelected(state); }); menu.Items.Add(open);
                if (single != null && !single.IsDirectory) menu.Items.Add(Item("Open with…", delegate { ShellInterop.OpenWith(this, single.FullPath); }));
                if (single != null && single.IsDirectory)
                {
                    menu.Items.Add(Item("Open in New Tab", delegate { OpenNewTab(single.FullPath, true); }));
                    menu.Items.Add(Item("Open in New Ferry Window", delegate { new MainWindow(settings, single.FullPath).Show(); }));
                    menu.Items.Add(Item("Pin to Sidebar", delegate { PinFolder(single.FullPath); }));
                }
                menu.Items.Add(new Separator());
                menu.Items.Add(Item("Cut", delegate { ClipboardHelper.Copy(paths, true); }));
                menu.Items.Add(Item("Copy", delegate { ClipboardHelper.Copy(paths, false); }));
                menu.Items.Add(Item(paths.Count > 1 ? "Batch Rename…" : "Rename", delegate { RenameSelected(state); }));
                menu.Items.Add(Item("Delete", delegate { DeleteSelected(state, false); }));
                menu.Items.Add(new Separator());
                MenuItem compressZip = Item("Compress to ZIP", delegate { CompressSelected(state, paths); });
                compressZip.IsEnabled = !archiveOperationActive;
                menu.Items.Add(compressZip);
                if (single != null && ArchiveHelper.IsZip(single.FullPath))
                {
                    MenuItem extractHere = Item("Extract Here", delegate { ExtractZip(state, single.FullPath, false); });
                    MenuItem extractNamed = Item("Extract to \"" + Path.GetFileNameWithoutExtension(single.FullPath) + "\\\"", delegate { ExtractZip(state, single.FullPath, true); });
                    extractHere.IsEnabled = !archiveOperationActive;
                    extractNamed.IsEnabled = !archiveOperationActive;
                    menu.Items.Add(extractHere);
                    menu.Items.Add(extractNamed);
                }
                menu.Items.Add(new Separator());
                menu.Items.Add(Item("Create Shortcut", delegate { CreateShortcut(paths); }));
                menu.Items.Add(Item("Copy Path", delegate { Clipboard.SetText(string.Join(Environment.NewLine, paths.ToArray())); }));
                menu.Items.Add(Item("Open Terminal Here", delegate { ShellInterop.OpenTerminal(settings.TerminalCommand, settings.TerminalArguments, single != null && single.IsDirectory ? single.FullPath : state.CurrentPath); }));
                menu.Items.Add(Item("Open in Explorer", delegate { ShellInterop.OpenExplorer(single == null ? state.CurrentPath : single.FullPath, single != null && !single.IsDirectory); }));
                if (single != null) menu.Items.Add(Item("Properties", delegate { ShellInterop.ShowProperties(this, single.FullPath); }));
                menu.Items.Add(new Separator());
                menu.Items.Add(Item("Show more options", delegate { Point p = PointToScreen(Mouse.GetPosition(this)); if (!ShellContextMenu.Show(this, paths, p)) MessageBox.Show(this, "Windows detailed context menu is not available for this selection.", "Ferry", MessageBoxButton.OK, MessageBoxImage.Information); }));
            }
            else
            {
                menu.Items.Add(Item("New Folder", delegate { CreateNewFolder(state); }));
                MenuItem paste = Item("Paste", delegate { Paste(state); }); paste.IsEnabled = ClipboardHelper.CanPaste(); menu.Items.Add(paste);
                menu.Items.Add(Item("Refresh", delegate { SafeRefreshFolderIncremental(state, true); }));
                MenuItem terminalHere = Item("Open Terminal Here", delegate { OpenTerminalHere(state); }); terminalHere.InputGestureText = "F12"; menu.Items.Add(terminalHere);
                menu.Items.Add(Item("Open in Explorer", delegate { ShellInterop.OpenExplorer(state.CurrentPath, false); }));
                menu.Items.Add(new Separator());
                menu.Items.Add(Item("Show more options", delegate { Point p = PointToScreen(Mouse.GetPosition(this)); List<string> current = new List<string>(); current.Add(state.CurrentPath); if (!ShellContextMenu.Show(this, current, p)) ShellInterop.OpenExplorer(state.CurrentPath, false); }));
            }
            view.ContextMenu = menu;
        }

        private MenuItem Item(string header, Action action)
        {
            MenuItem item = new MenuItem { Header = header }; item.Click += delegate { action(); }; return item;
        }

        private void HandleMouseMoveDrag(Selector view, TabState state, MouseEventArgs e)
        {
            TabViewContext ctx = state != null && contexts.ContainsKey(state.Id) ? contexts[state.Id] : null;
            if (ctx == null) return;

            if ((ctx.RubberBandPending || ctx.RubberBandActive) && ctx.RubberBandView == view)
            {
                HandleRubberBandMouseMove(ctx, view, e);
                return;
            }

            if (state != null && state.IsRecycleBin) return;
            if (e.LeftButton != MouseButtonState.Pressed) return;
            Point p = e.GetPosition(this);
            if (Math.Abs(p.X - dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(p.Y - dragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;

            if (!ctx.ItemDragArmed) return;

            List<string> paths = GetSelectedPaths(state);
            if (paths.Count == 0) return;

            if (ctx.PendingMultiSelectionClick && ctx.PendingMultiSelectionView == view)
                ctx.PendingMultiSelectionDragStarted = true;

            InvalidateRenameUndo();
            DataObject data = new DataObject(DataFormats.FileDrop, paths.ToArray());
            try
            {
                DragDropEffects finalEffect = DragDrop.DoDragDrop((DependencyObject)view, data, DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link);
                CompleteExternalMoveIfRequired(data, finalEffect, paths, state);
            }
            finally
            {
                CancelPendingMultiSelectionGesture(ctx);
                ctx.ItemDragArmed = false;
            }
        }


        private void CompleteExternalMoveIfRequired(DataObject data, DragDropEffects finalEffect, IList<string> sourcePaths, TabState state)
        {
            // Windows Shell can complete a MOVE in two ways:
            // 1) optimized move: the target moves/deletes the source itself; or
            // 2) unoptimized move: the target copies the data and asks the source to delete it.
            // Only case (2) requires Ferry to remove the originals.  Microsoft documents that
            // the safe signal is BOTH DoDragDrop returning MOVE and the target writing
            // "Performed DropEffect" = MOVE back into the same IDataObject.
            bool performedMove = ClipboardHelper.WasUnoptimizedMovePerformed(data);
            Logger.Write("External D&D completed: finalEffect=" + finalEffect + ", performedMove=" + performedMove + ".");
            if ((finalEffect & DragDropEffects.Move) != DragDropEffects.Move) return;
            if (!performedMove) return;

            List<string> remainingSources = new List<string>();
            for (int i = 0; i < sourcePaths.Count; i++)
            {
                string path = sourcePaths[i];
                if (string.IsNullOrEmpty(path)) continue;
                if (File.Exists(path) || Directory.Exists(path)) remainingSources.Add(path);
            }

            // An optimized Shell move may already have removed every source path.  In that
            // case there is nothing left for Ferry to do, and most importantly no second delete.
            if (remainingSources.Count == 0)
            {
                if (state != null) ScheduleFolderRefresh(state);
                return;
            }

            try
            {
                bool completed = ShellFileOperations.DeleteAfterExternalMove(remainingSources);
                if (!completed)
                    throw new IOException("Windows did not complete cleanup of the original item(s) after the external move.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "The destination accepted the drag as a Move, but Ferry could not remove one or more original item(s).\n\n" + ex.Message,
                    "Ferry", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (state != null) ScheduleFolderRefresh(state);
            }
        }

        private void BeginRubberBandGesture(TabViewContext ctx, Selector view, Point start, FileItem startItem,
                                            ModifierKeys modifiers, HashSet<FileItem> initialSelection, bool passThroughPending)
        {
            if (ctx == null || view == null) return;
            EndRubberBandGesture(ctx, false);
            ctx.RubberBandPending = true;
            ctx.RubberBandActive = false;
            ctx.RubberBandView = view;
            ctx.RubberBandStart = start;
            ctx.RubberBandStartItem = startItem;
            ctx.RubberBandBackgroundFocusItem = null;
            ctx.RubberBandModifiers = modifiers;
            ctx.RubberBandInitialSelection = initialSelection ?? new HashSet<FileItem>();
            ctx.RubberBandPassThroughPending = passThroughPending;
            ctx.RubberBandCurrentPoint = start;
            ctx.RubberBandCurrentHitSet = new HashSet<FileItem>();
            ctx.RubberBandLastVerticalDirection = 0;
            ctx.RubberBandSelectionAnchor = null;
            ctx.RubberBandCtrlShiftBaseSelection = null;
            ctx.RubberBandCtrlShiftTrueBackgroundMode = false;
            ctx.RubberBandTransitionMode = false;
            ctx.RubberBandCtrlShiftShiftReleasedMode = false;
            ctx.RubberBandCtrlShiftShiftReleasedToggleSet = null;
            ctx.RubberBandAutoScrollAccumulator = 0;
            if (ctx.RubberBandAutoScrollTimer != null) ctx.RubberBandAutoScrollTimer.Stop();
            if (ctx.RubberBandRectangle != null)
            {
                ctx.RubberBandRectangle.Visibility = Visibility.Collapsed;
                ctx.RubberBandRectangle.Width = 0;
                ctx.RubberBandRectangle.Height = 0;
            }
        }

        private HashSet<FileItem> SnapshotSelection(Selector view)
        {
            HashSet<FileItem> result = new HashSet<FileItem>();
            IList selected = GetSelectedItemsList(view);
            if (selected == null) return result;
            for (int i = 0; i < selected.Count; i++)
            {
                FileItem item = selected[i] as FileItem;
                if (item != null) result.Add(item);
            }
            return result;
        }

        private void RestoreSelectionSnapshot(TabViewContext ctx, Selector view)
        {
            if (ctx == null || view == null) return;
            IList selected = GetSelectedItemsList(view);
            if (selected == null) return;
            HashSet<FileItem> snapshot = ctx.RubberBandInitialSelection ?? new HashSet<FileItem>();
            ctx.IsReconciling = true;
            try
            {
                List<FileItem> remove = new List<FileItem>();
                for (int i = 0; i < selected.Count; i++)
                {
                    FileItem item = selected[i] as FileItem;
                    if (item != null && !snapshot.Contains(item)) remove.Add(item);
                }
                for (int i = 0; i < remove.Count; i++) selected.Remove(remove[i]);
                foreach (FileItem item in snapshot)
                    if (!selected.Contains(item)) selected.Add(item);
            }
            finally { ctx.IsReconciling = false; }
        }

        private void ApplyAdditivePendingSelection(TabViewContext ctx, Selector view, FileItem item)
        {
            if (ctx == null || view == null || item == null) return;
            IList selected = GetSelectedItemsList(view);
            if (selected == null) return;
            ctx.IsReconciling = true;
            try
            {
                HashSet<FileItem> initial = ctx.RubberBandInitialSelection ?? new HashSet<FileItem>();
                List<FileItem> remove = new List<FileItem>();
                for (int i = 0; i < selected.Count; i++)
                {
                    FileItem candidate = selected[i] as FileItem;
                    if (candidate != null && !initial.Contains(candidate) && candidate != item) remove.Add(candidate);
                }
                for (int i = 0; i < remove.Count; i++) selected.Remove(remove[i]);
                foreach (FileItem candidate in initial)
                    if (!selected.Contains(candidate)) selected.Add(candidate);
                if (!selected.Contains(item)) selected.Add(item);
            }
            finally { ctx.IsReconciling = false; }
        }

        private FileItem GetExtendedSelectionAnchor(Selector selector)
        {
            ListBox listBox = selector as ListBox;
            if (listBox == null) return null;
            try
            {
                System.Reflection.BindingFlags flags =
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public;
                System.Reflection.PropertyInfo anchor = typeof(ListBox).GetProperty("AnchorItem", flags);
                return anchor == null ? null : anchor.GetValue(listBox, null) as FileItem;
            }
            catch { return null; }
        }

        private void SetExtendedSelectionAnchorOnly(Selector selector, FileItem item)
        {
            ListBox listBox = selector as ListBox;
            if (listBox == null || item == null) return;
            ListBoxItem container = selector.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
            try
            {
                System.Reflection.BindingFlags flags =
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public;
                System.Reflection.PropertyInfo anchor = typeof(ListBox).GetProperty("AnchorItem", flags);
                if (anchor != null && anchor.CanWrite) anchor.SetValue(listBox, item, null);
                System.Reflection.PropertyInfo lastAction = typeof(ListBox).GetProperty("LastActionItem", flags);
                if (lastAction != null && lastAction.CanWrite && container != null)
                    lastAction.SetValue(listBox, container, null);
            }
            catch { }
        }

        private void PrepareCtrlShiftRubberBandBase(TabViewContext ctx, Selector view)
        {
            if (ctx == null || view == null) return;
            HashSet<FileItem> baseline = new HashSet<FileItem>();
            HashSet<FileItem> initial = ctx.RubberBandInitialSelection ?? new HashSet<FileItem>();
            foreach (FileItem item in initial) baseline.Add(item);

            FileItem anchorItem = ctx.RubberBandSelectionAnchor;
            FileItem startItem = ctx.RubberBandStartItem;
            int anchorIndex = anchorItem == null ? -1 : view.Items.IndexOf(anchorItem);
            int startIndex = startItem == null ? -1 : view.Items.IndexOf(startItem);
            if (anchorIndex >= 0 && startIndex >= 0)
            {
                int first = Math.Min(anchorIndex, startIndex);
                int last = Math.Max(anchorIndex, startIndex);
                for (int i = first; i <= last; i++)
                {
                    FileItem candidate = view.Items[i] as FileItem;
                    if (candidate != null) baseline.Add(candidate);
                }
            }
            ctx.RubberBandCtrlShiftBaseSelection = baseline;
        }

        private HashSet<FileItem> BuildCtrlShiftShiftReleaseToggleSet(TabViewContext ctx, Selector view)
        {
            HashSet<FileItem> result = new HashSet<FileItem>();
            if (ctx == null || view == null) return result;

            FileItem anchorItem = ctx.RubberBandSelectionAnchor;
            FileItem startItem = ctx.RubberBandStartItem;
            int anchorIndex = anchorItem == null ? -1 : view.Items.IndexOf(anchorItem);
            int startIndex = startItem == null ? -1 : view.Items.IndexOf(startItem);
            if (anchorIndex < 0 || startIndex < 0) return result;

            int first = Math.Min(anchorIndex, startIndex);
            int last = Math.Max(anchorIndex, startIndex);
            for (int i = first; i <= last; i++)
            {
                FileItem candidate = view.Items[i] as FileItem;
                if (candidate != null && candidate != startItem) result.Add(candidate);
            }
            return result;
        }

        private void HandleRubberBandMouseMove(TabViewContext ctx, Selector view, MouseEventArgs e)
        {
            if (ctx == null || view == null || ctx.RubberBandView != view) return;
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                EndRubberBandGesture(ctx, true);
                return;
            }

            Point current = e.GetPosition(ctx.Container);
            if (!ctx.RubberBandActive)
            {
                double dx = Math.Abs(current.X - ctx.RubberBandStart.X);
                double dy = Math.Abs(current.Y - ctx.RubberBandStart.Y);
                if (dx < SystemParameters.MinimumHorizontalDragDistance && dy < SystemParameters.MinimumVerticalDragDistance) return;

                if (ctx.RubberBandModifiers == (ModifierKeys.Control | ModifierKeys.Shift) &&
                    !ctx.RubberBandCtrlShiftTrueBackgroundMode)
                    PrepareCtrlShiftRubberBandBase(ctx, view);
                else if (ctx.RubberBandPassThroughPending)
                    RestoreSelectionSnapshot(ctx, view);

                ctx.RubberBandPending = false;
                ctx.RubberBandActive = true;
                ctx.RubberBandPassThroughPending = false;
                if (Mouse.Captured != ctx.Container) Mouse.Capture(ctx.Container);
                if (ctx.RubberBandRectangle != null) ctx.RubberBandRectangle.Visibility = Visibility.Visible;
                if (ctx.RubberBandAutoScrollTimer != null) ctx.RubberBandAutoScrollTimer.Start();
            }

            UpdateActiveRubberBand(ctx, view, current);
            e.Handled = true;
        }

        private void UpdateActiveRubberBand(TabViewContext ctx, Selector view, Point current)
        {
            if (ctx == null || view == null || !ctx.RubberBandActive) return;
            ctx.RubberBandCurrentPoint = current;

            double width = Math.Max(0, ctx.Container.ActualWidth);
            double height = Math.Max(0, ctx.Container.ActualHeight);
            double x1 = Math.Max(0, Math.Min(width, ctx.RubberBandStart.X));
            double y1 = Math.Max(0, Math.Min(height, ctx.RubberBandStart.Y));
            double x2 = Math.Max(0, Math.Min(width, current.X));
            double y2 = Math.Max(0, Math.Min(height, current.Y));
            double left = Math.Min(x1, x2);
            double top = Math.Min(y1, y2);
            double rectWidth = Math.Max(1.0, Math.Abs(x2 - x1));
            double rectHeight = Math.Max(1.0, Math.Abs(y2 - y1));
            if (left + rectWidth > width) rectWidth = Math.Max(1.0, width - left);
            if (top + rectHeight > height) rectHeight = Math.Max(1.0, height - top);
            Rect rect = new Rect(left, top, rectWidth, rectHeight);
            ShowRubberBandRectangle(ctx, rect);
            UpdateRubberBandSelection(ctx, view, rect);
        }

        private void HandleRubberBandAutoScrollTick(TabViewContext ctx)
        {
            if (ctx == null || !ctx.RubberBandActive || ctx.RubberBandView == null)
            {
                if (ctx != null && ctx.RubberBandAutoScrollTimer != null) ctx.RubberBandAutoScrollTimer.Stop();
                return;
            }
            if (Mouse.LeftButton != MouseButtonState.Pressed)
            {
                EndRubberBandGesture(ctx, true);
                return;
            }

            Selector view = ctx.RubberBandView;
            ScrollViewer scroll = FindVisualChild<ScrollViewer>(view);
            if (scroll == null || scroll.ScrollableHeight <= 0) return;

            Point pointer = Mouse.GetPosition(scroll);
            double topBoundary = 0;
            ListView listView = view as ListView;
            if (listView != null)
            {
                GridViewHeaderRowPresenter header = FindVisualChild<GridViewHeaderRowPresenter>(listView);
                if (header != null && header.ActualHeight > 0)
                {
                    try
                    {
                        Point hp = header.TranslatePoint(new Point(0, 0), scroll);
                        topBoundary = Math.Max(0, hp.Y + header.ActualHeight);
                    }
                    catch { topBoundary = header.ActualHeight; }
                }
            }

            const double edge = 30.0;
            int direction = 0;
            double depth = 0;
            double bottomBoundary = Math.Max(topBoundary, scroll.ActualHeight);
            if (pointer.Y < topBoundary + edge)
            {
                direction = -1;
                depth = (topBoundary + edge) - pointer.Y;
            }
            else if (pointer.Y > bottomBoundary - edge)
            {
                direction = 1;
                depth = pointer.Y - (bottomBoundary - edge);
            }

            if (direction == 0)
            {
                ctx.RubberBandAutoScrollAccumulator = 0;
                return;
            }

            // 100 preserves the validated Prototype 19/20 curve exactly. RC3 makes the setting
            // scale the whole curve (not only its distant cap), so values above/below 100 are
            // perceptibly different at the same pointer depth. The selectable range is 30-300.
            double maxSpeed = Math.Max(30.0, Math.Min(300.0, settings.RubberBandAutoScrollSpeed));
            double normalizedDepth = Math.Max(0, depth) / edge;
            double baseLinesPerSecond = normalizedDepth <= 1.0
                ? 2.5 + normalizedDepth * 6.0
                : 8.5 + (normalizedDepth - 1.0) * 14.0;
            double speedScale = maxSpeed / 100.0;
            double linesPerSecond = Math.Min(maxSpeed, baseLinesPerSecond * speedScale);
            ctx.RubberBandAutoScrollAccumulator += linesPerSecond * 0.025;
            int maxStepsPerTick = Math.Max(1, (int)Math.Ceiling(maxSpeed * 0.025));
            int steps = Math.Min(maxStepsPerTick, (int)Math.Floor(ctx.RubberBandAutoScrollAccumulator));
            if (steps <= 0) return;
            ctx.RubberBandAutoScrollAccumulator -= steps;

            double before = scroll.VerticalOffset;
            for (int i = 0; i < steps; i++)
            {
                if (direction < 0) scroll.LineUp(); else scroll.LineDown();
            }
            view.UpdateLayout();
            double after = scroll.VerticalOffset;
            if (Math.Abs(after - before) < 0.0001) return;

            double offsetDelta = after - before;
            // ListView's content-scroll offset is item-based, so convert it back to pixels.
            // GridView uses Ferry's VirtualizingWrapPanel whose IScrollInfo offsets are already
            // pixel coordinates even with CanContentScroll=true. Treating those as item counts
            // would over-correct the rubber-band origin during grid auto-scroll.
            double pixelDelta = view is ListView && scroll.CanContentScroll
                ? offsetDelta * EstimateRealizedRowHeight(view)
                : offsetDelta;
            ctx.RubberBandStart = new Point(ctx.RubberBandStart.X, ctx.RubberBandStart.Y - pixelDelta);
            Point current = Mouse.GetPosition(ctx.Container);
            UpdateActiveRubberBand(ctx, view, current);
        }

        private double EstimateRealizedRowHeight(Selector view)
        {
            if (view == null) return 24.0;
            double total = 0;
            int count = 0;
            for (int i = 0; i < view.Items.Count && count < 12; i++)
            {
                FrameworkElement container = view.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                if (container == null || container.ActualHeight <= 0) continue;
                total += container.ActualHeight;
                count++;
            }
            return count == 0 ? 24.0 : Math.Max(1.0, total / count);
        }

        private T FindVisualChild<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null) return null;
            int count = 0;
            try { count = VisualTreeHelper.GetChildrenCount(root); } catch { return null; }
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                T match = child as T;
                if (match != null) return match;
                match = FindVisualChild<T>(child);
                if (match != null) return match;
            }
            return null;
        }

        private void ShowRubberBandRectangle(TabViewContext ctx, Rect rect)
        {
            if (ctx == null || ctx.RubberBandRectangle == null) return;
            Canvas.SetLeft(ctx.RubberBandRectangle, rect.Left);
            Canvas.SetTop(ctx.RubberBandRectangle, rect.Top);
            ctx.RubberBandRectangle.Width = Math.Max(0, rect.Width);
            ctx.RubberBandRectangle.Height = Math.Max(0, rect.Height);
        }

        private void UpdateRubberBandSelection(TabViewContext ctx, Selector view, Rect selectionRect)
        {
            if (ctx == null || view == null) return;
            HashSet<FileItem> target = new HashSet<FileItem>();

            for (int i = 0; i < view.Items.Count; i++)
            {
                FrameworkElement container = view.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                if (container == null || container.ActualWidth <= 0 || container.ActualHeight <= 0) continue;

                Rect itemRect;
                try
                {
                    Point origin = container.TranslatePoint(new Point(0, 0), ctx.Container);
                    double itemLeft = origin.X;
                    double itemWidth = container.ActualWidth;
                    if (view is ListView && ctx.ListGrid != null)
                    {
                        double gutter = GetListGutterWidth(ctx);
                        double columnsWidth = GetListDataColumnsWidth(ctx);
                        itemLeft += gutter;
                        double available = Math.Max(0, container.ActualWidth - gutter);
                        itemWidth = columnsWidth > 0 ? Math.Min(available, columnsWidth) : available;
                    }
                    itemRect = new Rect(itemLeft, origin.Y, Math.Max(1, itemWidth), Math.Max(1, container.ActualHeight));
                }
                catch { continue; }

                bool intersects = selectionRect.Right > itemRect.Left && selectionRect.Left < itemRect.Right &&
                                  selectionRect.Bottom > itemRect.Top && selectionRect.Top < itemRect.Bottom;
                if (!intersects) continue;

                FileItem item = container.DataContext as FileItem;
                if (item != null) target.Add(item);
            }

            if (ctx.RubberBandCurrentHitSet == null) ctx.RubberBandCurrentHitSet = new HashSet<FileItem>();
            HashSet<FileItem> previousHits = new HashSet<FileItem>(ctx.RubberBandCurrentHitSet);

            int verticalDirection = 0;
            double verticalDelta = ctx.RubberBandCurrentPoint.Y - ctx.RubberBandStart.Y;
            if (verticalDelta > 0.5) verticalDirection = 1;
            else if (verticalDelta < -0.5) verticalDirection = -1;
            if (ctx.RubberBandLastVerticalDirection != 0 && verticalDirection != 0 &&
                verticalDirection != ctx.RubberBandLastVerticalDirection)
            {
                // Crossing the anchor changes which side of the content rectangle is active.
                // Drop retained off-screen hits and rebuild from currently realized geometry.
                ctx.RubberBandCurrentHitSet.Clear();
            }
            if (verticalDirection != 0) ctx.RubberBandLastVerticalDirection = verticalDirection;

            // Reconcile every realized container against the current rectangle. Items that were
            // hit before scrolling and are now virtualized stay in the set, so selection does not
            // disappear merely because their containers left the viewport.
            for (int i = 0; i < view.Items.Count; i++)
            {
                FrameworkElement container = view.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                if (container == null) continue;
                FileItem realizedItem = container.DataContext as FileItem;
                if (realizedItem == null) continue;
                if (target.Contains(realizedItem)) ctx.RubberBandCurrentHitSet.Add(realizedItem);
                else ctx.RubberBandCurrentHitSet.Remove(realizedItem);
            }
            target = new HashSet<FileItem>(ctx.RubberBandCurrentHitSet);

            HashSet<FileItem> desired = new HashSet<FileItem>();
            HashSet<FileItem> initial = ctx.RubberBandInitialSelection ?? new HashSet<FileItem>();

            // Prototype 16 Explorer observation:
            // - Modifiers added after MouseDown do not join a Normal/Shift-started band.
            // - Shift-started rubber-band selection itself behaves as Normal (the Shift range
            //   state exists only before the drag threshold when starting on row whitespace).
            // - A Ctrl-started band begins as XOR while Ctrl remains held. If Ctrl is released,
            //   the existing selection is left untouched and subsequent boundary crossings use
            //   ordinary enter=select / leave=deselect behavior. Releasing Ctrl before the
            //   threshold therefore preserves the initial set and adds the first hit rows.
            if (ctx.RubberBandModifiers == ModifierKeys.Control &&
                (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
                ctx.RubberBandTransitionMode = true;

            // Prototype 18 Explorer observation: a Ctrl+Shift-started band has asymmetric
            // partial-release behavior. Releasing Ctrl while keeping Shift leaves the validated
            // Ctrl+Shift state machine unchanged. Releasing Shift while keeping Ctrl freezes the
            // current selection, and subsequent hit-boundary crossings toggle the anchor-to-start
            // range excluding the start row. Re-pressing Shift does not re-enter the original mode.
            if (ctx.RubberBandModifiers == (ModifierKeys.Control | ModifierKeys.Shift) &&
                !ctx.RubberBandCtrlShiftTrueBackgroundMode &&
                !ctx.RubberBandCtrlShiftShiftReleasedMode &&
                (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control &&
                (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
            {
                ctx.RubberBandCtrlShiftShiftReleasedMode = true;
                ctx.RubberBandCtrlShiftShiftReleasedToggleSet = BuildCtrlShiftShiftReleaseToggleSet(ctx, view);
            }

            if (ctx.RubberBandTransitionMode)
            {
                IList transitionSelected = GetSelectedItemsList(view);
                if (transitionSelected == null) return;

                HashSet<FileItem> entered = new HashSet<FileItem>(target);
                entered.ExceptWith(previousHits);
                HashSet<FileItem> left = new HashSet<FileItem>(previousHits);
                left.ExceptWith(target);

                ctx.IsReconciling = true;
                try
                {
                    foreach (FileItem item in left)
                        if (transitionSelected.Contains(item)) transitionSelected.Remove(item);
                    foreach (FileItem item in entered)
                        if (!transitionSelected.Contains(item)) transitionSelected.Add(item);
                }
                finally { ctx.IsReconciling = false; }

                UpdateStatus();
                return;
            }

            if (ctx.RubberBandCtrlShiftShiftReleasedMode)
            {
                IList transitionSelected = GetSelectedItemsList(view);
                if (transitionSelected == null) return;

                HashSet<FileItem> changed = new HashSet<FileItem>(target);
                changed.SymmetricExceptWith(previousHits);
                changed.Remove(ctx.RubberBandStartItem);
                HashSet<FileItem> toggleSet = ctx.RubberBandCtrlShiftShiftReleasedToggleSet ?? new HashSet<FileItem>();

                ctx.IsReconciling = true;
                try
                {
                    foreach (FileItem boundaryItem in changed)
                    {
                        // Explorer applies this once per row boundary crossed. The crossed row's
                        // current selected state is preserved; the anchored pre-start span toggles.
                        foreach (FileItem item in toggleSet)
                        {
                            if (transitionSelected.Contains(item)) transitionSelected.Remove(item);
                            else transitionSelected.Add(item);
                        }
                    }
                }
                finally { ctx.IsReconciling = false; }

                UpdateStatus();
                return;
            }

            if (ctx.RubberBandModifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                if (ctx.RubberBandCtrlShiftTrueBackgroundMode)
                {
                    // Explorer observation (Prototype 21 research): Ctrl+Shift begun on true
                    // background is identical to Ctrl rubber-band: MouseDown snapshot XOR current hits.
                    foreach (FileItem candidate in initial) desired.Add(candidate);
                    foreach (FileItem candidate in target)
                    {
                        if (!desired.Add(candidate)) desired.Remove(candidate);
                    }
                }
                else
                {
                    // Row-whitespace Ctrl+Shift keeps the validated anchor-to-start baseline,
                    // then XORs the moving band's currently hit rows (excluding the start row).
                    // Prototype 18 keeps this state machine when Ctrl is released but Shift remains;
                    // the Shift-release/Ctrl-held transition is handled above.
                    HashSet<FileItem> baseline = ctx.RubberBandCtrlShiftBaseSelection ?? initial;
                    foreach (FileItem candidate in baseline) desired.Add(candidate);
                    foreach (FileItem candidate in target)
                    {
                        if (candidate == ctx.RubberBandStartItem) continue;
                        if (!desired.Add(candidate)) desired.Remove(candidate);
                    }
                }
            }
            else if (ctx.RubberBandModifiers == ModifierKeys.Control)
            {
                // Ctrl held from MouseDown and still held: XOR against the MouseDown snapshot.
                foreach (FileItem item in initial) desired.Add(item);
                foreach (FileItem item in target)
                {
                    if (!desired.Add(item)) desired.Remove(item);
                }
            }
            else
            {
                // Normal and Shift-started active bands both use the current rectangle only.
                // Ctrl/Shift pressed later during a Normal-started gesture are ignored.
                foreach (FileItem item in target) desired.Add(item);
            }

            IList selected = GetSelectedItemsList(view);
            if (selected == null) return;

            ctx.IsReconciling = true;
            try
            {
                List<FileItem> remove = new List<FileItem>();
                for (int i = 0; i < selected.Count; i++)
                {
                    FileItem item = selected[i] as FileItem;
                    if (item != null && !desired.Contains(item)) remove.Add(item);
                }
                for (int i = 0; i < remove.Count; i++) selected.Remove(remove[i]);
                foreach (FileItem item in desired)
                    if (!selected.Contains(item)) selected.Add(item);
            }
            finally { ctx.IsReconciling = false; }

            UpdateStatus();
        }

        private IList GetSelectedItemsList(Selector view)
        {
            ListView lv = view as ListView;
            if (lv != null) return lv.SelectedItems;
            ListBox lb = view as ListBox;
            return lb == null ? null : lb.SelectedItems;
        }

        private void ReconcileSelectorSelection(TabViewContext ctx, Selector target, HashSet<FileItem> desired)
        {
            if (ctx == null || target == null || desired == null) return;
            IList selected = GetSelectedItemsList(target);
            if (selected == null) return;

            // Reconcile incrementally instead of UnselectAll + re-add. This is important for the
            // finishing-phase large-folder tests: if the hidden selector already matches most of
            // the visible selector, a view switch only touches the actual delta.
            HashSet<FileItem> current = new HashSet<FileItem>();
            for (int i = 0; i < selected.Count; i++)
            {
                FileItem item = selected[i] as FileItem;
                if (item != null) current.Add(item);
            }

            ctx.IsReconciling = true;
            try
            {
                List<FileItem> remove = new List<FileItem>();
                foreach (FileItem item in current)
                    if (!desired.Contains(item)) remove.Add(item);
                for (int i = 0; i < remove.Count; i++) selected.Remove(remove[i]);

                foreach (FileItem item in desired)
                    if (!current.Contains(item)) selected.Add(item);
            }
            finally { ctx.IsReconciling = false; }

            // Ferry keeps one logical Shift anchor per tab. Mirror that anchor into the newly
            // visible WPF selector so a subsequent native/owned Shift gesture starts from the
            // same place after List <-> Grid switching.
            if (ctx.SelectionAnchorItem != null && target.Items.IndexOf(ctx.SelectionAnchorItem) >= 0)
                SetExtendedSelectionAnchorOnly(target, ctx.SelectionAnchorItem);
        }

        private void EndRubberBandGesture(TabViewContext ctx, bool keepSelection)
        {
            if (ctx == null) return;
            Selector view = ctx.RubberBandView;
            ctx.RubberBandPending = false;
            ctx.RubberBandActive = false;
            ctx.RubberBandView = null;
            ctx.RubberBandStartItem = null;
            ctx.RubberBandBackgroundFocusItem = null;
            ctx.RubberBandModifiers = ModifierKeys.None;
            ctx.RubberBandInitialSelection = null;
            ctx.RubberBandPassThroughPending = false;
            ctx.RubberBandCurrentHitSet = null;
            ctx.RubberBandLastVerticalDirection = 0;
            ctx.RubberBandSelectionAnchor = null;
            ctx.RubberBandCtrlShiftBaseSelection = null;
            ctx.RubberBandCtrlShiftTrueBackgroundMode = false;
            ctx.RubberBandTransitionMode = false;
            ctx.RubberBandCtrlShiftShiftReleasedMode = false;
            ctx.RubberBandCtrlShiftShiftReleasedToggleSet = null;
            ctx.RubberBandAutoScrollAccumulator = 0;
            if (ctx.RubberBandAutoScrollTimer != null) ctx.RubberBandAutoScrollTimer.Stop();
            if (ctx.RubberBandRectangle != null)
            {
                ctx.RubberBandRectangle.Visibility = Visibility.Collapsed;
                ctx.RubberBandRectangle.Width = 0;
                ctx.RubberBandRectangle.Height = 0;
            }
            if (Mouse.Captured == ctx.Container || (view != null && Mouse.Captured == view)) Mouse.Capture(null);
        }

        private void ViewDragOver(ItemsControl view, TabState state, DragEventArgs e)
        {
            TabViewContext ctx = state != null && contexts.ContainsKey(state.Id) ? contexts[state.Id] : null;
            if (state == null || state.IsRecycleBin || !e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if (ctx != null) ClearDropTargetHighlight(ctx);
                e.Effects = DragDropEffects.None; e.Handled = true; return;
            }
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            Point point = e.GetPosition(view);
            string destination = ResolveDropDestination(view, state, point);
            List<string> actionable = GetActionableDropPaths(files, destination);
            if (actionable.Count == 0)
            {
                e.Effects = DragDropEffects.None;
                if (ctx != null) ClearDropTargetHighlight(ctx);
            }
            else
            {
                e.Effects = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control ? DragDropEffects.Copy : (SameDrive(actionable[0], destination) ? DragDropEffects.Move : DragDropEffects.Copy);
                if (ctx != null) UpdateDropTargetHighlight(ctx, view, point);
            }
            e.Handled = true;
        }

        private void ViewDrop(ItemsControl view, TabState state, DragEventArgs e)
        {
            TabViewContext ctx = state != null && contexts.ContainsKey(state.Id) ? contexts[state.Id] : null;
            try
            {
                if (state == null || state.IsRecycleBin) return;
                string[] files = e.Data.GetData(DataFormats.FileDrop) as string[]; if (files == null || files.Length == 0) return;
                string destination = ResolveDropDestination(view, state, e.GetPosition(view));
                List<string> actionable = GetActionableDropPaths(files, destination);
                if (actionable.Count == 0) { e.Effects = DragDropEffects.None; return; }
                bool copy = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control || !SameDrive(actionable[0], destination);
                InvalidateRenameUndo();
                if (copy) ShellFileOperations.Copy(actionable, destination); else ShellFileOperations.Move(actionable, destination);
                ScheduleFolderRefresh(state);
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Ferry", MessageBoxButton.OK, MessageBoxImage.Error); }
            finally
            {
                if (ctx != null) ClearDropTargetHighlight(ctx);
                e.Handled = true;
            }
        }

        private void UpdateDropTargetHighlight(TabViewContext ctx, ItemsControl view, Point point)
        {
            if (ctx == null) return;
            Control container = GetItemContainerUnderPoint(view, point);
            FileItem target = container == null ? null : container.DataContext as FileItem;
            if (target == null || !target.IsDirectory || target.IsRecycleItem)
            {
                ClearDropTargetHighlight(ctx);
                return;
            }
            if (ctx.DropTargetContainer == container) return;
            ClearDropTargetHighlight(ctx);
            ctx.DropTargetContainer = container;
            // RC3: remember LOCAL values, not the currently resolved style value. Restoring
            // a resolved null/brush as a new local value blocks WPF selection Style triggers
            // and was a likely source of post-D&D missing/ghost selection highlights.
            ctx.DropTargetBackgroundLocal = container.ReadLocalValue(Control.BackgroundProperty);
            ctx.DropTargetBorderBrushLocal = container.ReadLocalValue(Control.BorderBrushProperty);
            ctx.DropTargetBorderThicknessLocal = container.ReadLocalValue(Control.BorderThicknessProperty);
            Color highlight = SystemColors.HighlightColor;
            container.SetValue(Control.BackgroundProperty, new SolidColorBrush(Color.FromArgb(52, highlight.R, highlight.G, highlight.B)));
            container.SetValue(Control.BorderBrushProperty, SystemColors.HighlightBrush);
            container.SetValue(Control.BorderThicknessProperty, new Thickness(1));
        }

        private TabViewContext FindContextForItemsControl(ItemsControl view)
        {
            if (view == null) return null;
            foreach (TabViewContext ctx in contexts.Values)
            {
                if (ctx == null) continue;
                if (ReferenceEquals(view, ctx.ListView) || ReferenceEquals(view, ctx.GridView)) return ctx;
            }
            return null;
        }

        private bool IsListGutterColumn(GridViewColumn column)
        {
            if (column == null) return false;
            GridViewColumnHeader header = column.Header as GridViewColumnHeader;
            return header != null && string.Equals(Convert.ToString(header.Tag), ListGutterColumnTag, StringComparison.Ordinal);
        }

        private double GetListGutterWidth(TabViewContext ctx)
        {
            if (ctx == null || ctx.ListGrid == null) return ListTrueBackgroundGutterWidth;
            for (int i = 0; i < ctx.ListGrid.Columns.Count; i++)
            {
                GridViewColumn column = ctx.ListGrid.Columns[i];
                if (!IsListGutterColumn(column)) continue;
                double width = column.ActualWidth > 0 ? column.ActualWidth : column.Width;
                return double.IsNaN(width) || double.IsInfinity(width) ? ListTrueBackgroundGutterWidth : Math.Max(0, width);
            }
            return 0;
        }

        private double GetListDataColumnsWidth(TabViewContext ctx)
        {
            if (ctx == null || ctx.ListGrid == null) return 0;
            double width = 0;
            for (int i = 0; i < ctx.ListGrid.Columns.Count; i++)
            {
                GridViewColumn column = ctx.ListGrid.Columns[i];
                if (IsListGutterColumn(column)) continue;
                double columnWidth = column.ActualWidth > 0 ? column.ActualWidth : column.Width;
                if (!double.IsNaN(columnWidth) && !double.IsInfinity(columnWidth)) width += Math.Max(0, columnWidth);
            }
            return width;
        }

        private bool IsPointInsideListInteractionBounds(ListView view, Control container, Point pointInView)
        {
            if (view == null || container == null) return false;
            TabViewContext ctx = FindContextForItemsControl(view);
            if (ctx == null) return true;

            FrameworkElement element = container as FrameworkElement;
            if (element == null || element.ActualHeight <= 0) return false;

            try
            {
                Point origin = element.TranslatePoint(new Point(0, 0), view);
                double gutter = GetListGutterWidth(ctx);
                double dataWidth = GetListDataColumnsWidth(ctx);
                double available = Math.Max(0, element.ActualWidth - gutter);
                double width = dataWidth > 0 ? Math.Min(dataWidth, available) : available;
                Rect interaction = new Rect(origin.X + gutter, origin.Y, Math.Max(0, width), Math.Max(0, element.ActualHeight));
                return interaction.Contains(pointInView);
            }
            catch
            {
                // If WPF cannot translate the container during a transient layout pass, keep the
                // pre-RC18 behavior rather than making a legitimate item unexpectedly unclickable.
                return true;
            }
        }

        private Control GetItemContainerUnderPoint(ItemsControl view, Point point)
        {
            DependencyObject hit = view.InputHitTest(point) as DependencyObject;
            while (hit != null && hit != view && !(hit is ListViewItem) && !(hit is ListBoxItem)) hit = VisualTreeHelper.GetParent(hit);
            Control container = hit as Control;
            ListView list = view as ListView;
            if (list != null && container is ListViewItem && !IsPointInsideListInteractionBounds(list, container, point))
                return null;
            return container;
        }

        private void ClearDropTargetHighlight(TabViewContext ctx)
        {
            if (ctx == null || ctx.DropTargetContainer == null) return;
            Control container = ctx.DropTargetContainer;
            RestoreLocalValue(container, Control.BackgroundProperty, ctx.DropTargetBackgroundLocal);
            RestoreLocalValue(container, Control.BorderBrushProperty, ctx.DropTargetBorderBrushLocal);
            RestoreLocalValue(container, Control.BorderThicknessProperty, ctx.DropTargetBorderThicknessLocal);
            container.InvalidateProperty(Control.BackgroundProperty);
            container.InvalidateVisual();
            ctx.DropTargetContainer = null;
            ctx.DropTargetBackgroundLocal = DependencyProperty.UnsetValue;
            ctx.DropTargetBorderBrushLocal = DependencyProperty.UnsetValue;
            ctx.DropTargetBorderThicknessLocal = DependencyProperty.UnsetValue;
        }

        private void RestoreLocalValue(DependencyObject target, DependencyProperty property, object localValue)
        {
            if (target == null || property == null) return;
            if (localValue == null || localValue == DependencyProperty.UnsetValue) target.ClearValue(property);
            else target.SetValue(property, localValue);
        }

        private string ResolveDropDestination(ItemsControl view, TabState state, Point point)
        {
            string destination = state.CurrentPath;
            FileItem target = GetItemUnderPoint(view, point);
            if (target != null && target.IsDirectory && !target.IsRecycleItem) destination = target.FullPath;
            return destination;
        }

        private List<string> GetActionableDropPaths(string[] files, string destination)
        {
            List<string> result = new List<string>();
            if (files == null || string.IsNullOrEmpty(destination)) return result;
            for (int i = 0; i < files.Length; i++)
            {
                string source = files[i]; if (string.IsNullOrEmpty(source)) continue;
                if (SamePath(source, destination)) continue;
                string parent = null;
                try { parent = Path.GetDirectoryName(source.TrimEnd('\\', '/')); } catch { }
                // A drop back into the item's existing parent is a no-op even when Ctrl is held.
                if (!string.IsNullOrEmpty(parent) && SamePath(parent, destination)) continue;
                result.Add(source);
            }
            return result;
        }

        private bool SamePath(string a, string b)
        {
            try { return string.Equals(Path.GetFullPath(a).TrimEnd('\\'), Path.GetFullPath(b).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase); } catch { return string.Equals(a, b, StringComparison.OrdinalIgnoreCase); }
        }

        private FileItem GetItemUnderPoint(ItemsControl view, Point point)
        {
            FrameworkElement fe = GetItemContainerUnderPoint(view, point) as FrameworkElement;
            return fe == null ? null : fe.DataContext as FileItem;
        }

        private bool SameDrive(string a, string b) { try { return string.Equals(Path.GetPathRoot(a), Path.GetPathRoot(b), StringComparison.OrdinalIgnoreCase); } catch { return false; } }

        private void TabsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source != tabs) return;
            if (searchDebounceTimer != null) searchDebounceTimer.Stop();
            TabViewContext now = ActiveContext;
            if (lastSelectedContext != null && lastSelectedContext != now) { CaptureColumnSettings(lastSelectedContext); CancelGridThumbnailLoad(lastSelectedContext); }
            if (now != null && now != lastSelectedContext) BuildListColumns(now, now.State.IsSearching);
            lastSelectedContext = now;
            if (now != null && string.Equals(currentViewMode, "Grid", StringComparison.OrdinalIgnoreCase)) StartGridThumbnailLoad(now);
            UpdateToolbarForActive(); UpdateStatus();
        }

        private TabState ActiveState
        {
            get { TabItem item = tabs == null ? null : tabs.SelectedItem as TabItem; return item == null ? null : item.Tag as TabState; }
        }

        private TabViewContext ActiveContext
        {
            get { TabState state = ActiveState; if (state == null || !contexts.ContainsKey(state.Id)) return null; return contexts[state.Id]; }
        }

        private void Navigate(string path, bool addHistory)
        {
            TabState state = ActiveState; if (state == null) { OpenNewTab(path, true); return; } LoadFolder(state, path, addHistory);
        }

        private void LoadFolder(TabState state, string path, bool addHistory)
        {
            if (state == null) return;
            if (TabState.IsRecycleBinPath(path)) { LoadRecycleBin(state, addHistory); return; }
            if (!Directory.Exists(path)) return;
            TabViewContext ctx = contexts[state.Id];
            ctx.PreSearchItems = null;
            ctx.PreSearchSelection = null;
            if (ctx == ActiveContext) CaptureColumnSettings(ctx);
            StopSearchDrain(ctx);
            state.CancelBackgroundWork();
            if (addHistory && !string.Equals(state.CurrentPath, path, StringComparison.OrdinalIgnoreCase)) { state.BackHistory.Add(state.CurrentPath); state.ForwardHistory.Clear(); }
            state.CurrentPath = Path.GetFullPath(path); state.Title = TabState.BuildTitle(state.CurrentPath); state.IsSearching = false; state.SearchText = string.Empty; state.ClearUnsortedTail(); SetTabHeader(ctx);
            CancellationTokenSource cts = new CancellationTokenSource(); state.LoadCancellation = cts; CancellationToken token = cts.Token;
            ResetItemsForView(ctx); BuildListColumns(ctx, false); ShowCurrentView(ctx); SetupWatcher(ctx); UpdateToolbarForActive(); SetStatusMessage("Loading…");
            string expected = state.CurrentPath;

            Task.Run(delegate
            {
                List<FileItem> batch = new List<FileItem>(); int total = 0;
                try
                {
                    foreach (string entry in Directory.EnumerateFileSystemEntries(expected))
                    {
                        token.ThrowIfCancellationRequested(); FileItem item = CreateBasicItem(entry, null); if (item == null) continue; batch.Add(item); total++;
                        if (batch.Count >= 64) { PostBatch(state, expected, batch, token); batch = new List<FileItem>(); }
                    }
                    if (batch.Count > 0) PostBatch(state, expected, batch, token);
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        if (token.IsCancellationRequested || state.CurrentPath != expected) return;
                        ApplySort(state, false); StartDeferredMetadata(state, expected, token, true); UpdateStatus();
                    }));
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { Dispatcher.BeginInvoke(new Action(delegate { if (!token.IsCancellationRequested) SetStatusMessage(ex.Message); })); }
            }, token);
        }

        private void SafeRefreshFolderIncremental(TabState state, bool explicitSort)
        {
            try { RefreshFolderIncremental(state, explicitSort); }
            catch (Exception ex)
            {
                Logger.Write("Refresh start failed: " + ex);
                try { SetStatusMessage("Refresh failed. Press F5 to retry."); } catch { }
            }
        }

        private void ScheduleFolderRefresh(TabState state)
        {
            if (state == null || state.IsRecycleBin || state.IsSearching || !contexts.ContainsKey(state.Id)) return;
            TabViewContext ctx = contexts[state.Id];
            try
            {
                ctx.RefreshTimer.Stop();
                ctx.RefreshTimer.Start();
            }
            catch (Exception ex) { Logger.Write("Refresh scheduling failed: " + ex); }
        }

        private void RefreshFolderIncremental(TabState state, bool explicitSort)
        {
            if (state == null || state.IsRecycleBin || state.IsSearching || !Directory.Exists(state.CurrentPath)) return;
            string expected = state.CurrentPath;
            int generation = Interlocked.Increment(ref state.RefreshGeneration);
            try { if (state.LoadCancellation != null) state.LoadCancellation.Cancel(); } catch { }
            CancellationTokenSource cts = new CancellationTokenSource(); state.LoadCancellation = cts; CancellationToken token = cts.Token;

            Task.Run(delegate
            {
                try
                {
                    List<FileItem> fresh = new List<FileItem>();
                    foreach (string entry in Directory.EnumerateFileSystemEntries(expected))
                    {
                        token.ThrowIfCancellationRequested();
                        FileItem item = CreateBasicItem(entry, null);
                        if (item != null) fresh.Add(item);
                    }
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        if (token.IsCancellationRequested || generation != state.RefreshGeneration || state.IsSearching || state.IsRecycleBin || !string.Equals(state.CurrentPath, expected, StringComparison.OrdinalIgnoreCase)) return;
                        try
                        {
                            ReconcileFolderItems(state, fresh, !explicitSort);
                            if (explicitSort) ApplySort(state, false);
                            StartDeferredMetadata(state, expected, token, explicitSort);
                            UpdateStatus();
                        }
                        catch (Exception ex)
                        {
                            Logger.Write("Refresh UI reconciliation failed: " + ex);
                            try { SetStatusMessage("Refresh failed. Press F5 to retry."); } catch { }
                        }
                    }));
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Logger.Write("Refresh enumeration failed: " + ex);
                    try { Dispatcher.BeginInvoke(new Action(delegate { if (!token.IsCancellationRequested && generation == state.RefreshGeneration) SetStatusMessage(ex.Message); })); } catch { }
                }
            }, token);
        }

        private void ReconcileFolderItems(TabState state, List<FileItem> fresh, bool appendNewItemsToTail)
        {
            if (state == null || fresh == null) return;
            Dictionary<string, FileItem> incoming = new Dictionary<string, FileItem>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < fresh.Count; i++)
            {
                FileItem item = fresh[i];
                if (item != null && !string.IsNullOrEmpty(item.FullPath)) incoming[item.FullPath] = item;
            }

            Dictionary<string, FileItem> existing = new Dictionary<string, FileItem>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < state.Items.Count; i++)
            {
                FileItem item = state.Items[i];
                if (item != null && !string.IsNullOrEmpty(item.FullPath)) existing[item.FullPath] = item;
            }

            // Do not mutate the ObservableCollection inside CollectionView.DeferRefresh().
            // With WPF selection/virtualization this can throw during paste/delete refreshes.
            // Suppress SelectionChanged status enumeration while the view is applying source changes.
            TabViewContext reconcileContext = contexts.ContainsKey(state.Id) ? contexts[state.Id] : null;
            if (reconcileContext != null) reconcileContext.IsReconciling = true;
            try
            {
                for (int i = state.Items.Count - 1; i >= 0; i--)
                {
                    FileItem current = state.Items[i];
                    if (current == null || !incoming.ContainsKey(current.FullPath))
                    {
                        if (current != null) state.RemoveUnsortedTail(current.FullPath);
                        state.Items.RemoveAt(i);
                    }
                }

                for (int i = 0; i < fresh.Count; i++)
                {
                    FileItem item = fresh[i];
                    FileItem old;
                    if (!existing.TryGetValue(item.FullPath, out old))
                    {
                        if (appendNewItemsToTail) state.MarkUnsortedTail(item.FullPath);
                        state.Items.Add(item);
                        continue;
                    }
                    if (BasicItemChanged(old, item))
                    {
                        // RC5: retain the existing FileItem identity while a file is changing
                        // (downloads, copies, writes). Replacing the object breaks WPF selection,
                        // focus and D&D on frequently-updated files such as .crdownload.
                        old.UpdateBasicFrom(item);
                    }
                }
            }
            finally
            {
                if (reconcileContext != null) reconcileContext.IsReconciling = false;
            }

            if (reconcileContext != null) ApplyPasteFeedbackSelection(state, reconcileContext);
        }

        private bool BasicItemChanged(FileItem oldItem, FileItem newItem)
        {
            if (oldItem == null || newItem == null) return true;
            if (!string.Equals(oldItem.Name, newItem.Name, StringComparison.Ordinal)) return true;
            if (oldItem.IsDirectory != newItem.IsDirectory) return true;
            if (oldItem.SizeBytes != newItem.SizeBytes) return true;
            if (oldItem.Modified != newItem.Modified) return true;
            if (oldItem.Created != newItem.Created) return true;
            if (oldItem.IsHidden != newItem.IsHidden) return true;
            return false;
        }

        private void LoadRecycleBin(TabState state, bool addHistory)
        {
            if (state == null) return;
            TabViewContext ctx = contexts[state.Id];
            if (ctx == ActiveContext) CaptureColumnSettings(ctx);
            StopSearchDrain(ctx);
            state.CancelBackgroundWork();
            if (addHistory && !state.IsRecycleBin) { state.BackHistory.Add(state.CurrentPath); state.ForwardHistory.Clear(); }
            state.CurrentPath = TabState.RecycleBinPath; state.Title = "Recycle Bin"; state.IsSearching = false; state.SearchText = string.Empty; state.ClearUnsortedTail(); SetTabHeader(ctx);
            CancellationTokenSource cts = new CancellationTokenSource(); state.LoadCancellation = cts; CancellationToken token = cts.Token;
            ResetItemsForView(ctx); BuildListColumns(ctx, false); ShowCurrentView(ctx); SetupWatcher(ctx); UpdateToolbarForActive(); SetStatusMessage("Loading Recycle Bin…");

            Task.Run(delegate
            {
                try
                {
                    IList<RecycleBinEntry> entries = RecycleBinService.EnumerateCurrentUser();
                    List<FileItem> items = new List<FileItem>();
                    for (int i = 0; i < entries.Count; i++)
                    {
                        token.ThrowIfCancellationRequested();
                        RecycleBinEntry entry = entries[i];
                        FileItem item = new FileItem();
                        item.FullPath = entry.RecycledPath;
                        item.Name = entry.Name;
                        item.IsDirectory = entry.IsDirectory;
                        item.IsRecycleItem = true;
                        item.OriginalPath = entry.OriginalPath;
                        item.RecycleMetadataPath = entry.MetadataPath;
                        item.RelativeLocation = SafeParentPath(entry.OriginalPath);
                        item.SizeBytes = entry.IsDirectory ? (long?)null : entry.SizeBytes;
                        item.Modified = entry.DeletedAt;
                        item.Created = entry.DeletedAt;
                        item.ItemCountText = entry.IsDirectory ? "—" : null;
                        item.TypeName = ShellInterop.GetTypeName(entry.OriginalPath, entry.IsDirectory);
                        try
                        {
                            item.ListIcon = ShellInterop.GetSmallTypeIcon(entry.RecycledPath, entry.IsDirectory);
                            if (string.Equals(currentViewMode, "Grid", StringComparison.OrdinalIgnoreCase)) item.Icon = ShellInterop.GetThumbnailOrIcon(entry.RecycledPath, (int)settings.GridIconSize);
                        }
                        catch { }
                        items.Add(item);
                    }
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        if (token.IsCancellationRequested || !state.IsRecycleBin) return;
                        for (int i = 0; i < items.Count; i++) state.Items.Add(items[i]);
                        ApplySort(state, true); UpdateStatus();
                    }));
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { Dispatcher.BeginInvoke(new Action(delegate { if (!token.IsCancellationRequested) SetStatusMessage(ex.Message); })); }
            }, token);
        }

        private string SafeParentPath(string path)
        {
            try { string parent = Path.GetDirectoryName(path); return string.IsNullOrEmpty(parent) ? string.Empty : parent; }
            catch { return string.Empty; }
        }

        private void ResetItemsForView(TabViewContext ctx)
        {
            if (ctx == null || ctx.State == null) return;
            CancelGridThumbnailLoad(ctx);
            // Detach the controls first so clearing a large search result set does not force
            // WPF to synchronously recycle thousands of item containers on the UI thread.
            ctx.ListView.ItemsSource = null;
            ctx.GridView.ItemsSource = null;
            ctx.State.Items.Clear();
            ctx.ListView.ItemsSource = ctx.State.Items;
            ctx.GridView.ItemsSource = ctx.State.Items;
        }

        private void PostBatch(TabState state, string expected, List<FileItem> batch, CancellationToken token)
        {
            FileItem[] copy = batch.ToArray(); Dispatcher.BeginInvoke(new Action(delegate { if (token.IsCancellationRequested || state.CurrentPath != expected || state.IsSearching) return; for (int i = 0; i < copy.Length; i++) state.Items.Add(copy[i]); UpdateStatus(); }));
        }

        private FileItem CreateBasicItem(string path, string searchRoot)
        {
            try
            {
                FileAttributes attrs = File.GetAttributes(path); if ((attrs & FileAttributes.System) != 0) return null; if (!settings.ShowHidden && (attrs & FileAttributes.Hidden) != 0) return null;
                bool dir = (attrs & FileAttributes.Directory) != 0; FileSystemInfo fsi = dir ? (FileSystemInfo)new DirectoryInfo(path) : new FileInfo(path);
                FileItem item = new FileItem(); item.FullPath = path; item.Name = fsi.Name; item.IsDirectory = dir; item.IsHidden = (attrs & FileAttributes.Hidden) != 0; item.Modified = fsi.LastWriteTime; item.Created = fsi.CreationTime; item.SizeBytes = dir ? (long?)null : ((FileInfo)fsi).Length; item.ItemCountText = dir ? "…" : null;
                item.TypeName = dir ? "File folder" : (string.IsNullOrEmpty(Path.GetExtension(path)) ? "File" : Path.GetExtension(path).TrimStart('.').ToUpperInvariant() + " file");
                // List view must have a cheap Shell icon as soon as the row appears.  Deferred
                // metadata may refine it later, but a transient Shell failure must not leave the
                // row blank until F5.
                try { item.ListIcon = ShellInterop.GetSmallTypeIcon(path, dir); } catch { }
                if (!string.IsNullOrEmpty(searchRoot)) { string parent = Path.GetDirectoryName(path); if (parent != null && parent.StartsWith(searchRoot, StringComparison.OrdinalIgnoreCase)) { string rel = parent.Substring(searchRoot.Length).TrimStart('\\'); item.RelativeLocation = string.IsNullOrEmpty(rel) ? "." : rel; } }
                return item;
            }
            catch { return null; }
        }

        private void StartDeferredMetadata(TabState state, string expectedPath, CancellationToken token, bool sortWhenComplete)
        {
            bool loadGridThumbnails = string.Equals(currentViewMode, "Grid", StringComparison.OrdinalIgnoreCase);
            FileItem[] items = state.Items.ToArray(); Queue<FileItem> queue = new Queue<FileItem>(items); object gate = new object(); int workers = Math.Min(4, Math.Max(1, Environment.ProcessorCount / 2)); Task[] tasks = new Task[workers];
            for (int w = 0; w < workers; w++)
            {
                tasks[w] = Task.Run(delegate
                {
                    while (true)
                    {
                        token.ThrowIfCancellationRequested(); FileItem item;
                        lock (gate) { if (queue.Count == 0) break; item = queue.Dequeue(); }
                        string type = item.TypeName; ImageSource listIcon = null; ImageSource thumbnail = null; string countText = item.ItemCountText; int? count = null;
                        try
                        {
                            type = ShellInterop.GetTypeNameFast(item.FullPath, item.IsDirectory);
                            listIcon = item.ListIcon ?? ShellInterop.GetSmallTypeIcon(item.FullPath, item.IsDirectory);
                            if (loadGridThumbnails) thumbnail = ShellInterop.GetThumbnailOrIcon(item.FullPath, (int)settings.GridIconSize);
                            if (item.IsDirectory) { count = CountVisibleChildren(item.FullPath); countText = count.HasValue ? count.Value.ToString("N0") + " items" : "—"; }
                        }
                        catch { if (item.IsDirectory) countText = "—"; }
                        Dispatcher.BeginInvoke(new Action(delegate { if (token.IsCancellationRequested || state.CurrentPath != expectedPath || state.IsSearching) return; item.TypeName = type; if (listIcon != null) item.ListIcon = listIcon; if (thumbnail != null) item.Icon = thumbnail; item.ItemCount = count; item.ItemCountText = countText; }));
                    }
                }, token);
            }
            Task.WhenAll(tasks).ContinueWith(delegate
            {
                try
                {
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        if (token.IsCancellationRequested || state.CurrentPath != expectedPath || state.IsSearching) return;
                        try { if (sortWhenComplete) ApplySort(state, true); UpdateStatus(); }
                        catch (Exception ex) { Logger.Write("Deferred metadata completion failed: " + ex); }
                    }));
                }
                catch { }
            });
        }

        private int? CountVisibleChildren(string path)
        {
            try
            {
                int count = 0; foreach (string child in Directory.EnumerateFileSystemEntries(path)) { try { FileAttributes attrs = File.GetAttributes(child); if ((attrs & FileAttributes.System) != 0) continue; if (!settings.ShowHidden && (attrs & FileAttributes.Hidden) != 0) continue; count++; } catch { } } return count;
            }
            catch { return null; }
        }

        private void ApplySort(TabState state, bool refresh)
        {
            ApplySort(state, refresh, true);
        }

        private void ApplySort(TabState state, bool refresh, bool commitUnsortedTail)
        {
            if (state == null) return; ICollectionView view = CollectionViewSource.GetDefaultView(state.Items); ListCollectionView list = view as ListCollectionView; if (list == null) return;
            if (!state.IsSearching && commitUnsortedTail) state.ClearUnsortedTail();
            string key = state.IsSearching && settings.SortKey == "Items" ? "Name" : settings.SortKey;
            list.CustomSort = new FileItemComparer(key, settings.SortDescending, settings.SortFoldersFirst, state, !state.IsSearching); list.Refresh();
        }

        private void SetupWatcher(TabViewContext ctx)
        {
            if (ctx.Watcher != null) { try { ctx.Watcher.EnableRaisingEvents = false; ctx.Watcher.Dispose(); } catch { } ctx.Watcher = null; }
            if (ctx == null || ctx.State == null || ctx.State.IsRecycleBin) return;
            try
            {
                FileSystemWatcher watcher = new FileSystemWatcher(ctx.State.CurrentPath); watcher.IncludeSubdirectories = false; watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size;
                FileSystemEventHandler changed = delegate { try { Dispatcher.BeginInvoke(new Action(delegate { ScheduleFolderRefresh(ctx.State); })); } catch { } };
                RenamedEventHandler renamed = delegate { try { Dispatcher.BeginInvoke(new Action(delegate { ScheduleFolderRefresh(ctx.State); })); } catch { } };
                watcher.Created += changed; watcher.Deleted += changed; watcher.Changed += changed; watcher.Renamed += renamed; watcher.EnableRaisingEvents = true; ctx.Watcher = watcher;
            }
            catch { }
        }

        private void StartSearch()
        {
            TabState state = ActiveState; if (state == null || state.IsRecycleBin) return; string query = searchBox.Text.Trim(); if (query.Length == 0) { ClearSearchText(); return; }
            TabViewContext ctx = contexts[state.Id];
            CaptureColumnSettings(ctx);
            StopSearchDrain(ctx);
            if (!state.IsSearching)
            {
                ctx.PreSearchItems = new List<FileItem>(state.Items);
                ctx.PreSearchSelection = new HashSet<string>(GetSelectedPaths(state), StringComparer.OrdinalIgnoreCase);
            }
            state.CancelBackgroundWork(); state.IsSearching = true; state.SearchText = query; ResetItemsForView(ctx); BuildListColumns(ctx, true); ShowCurrentView(ctx); SetStatusMessage("Searching…");
            CancellationTokenSource cts = new CancellationTokenSource(); state.SearchCancellation = cts; CancellationToken token = cts.Token; string root = state.CurrentPath; ConcurrentQueue<FileItem> pending = new ConcurrentQueue<FileItem>();
            DispatcherTimer drain = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(70) };
            ctx.SearchDrainTimer = drain;
            drain.Tick += delegate
            {
                if (token.IsCancellationRequested || !state.IsSearching || !string.Equals(state.SearchText, query, StringComparison.Ordinal)) { drain.Stop(); if (ctx.SearchDrainTimer == drain) ctx.SearchDrainTimer = null; return; }
                int n = 0; FileItem item; while (n < 100 && pending.TryDequeue(out item)) { state.Items.Add(item); n++; } if (n > 0) { ApplySort(state, !string.Equals(settings.SortKey, "Items", StringComparison.OrdinalIgnoreCase)); UpdateStatus(); }
            };
            drain.Start();
            SearchRequest request = new SearchRequest { RootPath = root, Query = query, Mode = Convert.ToString(searchModeBox.SelectedItem), ShowHidden = settings.ShowHidden };
            SearchService.SearchAsync(request, delegate(string path) { if (token.IsCancellationRequested) return; FileItem item = CreateBasicItem(path, root); if (item != null) pending.Enqueue(item); }, token).ContinueWith(delegate
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (token.IsCancellationRequested || !state.IsSearching || state.SearchText != query) { drain.Stop(); if (ctx.SearchDrainTimer == drain) ctx.SearchDrainTimer = null; return; }
                    FileItem last; while (pending.TryDequeue(out last)) state.Items.Add(last); drain.Stop(); if (ctx.SearchDrainTimer == drain) ctx.SearchDrainTimer = null; ApplySort(state, false); StartDeferredSearchMetadata(state, root, token); UpdateStatus();
                }));
            });
        }

        private void StartDeferredSearchMetadata(TabState state, string root, CancellationToken token)
        {
            bool loadGridThumbnails = string.Equals(currentViewMode, "Grid", StringComparison.OrdinalIgnoreCase);
            FileItem[] items = state.Items.ToArray(); Queue<FileItem> queue = new Queue<FileItem>(items); object gate = new object(); Task[] tasks = new Task[Math.Min(4, Math.Max(1, Environment.ProcessorCount / 2))];
            for (int i = 0; i < tasks.Length; i++) tasks[i] = Task.Run(delegate
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested(); FileItem item; lock (gate) { if (queue.Count == 0) break; item = queue.Dequeue(); }
                    try
                    {
                        string type = ShellInterop.GetTypeNameFast(item.FullPath, item.IsDirectory);
                        ImageSource listIcon = item.ListIcon ?? ShellInterop.GetSmallTypeIcon(item.FullPath, item.IsDirectory);
                        ImageSource thumbnail = loadGridThumbnails ? ShellInterop.GetThumbnailOrIcon(item.FullPath, (int)settings.GridIconSize) : null;
                        int? count = item.IsDirectory ? CountVisibleChildren(item.FullPath) : (int?)null;
                        Dispatcher.BeginInvoke(new Action(delegate { if (!token.IsCancellationRequested && state.IsSearching) { item.TypeName = type; if (listIcon != null) item.ListIcon = listIcon; if (thumbnail != null) item.Icon = thumbnail; if (item.IsDirectory) { item.ItemCount = count; item.ItemCountText = count.HasValue ? count.Value.ToString("N0") + " items" : "—"; } } }));
                    }
                    catch { }
                }
            }, token);
            Task.WhenAll(tasks).ContinueWith(delegate { Dispatcher.BeginInvoke(new Action(delegate { if (!token.IsCancellationRequested && state.IsSearching) { ApplySort(state, true); UpdateStatus(); } })); });
        }

        private void SearchBoxChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSearchClearButton();
            if (suppressSearchTextEvent) return;
            if (searchDebounceTimer != null) searchDebounceTimer.Stop();
            TabState state = ActiveState;
            string query = searchBox.Text.Trim();
            if (query.Length == 0)
            {
                if (state != null && state.IsSearching) ExitSearch();
                else if (state != null) state.SearchText = string.Empty;
                return;
            }
            if (state != null) state.SearchText = query;
            if (searchDebounceTimer != null) searchDebounceTimer.Start(); else StartSearch();
        }

        private void UpdateSearchClearButton()
        {
            if (searchClearButton != null) searchClearButton.Visibility = searchBox != null && searchBox.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ClearSearchText()
        {
            if (searchDebounceTimer != null) searchDebounceTimer.Stop();
            TabState state = ActiveState;
            if (state != null && state.IsSearching)
            {
                ExitSearch();
                return;
            }
            suppressSearchTextEvent = true;
            if (searchBox != null) searchBox.Text = string.Empty;
            suppressSearchTextEvent = false;
            if (state != null) state.SearchText = string.Empty;
            UpdateSearchClearButton();
        }

        private void StopSearchDrain(TabViewContext ctx)
        {
            if (ctx == null || ctx.SearchDrainTimer == null) return;
            try { ctx.SearchDrainTimer.Stop(); } catch { }
            ctx.SearchDrainTimer = null;
        }

        private void AddSelection(Selector selector, FileItem item)
        {
            if (item == null || selector == null) return;
            ListView lv = selector as ListView;
            if (lv != null)
            {
                if (!lv.SelectedItems.Contains(item)) lv.SelectedItems.Add(item);
                return;
            }
            ListBox lb = selector as ListBox;
            if (lb != null && !lb.SelectedItems.Contains(item)) lb.SelectedItems.Add(item);
        }

        private void ExitSearch()
        {
            TabState state = ActiveState; if (state == null || !state.IsSearching) { ClearSearchText(); return; }
            TabViewContext ctx = contexts[state.Id];
            if (searchDebounceTimer != null) searchDebounceTimer.Stop();
            StopSearchDrain(ctx);
            state.CancelBackgroundWork();

            suppressSearchTextEvent = true;
            searchBox.Text = string.Empty;
            suppressSearchTextEvent = false;
            state.SearchText = string.Empty;
            state.IsSearching = false;
            UpdateSearchClearButton();

            // Restore the pre-search folder snapshot synchronously. Controls are detached while the
            // ObservableCollection is rebuilt, avoiding the multi-second container-recycling pause
            // that occurred when an empty query forced a full filesystem reload.
            if (ctx.PreSearchItems != null)
            {
                ctx.ListView.ItemsSource = null;
                ctx.GridView.ItemsSource = null;
                state.Items.Clear();
                for (int i = 0; i < ctx.PreSearchItems.Count; i++) state.Items.Add(ctx.PreSearchItems[i]);
                ctx.ListView.ItemsSource = state.Items;
                ctx.GridView.ItemsSource = state.Items;
                BuildListColumns(ctx, false);
                ShowCurrentView(ctx);
                ApplySort(state, true, false);

                if (ctx.PreSearchSelection != null && ctx.PreSearchSelection.Count > 0)
                {
                    Selector selector = currentViewMode == "Grid" ? (Selector)ctx.GridView : (Selector)ctx.ListView;
                    foreach (object obj in selector.Items)
                    {
                        FileItem item = obj as FileItem;
                        if (item != null && ctx.PreSearchSelection.Contains(item.FullPath))
                            AddSelection(selector, item);
                    }
                }
            }
            else
            {
                BuildListColumns(ctx, false);
                ShowCurrentView(ctx);
            }

            ctx.PreSearchItems = null;
            ctx.PreSearchSelection = null;
            UpdateStatus();

            // Reconcile any folder changes that happened while Search was active, without delaying
            // the immediate visual return to the normal folder view.
            ScheduleFolderRefresh(state);
        }

        private void UpdateToolbarForActive()
        {
            TabState state = ActiveState; backButton.IsEnabled = state != null && state.BackHistory.Count > 0; forwardButton.IsEnabled = state != null && state.ForwardHistory.Count > 0; BuildBreadcrumb(state == null ? null : state.CurrentPath);
            bool searchable = state != null && !state.IsRecycleBin;
            searchBox.IsEnabled = searchable; searchModeBox.IsEnabled = searchable;
            if (state != null) { suppressSearchTextEvent = true; searchBox.Text = state.IsSearching ? state.SearchText : string.Empty; suppressSearchTextEvent = false; UpdateSearchClearButton(); }
        }

        private void BuildBreadcrumb(string path)
        {
            breadcrumbPanel.Children.Clear(); if (string.IsNullOrEmpty(path)) return;
            if (TabState.IsRecycleBinPath(path)) { AddCrumb("Recycle Bin", TabState.RecycleBinPath); ScrollBreadcrumbToCurrent(); return; }
            try
            {
                string root = Path.GetPathRoot(path); string current = root; AddCrumb(root.TrimEnd('\\') + "\\", root);
                string remaining = path.Substring(root.Length).Trim('\\'); if (remaining.Length == 0) { ScrollBreadcrumbToCurrent(); return; } string[] parts = remaining.Split('\\');
                for (int i = 0; i < parts.Length; i++) { breadcrumbPanel.Children.Add(new TextBlock { Text = "  ›  ", VerticalAlignment = VerticalAlignment.Center }); current = Path.Combine(current, parts[i]); AddCrumb(parts[i], current); }
            }
            catch { AddCrumb(path, path); }
            ScrollBreadcrumbToCurrent();
        }

        private void ScrollBreadcrumbToCurrent()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(delegate
            {
                ScrollViewer scroll = breadcrumbPanel == null ? null : breadcrumbPanel.Parent as ScrollViewer;
                if (scroll != null) scroll.ScrollToRightEnd();
            }));
        }

        private void AddCrumb(string label, string path)
        {
            Button b = new Button { Content = label, Tag = path, Padding = new Thickness(5, 2, 5, 2), BorderThickness = new Thickness(0), Background = Brushes.Transparent }; b.Click += delegate { Navigate((string)b.Tag, true); }; breadcrumbPanel.Children.Add(b);
        }

        private bool IsLocationBoxVisible()
        {
            return locationBox != null && locationBox.Visibility == Visibility.Visible;
        }

        private void HideLocationBox(bool restorePreviousFocus)
        {
            if (locationBox != null) locationBox.Visibility = Visibility.Hidden;
            FrameworkElement parent = breadcrumbPanel == null ? null : breadcrumbPanel.Parent as FrameworkElement;
            if (parent != null) parent.Visibility = Visibility.Visible;

            IInputElement returnFocus = locationReturnFocus;
            locationReturnFocus = null;
            if (!restorePreviousFocus) return;

            // Ctrl+L temporarily moves keyboard focus into the address editor.  Restore the exact
            // element that owned focus before entering it (normally the current ListBoxItem), so
            // Up/Down navigation continues from the same item instead of from the Selector itself.
            try
            {
                if (returnFocus != null && Keyboard.Focus(returnFocus) != null) return;
            }
            catch
            {
                // The previously focused element can disappear after a tab/view change.
                // Fall back to the active selector below.
            }

            TabViewContext ctx = ActiveContext;
            if (ctx != null)
            {
                if (string.Equals(currentViewMode, "Grid", StringComparison.OrdinalIgnoreCase)) ctx.GridView.Focus();
                else ctx.ListView.Focus();
            }
        }

        private void LocationBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string path = Environment.ExpandEnvironmentVariables(locationBox.Text);
                if (Directory.Exists(path))
                {
                    HideLocationBox(false);
                    Navigate(path, true);
                }
                else
                {
                    MessageBox.Show(this, "Folder not found.\n\n" + path, "Ferry", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                HideLocationBox(true);
                e.Handled = true;
            }
        }

        private void ShowLocationBox()
        {
            TabState state = ActiveState;
            if (state == null || state.IsRecycleBin) return;
            locationReturnFocus = Keyboard.FocusedElement;
            FrameworkElement parent = breadcrumbPanel.Parent as FrameworkElement;
            if (parent != null) parent.Visibility = Visibility.Collapsed;
            locationBox.Text = state.CurrentPath;
            locationBox.Visibility = Visibility.Visible;
            locationBox.Focus();
            locationBox.SelectAll();
        }

        private void ToggleLocationBox()
        {
            if (IsLocationBoxVisible()) HideLocationBox(true);
            else ShowLocationBox();
        }

        private void GoBack()
        {
            TabState state = ActiveState; if (state == null || state.BackHistory.Count == 0) return; string dest = state.BackHistory[state.BackHistory.Count - 1]; state.BackHistory.RemoveAt(state.BackHistory.Count - 1); state.ForwardHistory.Add(state.CurrentPath); LoadFolder(state, dest, false);
        }
        private void GoForward()
        {
            TabState state = ActiveState; if (state == null || state.ForwardHistory.Count == 0) return; string dest = state.ForwardHistory[state.ForwardHistory.Count - 1]; state.ForwardHistory.RemoveAt(state.ForwardHistory.Count - 1); state.BackHistory.Add(state.CurrentPath); LoadFolder(state, dest, false);
        }
        private void GoUp() { TabState state = ActiveState; if (state == null || state.IsRecycleBin) return; try { DirectoryInfo p = Directory.GetParent(state.CurrentPath); if (p != null) Navigate(p.FullName, true); } catch { } }

        private void OpenSelected(TabState state)
        {
            List<FileItem> items = GetSelectedItemsInViewOrder(state);
            if (items.Count == 0) return;
            if (items.Count == 1)
            {
                OpenItem(items[0], true);
                return;
            }

            // Multi-open: every selected file is opened. If the focused/current selected
            // item is a folder, that folder replaces the current Ferry tab. Other selected
            // folders open in additional Ferry tabs without stealing focus from that tab.
            FileItem focused = GetFocusedSelectedItem(state, items);
            string focusedDirectory = null;
            if (focused != null) TryGetNavigableDirectory(focused, out focusedDirectory);

            if (!string.IsNullOrEmpty(focusedDirectory))
                LoadFolder(state, focusedDirectory, true);

            for (int i = 0; i < items.Count; i++)
            {
                FileItem item = items[i];
                if (item == null || item.IsRecycleItem) continue;
                string directory;
                if (TryGetNavigableDirectory(item, out directory))
                {
                    if (focused != null && string.Equals(item.FullPath, focused.FullPath, StringComparison.OrdinalIgnoreCase))
                        continue;
                    OpenNewTab(directory, false);
                }
                else
                {
                    OpenFileItem(item);
                }
            }
        }

        private void OpenItem(FileItem item, bool useCurrentTabForDirectory)
        {
            if (item == null || item.IsRecycleItem) return;
            string directory;
            if (TryGetNavigableDirectory(item, out directory))
            {
                if (useCurrentTabForDirectory) Navigate(directory, true);
                else OpenNewTab(directory, true);
                return;
            }
            OpenFileItem(item);
        }

        private bool TryGetNavigableDirectory(FileItem item, out string directory)
        {
            directory = null;
            if (item == null) return false;
            if (item.IsDirectory)
            {
                directory = item.FullPath;
                return true;
            }
            if (!string.Equals(Path.GetExtension(item.FullPath), ".lnk", StringComparison.OrdinalIgnoreCase)) return false;
            string target = ShortcutHelper.ResolveTarget(item.FullPath);
            if (!string.IsNullOrEmpty(target) && Directory.Exists(target))
            {
                directory = target;
                return true;
            }
            return false;
        }

        private void OpenFileItem(FileItem item)
        {
            if (item == null || item.IsRecycleItem) return;
            if (string.Equals(Path.GetExtension(item.FullPath), ".lnk", StringComparison.OrdinalIgnoreCase))
            {
                string target = ShortcutHelper.ResolveTarget(item.FullPath);
                if (!string.IsNullOrEmpty(target) && File.Exists(target))
                {
                    ShellInterop.OpenPath(target);
                    return;
                }
            }
            ShellInterop.OpenPath(item.FullPath);
        }

        private FileItem GetFocusedSelectedItem(TabState state, List<FileItem> selectedItems)
        {
            if (state == null || selectedItems == null || selectedItems.Count == 0) return null;
            DependencyObject current = Keyboard.FocusedElement as DependencyObject;
            while (current != null)
            {
                ListViewItem lvi = current as ListViewItem;
                if (lvi != null)
                {
                    FileItem item = lvi.DataContext as FileItem;
                    if (IsInSelection(item, selectedItems)) return item;
                    break;
                }
                ListBoxItem lbi = current as ListBoxItem;
                if (lbi != null)
                {
                    FileItem item = lbi.DataContext as FileItem;
                    if (IsInSelection(item, selectedItems)) return item;
                    break;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return selectedItems[0];
        }

        private bool IsInSelection(FileItem item, List<FileItem> selectedItems)
        {
            if (item == null || selectedItems == null) return false;
            for (int i = 0; i < selectedItems.Count; i++)
                if (selectedItems[i] != null && string.Equals(selectedItems[i].FullPath, item.FullPath, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private List<FileItem> GetSelectedItemsInViewOrder(TabState state)
        {
            List<FileItem> result = new List<FileItem>();
            if (state == null || !contexts.ContainsKey(state.Id)) return result;
            HashSet<string> selected = new HashSet<string>(GetSelectedPaths(state), StringComparer.OrdinalIgnoreCase);
            ICollectionView view = CollectionViewSource.GetDefaultView(state.Items);
            foreach (object obj in view)
            {
                FileItem item = obj as FileItem;
                if (item != null && selected.Contains(item.FullPath)) result.Add(item);
            }
            return result;
        }

        private FileItem GetSingleSelected(TabState state)
        {
            TabViewContext ctx = contexts[state.Id]; IList selected = currentViewMode == "Grid" ? (IList)ctx.GridView.SelectedItems : (IList)ctx.ListView.SelectedItems; return selected.Count == 1 ? selected[0] as FileItem : null;
        }

        private List<FileItem> GetSelectedItems(TabState state)
        {
            List<FileItem> result = new List<FileItem>();
            if (state == null || !contexts.ContainsKey(state.Id)) return result;
            TabViewContext ctx = contexts[state.Id];
            IList selected = currentViewMode == "Grid" ? (IList)ctx.GridView.SelectedItems : (IList)ctx.ListView.SelectedItems;
            for (int i = 0; i < selected.Count; i++) { FileItem item = selected[i] as FileItem; if (item != null) result.Add(item); }
            return result;
        }

        private List<string> GetSelectedPaths(TabState state)
        {
            List<string> paths = new List<string>();
            if (state == null) return paths;
            TabViewContext ctx = contexts[state.Id];
            IList selected = currentViewMode == "Grid" ? (IList)ctx.GridView.SelectedItems : (IList)ctx.ListView.SelectedItems;
            HashSet<string> selectedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < selected.Count; i++)
            {
                FileItem selectedItem = selected[i] as FileItem;
                if (selectedItem != null) selectedSet.Add(selectedItem.FullPath);
            }
            ICollectionView view = CollectionViewSource.GetDefaultView(state.Items);
            foreach (object obj in view)
            {
                FileItem item = obj as FileItem;
                if (item != null && selectedSet.Contains(item.FullPath)) paths.Add(item.FullPath);
            }
            return paths;
        }

        private void RenameSelected(TabState state)
        {
            if (state == null || state.IsRecycleBin) return;
            List<FileItem> selectedItems = GetSelectedItems(state); if (selectedItems.Count == 0) return;
            if (selectedItems.Count == 1)
            {
                BeginInlineRename(state, selectedItems[0]);
                return;
            }

            // Ferry keeps its explicit batch-rename tool for a multi-selection. Normal one-item
            // Rename (F2/context menu) is now fully inline, matching Explorer.
            List<string> paths = GetSelectedPaths(state);
            RenameDialog dialog = new RenameDialog(this, paths);
            if (dialog.ShowDialog() == true) { lastRenameUndo = dialog.UndoRecords; ScheduleFolderRefresh(state); }
        }

        private void BeginInlineRename(TabState state, FileItem item)
        {
            if (state == null || item == null || state.IsRecycleBin || !contexts.ContainsKey(state.Id)) return;
            TabViewContext ctx = contexts[state.Id];
            CancelOtherInlineRenames(ctx, item);
            Selector selector = string.Equals(currentViewMode, "Grid", StringComparison.OrdinalIgnoreCase) ? (Selector)ctx.GridView : (Selector)ctx.ListView;
            SetSingleSelection(selector, item);
            item.BeginRename();
            if (selector is ListView) ((ListView)selector).ScrollIntoView(item); else if (selector is ListBox) ((ListBox)selector).ScrollIntoView(item);
            UpdateStatus();
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(delegate { FocusInlineRenameEditor(ctx, selector, item); }));
        }

        private void CancelOtherInlineRenames(TabViewContext ctx, FileItem except)
        {
            if (ctx == null || ctx.State == null) return;
            for (int i = 0; i < ctx.State.Items.Count; i++)
            {
                FileItem candidate = ctx.State.Items[i];
                if (candidate != null && candidate != except && candidate.IsRenaming) candidate.CancelRename();
            }
        }

        private void FocusInlineRenameEditor(TabViewContext ctx, Selector selector, FileItem item)
        {
            FocusInlineRenameEditor(ctx, selector, item, 0);
        }

        private void FocusInlineRenameEditor(TabViewContext ctx, Selector selector, FileItem item, int attempt)
        {
            if (ctx == null || selector == null || item == null || !item.IsRenaming) return;
            try
            {
                ctx.Container.UpdateLayout();
                DependencyObject container = null;
                ListView lv = selector as ListView; if (lv != null) container = lv.ItemContainerGenerator.ContainerFromItem(item) as DependencyObject;
                ListBox lb = selector as ListBox; if (lb != null) container = lb.ItemContainerGenerator.ContainerFromItem(item) as DependencyObject;
                TextBox editor = container == null ? null : FindInlineRenameEditor(container);
                if (editor == null)
                {
                    if (attempt < 3) Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate { FocusInlineRenameEditor(ctx, selector, item, attempt + 1); }));
                    return;
                }
                editor.Focus();
                string text = editor.Text ?? string.Empty;
                string extension = item.IsDirectory ? string.Empty : Path.GetExtension(text);
                int selectionLength = item.IsDirectory ? text.Length : Math.Max(0, text.Length - extension.Length);
                editor.Select(0, selectionLength);
            }
            catch
            {
                if (attempt < 3) Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate { FocusInlineRenameEditor(ctx, selector, item, attempt + 1); }));
            }
        }

        private TextBox FindInlineRenameEditor(DependencyObject root)
        {
            if (root == null) return null;
            TextBox own = root as TextBox;
            if (own != null && string.Equals(Convert.ToString(own.Tag), InlineRenameEditorTag, StringComparison.Ordinal)) return own;
            int count = 0; try { count = VisualTreeHelper.GetChildrenCount(root); } catch { return null; }
            for (int i = 0; i < count; i++)
            {
                TextBox found = FindInlineRenameEditor(VisualTreeHelper.GetChild(root, i));
                if (found != null) return found;
            }
            return null;
        }

        private void InlineRenameEditorLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox editor = sender as TextBox; FileItem item = editor == null ? null : editor.DataContext as FileItem;
            if (editor == null || item == null || !item.IsRenaming) return;
            TabViewContext ctx = FindContextForItem(item);
            if (ctx != null) CommitInlineRename(ctx.State, item, editor);
        }

        private TabViewContext FindContextForItem(FileItem item)
        {
            if (item == null) return null;
            foreach (TabViewContext ctx in contexts.Values) if (ctx != null && ctx.State != null && ctx.State.Items.Contains(item)) return ctx;
            return null;
        }

        private bool CommitInlineRename(TabState state, FileItem item, TextBox editor)
        {
            if (state == null || item == null || !item.IsRenaming) return true;
            if (editor != null)
            {
                BindingExpression binding = editor.GetBindingExpression(TextBox.TextProperty);
                if (binding != null) binding.UpdateSource();
            }
            string newName = item.RenameText ?? string.Empty;
            if (string.Equals(newName, item.Name, StringComparison.Ordinal)) { item.CancelRename(); return true; }
            string error = RenameEngine.ValidateBaseName(newName);
            if (error != null) { KeepInlineRenameAfterError(state, item, editor, error); return false; }

            string oldPath = item.FullPath;
            string targetPath = RenameEngine.BuildTargetPathFullName(oldPath, newName);
            RenameEntry entry = new RenameEntry { SourcePath = oldPath, CurrentName = item.Name, NewBaseName = newName, TargetPath = targetPath, NewName = newName, IsValid = true };
            List<RenameEntry> entries = new List<RenameEntry>(); entries.Add(entry);
            error = RenameEngine.ValidateBatch(entries);
            if (error != null) { KeepInlineRenameAfterError(state, item, editor, error); return false; }

            try
            {
                lastRenameUndo = RenameEngine.ExecuteRename(entries);
                long tailOrder; bool wasTail = state.TryGetUnsortedTailOrder(oldPath, out tailOrder);
                state.RemoveUnsortedTail(oldPath); if (wasTail) state.MarkUnsortedTail(targetPath);
                item.ApplyRenameResult(targetPath);
                item.TypeName = item.IsDirectory ? "File folder" : (string.IsNullOrEmpty(Path.GetExtension(targetPath)) ? "File" : Path.GetExtension(targetPath).TrimStart('.').ToUpperInvariant() + " file");
                item.ListIcon = null; item.Icon = null;
                ScheduleFolderRefresh(state);
                if (string.Equals(currentViewMode, "Grid", StringComparison.OrdinalIgnoreCase) && contexts.ContainsKey(state.Id)) StartGridThumbnailLoad(contexts[state.Id]);
                UpdateStatus();
                return true;
            }
            catch (Exception ex) { KeepInlineRenameAfterError(state, item, editor, ex.Message); return false; }
        }

        private void KeepInlineRenameAfterError(TabState state, FileItem item, TextBox editor, string message)
        {
            MessageBox.Show(this, message, "Ferry", MessageBoxButton.OK, MessageBoxImage.Warning);
            if (item == null) return;
            item.IsRenaming = true;
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(delegate
            {
                if (editor != null && item.IsRenaming) { editor.Focus(); editor.SelectAll(); }
                else if (state != null && contexts.ContainsKey(state.Id))
                {
                    Selector selector = string.Equals(currentViewMode, "Grid", StringComparison.OrdinalIgnoreCase) ? (Selector)contexts[state.Id].GridView : (Selector)contexts[state.Id].ListView;
                    FocusInlineRenameEditor(contexts[state.Id], selector, item);
                }
            }));
        }

        private void CancelInlineRename(FileItem item)
        {
            if (item != null) item.CancelRename();
        }

        private void InvalidateRenameUndo()
        {
            lastRenameUndo = null;
        }

        private void UndoLastRename()
        {
            if (lastRenameUndo == null || lastRenameUndo.Count == 0) return;
            try
            {
                RenameEngine.Undo(lastRenameUndo);
                lastRenameUndo = null;
                TabState state = ActiveState;
                if (state != null) ScheduleFolderRefresh(state);
            }
            catch (Exception ex)
            {
                // A stale rename record must never survive another file mutation and surprise the user.
                lastRenameUndo = null;
                Logger.Write("Rename undo failed: " + ex);
                MessageBox.Show(this, ex.Message, "Ferry", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteSelected(TabState state, bool permanent)
        {
            if (state != null && state.IsRecycleBin) { DeleteRecycleItemsPermanently(GetSelectedItems(state)); return; }
            List<string> paths = GetSelectedPaths(state); if (paths.Count == 0) return; InvalidateRenameUndo(); try { if (permanent) ShellFileOperations.DeletePermanently(paths); else ShellFileOperations.DeleteToRecycleBin(paths); ScheduleFolderRefresh(state); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Ferry", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void Paste(TabState state)
        {
            if (state == null || state.IsRecycleBin || !contexts.ContainsKey(state.Id)) return;
            TabViewContext ctx = contexts[state.Id];
            PasteFeedbackSession feedback = null;
            try
            {
                InvalidateRenameUndo();
                List<string> sourcePaths = ClipboardHelper.GetPasteSourcePaths();
                feedback = BeginPasteFeedback(state.CurrentPath, sourcePaths);
                ctx.PasteFeedback = feedback;

                bool completed = ClipboardHelper.Paste(state.CurrentPath);
                if (feedback != null)
                {
                    feedback.OperationCompleted = completed;
                    feedback.OperationFinished = true;
                }

                // SHFileOperation is synchronous, but its progress UI may pump Windows messages.
                // Keeping the feedback session active while the operation runs lets any watcher-
                // driven incremental refresh select arriving top-level items progressively.  The
                // explicit final refresh below then leaves the complete paste result selected.
                if (completed || feedback != null) ScheduleFolderRefresh(state);
            }
            catch (Exception ex)
            {
                if (feedback != null) feedback.OperationFinished = true;
                Logger.Write("Paste failed: " + ex);
                MessageBox.Show(this, ex.Message, "Ferry", MessageBoxButton.OK, MessageBoxImage.Error);
                ScheduleFolderRefresh(state);
            }
        }

        private PasteFeedbackSession BeginPasteFeedback(string destination, IList<string> sourcePaths)
        {
            if (string.IsNullOrEmpty(destination) || sourcePaths == null || sourcePaths.Count == 0) return null;
            PasteFeedbackSession session = new PasteFeedbackSession();
            session.Destination = destination;
            session.SourcePaths = new List<string>(sourcePaths);
            session.SourceDirectoryPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < sourcePaths.Count; i++)
            {
                try { if (Directory.Exists(sourcePaths[i])) session.SourceDirectoryPaths.Add(sourcePaths[i]); } catch { }
            }
            session.Before = SnapshotTopLevelEntries(destination);
            return session;
        }

        private Dictionary<string, PasteEntryStamp> SnapshotTopLevelEntries(string directory)
        {
            Dictionary<string, PasteEntryStamp> result = new Dictionary<string, PasteEntryStamp>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (string path in Directory.EnumerateFileSystemEntries(directory))
                {
                    PasteEntryStamp stamp = CapturePasteEntryStamp(path);
                    if (stamp != null) result[path] = stamp;
                }
            }
            catch { }
            return result;
        }

        private PasteEntryStamp CapturePasteEntryStamp(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    DirectoryInfo info = new DirectoryInfo(path);
                    return new PasteEntryStamp { IsDirectory = true, Length = 0, LastWriteUtcTicks = info.LastWriteTimeUtc.Ticks, CreationUtcTicks = info.CreationTimeUtc.Ticks };
                }
                if (File.Exists(path))
                {
                    FileInfo info = new FileInfo(path);
                    return new PasteEntryStamp { IsDirectory = false, Length = info.Length, LastWriteUtcTicks = info.LastWriteTimeUtc.Ticks, CreationUtcTicks = info.CreationTimeUtc.Ticks };
                }
            }
            catch { }
            return null;
        }

        private bool PasteEntryChanged(PasteEntryStamp before, string currentPath)
        {
            if (before == null) return true;
            PasteEntryStamp after = CapturePasteEntryStamp(currentPath);
            if (after == null) return false;
            return before.IsDirectory != after.IsDirectory ||
                   before.Length != after.Length ||
                   before.LastWriteUtcTicks != after.LastWriteUtcTicks ||
                   before.CreationUtcTicks != after.CreationUtcTicks;
        }

        private bool IsLikelyRenamedPasteResult(string candidatePath, string sourcePath, bool sourceIsDirectory)
        {
            try
            {
                string candidateName = Path.GetFileName(candidatePath);
                string sourceName = Path.GetFileName(sourcePath);
                if (string.IsNullOrEmpty(candidateName) || string.IsNullOrEmpty(sourceName)) return false;

                if (sourceIsDirectory)
                    return candidateName.StartsWith(sourceName, StringComparison.OrdinalIgnoreCase);

                string sourceExtension = Path.GetExtension(sourceName);
                string candidateExtension = Path.GetExtension(candidateName);
                if (!string.Equals(sourceExtension, candidateExtension, StringComparison.OrdinalIgnoreCase)) return false;
                string sourceStem = Path.GetFileNameWithoutExtension(sourceName);
                string candidateStem = Path.GetFileNameWithoutExtension(candidateName);
                return candidateStem.StartsWith(sourceStem, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private void ApplyPasteFeedbackSelection(TabState state, TabViewContext ctx)
        {
            if (state == null || ctx == null || ctx.PasteFeedback == null) return;
            PasteFeedbackSession session = ctx.PasteFeedback;
            if (!string.Equals(state.CurrentPath, session.Destination, StringComparison.OrdinalIgnoreCase))
            {
                ctx.PasteFeedback = null;
                return;
            }

            HashSet<string> displayedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, FileItem> byPath = new Dictionary<string, FileItem>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < state.Items.Count; i++)
            {
                FileItem item = state.Items[i];
                if (item == null || string.IsNullOrEmpty(item.FullPath)) continue;
                displayedPaths.Add(item.FullPath);
                byPath[item.FullPath] = item;
            }

            HashSet<string> resultPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < session.SourcePaths.Count; i++)
            {
                string source = session.SourcePaths[i];
                string name = null;
                try { name = Path.GetFileName(source.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)); } catch { }
                if (string.IsNullOrEmpty(name)) continue;
                string expected = Path.Combine(session.Destination, name);

                PasteEntryStamp oldStamp;
                bool existedBefore = session.Before.TryGetValue(expected, out oldStamp);
                bool sameSourceAndDestination = string.Equals(source, expected, StringComparison.OrdinalIgnoreCase);
                if (displayedPaths.Contains(expected))
                {
                    if (!existedBefore ||
                        (!sameSourceAndDestination && PasteEntryChanged(oldStamp, expected)) ||
                        (!File.Exists(source) && !Directory.Exists(source)) ||
                        (session.OperationFinished && session.OperationCompleted && !sameSourceAndDestination))
                        resultPaths.Add(expected);
                }

                // Same-folder Copy and Windows Keep-both/conflict-renaming create a new sibling
                // whose basename retains the original stem/name.  Limit the fallback to entries
                // that did not exist before the Paste so unrelated pre-existing items are never
                // pulled into the operation-result selection.
                foreach (string candidate in displayedPaths)
                {
                    if (session.Before.ContainsKey(candidate) || resultPaths.Contains(candidate)) continue;
                    bool sourceIsDirectory = session.SourceDirectoryPaths != null && session.SourceDirectoryPaths.Contains(source);
                    if (IsLikelyRenamedPasteResult(candidate, source, sourceIsDirectory)) resultPaths.Add(candidate);
                }
            }

            HashSet<FileItem> desired = new HashSet<FileItem>();
            foreach (string path in resultPaths)
            {
                FileItem item;
                if (byPath.TryGetValue(path, out item)) desired.Add(item);
            }

            if (desired.Count > 0)
            {
                ReconcileSelectorSelection(ctx, ctx.ListView, desired);
                ReconcileSelectorSelection(ctx, ctx.GridView, desired);

                FileItem anchor = null;
                ICollectionView view = CollectionViewSource.GetDefaultView(state.Items);
                foreach (object obj in view)
                {
                    FileItem item = obj as FileItem;
                    if (item != null && desired.Contains(item)) { anchor = item; break; }
                }
                if (anchor != null)
                {
                    ctx.SelectionAnchorItem = anchor;
                    ctx.KeyboardNavigationItem = anchor;
                    SetExtendedSelectionAnchorOnly(ctx.ListView, anchor);
                    SetExtendedSelectionAnchorOnly(ctx.GridView, anchor);
                    Selector visible = string.Equals(currentViewMode, "Grid", StringComparison.OrdinalIgnoreCase) ? (Selector)ctx.GridView : (Selector)ctx.ListView;
                    ListView lv = visible as ListView;
                    if (lv != null) lv.ScrollIntoView(anchor);
                    ListBox lb = visible as ListBox;
                    if (lb != null) lb.ScrollIntoView(anchor);
                    UpdateSelectionAnchorVisual(ctx);
                }
                UpdateStatus();
            }

            // Once SHFileOperation has returned, this reconciliation is the authoritative final
            // result.  Clearing the bounded session prevents a later unrelated watcher refresh
            // from resurrecting an old Paste selection.
            if (session.OperationFinished) ctx.PasteFeedback = null;
        }

        private void CreateNewFolder(TabState state)
        {
            if (state == null || state.IsRecycleBin || state.IsSearching || !contexts.ContainsKey(state.Id)) return;
            try
            {
                InvalidateRenameUndo();
                string basePath = Path.Combine(state.CurrentPath, "New folder"); string path = basePath; int n = 2; while (Directory.Exists(path) || File.Exists(path)) path = basePath + " (" + n++ + ")";
                Directory.CreateDirectory(path);
                FileItem item = CreateBasicItem(path, null);
                if (item == null) { ScheduleFolderRefresh(state); return; }
                try { item.ListIcon = ShellInterop.GetSmallTypeIcon(path, true); if (string.Equals(currentViewMode, "Grid", StringComparison.OrdinalIgnoreCase)) item.Icon = ShellInterop.GetThumbnailOrIcon(path, (int)settings.GridIconSize); } catch { }
                item.ItemCount = 0; item.ItemCountText = "0 items";
                state.MarkUnsortedTail(path);
                state.Items.Add(item);
                ApplySort(state, true, false);
                BeginInlineRename(state, item);
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Ferry", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void CreateShortcut(IList<string> paths)
        {
            if (ActiveState != null && ActiveState.IsRecycleBin) return;
            try { InvalidateRenameUndo(); for (int i = 0; i < paths.Count; i++) ShortcutHelper.Create(paths[i]); TabState state = ActiveState; if (state != null) ScheduleFolderRefresh(state); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Ferry", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private async void CompressSelected(TabState state, IList<string> paths)
        {
            if (state == null || state.IsRecycleBin || paths == null || paths.Count == 0) return;
            if (archiveOperationActive)
            {
                ShowTimedStatusMessage("An archive operation is already in progress.", 2500);
                return;
            }

            InvalidateRenameUndo();
            List<string> copy = new List<string>();
            for (int i = 0; i < paths.Count; i++) copy.Add(paths[i]);

            string initialDestination;
            try { initialDestination = ArchiveHelper.SuggestZipPath(copy, state.CurrentPath); }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Ferry", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            CreateZipSetupWindow setup = new CreateZipSetupWindow(this, copy, initialDestination);
            if (setup.ShowDialog() != true) return;

            string destination = setup.DestinationZipPath;
            bool overwrite = false;
            if (File.Exists(destination))
            {
                ChoiceDialogResult replace = ChoiceDialog.ShowYesNo(
                    this,
                    "Replace ZIP?",
                    "The destination ZIP already exists. Replace it?\n\n" + destination,
                    ChoiceDialogResult.No);
                if (replace != ChoiceDialogResult.Yes) return;
                overwrite = true;
            }

            BeginArchiveOperation("Compressing ZIP");
            try
            {
                Progress<ArchiveProgressInfo> progress = new Progress<ArchiveProgressInfo>(UpdateArchiveProgress);
                ArchiveOperationResult result = await archiveService.CreateZipAsync(
                    copy,
                    destination,
                    overwrite,
                    progress,
                    archiveCancellation.Token);

                string completionMessage = result.Status == ArchiveOperationStatus.Completed
                    ? "ZIP compression complete."
                    : "ZIP compression cancelled.";
                EndArchiveOperation(completionMessage);

                if (contexts.ContainsKey(state.Id) && !state.IsSearching) ScheduleFolderRefresh(state);
                else UpdateStatus();
            }
            catch (Exception ex)
            {
                EndArchiveOperation(null);
                MessageBox.Show(this, ex.Message, "Compression failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExtractZip(TabState state, string zipPath, bool namedFolder)
        {
            if (state == null || state.IsRecycleBin || string.IsNullOrEmpty(zipPath)) return;
            if (archiveOperationActive)
            {
                ShowTimedStatusMessage("An archive operation is already in progress.", 2500);
                return;
            }

            InvalidateRenameUndo();
            string initialDestination;
            try { initialDestination = ArchiveHelper.SuggestExtractionDirectory(zipPath, namedFolder); }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Ferry", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ExtractZipSetupWindow setup = new ExtractZipSetupWindow(this, zipPath, initialDestination);
            if (setup.ShowDialog() != true) return;

            BeginArchiveOperation("Extracting ZIP");
            try
            {
                Progress<ArchiveProgressInfo> progress = new Progress<ArchiveProgressInfo>(UpdateArchiveProgress);
                ArchiveOperationResult result = await archiveService.ExtractZipAsync(
                    setup.SourceZipPath,
                    setup.DestinationDirectory,
                    archiveThresholds,
                    ConfirmArchiveSafetyRiskAsync,
                    ConfirmArchiveConflictAsync,
                    progress,
                    archiveCancellation.Token);

                string completionMessage = "ZIP extraction complete.";
                if (result.Status == ArchiveOperationStatus.Cancelled)
                {
                    completionMessage = result.CompletedFiles > 0
                        ? "ZIP extraction cancelled (partial result kept)."
                        : "ZIP extraction cancelled.";
                    if (result.CompletedFiles > 0)
                    {
                        MessageBox.Show(
                            this,
                            "Extraction was cancelled. " + result.CompletedFiles.ToString("N0") + " completed file(s) remain in the destination.",
                            "Extraction cancelled",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
                else if (result.Status == ArchiveOperationStatus.Partial || result.Status == ArchiveOperationStatus.Failed)
                {
                    completionMessage = result.Status == ArchiveOperationStatus.Partial
                        ? "ZIP extraction stopped (partial result kept)."
                        : "ZIP extraction failed.";
                    string detail = string.IsNullOrEmpty(result.ErrorMessage) ? "Extraction did not complete." : result.ErrorMessage;
                    if (result.Status == ArchiveOperationStatus.Partial)
                        detail = "Extraction stopped after " + result.CompletedFiles.ToString("N0") + " file(s) were completed. Those files remain in the destination.\n\n" + detail;
                    MessageBox.Show(this, detail, "Extraction failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (result.SkippedFiles > 0 || result.KeptBothConflicts > 0)
                {
                    var summaryParts = new List<string>();
                    if (result.KeptBothConflicts > 0)
                        summaryParts.Add(result.KeptBothConflicts.ToString("N0") + " conflict(s) kept separately");
                    if (result.SkippedFiles > 0)
                        summaryParts.Add(result.SkippedFiles.ToString("N0") + " file(s) skipped");
                    completionMessage = "ZIP extraction complete. " + String.Join(", ", summaryParts.ToArray()) + ".";
                }

                EndArchiveOperation(completionMessage);
                if (contexts.ContainsKey(state.Id) && !state.IsSearching) ScheduleFolderRefresh(state);
                else UpdateStatus();
            }
            catch (InvalidDataException ex)
            {
                EndArchiveOperation("ZIP extraction blocked.");
                MessageBox.Show(
                    this,
                    "Ferry blocked this ZIP because its path information is unsafe or invalid.\n\n" + ex.Message,
                    "ZIP blocked",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                EndArchiveOperation(null);
                MessageBox.Show(this, ex.Message, "Extraction failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Task<bool> ConfirmArchiveSafetyRiskAsync(ArchiveSafetyReport report)
        {
            bool continueExtraction = false;
            Action show = delegate
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("This ZIP may require unusually large resources.");
                builder.AppendLine();
                builder.AppendLine("Compressed data: " + FormatArchiveBytes(report.CompressedBytes));
                builder.AppendLine("Expanded data:   " + FormatArchiveBytes(report.ExpandedBytes));
                builder.AppendLine("Files:           " + report.FileCount.ToString("N0"));
                builder.AppendLine("Compression ratio: " + FormatArchiveRatio(report.CompressionRatio));
                builder.AppendLine();
                builder.AppendLine("Triggered warning(s):");
                for (int i = 0; i < report.Issues.Count; i++) builder.AppendLine("  • " + report.Issues[i].Message);
                builder.AppendLine();
                builder.Append("Continue extracting?");

                ChoiceDialogResult answer = ChoiceDialog.ShowYesNo(
                    this,
                    "ZIP resource warning",
                    builder.ToString(),
                    ChoiceDialogResult.No);
                continueExtraction = answer == ChoiceDialogResult.Yes;
            };

            if (Dispatcher.CheckAccess()) show(); else Dispatcher.Invoke(show);
            return Task.FromResult(continueExtraction);
        }

        private Task<ArchiveConflictResolution> ConfirmArchiveConflictAsync(ArchiveOverwriteRequest request)
        {
            ArchiveConflictResolution resolution = null;
            Action show = delegate
            {
                resolution = ChoiceDialog.ShowArchiveConflict(this, request);
            };

            if (Dispatcher.CheckAccess()) show(); else Dispatcher.Invoke(show);
            return Task.FromResult(resolution ?? new ArchiveConflictResolution
            {
                Decision = ArchiveOverwriteDecision.Cancel
            });
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
            if (container != null) pinnedListBox.SelectedItem = container.DataContext;
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

        private void EmptyRecycleBin()
        {
            try { InvalidateRenameUndo(); RecycleBinService.Empty(this); TabState state = ActiveState; if (state != null && state.IsRecycleBin) LoadFolder(state, TabState.RecycleBinPath, false); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Ferry", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void RestoreRecycleItems(IList<FileItem> items)
        {
            try { InvalidateRenameUndo(); RecycleBinService.Restore(items); TabState state = ActiveState; if (state != null && state.IsRecycleBin) LoadFolder(state, TabState.RecycleBinPath, false); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Ferry", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void DeleteRecycleItemsPermanently(IList<FileItem> items)
        {
            if (items == null || items.Count == 0) return;
            MessageBoxResult confirm = MessageBox.Show(this, "Permanently delete the selected item(s)?", "Ferry", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
            try { InvalidateRenameUndo(); RecycleBinService.DeletePermanently(items); TabState state = ActiveState; if (state != null && state.IsRecycleBin) LoadFolder(state, TabState.RecycleBinPath, false); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Ferry", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void SetTemporaryView(string mode)
        {
            string previousMode = currentViewMode;
            bool changingView = !string.Equals(previousMode, mode, StringComparison.OrdinalIgnoreCase);
            Dictionary<TabViewContext, HashSet<FileItem>> selectionSnapshots = null;

            // ListView and GridView are separate WPF Selector instances. They share the same
            // ItemsSource, but WPF does not share their SelectedItems state. Without an explicit
            // hand-off, the newly shown view can expose an old/stale selection from an earlier
            // visit (for example 1000 items in List becoming 1026 in Grid). Snapshot the visible
            // selector before changing currentViewMode, then reconcile the destination selector
            // to exactly the same FileItem set.
            if (changingView)
            {
                selectionSnapshots = new Dictionary<TabViewContext, HashSet<FileItem>>();
                bool previousWasGrid = string.Equals(previousMode, "Grid", StringComparison.OrdinalIgnoreCase);
                foreach (TabViewContext ctx in contexts.Values)
                {
                    Selector source = previousWasGrid ? (Selector)ctx.GridView : (Selector)ctx.ListView;
                    selectionSnapshots[ctx] = SnapshotSelection(source);
                }
            }

            currentViewMode = mode;
            bool grid = string.Equals(mode, "Grid", StringComparison.OrdinalIgnoreCase);
            foreach (TabViewContext ctx in contexts.Values)
            {
                ShowCurrentView(ctx);
                if (changingView && selectionSnapshots != null)
                {
                    Selector target = grid ? (Selector)ctx.GridView : (Selector)ctx.ListView;
                    HashSet<FileItem> desired;
                    if (selectionSnapshots.TryGetValue(ctx, out desired))
                        ReconcileSelectorSelection(ctx, target, desired);
                }
                if (!grid) CancelGridThumbnailLoad(ctx);
            }
            if (grid && ActiveContext != null) StartGridThumbnailLoad(ActiveContext);
            UpdateStatus();
        }

        private void ShowCurrentView(TabViewContext ctx)
        {
            bool list = string.Equals(currentViewMode, "List", StringComparison.OrdinalIgnoreCase); ctx.ListView.Visibility = list ? Visibility.Visible : Visibility.Collapsed; ctx.GridView.Visibility = list ? Visibility.Collapsed : Visibility.Visible;
        }

        private void CancelGridThumbnailLoad(TabViewContext ctx)
        {
            if (ctx == null || ctx.GridThumbnailCancellation == null) return;
            try { ctx.GridThumbnailCancellation.Cancel(); } catch { }
            ctx.GridThumbnailCancellation = null;
        }

        private void StartGridThumbnailLoad(TabViewContext ctx)
        {
            if (ctx == null || ctx.State == null) return;
            CancelGridThumbnailLoad(ctx);
            FileItem[] pending = ctx.State.Items.Where(delegate(FileItem item) { return item != null && item.Icon == null; }).ToArray();
            if (pending.Length == 0) return;
            CancellationTokenSource cts = new CancellationTokenSource();
            ctx.GridThumbnailCancellation = cts;
            CancellationToken token = cts.Token;
            TabState state = ctx.State;
            Queue<FileItem> queue = new Queue<FileItem>(pending); object gate = new object();
            int workerCount = Math.Min(2, Math.Max(1, Environment.ProcessorCount / 2));
            for (int worker = 0; worker < workerCount; worker++)
            {
                Task.Run(delegate
                {
                    while (true)
                    {
                        token.ThrowIfCancellationRequested(); FileItem item;
                        lock (gate) { if (queue.Count == 0) break; item = queue.Dequeue(); }
                        ImageSource thumbnail = null;
                        try { thumbnail = ShellInterop.GetThumbnailOrIcon(item.FullPath, (int)settings.GridIconSize); } catch { }
                        if (thumbnail == null) continue;
                        Dispatcher.BeginInvoke(new Action(delegate
                        {
                            if (token.IsCancellationRequested || !contexts.ContainsKey(state.Id) || !state.Items.Contains(item)) return;
                            item.Icon = thumbnail;
                        }));
                    }
                }, token);
            }
        }

        private void ShowColumnsMenu(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button; if (button == null) return; ContextMenu menu = new ContextMenu(); string[] names = new string[] { "Name", "Items", "Type", "Size", "Modified", "Created" };
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i]; MenuItem item = new MenuItem { Header = name, IsCheckable = true, IsChecked = !settings.HiddenColumns.Contains(name), Tag = name };
                item.Click += delegate(object s, RoutedEventArgs args) { CaptureColumnSettings(); MenuItem mi = (MenuItem)s; string col = (string)mi.Tag; if (mi.IsChecked) settings.HiddenColumns.Remove(col); else if (!settings.HiddenColumns.Contains(col)) settings.HiddenColumns.Add(col); foreach (TabViewContext ctx in contexts.Values) BuildListColumns(ctx, ctx.State.IsSearching); };
                menu.Items.Add(item);
            }
            button.ContextMenu = menu; menu.PlacementTarget = button; menu.Placement = PlacementMode.Bottom; menu.IsOpen = true;
        }

        private void ApplySidebarLayoutFromSettings()
        {
            if (mainGrid == null || mainGrid.ColumnDefinitions.Count < 2) return;
            ColumnDefinition sidebarColumn = mainGrid.ColumnDefinitions[0];
            if (settings.SidebarVisible)
            {
                double width = Math.Max(50, Math.Min(480, settings.SidebarWidth));
                settings.SidebarWidth = width;
                sidebarColumn.MinWidth = 50;
                sidebarColumn.MaxWidth = 480;
                sidebarColumn.Width = new GridLength(width);
                if (sidebarBorder != null) sidebarBorder.Visibility = Visibility.Visible;
                if (sidebarSplitter != null) sidebarSplitter.Visibility = Visibility.Visible;
            }
            else
            {
                sidebarColumn.MinWidth = 0;
                sidebarColumn.MaxWidth = double.PositiveInfinity;
                sidebarColumn.Width = new GridLength(0);
                if (sidebarBorder != null) sidebarBorder.Visibility = Visibility.Collapsed;
                if (sidebarSplitter != null) sidebarSplitter.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowSettings(object sender, RoutedEventArgs e)
        {
            CaptureColumnSettings(); SettingsWindow window = new SettingsWindow(this, settings); if (window.ShowDialog() == true && window.Result != null) { settings = window.Result; Logger.Configure(settings.DebugLogging); currentViewMode = settings.DefaultView; ApplySidebarLayoutFromSettings(); searchModeBox.SelectedItem = settings.SearchMode; BuildSidebar(); foreach (TabViewContext ctx in contexts.Values) { BuildListColumns(ctx, ctx.State.IsSearching); ShowCurrentView(ctx); if (!ctx.State.IsSearching) LoadFolder(ctx.State, ctx.State.CurrentPath, false); } SettingsStore.Save(settings); }
        }

        private void CloseTab(TabState state)
        {
            if (state == null || !contexts.ContainsKey(state.Id)) return; TabViewContext ctx = contexts[state.Id]; StopSearchDrain(ctx); CancelGridThumbnailLoad(ctx); state.CancelBackgroundWork(); if (ctx.Watcher != null) try { ctx.Watcher.Dispose(); } catch { } tabs.Items.Remove(ctx.TabItem); contexts.Remove(state.Id); if (tabs.Items.Count == 0) Close();
        }

        private void BeginArchiveOperation(string label)
        {
            if (archiveCancellation != null)
            {
                try { archiveCancellation.Dispose(); } catch { }
            }
            archiveCancellation = new CancellationTokenSource();
            archiveOperationActive = true;
            archiveOperationLabel = string.IsNullOrEmpty(label) ? "Archive operation" : label;
            archiveProgressInfo = new ArchiveProgressInfo { Phase = ArchivePhase.Scanning, CurrentItem = "Preparing..." };
            UpdateStatus();
        }

        private void CancelArchiveOperation()
        {
            if (!archiveOperationActive || archiveCancellation == null) return;
            if (!archiveCancellation.IsCancellationRequested) archiveCancellation.Cancel();
            if (archiveCancelButton != null) archiveCancelButton.IsEnabled = false;
            if (statusText != null) statusText.Text = "Cancelling archive operation...";
        }

        private void UpdateArchiveProgress(ArchiveProgressInfo info)
        {
            if (!archiveOperationActive || info == null) return;
            archiveProgressInfo = info;
            UpdateStatus();
        }

        private void EndArchiveOperation(string completionMessage)
        {
            archiveOperationActive = false;
            archiveProgressInfo = null;
            archiveOperationLabel = null;
            if (archiveCancellation != null)
            {
                try { archiveCancellation.Dispose(); } catch { }
                archiveCancellation = null;
            }
            if (archiveCancelButton != null)
            {
                archiveCancelButton.Visibility = Visibility.Collapsed;
                archiveCancelButton.IsEnabled = true;
            }
            if (statusProgress != null)
            {
                statusProgress.Visibility = Visibility.Collapsed;
                statusProgress.IsIndeterminate = false;
                statusProgress.Value = 0;
            }
            if (closeAfterArchiveCancellation)
            {
                closeAfterArchiveCancellation = false;
                Dispatcher.BeginInvoke(new Action(Close));
                return;
            }
            if (!string.IsNullOrEmpty(completionMessage))
            {
                ShowTimedStatusMessage(completionMessage, 3500);
                return;
            }
            UpdateStatus();
        }

        private void ShowTimedStatusMessage(string message, int milliseconds)
        {
            transientStatusMessage = message ?? string.Empty;
            transientStatusUntilUtc = DateTime.UtcNow.AddMilliseconds(Math.Max(1, milliseconds));
            if (transientStatusTimer == null)
            {
                transientStatusTimer = new DispatcherTimer(DispatcherPriority.Background);
                transientStatusTimer.Tick += delegate
                {
                    if (DateTime.UtcNow < transientStatusUntilUtc) return;
                    transientStatusTimer.Stop();
                    transientStatusMessage = null;
                    UpdateStatus();
                };
            }
            transientStatusTimer.Stop();
            transientStatusTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(1, milliseconds));
            transientStatusTimer.Start();
            UpdateStatus();
        }

        private bool ShowTransientStatusMessage()
        {
            if (string.IsNullOrEmpty(transientStatusMessage)) return false;
            if (DateTime.UtcNow >= transientStatusUntilUtc)
            {
                transientStatusMessage = null;
                if (transientStatusTimer != null) transientStatusTimer.Stop();
                return false;
            }
            HideArchiveStatusControls();
            if (statusText != null)
            {
                statusText.Text = transientStatusMessage;
                statusText.ToolTip = transientStatusMessage;
            }
            return true;
        }

        private bool ShowArchiveActivityStatus()
        {
            if (!archiveOperationActive) return false;

            ArchiveProgressInfo info = archiveProgressInfo;
            string label = FormatArchiveProgressStatus(info);
            if (statusText != null)
            {
                statusText.Text = label;
                statusText.ToolTip = label;
            }

            if (statusProgress != null)
            {
                statusProgress.Visibility = Visibility.Visible;
                bool indeterminate = info == null || info.Phase == ArchivePhase.Scanning || info.Phase == ArchivePhase.WaitingForConfirmation;
                statusProgress.IsIndeterminate = indeterminate;
                if (!indeterminate) statusProgress.Value = info == null ? 0 : info.OverallPercent;
            }

            if (archiveCancelButton != null)
            {
                archiveCancelButton.Visibility = Visibility.Visible;
                archiveCancelButton.IsEnabled = archiveCancellation != null && !archiveCancellation.IsCancellationRequested;
            }
            return true;
        }

        private void HideArchiveStatusControls()
        {
            if (statusProgress != null)
            {
                statusProgress.Visibility = Visibility.Collapsed;
                statusProgress.IsIndeterminate = false;
            }
            if (archiveCancelButton != null)
            {
                archiveCancelButton.Visibility = Visibility.Collapsed;
                archiveCancelButton.IsEnabled = true;
            }
        }

        private string FormatArchiveProgressStatus(ArchiveProgressInfo info)
        {
            if (info == null) return (archiveOperationLabel ?? "Archive operation") + "...";

            string phase;
            switch (info.Phase)
            {
                case ArchivePhase.Scanning: phase = "Scanning"; break;
                case ArchivePhase.WaitingForConfirmation: phase = "Waiting for confirmation"; break;
                case ArchivePhase.Compressing: phase = "Compressing"; break;
                case ArchivePhase.Extracting: phase = "Extracting"; break;
                case ArchivePhase.Finalizing: phase = "Finalizing"; break;
                case ArchivePhase.Cancelled: phase = "Cancelling"; break;
                default: phase = archiveOperationLabel ?? "Archive operation"; break;
            }

            StringBuilder builder = new StringBuilder(phase);
            if (!string.IsNullOrEmpty(info.CurrentItem) &&
                info.Phase != ArchivePhase.Scanning &&
                info.Phase != ArchivePhase.WaitingForConfirmation &&
                info.Phase != ArchivePhase.Finalizing)
            {
                builder.Append(" — ");
                builder.Append(info.CurrentItem);
            }
            if (info.TotalBytes > 0)
            {
                builder.Append("  •  ");
                builder.Append(FormatArchiveBytes(info.ProcessedBytes));
                builder.Append(" / ");
                builder.Append(FormatArchiveBytes(info.TotalBytes));
                builder.Append(" (");
                builder.Append(info.OverallPercent.ToString("F0"));
                builder.Append("%)");
            }
            else if (info.TotalFiles > 0)
            {
                builder.Append("  •  Files ");
                builder.Append(info.ProcessedFiles.ToString("N0"));
                builder.Append(" / ");
                builder.Append(info.TotalFiles.ToString("N0"));
            }

            if (info.TotalFiles > 0 && info.TotalBytes > 0)
            {
                builder.Append("  •  Files ");
                builder.Append(info.ProcessedFiles.ToString("N0"));
                builder.Append(" / ");
                builder.Append(info.TotalFiles.ToString("N0"));
            }

            if (info.BytesPerSecond > 0)
            {
                builder.Append("  •  ");
                builder.Append(FormatArchiveBytes((long)info.BytesPerSecond));
                builder.Append("/s");
                if (info.EstimatedRemaining.HasValue)
                {
                    builder.Append("  •  Remaining ");
                    builder.Append(FormatArchiveEta(info.EstimatedRemaining.Value));
                }
            }
            else if (info.Phase == ArchivePhase.Compressing || info.Phase == ArchivePhase.Extracting)
            {
                builder.Append("  •  Remaining calculating...");
            }

            return builder.ToString();
        }

        private static string FormatArchiveBytes(long bytes)
        {
            if (bytes < 0) bytes = 0;
            double value = bytes;
            string[] units = new string[] { "B", "KiB", "MiB", "GiB", "TiB" };
            int unit = 0;
            while (value >= 1024.0 && unit < units.Length - 1)
            {
                value /= 1024.0;
                unit++;
            }
            if (unit == 0) return ((long)value).ToString("N0") + " " + units[unit];
            return value.ToString(value >= 100 ? "F0" : value >= 10 ? "F1" : "F2") + " " + units[unit];
        }

        private static string FormatArchiveEta(TimeSpan value)
        {
            if (value < TimeSpan.Zero) value = TimeSpan.Zero;
            if (value.TotalSeconds < 60) return Math.Ceiling(value.TotalSeconds).ToString("F0") + " sec";
            if (value.TotalMinutes < 60) return Math.Ceiling(value.TotalMinutes).ToString("F0") + " min";
            return ((int)value.TotalHours).ToString() + "h " + value.Minutes.ToString() + "m";
        }

        private static string FormatArchiveRatio(double ratio)
        {
            if (double.IsInfinity(ratio)) return "∞";
            if (double.IsNaN(ratio)) return "n/a";
            return ratio.ToString("N1") + "×";
        }

        private void SetStatusMessage(string message)
        {
            if (ShowArchiveActivityStatus()) return;
            if (ShowTransientStatusMessage()) return;
            HideArchiveStatusControls();
            if (statusText != null)
            {
                statusText.Text = message ?? string.Empty;
                statusText.ToolTip = message ?? string.Empty;
            }
        }

        private void UpdateStatus()
        {
            if (ShowArchiveActivityStatus()) return;
            if (ShowTransientStatusMessage()) return;
            HideArchiveStatusControls();
            TabState state = ActiveState;
            if (state == null)
            {
                if (statusText != null) { statusText.Text = "Ready"; statusText.ToolTip = "Ready"; }
                return;
            }
            List<string> selected = GetSelectedPaths(state);
            string prefix = state.IsSearching ? state.Items.Count.ToString("N0") + " results" : state.Items.Count.ToString("N0") + " items";
            if (selected.Count > 0) prefix += "   •   " + selected.Count.ToString("N0") + " selected";
            if (statusText != null) { statusText.Text = prefix; statusText.ToolTip = prefix; }
        }

        private void OnKeyDownAfterControls(object sender, KeyEventArgs e)
        {
            ModifierKeys mods = Keyboard.Modifiers;
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (mods != ModifierKeys.None) return;

            // Ignore text editing and other non-file-view keyboard targets.
            if (Keyboard.FocusedElement is TextBox) return;

            TabViewContext ctx = ActiveContext;
            bool gridMode = ctx != null && string.Equals(currentViewMode, "Grid", StringComparison.OrdinalIgnoreCase);
            bool supportedKey = key == Key.Down || key == Key.Up ||
                                (gridMode && (key == Key.Left || key == Key.Right));
            if (!supportedKey) return;

            Selector selector = ctx == null ? null : (gridMode ? (Selector)ctx.GridView : (Selector)ctx.ListView);
            if (selector == null || !selector.IsKeyboardFocusWithin) return;

            SyncPlainArrowAnchorAfterControlNavigation(ctx, selector);
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            ModifierKeys mods = Keyboard.Modifiers; TabState state = ActiveState; Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            TabViewContext navContext = ActiveContext;
            bool navGridMode = navContext != null && string.Equals(currentViewMode, "Grid", StringComparison.OrdinalIgnoreCase);
            bool navArrowKey = key == Key.Down || key == Key.Up ||
                               (navGridMode && (key == Key.Left || key == Key.Right));
            if ((mods == ModifierKeys.None || mods == ModifierKeys.Shift) && navArrowKey)
            {
                Selector navSelector = navContext == null ? null : (navGridMode ? (Selector)navContext.GridView : (Selector)navContext.ListView);
                if (TryHandleEmptySelectionArrowNavigation(navContext, navSelector, key, mods == ModifierKeys.Shift))
                {
                    e.Handled = true;
                    return;
                }

            }
            TextBox inlineEditor = Keyboard.FocusedElement as TextBox;
            if (inlineEditor != null && string.Equals(Convert.ToString(inlineEditor.Tag), InlineRenameEditorTag, StringComparison.Ordinal))
            {
                FileItem renameItem = inlineEditor.DataContext as FileItem; TabViewContext renameContext = FindContextForItem(renameItem);
                if (key == Key.Enter)
                {
                    bool committed = renameContext != null && CommitInlineRename(renameContext.State, renameItem, inlineEditor);
                    if (committed && renameContext != null) { if (string.Equals(currentViewMode, "Grid", StringComparison.OrdinalIgnoreCase)) renameContext.GridView.Focus(); else renameContext.ListView.Focus(); }
                    e.Handled = true; return;
                }
                if (key == Key.Escape)
                {
                    CancelInlineRename(renameItem);
                    if (renameContext != null) { if (string.Equals(currentViewMode, "Grid", StringComparison.OrdinalIgnoreCase)) renameContext.GridView.Focus(); else renameContext.ListView.Focus(); }
                    e.Handled = true; return;
                }
            }
            if (mods == ModifierKeys.Control && key == Key.T) { OpenNewTab(settings.HomePath, true); e.Handled = true; }
            else if (mods == ModifierKeys.Control && key == Key.W) { if (state != null) CloseTab(state); e.Handled = true; }
            else if (mods == ModifierKeys.Control && key == Key.Tab) { CycleTab(1); e.Handled = true; }
            else if (mods == (ModifierKeys.Control | ModifierKeys.Shift) && key == Key.Tab) { CycleTab(-1); e.Handled = true; }
            else if (mods == ModifierKeys.Control && key == Key.L) { ToggleLocationBox(); e.Handled = true; }
            else if (mods == ModifierKeys.Control && key == Key.F) { searchBox.Focus(); searchBox.SelectAll(); e.Handled = true; }
            else if (mods == ModifierKeys.Control && key == Key.A && state != null && !(Keyboard.FocusedElement is TextBox)) { TabViewContext ctx = ActiveContext; if (currentViewMode == "Grid") ctx.GridView.SelectAll(); else ctx.ListView.SelectAll(); e.Handled = true; }
            else if (mods == ModifierKeys.Control && key == Key.C && state != null && !(Keyboard.FocusedElement is TextBox)) { ClipboardHelper.Copy(GetSelectedPaths(state), false); e.Handled = true; }
            else if (mods == ModifierKeys.Control && key == Key.X && state != null && !(Keyboard.FocusedElement is TextBox)) { ClipboardHelper.Copy(GetSelectedPaths(state), true); e.Handled = true; }
            else if (mods == ModifierKeys.Control && key == Key.V && state != null && !(Keyboard.FocusedElement is TextBox)) { Paste(state); e.Handled = true; }
            else if (mods == ModifierKeys.Control && key == Key.Z && !(Keyboard.FocusedElement is TextBox)) { UndoLastRename(); e.Handled = true; }
            else if (mods == ModifierKeys.Alt && key == Key.Left) { GoBack(); e.Handled = true; }
            else if (mods == ModifierKeys.Alt && key == Key.Right) { GoForward(); e.Handled = true; }
            else if (mods == ModifierKeys.Alt && key == Key.Up) { GoUp(); e.Handled = true; }
            else if (mods == ModifierKeys.Alt && key == Key.Enter && state != null) { FileItem item = GetSingleSelected(state); if (item != null) ShellInterop.ShowProperties(this, item.FullPath); e.Handled = true; }
            else if (mods == ModifierKeys.None && key == Key.F12 && state != null && !state.IsRecycleBin) { OpenTerminalHere(state); e.Handled = true; }
            else if (key == Key.F5 && state != null) { if (state.IsSearching) StartSearch(); else SafeRefreshFolderIncremental(state, true); e.Handled = true; }
            else if (key == Key.F2 && state != null && !(Keyboard.FocusedElement is TextBox)) { RenameSelected(state); e.Handled = true; }
            else if (key == Key.Delete && state != null && !(Keyboard.FocusedElement is TextBox)) { DeleteSelected(state, (mods & ModifierKeys.Shift) == ModifierKeys.Shift); e.Handled = true; }
            else if (key == Key.Enter && state != null && !(Keyboard.FocusedElement is TextBox)) { OpenSelected(state); e.Handled = true; }
            else if (key == Key.Escape && IsLocationBoxVisible()) { HideLocationBox(true); e.Handled = true; }
            else if (key == Key.Escape && state != null)
            {
                if (state.IsSearching || (searchBox != null && searchBox.Text.Length > 0)) ClearSearchText();
                else if (searchBox != null && Keyboard.FocusedElement == searchBox)
                {
                    TabViewContext ctx = ActiveContext;
                    if (ctx != null) { if (currentViewMode == "Grid") ctx.GridView.Focus(); else ctx.ListView.Focus(); }
                }
                else ClearSelection(state);
                e.Handled = true;
            }
        }

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (ActiveState != null && ActiveState.IsRecycleBin) return;
            if (string.IsNullOrEmpty(e.Text) || Keyboard.FocusedElement is TextBox || Keyboard.FocusedElement is ComboBox) return; if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt)) != 0) return; if (char.IsControl(e.Text[0])) return;
            searchBox.Focus(); searchBox.CaretIndex = searchBox.Text.Length; searchBox.SelectedText = e.Text; searchBox.CaretIndex = searchBox.Text.Length; e.Handled = true;
        }

        private void CycleTab(int direction)
        {
            if (tabs.Items.Count < 2) return; int index = tabs.SelectedIndex + direction; if (index < 0) index = tabs.Items.Count - 1; if (index >= tabs.Items.Count) index = 0; tabs.SelectedIndex = index;
        }

        private void CaptureColumnSettings()
        {
            CaptureColumnSettings(ActiveContext);
        }

        private void CaptureColumnSettings(TabViewContext ctx)
        {
            if (ctx == null || ctx.ListGrid == null) return; List<string> order = new List<string>();
            for (int i = 0; i < ctx.ListGrid.Columns.Count; i++)
            {
                GridViewColumn c = ctx.ListGrid.Columns[i]; GridViewColumnHeader h = c.Header as GridViewColumnHeader; if (h == null || h.Tag == null) continue; string name = Convert.ToString(h.Tag); if (name == ListGutterColumnTag || name == "Location" || name == "OriginalLocation") continue; order.Add(name); settings.ColumnWidths[name] = c.ActualWidth > 0 ? c.ActualWidth : c.Width;
            }
            // Preserve hidden columns in their old relative positions by appending them.
            string[] all = new string[] { "Name", "Items", "Type", "Size", "Modified", "Created" }; for (int i = 0; i < all.Length; i++) if (!order.Contains(all[i])) order.Add(all[i]); settings.ColumnOrder = order;
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (archiveOperationActive)
            {
                ChoiceDialogResult answer = ChoiceDialog.ShowYesNo(
                    this,
                    "Archive operation in progress",
                    "An archive operation is still running. Cancel it and close Ferry?",
                    ChoiceDialogResult.No);
                if (answer != ChoiceDialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }

                e.Cancel = true;
                closeAfterArchiveCancellation = true;
                CancelArchiveOperation();
                return;
            }

            CaptureColumnSettings(); if (WindowState == WindowState.Normal) { settings.WindowWidth = Width; settings.WindowHeight = Height; settings.WindowLeft = Left; settings.WindowTop = Top; } settings.WindowMaximized = WindowState == WindowState.Maximized; settings.SidebarWidth = mainGrid.ColumnDefinitions[0].ActualWidth > 0 ? mainGrid.ColumnDefinitions[0].ActualWidth : settings.SidebarWidth; settings.SearchMode = Convert.ToString(searchModeBox.SelectedItem); try { SettingsStore.Save(settings); } catch { }
            foreach (TabViewContext ctx in contexts.Values) { StopSearchDrain(ctx); CancelGridThumbnailLoad(ctx); ctx.State.CancelBackgroundWork(); if (ctx.Watcher != null) try { ctx.Watcher.Dispose(); } catch { } }
        }
    }
}
