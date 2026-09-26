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
    internal sealed partial class SettingsWindow
    {
        private void SaveSettings(object sender, RoutedEventArgs e) { ApplyFields(settings); Result = settings; DialogResult = true; }
        private void ExportSettings(object sender, RoutedEventArgs e) { ApplyFields(settings); SaveFileDialog d = new SaveFileDialog { FileName = "FerrySettings.json", Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*" }; if (d.ShowDialog(this) == true) SettingsStore.Export(settings, d.FileName); }
        private void ImportSettings(object sender, RoutedEventArgs e) { OpenFileDialog d = new OpenFileDialog { Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*" }; if (d.ShowDialog(this) == true) { try { AppSettings imported = SettingsStore.Import(d.FileName); CopyInto(imported, settings); LoadFields(settings); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Ferry", MessageBoxButton.OK, MessageBoxImage.Error); } } }
        private void ResetSettings(object sender, RoutedEventArgs e)
        {
            System.Collections.Generic.List<TodoEntry> todo = CloneTodoEntries(settings.TodoEntries);
            CopyInto(new AppSettings(), settings);
            settings.TodoEntries = todo;
            LoadFields(settings);
        }
        private void ApplyFields(AppSettings s) { s.HomePath = Directory.Exists(home.Text) ? home.Text : s.HomePath; s.DefaultView = Convert.ToString(view.SelectedItem); s.ShowHidden = hidden.IsChecked == true; s.SidebarVisible = sidebar.IsChecked == true; double width; if (double.TryParse(sidebarWidth.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out width) || double.TryParse(sidebarWidth.Text, out width)) s.SidebarWidth = Math.Max(50, Math.Min(480, width)); s.SortFoldersFirst = sortFoldersFirst.IsChecked == true; s.RubberBandAutoScrollSpeed = Math.Max(30, Math.Min(300, Math.Round(rubberBandAutoScrollSpeed.Value))); s.SearchMode = Convert.ToString(search.SelectedItem); s.TerminalCommand = terminal.Text; s.TerminalArguments = terminalArgs.Text; s.DebugLogging = debug.IsChecked == true; }
        private void LoadFields(AppSettings s) { home.Text = s.HomePath; view.SelectedItem = s.DefaultView; hidden.IsChecked = s.ShowHidden; sidebar.IsChecked = s.SidebarVisible; sidebarWidth.Text = Math.Round(s.SidebarWidth).ToString(CultureInfo.InvariantCulture); sortFoldersFirst.IsChecked = s.SortFoldersFirst; rubberBandAutoScrollSpeed.Value = Math.Max(30, Math.Min(300, s.RubberBandAutoScrollSpeed)); rubberBandAutoScrollSpeedValue.Text = Math.Round(rubberBandAutoScrollSpeed.Value).ToString(CultureInfo.InvariantCulture); search.SelectedItem = s.SearchMode; terminal.Text = s.TerminalCommand; terminalArgs.Text = s.TerminalArguments; debug.IsChecked = s.DebugLogging; }
        private static AppSettings Clone(AppSettings s) { AppSettings n = new AppSettings(); CopyInto(s, n); return n; }
        private static void CopyInto(AppSettings a, AppSettings b)
        {
            b.HomePath=a.HomePath; b.DefaultView=a.DefaultView; b.ShowHidden=a.ShowHidden; b.SortKey=a.SortKey; b.SortDescending=a.SortDescending; b.SortFoldersFirst=a.SortFoldersFirst; b.SearchMode=a.SearchMode; b.WindowWidth=a.WindowWidth; b.WindowHeight=a.WindowHeight; b.WindowLeft=a.WindowLeft; b.WindowTop=a.WindowTop; b.WindowMaximized=a.WindowMaximized; b.SidebarWidth=a.SidebarWidth; b.SidebarVisible=a.SidebarVisible; b.GridIconSize=a.GridIconSize; b.RubberBandAutoScrollSpeed=a.RubberBandAutoScrollSpeed; b.TerminalCommand=a.TerminalCommand; b.TerminalArguments=a.TerminalArguments; b.DebugLogging=a.DebugLogging;
            b.PinnedFolders = new System.Collections.Generic.List<string>(a.PinnedFolders); b.ColumnWidths = new System.Collections.Generic.Dictionary<string,double>(a.ColumnWidths, StringComparer.OrdinalIgnoreCase); b.ColumnOrder = new System.Collections.Generic.List<string>(a.ColumnOrder); b.HiddenColumns = new System.Collections.Generic.List<string>(a.HiddenColumns);
            b.TodoEntries = CloneTodoEntries(a.TodoEntries);
        }

        private static System.Collections.Generic.List<TodoEntry> CloneTodoEntries(System.Collections.Generic.IList<TodoEntry> source)
        {
            System.Collections.Generic.List<TodoEntry> result = new System.Collections.Generic.List<TodoEntry>();
            if (source == null) return result;
            for (int i = 0; i < source.Count; i++)
            {
                TodoEntry item = source[i];
                if (item != null) result.Add(new TodoEntry { Text = item.Text, Memo = item.Memo });
            }
            return result;
        }
    }
}
