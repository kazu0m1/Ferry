using System;
using System.IO;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace Ferry
{
    internal sealed class SettingsWindow : Window
    {
        private readonly AppSettings settings;
        private TextBox home;
        private ComboBox view;
        private CheckBox hidden;
        private CheckBox sidebar;
        private TextBox sidebarWidth;
        private CheckBox sortFoldersFirst;
        private ComboBox search;
        private TextBox terminal;
        private TextBox terminalArgs;
        private CheckBox debug;
        public AppSettings Result { get; private set; }

        public SettingsWindow(Window owner, AppSettings current)
        {
            Owner = owner; Title = "Ferry Settings"; Width = 650; Height = 520; WindowStartupLocation = WindowStartupLocation.CenterOwner;
            settings = Clone(current); Content = BuildUi();
        }

        private UIElement BuildUi()
        {
            DockPanel root = new DockPanel { Margin = new Thickness(18) };
            StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
            Button import = MakeButton("Import…"); import.Click += ImportSettings; Button export = MakeButton("Export…"); export.Click += ExportSettings; Button reset = MakeButton("Reset"); reset.Click += ResetSettings;
            Button cancel = MakeButton("Cancel"); cancel.Click += delegate { DialogResult = false; }; Button save = MakeButton("Save"); save.IsDefault = true; save.Click += SaveSettings;
            buttons.Children.Add(import); buttons.Children.Add(export); buttons.Children.Add(reset); buttons.Children.Add(new Border { Width = 18 }); buttons.Children.Add(cancel); buttons.Children.Add(save); DockPanel.SetDock(buttons, Dock.Bottom); root.Children.Add(buttons);

            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            StackPanel panel = new StackPanel();
            panel.Children.Add(Heading("General"));
            Grid homeGrid = new Grid(); homeGrid.ColumnDefinitions.Add(new ColumnDefinition()); homeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            home = new TextBox { Text = settings.HomePath, Padding = new Thickness(7, 5, 7, 5) }; homeGrid.Children.Add(home);
            Button browse = new Button { Content = "Browse…", Margin = new Thickness(8, 0, 0, 0), Padding = new Thickness(10, 5, 10, 5) }; browse.Click += BrowseHome; homeGrid.Children.Add(browse); Grid.SetColumn(browse, 1);
            panel.Children.Add(Labeled("Home folder", homeGrid));
            view = new ComboBox { MinWidth = 160 }; view.Items.Add("List"); view.Items.Add("Grid"); view.SelectedItem = settings.DefaultView; panel.Children.Add(Labeled("Default view", view));
            hidden = new CheckBox { Content = "Show hidden items", IsChecked = settings.ShowHidden, Margin = new Thickness(0, 6, 0, 6) }; panel.Children.Add(hidden);
            sidebar = new CheckBox { Content = "Show sidebar", IsChecked = settings.SidebarVisible, Margin = new Thickness(0, 6, 0, 6) }; panel.Children.Add(sidebar);
            sidebarWidth = new TextBox { Text = Math.Round(settings.SidebarWidth).ToString(CultureInfo.InvariantCulture), Width = 90, Padding = new Thickness(7, 5, 7, 5), HorizontalAlignment = HorizontalAlignment.Left }; panel.Children.Add(Labeled("Sidebar width (50–480)", sidebarWidth));
            sortFoldersFirst = new CheckBox { Content = "Sort folders before files", IsChecked = settings.SortFoldersFirst, Margin = new Thickness(0, 6, 0, 6) }; panel.Children.Add(sortFoldersFirst);

            panel.Children.Add(Heading("Search")); search = new ComboBox { MinWidth = 160 }; search.Items.Add("Contains"); search.Items.Add("StartsWith"); search.SelectedItem = settings.SearchMode; panel.Children.Add(Labeled("Default match mode", search));
            panel.Children.Add(Heading("External terminal")); terminal = new TextBox { Text = settings.TerminalCommand, Padding = new Thickness(7, 5, 7, 5) }; panel.Children.Add(Labeled("Command (blank = Auto)", terminal)); terminalArgs = new TextBox { Text = settings.TerminalArguments, Padding = new Thickness(7, 5, 7, 5) }; panel.Children.Add(Labeled("Arguments (custom terminal only)", terminalArgs)); panel.Children.Add(new TextBlock { Text = "Auto tries Windows Terminal, then Windows PowerShell, then Command Prompt.", Foreground = SystemColors.GrayTextBrush, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) });
            panel.Children.Add(Heading("Troubleshooting")); debug = new CheckBox { Content = "Enable debug logging (OFF recommended)", IsChecked = settings.DebugLogging }; panel.Children.Add(debug);

            // Keep About visually separate from the troubleshooting/logging controls above it.
            panel.Children.Add(new Border { Height = 12 });
            panel.Children.Add(Heading("About Ferry"));
            StackPanel about = new StackPanel { Margin = new Thickness(0, 0, 0, 6) };
            about.Children.Add(new TextBlock { Text = "Ferry", FontSize = 15, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
            about.Children.Add(new TextBlock { Text = "Version " + GetDisplayVersion(), Margin = new Thickness(0, 0, 0, 3) });
            about.Children.Add(new TextBlock { Text = "Created by kazu0m1", Margin = new Thickness(0, 0, 0, 3) });
            about.Children.Add(new TextBlock { Text = "GitHub: github.com/kazu0m1/Ferry", Margin = new Thickness(0, 0, 0, 3) });
            about.Children.Add(new TextBlock { Text = "Licensed under the MIT License.", Foreground = SystemColors.GrayTextBrush });
            panel.Children.Add(about);

            scroll.Content = panel; root.Children.Add(scroll); return root;
        }

        private static string GetDisplayVersion()
        {
            try
            {
                object[] attrs = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
                if (attrs != null && attrs.Length > 0)
                {
                    AssemblyInformationalVersionAttribute info = attrs[0] as AssemblyInformationalVersionAttribute;
                    if (info != null && !string.IsNullOrWhiteSpace(info.InformationalVersion)) return info.InformationalVersion;
                }
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                if (version != null) return version.ToString();
            }
            catch { }
            return "Unknown";
        }

        private TextBlock Heading(string text) { return new TextBlock { Text = text, FontSize = 17, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 12, 0, 8) }; }
        private FrameworkElement Labeled(string label, UIElement element) { StackPanel p = new StackPanel { Margin = new Thickness(0, 3, 0, 9) }; p.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 5) }); p.Children.Add(element); return p; }
        private Button MakeButton(string text) { return new Button { Content = text, MinWidth = 82, Padding = new Thickness(10, 5, 10, 5), Margin = new Thickness(4, 0, 0, 0) }; }

        private void BrowseHome(object sender, RoutedEventArgs e)
        {
            using (Forms.FolderBrowserDialog dialog = new Forms.FolderBrowserDialog()) { dialog.SelectedPath = Directory.Exists(home.Text) ? home.Text : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); if (dialog.ShowDialog() == Forms.DialogResult.OK) home.Text = dialog.SelectedPath; }
        }
        private void SaveSettings(object sender, RoutedEventArgs e) { ApplyFields(settings); Result = settings; DialogResult = true; }
        private void ExportSettings(object sender, RoutedEventArgs e) { ApplyFields(settings); SaveFileDialog d = new SaveFileDialog { FileName = "FerrySettings.json", Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*" }; if (d.ShowDialog(this) == true) SettingsStore.Export(settings, d.FileName); }
        private void ImportSettings(object sender, RoutedEventArgs e) { OpenFileDialog d = new OpenFileDialog { Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*" }; if (d.ShowDialog(this) == true) { try { AppSettings imported = SettingsStore.Import(d.FileName); CopyInto(imported, settings); LoadFields(settings); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Ferry", MessageBoxButton.OK, MessageBoxImage.Error); } } }
        private void ResetSettings(object sender, RoutedEventArgs e) { CopyInto(new AppSettings(), settings); LoadFields(settings); }
        private void ApplyFields(AppSettings s) { s.HomePath = Directory.Exists(home.Text) ? home.Text : s.HomePath; s.DefaultView = Convert.ToString(view.SelectedItem); s.ShowHidden = hidden.IsChecked == true; s.SidebarVisible = sidebar.IsChecked == true; double width; if (double.TryParse(sidebarWidth.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out width) || double.TryParse(sidebarWidth.Text, out width)) s.SidebarWidth = Math.Max(50, Math.Min(480, width)); s.SortFoldersFirst = sortFoldersFirst.IsChecked == true; s.SearchMode = Convert.ToString(search.SelectedItem); s.TerminalCommand = terminal.Text; s.TerminalArguments = terminalArgs.Text; s.DebugLogging = debug.IsChecked == true; }
        private void LoadFields(AppSettings s) { home.Text = s.HomePath; view.SelectedItem = s.DefaultView; hidden.IsChecked = s.ShowHidden; sidebar.IsChecked = s.SidebarVisible; sidebarWidth.Text = Math.Round(s.SidebarWidth).ToString(CultureInfo.InvariantCulture); sortFoldersFirst.IsChecked = s.SortFoldersFirst; search.SelectedItem = s.SearchMode; terminal.Text = s.TerminalCommand; terminalArgs.Text = s.TerminalArguments; debug.IsChecked = s.DebugLogging; }
        private static AppSettings Clone(AppSettings s) { AppSettings n = new AppSettings(); CopyInto(s, n); return n; }
        private static void CopyInto(AppSettings a, AppSettings b)
        {
            b.HomePath=a.HomePath; b.DefaultView=a.DefaultView; b.ShowHidden=a.ShowHidden; b.SortKey=a.SortKey; b.SortDescending=a.SortDescending; b.SortFoldersFirst=a.SortFoldersFirst; b.SearchMode=a.SearchMode; b.WindowWidth=a.WindowWidth; b.WindowHeight=a.WindowHeight; b.WindowLeft=a.WindowLeft; b.WindowTop=a.WindowTop; b.WindowMaximized=a.WindowMaximized; b.SidebarWidth=a.SidebarWidth; b.SidebarVisible=a.SidebarVisible; b.GridIconSize=a.GridIconSize; b.TerminalCommand=a.TerminalCommand; b.TerminalArguments=a.TerminalArguments; b.DebugLogging=a.DebugLogging;
            b.PinnedFolders = new System.Collections.Generic.List<string>(a.PinnedFolders); b.ColumnWidths = new System.Collections.Generic.Dictionary<string,double>(a.ColumnWidths, StringComparer.OrdinalIgnoreCase); b.ColumnOrder = new System.Collections.Generic.List<string>(a.ColumnOrder); b.HiddenColumns = new System.Collections.Generic.List<string>(a.HiddenColumns);
        }
    }
}
