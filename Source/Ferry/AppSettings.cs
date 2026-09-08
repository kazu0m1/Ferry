using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace Ferry
{
    internal sealed class AppSettings
    {
        public string HomePath { get; set; }
        public string DefaultView { get; set; }
        public bool ShowHidden { get; set; }
        public string SortKey { get; set; }
        public bool SortDescending { get; set; }
        public bool SortFoldersFirst { get; set; }
        public string SearchMode { get; set; }
        public double WindowWidth { get; set; }
        public double WindowHeight { get; set; }
        public double WindowLeft { get; set; }
        public double WindowTop { get; set; }
        public bool WindowMaximized { get; set; }
        public double SidebarWidth { get; set; }
        public bool SidebarVisible { get; set; }
        public double GridIconSize { get; set; }
        public string TerminalCommand { get; set; }
        public string TerminalArguments { get; set; }
        public List<string> PinnedFolders { get; set; }
        public Dictionary<string, double> ColumnWidths { get; set; }
        public List<string> ColumnOrder { get; set; }
        public List<string> HiddenColumns { get; set; }
        public bool DebugLogging { get; set; }

        public AppSettings()
        {
            HomePath = KnownFolders.Home;
            DefaultView = "List";
            ShowHidden = false;
            SortKey = "Name";
            SortDescending = false;
            SortFoldersFirst = true;
            SearchMode = "Contains";
            WindowWidth = 1180;
            WindowHeight = 760;
            WindowLeft = -1;
            WindowTop = -1;
            WindowMaximized = false;
            SidebarWidth = 220;
            SidebarVisible = true;
            GridIconSize = 64;
            TerminalCommand = string.Empty;
            TerminalArguments = string.Empty;
            PinnedFolders = new List<string>();
            ColumnWidths = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            ColumnWidths["Name"] = 420;
            ColumnWidths["Items"] = 90;
            ColumnWidths["Type"] = 160;
            ColumnWidths["Size"] = 110;
            ColumnWidths["Modified"] = 160;
            ColumnWidths["Created"] = 160;
            ColumnOrder = new List<string>(new string[] { "Name", "Items", "Type", "Size", "Modified", "Created" });
            HiddenColumns = new List<string>(new string[] { "Created" });
            DebugLogging = false;
        }
    }

    internal static class SettingsStore
    {
        private static string SettingsDirectory
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config"); }
        }

        public static string SettingsPath
        {
            get { return Path.Combine(SettingsDirectory, "settings.json"); }
        }

        public static AppSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return new AppSettings();
                string json = File.ReadAllText(SettingsPath, Encoding.UTF8);
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                AppSettings settings = serializer.Deserialize<AppSettings>(json);
                Normalize(settings);
                return settings;
            }
            catch
            {
                return new AppSettings();
            }
        }

        public static void Save(AppSettings settings)
        {
            Normalize(settings);
            Directory.CreateDirectory(SettingsDirectory);
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(settings);
            string temp = SettingsPath + ".tmp";
            File.WriteAllText(temp, PrettyJson(json), new UTF8Encoding(false));
            if (File.Exists(SettingsPath))
            {
                string backup = SettingsPath + ".bak";
                try { File.Replace(temp, SettingsPath, backup, true); if (File.Exists(backup)) File.Delete(backup); }
                catch { File.Delete(SettingsPath); File.Move(temp, SettingsPath); }
            }
            else
            {
                File.Move(temp, SettingsPath);
            }
        }

        public static void Export(AppSettings settings, string target)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            File.WriteAllText(target, PrettyJson(serializer.Serialize(settings)), new UTF8Encoding(false));
        }

        public static AppSettings Import(string source)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            AppSettings settings = serializer.Deserialize<AppSettings>(File.ReadAllText(source, Encoding.UTF8));
            Normalize(settings);
            return settings;
        }

        private static void Normalize(AppSettings s)
        {
            if (s == null) return;
            if (string.IsNullOrEmpty(s.HomePath)) s.HomePath = KnownFolders.Home;
            if (string.IsNullOrEmpty(s.DefaultView)) s.DefaultView = "List";
            if (string.IsNullOrEmpty(s.SortKey)) s.SortKey = "Name";
            if (string.IsNullOrEmpty(s.SearchMode)) s.SearchMode = "Contains";
            if (s.PinnedFolders == null) s.PinnedFolders = new List<string>();
            if (s.ColumnWidths == null) s.ColumnWidths = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            if (s.ColumnOrder == null) s.ColumnOrder = new List<string>(new string[] { "Name", "Items", "Type", "Size", "Modified", "Created" });
            if (s.HiddenColumns == null) s.HiddenColumns = new List<string>(new string[] { "Created" });
            if (s.WindowWidth < 640) s.WindowWidth = 1180;
            if (s.WindowHeight < 480) s.WindowHeight = 760;
            if (s.SidebarWidth < 120) s.SidebarWidth = 220;
            if (s.GridIconSize < 32) s.GridIconSize = 64;
        }

        private static string PrettyJson(string json)
        {
            StringBuilder output = new StringBuilder();
            bool quoted = false;
            bool escaped = false;
            int indent = 0;
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                if (c == '"' && !escaped) quoted = !quoted;
                if (!quoted && (c == '{' || c == '['))
                {
                    output.Append(c); output.AppendLine(); indent++; output.Append(new string(' ', indent * 2));
                }
                else if (!quoted && (c == '}' || c == ']'))
                {
                    output.AppendLine(); indent--; output.Append(new string(' ', indent * 2)); output.Append(c);
                }
                else if (!quoted && c == ',')
                {
                    output.Append(c); output.AppendLine(); output.Append(new string(' ', indent * 2));
                }
                else if (!quoted && c == ':') output.Append(": ");
                else output.Append(c);
                escaped = c == '\\' && !escaped;
                if (c != '\\') escaped = false;
            }
            return output.ToString();
        }
    }
}
