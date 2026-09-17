using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Ferry
{
    internal static partial class RenameEngine
    {
        public const string OriginalToken = "[Original filename]";
        private static readonly char[] InvalidNameChars = new char[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };
        private static readonly HashSet<string> ReservedNames = BuildReservedNames();

        public static string NumberToken(int width)
        {
            if (width < 1) width = 1;
            string[] numbers = new string[] { 1.ToString("D" + width), 2.ToString("D" + width), 3.ToString("D" + width) };
            return "[" + numbers[0] + ", " + numbers[1] + ", " + numbers[2] + "]";
        }

        public static string BuildTemplateName(string template, string originalBaseName, int number)
        {
            string result = template ?? string.Empty;
            result = result.Replace(OriginalToken, originalBaseName ?? string.Empty);
            for (int width = 1; width <= 8; width++)
                result = result.Replace(NumberToken(width), number.ToString("D" + width, CultureInfo.InvariantCulture));
            return result;
        }

        public static string GetBaseName(string path)
        {
            if (Directory.Exists(path)) return Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            return Path.GetFileNameWithoutExtension(path);
        }

        public static string GetExtension(string path)
        {
            return Directory.Exists(path) ? string.Empty : Path.GetExtension(path);
        }

        public static string BuildTargetPath(string sourcePath, string newBaseName)
        {
            string directory = Path.GetDirectoryName(sourcePath);
            if (string.IsNullOrEmpty(directory)) directory = Environment.CurrentDirectory;
            return Path.Combine(directory, newBaseName + GetExtension(sourcePath));
        }

        public static string BuildTargetPathFullName(string sourcePath, string newName)
        {
            string directory = Path.GetDirectoryName(sourcePath);
            if (string.IsNullOrEmpty(directory)) directory = Environment.CurrentDirectory;
            return Path.Combine(directory, newName);
        }

        public static string ValidateBaseName(string baseName)
        {
            if (string.IsNullOrEmpty(baseName)) return "Name cannot be empty.";
            if (baseName.IndexOfAny(InvalidNameChars) >= 0) return "Contains a character that Windows does not allow in file names.";
            for (int i = 0; i < baseName.Length; i++) if (baseName[i] < 32) return "Contains a control character that Windows does not allow.";
            if (baseName.EndsWith(" ", StringComparison.Ordinal) || baseName.EndsWith(".", StringComparison.Ordinal)) return "Windows file names cannot end with a space or period.";
            string devicePart = baseName;
            int dot = devicePart.IndexOf('.'); if (dot >= 0) devicePart = devicePart.Substring(0, dot);
            if (ReservedNames.Contains(devicePart)) return "This is a reserved Windows device name.";
            return null;
        }

        public static string ValidateBatch(IList<RenameEntry> entries)
        {
            Dictionary<string, RenameEntry> targets = new Dictionary<string, RenameEntry>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> sources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < entries.Count; i++) sources.Add(Normalize(entries[i].SourcePath));
            string first = null;
            for (int i = 0; i < entries.Count; i++)
            {
                RenameEntry entry = entries[i]; entry.IsValid = true; entry.Status = string.Empty;
                string error = ValidateBaseName(entry.NewBaseName);
                if (error != null) { entry.IsValid = false; entry.Status = error; }
                else
                {
                    string target = Normalize(entry.TargetPath);
                    if (targets.ContainsKey(target))
                    {
                        entry.IsValid = false; entry.Status = "Another selected item would get the same name.";
                        targets[target].IsValid = false; targets[target].Status = entry.Status;
                    }
                    else targets.Add(target, entry);
                    if (entry.IsValid && !PathsEqual(entry.SourcePath, entry.TargetPath))
                    {
                        bool exists = File.Exists(entry.TargetPath) || Directory.Exists(entry.TargetPath);
                        if (exists && !sources.Contains(target)) { entry.IsValid = false; entry.Status = "A file or folder with this name already exists."; }
                    }
                }
                if (!entry.IsValid && first == null) first = entry.Status;
            }
            return first;
        }

        private static string Normalize(string path) { try { return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); } catch { return path; } }
        private static bool PathsEqual(string a, string b) { return string.Equals(Normalize(a), Normalize(b), StringComparison.OrdinalIgnoreCase); }

        private static HashSet<string> BuildReservedNames()
        {
            HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            set.Add("CON"); set.Add("PRN"); set.Add("AUX"); set.Add("NUL");
            for (int i = 1; i <= 9; i++) { set.Add("COM" + i); set.Add("LPT" + i); }
            return set;
        }
    }
}
