using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    internal sealed class MainWindow : Window
    {
        private AppSettings settings;
        private readonly Dictionary<Guid, TabViewContext> contexts = new Dictionary<Guid, TabViewContext>();
        private TabControl tabs;
        private Grid mainGrid;
        private Border sidebarBorder;
        private GridSplitter sidebarSplitter;
        private StackPanel sidebarPanel;
        private StackPanel breadcrumbPanel;
        private TextBox locationBox;
        private TextBox searchBox;
        private Button searchClearButton;
        private DispatcherTimer searchDebounceTimer;
        private ComboBox searchModeBox;
        private TextBlock statusText;
        private ProgressBar statusProgress;
        private readonly SortedDictionary<int, string> archiveActivities = new SortedDictionary<int, string>();
        private int nextArchiveActivityId;
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

            Border toolbar = new Border { BorderBrush = SystemColors.ControlDarkBrush, BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(8, 7, 8, 7), Background = SystemColors.ControlBrush };
            DockPanel top = new DockPanel { LastChildFill = true };
            StackPanel nav = new StackPanel { Orientation = Orientation.Horizontal };
            backButton = ToolbarButton("←", "Back"); backButton.Click += delegate { GoBack(); };
            forwardButton = ToolbarButton("→", "Forward"); forwardButton.Click += delegate { GoForward(); };
            Button up = ToolbarButton("↑", "Up"); up.Click += delegate { GoUp(); };
            Button home = ToolbarButton("⌂", "Home"); home.Click += delegate { Navigate(settings.HomePath, true); };
            Button newTab = ToolbarButton("+", "New tab (Ctrl+T)"); newTab.Click += delegate { OpenNewTab(settings.HomePath, true); };
            nav.Children.Add(backButton); nav.Children.Add(forwardButton); nav.Children.Add(up); nav.Children.Add(home); nav.Children.Add(newTab);
            DockPanel.SetDock(nav, Dock.Left); top.Children.Add(nav);

            StackPanel right = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8, 0, 0, 0) };
            Button list = ToolbarButton("☷", "List view"); list.Click += delegate { SetTemporaryView("List"); };
            Button grid = ToolbarButton("▦", "Grid view"); grid.Click += delegate { SetTemporaryView("Grid"); };
            Button columns = ToolbarButton("Columns", "Show/hide list columns"); columns.Click += ShowColumnsMenu;
            Button settingsButton = ToolbarButton("⚙", "Settings"); settingsButton.Click += ShowSettings;
            searchModeBox = new ComboBox { Width = 102, Margin = new Thickness(6, 0, 4, 0), VerticalContentAlignment = VerticalAlignment.Center };
            searchModeBox.Items.Add("Contains"); searchModeBox.Items.Add("StartsWith"); searchModeBox.SelectedItem = settings.SearchMode;
            searchModeBox.SelectionChanged += delegate { if (searchModeBox.SelectedItem != null) { settings.SearchMode = Convert.ToString(searchModeBox.SelectedItem); if (!suppressSearchTextEvent && searchBox != null && searchBox.Text.Length > 0) StartSearch(); } };
            searchBox = new TextBox { Width = 200, Padding = new Thickness(7, 4, 7, 4), ToolTip = "Search current folder and subfolders (Ctrl+F)" };
            searchBox.TextChanged += SearchBoxChanged;
            searchClearButton = ToolbarButton("×", "Clear search");
            searchClearButton.MinWidth = 28;
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
            locationBox = new TextBox { Visibility = Visibility.Collapsed, Padding = new Thickness(7, 4, 7, 4) };
            locationBox.KeyDown += LocationBoxKeyDown;
            pathHost.Children.Add(crumbScroll); pathHost.Children.Add(locationBox);
            top.Children.Add(pathHost);
            toolbar.Child = top; root.Children.Add(toolbar); Grid.SetRow(toolbar, 0);

            mainGrid = new Grid();
            // Sidebar and file view meet at the same visual boundary.  The resize hit target is
            // overlaid on the sidebar edge instead of consuming its own layout column, so there is
            // no empty strip between the sidebar and the TabControl/file-view frame.
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = settings.SidebarVisible ? new GridLength(settings.SidebarWidth) : new GridLength(0), MinWidth = settings.SidebarVisible ? 50 : 0, MaxWidth = settings.SidebarVisible ? 480 : double.PositiveInfinity });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            sidebarPanel = new StackPanel();
            ScrollViewer sideScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = sidebarPanel };
            sidebarBorder = new Border { BorderThickness = new Thickness(0), Child = sideScroll, Background = SystemColors.ControlLightBrush, AllowDrop = true };
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
            statusText = new TextBlock { Text = "Ready", VerticalAlignment = VerticalAlignment.Center };
            statusProgress = new ProgressBar
            {
                Width = 150,
                Height = 12,
                Margin = new Thickness(12, 0, 0, 0),
                IsIndeterminate = true,
                Visibility = Visibility.Collapsed,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.Gray,
                Background = SystemColors.ControlLightBrush
            };
            statusGrid.Children.Add(statusText); Grid.SetColumn(statusText, 0);
            statusGrid.Children.Add(statusProgress); Grid.SetColumn(statusProgress, 1);
            status.Child = statusGrid; root.Children.Add(status); Grid.SetRow(status, 2);
            return root;
        }

        private Button ToolbarButton(string content, string tooltip)
        {
            return new Button { Content = content, ToolTip = tooltip, MinWidth = 34, Padding = new Thickness(7, 4, 7, 4), Margin = new Thickness(2, 0, 2, 0) };
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
            FrameworkElementFactory wrap = new FrameworkElementFactory(typeof(VirtualizingWrapPanel)); wrap.SetValue(VirtualizingWrapPanel.IsItemsHostProperty, true); wrap.SetValue(VirtualizingWrapPanel.ItemWidthProperty, 164.0); wrap.SetValue(VirtualizingWrapPanel.ItemHeightProperty, settings.GridIconSize + 78.0);
            ctx.GridView.ItemsPanel = new ItemsPanelTemplate(wrap); ScrollViewer.SetCanContentScroll(ctx.GridView, true); VirtualizingPanel.SetIsVirtualizing(ctx.GridView, true); ctx.GridView.ItemTemplate = BuildGridTemplate();
            BuildListColumns(ctx, false);
            AttachViewEvents(ctx.ListView, state); AttachViewEvents(ctx.GridView, state);
            ctx.Container.Children.Add(ctx.ListView); ctx.Container.Children.Add(ctx.GridView);
            ctx.TabItem = new TabItem { Tag = state, Content = ctx.Container };
            SetTabHeader(ctx);
            ctx.RefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) }; ctx.RefreshTimer.Tick += delegate { ctx.RefreshTimer.Stop(); if (!state.IsSearching) SafeRefreshFolderIncremental(state, false); };
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
            view.MouseDoubleClick += delegate(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left && !IsInlineRenameEditorSource(e.OriginalSource as DependencyObject)) OpenSelected(state); };
            element.PreviewMouseRightButtonDown += delegate(object sender, MouseButtonEventArgs e) { HandleRightMouseDown((ItemsControl)view, state, e); };
            ((FrameworkElement)view).ContextMenuOpening += delegate(object sender, ContextMenuEventArgs e) { BuildAndAssignContextMenu((FrameworkElement)view, state, e); };
            element.PreviewMouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e) { HandleLeftMouseDown((ItemsControl)view, state, e); };
            element.PreviewMouseLeftButtonUp += delegate(object sender, MouseButtonEventArgs e) { HandleLeftMouseUp((Selector)view, state, e); };
            element.MouseMove += delegate(object sender, MouseEventArgs e) { HandleMouseMoveDrag((Selector)view, state, e); };
            ((UIElement)view).DragOver += delegate(object sender, DragEventArgs e) { ViewDragOver((ItemsControl)view, state, e); };
            ((UIElement)view).DragLeave += delegate(object sender, DragEventArgs e) { if (contexts.ContainsKey(state.Id)) ClearDropTargetHighlight(contexts[state.Id]); };
            ((UIElement)view).Drop += delegate(object sender, DragEventArgs e) { ViewDrop((ItemsControl)view, state, e); };
            ((UIElement)view).MouseDown += delegate(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Middle) { bool hitItem = SelectUnderMouse((ItemsControl)view, e.GetPosition((IInputElement)view)); if (!hitItem) { ClearSelection(state); e.Handled = true; return; } FileItem item = GetSingleSelected(state); if (item != null && item.IsDirectory) OpenNewTab(item.FullPath, true); e.Handled = true; } };
            ((Selector)view).SelectionChanged += delegate { TabViewContext c; if (contexts.TryGetValue(state.Id, out c) && c.IsReconciling) return; UpdateStatus(); };
        }

        private void HandleLeftMouseDown(ItemsControl view, TabState state, MouseButtonEventArgs e)
        {
            dragStart = e.GetPosition(this);
            if (e.ChangedButton != MouseButton.Left || state == null || !contexts.ContainsKey(state.Id)) return;

            bool hitItem = IsItemUnderMouse(view, e.GetPosition(view));
            bool chrome = IsViewChrome(e.OriginalSource as DependencyObject, view);
            TabViewContext ctx = contexts[state.Id];
            ctx.ItemDragArmed = false;
            if (IsInlineRenameEditorSource(e.OriginalSource as DependencyObject)) return;

            if (!hitItem && !chrome)
            {
                CancelPendingMultiSelectionGesture(ctx);
                ClearSelection(state);
                ((Control)view).Focus();
                e.Handled = true;
                return;
            }

            // File D&D is armed only by mouse-down on an item. Dragging true empty
            // background or view chrome must never start a file drag.
            ctx.ItemDragArmed = hitItem && !chrome;

            // Preserve a native Extended multi-selection long enough to distinguish a click
            // from a drag. WPF otherwise collapses the selection on mouse-down before
            // DragDrop.DoDragDrop can package the selected set.
            Control container = GetItemContainerUnderPoint(view, e.GetPosition(view));
            if (container != null && Keyboard.Modifiers == ModifierKeys.None &&
                IsContainerSelected(container) && GetSelectedCount((Selector)view) > 1)
            {
                ctx.PendingMultiSelectionClick = true;
                ctx.PendingMultiSelectionView = (Selector)view;
                ctx.PendingMultiSelectionItem = container.DataContext as FileItem;
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
                SetSingleSelection(view, ctx.PendingMultiSelectionItem);
            }
            CancelPendingMultiSelectionGesture(ctx);
            ctx.ItemDragArmed = false;
            UpdateStatus();
            e.Handled = true;
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
            DependencyObject hit = view.InputHitTest(position) as DependencyObject;
            while (hit != null && !(hit is ListViewItem) && !(hit is ListBoxItem)) hit = VisualTreeHelper.GetParent(hit);
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
            DependencyObject hit = view.InputHitTest(position) as DependencyObject;
            while (hit != null && hit != view)
            {
                if (hit is ListViewItem || hit is ListBoxItem) return true;
                hit = VisualTreeHelper.GetParent(hit);
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
                menu.Items.Add(Item("Compress to ZIP", delegate { CompressSelected(state, paths); }));
                if (single != null && ArchiveHelper.IsZip(single.FullPath))
                {
                    menu.Items.Add(Item("Extract Here", delegate { ExtractZip(state, single.FullPath, false); }));
                    menu.Items.Add(Item("Extract to \"" + Path.GetFileNameWithoutExtension(single.FullPath) + "\\\"", delegate { ExtractZip(state, single.FullPath, true); }));
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
            if (state != null && state.IsRecycleBin) return;
            if (e.LeftButton != MouseButtonState.Pressed) return;
            Point p = e.GetPosition(this);
            if (Math.Abs(p.X - dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(p.Y - dragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;

            TabViewContext ctx = state != null && contexts.ContainsKey(state.Id) ? contexts[state.Id] : null;
            if (ctx == null || !ctx.ItemDragArmed) return;

            List<string> paths = GetSelectedPaths(state);
            if (paths.Count == 0) return;

            if (ctx.PendingMultiSelectionClick && ctx.PendingMultiSelectionView == view)
                ctx.PendingMultiSelectionDragStarted = true;

            InvalidateRenameUndo();
            DataObject data = new DataObject(DataFormats.FileDrop, paths.ToArray());
            try
            {
                DragDrop.DoDragDrop((DependencyObject)view, data, DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link);
            }
            finally
            {
                CancelPendingMultiSelectionGesture(ctx);
                if (ctx != null) ctx.ItemDragArmed = false;
            }
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

        private Control GetItemContainerUnderPoint(ItemsControl view, Point point)
        {
            DependencyObject hit = view.InputHitTest(point) as DependencyObject;
            while (hit != null && hit != view && !(hit is ListViewItem) && !(hit is ListBoxItem)) hit = VisualTreeHelper.GetParent(hit);
            return hit as Control;
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
            DependencyObject hit = view.InputHitTest(point) as DependencyObject;
            while (hit != null && !(hit is ListViewItem) && !(hit is ListBoxItem)) hit = VisualTreeHelper.GetParent(hit);
            FrameworkElement fe = hit as FrameworkElement; return fe == null ? null : fe.DataContext as FileItem;
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
            if (TabState.IsRecycleBinPath(path)) { AddCrumb("Recycle Bin", TabState.RecycleBinPath); return; }
            try
            {
                string root = Path.GetPathRoot(path); string current = root; AddCrumb(root.TrimEnd('\\') + "\\", root);
                string remaining = path.Substring(root.Length).Trim('\\'); if (remaining.Length == 0) return; string[] parts = remaining.Split('\\');
                for (int i = 0; i < parts.Length; i++) { breadcrumbPanel.Children.Add(new TextBlock { Text = "  ›  ", VerticalAlignment = VerticalAlignment.Center }); current = Path.Combine(current, parts[i]); AddCrumb(parts[i], current); }
            }
            catch { AddCrumb(path, path); }
        }

        private void AddCrumb(string label, string path)
        {
            Button b = new Button { Content = label, Tag = path, Padding = new Thickness(5, 2, 5, 2), BorderThickness = new Thickness(0), Background = Brushes.Transparent }; b.Click += delegate { Navigate((string)b.Tag, true); }; breadcrumbPanel.Children.Add(b);
        }

        private void LocationBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { string path = Environment.ExpandEnvironmentVariables(locationBox.Text); if (Directory.Exists(path)) { locationBox.Visibility = Visibility.Collapsed; breadcrumbPanel.Parent.SetValue(UIElement.VisibilityProperty, Visibility.Visible); Navigate(path, true); } else MessageBox.Show(this, "Folder not found.\n\n" + path, "Ferry", MessageBoxButton.OK, MessageBoxImage.Information); e.Handled = true; }
            else if (e.Key == Key.Escape) { locationBox.Visibility = Visibility.Collapsed; breadcrumbPanel.Parent.SetValue(UIElement.VisibilityProperty, Visibility.Visible); e.Handled = true; }
        }

        private void ShowLocationBox()
        {
            TabState state = ActiveState; if (state == null || state.IsRecycleBin) return; FrameworkElement parent = breadcrumbPanel.Parent as FrameworkElement; if (parent != null) parent.Visibility = Visibility.Collapsed; locationBox.Text = state.CurrentPath; locationBox.Visibility = Visibility.Visible; locationBox.Focus(); locationBox.SelectAll();
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
            if (state == null || state.IsRecycleBin) return;
            try
            {
                InvalidateRenameUndo();
                bool completed = ClipboardHelper.Paste(state.CurrentPath);
                if (completed) ScheduleFolderRefresh(state);
            }
            catch (Exception ex)
            {
                Logger.Write("Paste failed: " + ex);
                MessageBox.Show(this, ex.Message, "Ferry", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private void CompressSelected(TabState state, IList<string> paths)
        {
            if (state == null || state.IsRecycleBin || paths == null || paths.Count == 0) return;
            InvalidateRenameUndo(); List<string> copy = new List<string>(); for (int i = 0; i < paths.Count; i++) copy.Add(paths[i]);
            string outputDirectory = state.CurrentPath;
            int activityId = BeginArchiveActivity("Compressing to ZIP…");
            Task.Run(delegate
            {
                try
                {
                    ArchiveHelper.CompressToZip(copy, outputDirectory);
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        EndArchiveActivity(activityId, "ZIP compression complete.");
                        if (!state.IsSearching) ScheduleFolderRefresh(state); else UpdateStatus();
                    }));
                }
                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        EndArchiveActivity(activityId, null);
                        MessageBox.Show(this, ex.Message, "Ferry", MessageBoxButton.OK, MessageBoxImage.Error);
                    }));
                }
            });
        }

        private void ExtractZip(TabState state, string zipPath, bool namedFolder)
        {
            if (state == null || state.IsRecycleBin || string.IsNullOrEmpty(zipPath)) return;
            InvalidateRenameUndo();
            int activityId = BeginArchiveActivity("Extracting ZIP…");
            Task.Run(delegate
            {
                try
                {
                    if (namedFolder) ArchiveHelper.ExtractToNamedFolder(zipPath); else ArchiveHelper.ExtractHere(zipPath);
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        EndArchiveActivity(activityId, "ZIP extraction complete.");
                        if (!state.IsSearching) ScheduleFolderRefresh(state); else UpdateStatus();
                    }));
                }
                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        EndArchiveActivity(activityId, null);
                        MessageBox.Show(this, ex.Message, "Ferry", MessageBoxButton.OK, MessageBoxImage.Error);
                    }));
                }
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
            currentViewMode = mode;
            bool grid = string.Equals(mode, "Grid", StringComparison.OrdinalIgnoreCase);
            foreach (TabViewContext ctx in contexts.Values)
            {
                ShowCurrentView(ctx);
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

        private int BeginArchiveActivity(string label)
        {
            int id = ++nextArchiveActivityId;
            archiveActivities[id] = string.IsNullOrEmpty(label) ? "Archive operation in progress…" : label;
            UpdateStatus();
            return id;
        }

        private void EndArchiveActivity(int id, string completionMessage)
        {
            archiveActivities.Remove(id);
            if (archiveActivities.Count == 0 && !string.IsNullOrEmpty(completionMessage))
            {
                ShowTimedStatusMessage(completionMessage, 3000);
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
            if (statusProgress != null) statusProgress.Visibility = Visibility.Collapsed;
            if (statusText != null) statusText.Text = transientStatusMessage;
            return true;
        }

        private bool ShowArchiveActivityStatus()
        {
            if (archiveActivities.Count == 0) return false;
            string label = null;
            foreach (KeyValuePair<int, string> pair in archiveActivities) label = pair.Value;
            if (archiveActivities.Count > 1) label = "Archive operations in progress… (" + archiveActivities.Count.ToString() + ")";
            if (statusText != null) statusText.Text = label ?? "Archive operation in progress…";
            if (statusProgress != null) statusProgress.Visibility = Visibility.Visible;
            return true;
        }

        private void SetStatusMessage(string message)
        {
            if (ShowArchiveActivityStatus()) return;
            if (ShowTransientStatusMessage()) return;
            if (statusProgress != null) statusProgress.Visibility = Visibility.Collapsed;
            if (statusText != null) statusText.Text = message ?? string.Empty;
        }

        private void UpdateStatus()
        {
            if (ShowArchiveActivityStatus()) return;
            if (ShowTransientStatusMessage()) return;
            if (statusProgress != null) statusProgress.Visibility = Visibility.Collapsed;
            TabState state = ActiveState;
            if (state == null) { if (statusText != null) statusText.Text = "Ready"; return; }
            List<string> selected = GetSelectedPaths(state);
            string prefix = state.IsSearching ? state.Items.Count.ToString("N0") + " results" : state.Items.Count.ToString("N0") + " items";
            if (selected.Count > 0) prefix += "   •   " + selected.Count.ToString("N0") + " selected";
            if (statusText != null) statusText.Text = prefix;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            ModifierKeys mods = Keyboard.Modifiers; TabState state = ActiveState; Key key = e.Key == Key.System ? e.SystemKey : e.Key;
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
            else if (mods == ModifierKeys.Control && key == Key.L) { ShowLocationBox(); e.Handled = true; }
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
                GridViewColumn c = ctx.ListGrid.Columns[i]; GridViewColumnHeader h = c.Header as GridViewColumnHeader; if (h == null || h.Tag == null) continue; string name = Convert.ToString(h.Tag); if (name == "Location" || name == "OriginalLocation") continue; order.Add(name); settings.ColumnWidths[name] = c.ActualWidth > 0 ? c.ActualWidth : c.Width;
            }
            // Preserve hidden columns in their old relative positions by appending them.
            string[] all = new string[] { "Name", "Items", "Type", "Size", "Modified", "Created" }; for (int i = 0; i < all.Length; i++) if (!order.Contains(all[i])) order.Add(all[i]); settings.ColumnOrder = order;
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CaptureColumnSettings(); if (WindowState == WindowState.Normal) { settings.WindowWidth = Width; settings.WindowHeight = Height; settings.WindowLeft = Left; settings.WindowTop = Top; } settings.WindowMaximized = WindowState == WindowState.Maximized; settings.SidebarWidth = mainGrid.ColumnDefinitions[0].ActualWidth > 0 ? mainGrid.ColumnDefinitions[0].ActualWidth : settings.SidebarWidth; settings.SearchMode = Convert.ToString(searchModeBox.SelectedItem); try { SettingsStore.Save(settings); } catch { }
            foreach (TabViewContext ctx in contexts.Values) { StopSearchDrain(ctx); CancelGridThumbnailLoad(ctx); ctx.State.CancelBackgroundWork(); if (ctx.Watcher != null) try { ctx.Watcher.Dispose(); } catch { } }
        }

        private sealed class PinnedSidebarItem
        {
            public PinnedSidebarItem(string name, string fullPath) { Name = name; FullPath = fullPath; }
            public string Name { get; private set; }
            public string FullPath { get; private set; }
        }

        private sealed class InverseBooleanToVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                return value is bool && (bool)value ? Visibility.Collapsed : Visibility.Visible;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class TabViewContext
        {
            public TabState State; public TabItem TabItem; public Grid Container; public ListView ListView; public ListBox GridView; public GridView ListGrid; public FileSystemWatcher Watcher; public DispatcherTimer RefreshTimer; public DispatcherTimer SearchDrainTimer; public CancellationTokenSource GridThumbnailCancellation;
            public bool IsReconciling;
            public List<FileItem> PreSearchItems; public HashSet<string> PreSearchSelection;
            public Control DropTargetContainer; public object DropTargetBackgroundLocal = DependencyProperty.UnsetValue; public object DropTargetBorderBrushLocal = DependencyProperty.UnsetValue; public object DropTargetBorderThicknessLocal = DependencyProperty.UnsetValue;
            public bool ItemDragArmed; public bool PendingMultiSelectionClick; public Selector PendingMultiSelectionView; public FileItem PendingMultiSelectionItem; public bool PendingMultiSelectionDragStarted;
        }
    }
}
