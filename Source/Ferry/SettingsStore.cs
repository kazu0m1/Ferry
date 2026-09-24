using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace Ferry
{
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
            AppSettings settings = null;
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath, Encoding.UTF8);
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    settings = serializer.Deserialize<AppSettings>(json);
                }
            }
            catch
            {
                settings = null;
            }

            if (settings == null) settings = new AppSettings();
            Normalize(settings);
            MigrateLegacyTodo(settings);
            return settings;
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
            if (s.TodoEntries == null) s.TodoEntries = new List<TodoEntry>();
            if (s.WindowWidth < 640) s.WindowWidth = 1180;
            if (s.WindowHeight < 480) s.WindowHeight = 760;
            if (s.SidebarWidth < 50) s.SidebarWidth = 220;
            if (s.SidebarWidth > 480) s.SidebarWidth = 480;
            if (s.GridIconSize < 32) s.GridIconSize = 64;
            // Older settings files do not contain this property and deserialize it as 0.
            // Treat that as the v1.1 default; otherwise keep imported values inside the UI range.
            if (s.RubberBandAutoScrollSpeed <= 0) s.RubberBandAutoScrollSpeed = 100;
            else if (s.RubberBandAutoScrollSpeed < 30) s.RubberBandAutoScrollSpeed = 30;
            else if (s.RubberBandAutoScrollSpeed > 300) s.RubberBandAutoScrollSpeed = 300;
        }

        private static void MigrateLegacyTodo(AppSettings settings)
        {
            List<TodoEntry> legacy;
            if (!TodoStore.TryLoadLegacy(out legacy)) return;

            if ((settings.TodoEntries == null || settings.TodoEntries.Count == 0) && legacy != null && legacy.Count > 0)
                settings.TodoEntries = legacy;

            // Delete the old file only after the combined settings file has been written.
            // If writing fails, leave todo.json untouched so no user content is lost.
            try
            {
                Save(settings);
                TodoStore.DeleteLegacy();
            }
            catch
            {
            }
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
