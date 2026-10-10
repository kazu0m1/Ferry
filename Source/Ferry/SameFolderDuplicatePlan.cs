using System;
using System.Collections.Generic;
using System.IO;

namespace Ferry
{
    // Only plans new sibling names. Windows Shell remains responsible for copying all content.
    internal static class SameFolderDuplicatePlan
    {
        public static bool IsSameFolderSource(string source, string destination)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(destination)) return false;
            try
            {
                string parent = Path.GetDirectoryName(Path.GetFullPath(source).TrimEnd('\\', '/'));
                return !string.IsNullOrEmpty(parent) && SamePath(parent, destination);
            }
            catch { return false; }
        }

        private static bool SamePath(string a, string b)
        {
            return string.Equals(Path.GetFullPath(a).TrimEnd('\\', '/'),
                Path.GetFullPath(b).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);
        }

        public static List<string> CreateTargets(string destination, IList<string> sources)
        {
            if (string.IsNullOrEmpty(destination) || sources == null || sources.Count == 0)
                throw new ArgumentException("No items to duplicate.");

            // Reserve every existing name across BOTH files and directories and then reserve
            // each planned target. This keeps simultaneous duplicates collision-free.
            HashSet<string> reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in Directory.EnumerateFileSystemEntries(destination))
                reserved.Add(Path.GetFileName(path.TrimEnd('\\', '/')));

            List<string> targets = new List<string>();
            foreach (string source in sources)
            {
                if (!IsSameFolderSource(source, destination))
                    throw new IOException("An item is not in the destination folder: " + source);

                bool isDirectory = Directory.Exists(source);
                if (!isDirectory && !File.Exists(source))
                    throw new FileNotFoundException("The item to duplicate no longer exists.", source);

                string name = Path.GetFileName(source.TrimEnd('\\', '/'));
                string extension = isDirectory ? string.Empty : Path.GetExtension(name);
                string stem = isDirectory ? name : Path.GetFileNameWithoutExtension(name);
                if (stem.Length == 0) { stem = name; extension = string.Empty; }

                bool found = false;
                for (int number = 1; number < 100000; number++)
                {
                    string candidateName = stem + " (" + number + ")" + extension;
                    if (reserved.Contains(candidateName)) continue;
                    string candidatePath = Path.Combine(destination, candidateName);
                    // Recheck filesystem as well: another process could add an item during planning.
                    if (File.Exists(candidatePath) || Directory.Exists(candidatePath)) continue;
                    reserved.Add(candidateName);
                    targets.Add(candidatePath);
                    found = true;
                    break;
                }
                if (!found) throw new IOException("Could not find an unused duplicate name for " + name);
            }
            return targets;
        }
    }
}
