using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace Ferry
{
    internal sealed class TodoEntry : INotifyPropertyChanged
    {
        private int number;
        private string text;
        private string memo;

        [ScriptIgnore]
        public int Number
        {
            get { return number; }
            set
            {
                if (number == value) return;
                number = value;
                OnPropertyChanged("Number");
            }
        }

        public string Text
        {
            get { return text ?? string.Empty; }
            set
            {
                string next = value ?? string.Empty;
                if (string.Equals(text, next, StringComparison.Ordinal)) return;
                text = next;
                OnPropertyChanged("Text");
            }
        }

        public string Memo
        {
            get { return memo ?? string.Empty; }
            set
            {
                string next = value ?? string.Empty;
                if (string.Equals(memo, next, StringComparison.Ordinal)) return;
                memo = next;
                OnPropertyChanged("Memo");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public TodoEntry()
        {
            text = string.Empty;
            memo = string.Empty;
        }

        private void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(name));
        }
    }

    // Temporary compatibility helper for prototype builds that previously stored
    // To-Do data in config\todo.json. New builds persist To-Do inside settings.json.
    internal static class TodoStore
    {
        public static string LegacyTodoPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "todo.json"); }
        }

        public static bool TryLoadLegacy(out List<TodoEntry> entries)
        {
            entries = null;
            try
            {
                if (!File.Exists(LegacyTodoPath)) return false;
                string json = File.ReadAllText(LegacyTodoPath, Encoding.UTF8);
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                entries = serializer.Deserialize<List<TodoEntry>>(json) ?? new List<TodoEntry>();
                return true;
            }
            catch
            {
                entries = null;
                return false;
            }
        }

        public static void DeleteLegacy()
        {
            try
            {
                if (File.Exists(LegacyTodoPath)) File.Delete(LegacyTodoPath);
            }
            catch { }
        }
    }
}
