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
    internal sealed partial class SettingsWindow : Window
    {
        private readonly AppSettings settings;
        private TextBox home;
        private ComboBox view;
        private CheckBox hidden;
        private CheckBox sidebar;
        private TextBox sidebarWidth;
        private CheckBox sortFoldersFirst;
        private Slider rubberBandAutoScrollSpeed;
        private TextBlock rubberBandAutoScrollSpeedValue;
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

            panel.Children.Add(Heading("Selection"));
            StackPanel autoScrollSpeedPanel = new StackPanel { Orientation = Orientation.Horizontal };
            rubberBandAutoScrollSpeed = new Slider
            {
                Minimum = 30,
                Maximum = 300,
                Value = settings.RubberBandAutoScrollSpeed,
                TickFrequency = 10,
                IsSnapToTickEnabled = true,
                Width = 320,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            rubberBandAutoScrollSpeedValue = new TextBlock
            {
                Text = Math.Round(settings.RubberBandAutoScrollSpeed).ToString(CultureInfo.InvariantCulture),
                Width = 54,
                Margin = new Thickness(12, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            rubberBandAutoScrollSpeed.ValueChanged += delegate
            {
                if (rubberBandAutoScrollSpeedValue != null)
                    rubberBandAutoScrollSpeedValue.Text = Math.Round(rubberBandAutoScrollSpeed.Value).ToString(CultureInfo.InvariantCulture);
            };
            autoScrollSpeedPanel.Children.Add(rubberBandAutoScrollSpeed);
            autoScrollSpeedPanel.Children.Add(rubberBandAutoScrollSpeedValue);
            panel.Children.Add(Labeled("Rubber-band autoscroll speed (30–300)", autoScrollSpeedPanel));
            panel.Children.Add(new TextBlock
            {
                Text = "Default: 100. Higher values scale both acceleration and maximum speed while a selection rectangle is dragged beyond the top or bottom edge.",
                Foreground = SystemColors.GrayTextBrush,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, -4, 0, 8)
            });

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
    }
}
