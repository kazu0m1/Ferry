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

    internal static class TodoStore
    {
        private static string TodoDirectory
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config"); }
        }

        public static string TodoPath
        {
            get { return Path.Combine(TodoDirectory, "todo.json"); }
        }

        public static List<TodoEntry> Load()
        {
            try
            {
                if (!File.Exists(TodoPath)) return new List<TodoEntry>();
                string json = File.ReadAllText(TodoPath, Encoding.UTF8);
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                List<TodoEntry> entries = serializer.Deserialize<List<TodoEntry>>(json);
                return entries ?? new List<TodoEntry>();
            }
            catch
            {
                return new List<TodoEntry>();
            }
        }

        public static void Save(IList<TodoEntry> entries)
        {
            Directory.CreateDirectory(TodoDirectory);
            List<TodoEntry> snapshot = new List<TodoEntry>();
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    TodoEntry source = entries[i];
                    if (source == null) continue;
                    snapshot.Add(new TodoEntry { Text = source.Text, Memo = source.Memo });
                }
            }

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string json = PrettyJson(serializer.Serialize(snapshot));
            string temp = TodoPath + ".tmp";
            File.WriteAllText(temp, json, new UTF8Encoding(false));

            if (File.Exists(TodoPath))
            {
                string backup = TodoPath + ".bak";
                try
                {
                    File.Replace(temp, TodoPath, backup, true);
                    if (File.Exists(backup)) File.Delete(backup);
                }
                catch
                {
                    File.Delete(TodoPath);
                    File.Move(temp, TodoPath);
                }
            }
            else
            {
                File.Move(temp, TodoPath);
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
                    output.Append(c);
                    output.AppendLine();
                    indent++;
                    output.Append(new string(' ', indent * 2));
                }
                else if (!quoted && (c == '}' || c == ']'))
                {
                    output.AppendLine();
                    indent--;
                    output.Append(new string(' ', indent * 2));
                    output.Append(c);
                }
                else if (!quoted && c == ',')
                {
                    output.Append(c);
                    output.AppendLine();
                    output.Append(new string(' ', indent * 2));
                }
                else if (!quoted && c == ':')
                {
                    output.Append(": ");
                }
                else
                {
                    output.Append(c);
                }

                escaped = c == '\\' && !escaped;
                if (c != '\\') escaped = false;
            }

            return output.ToString();
        }
    }
}
