using System;
using System.Collections.Generic;
using System.IO;

namespace Ferry
{
    internal static class ArchiveHelper
    {
        public static bool IsZip(string path)
        {
            return File.Exists(path) && string.Equals(Path.GetExtension(path), ".zip", StringComparison.OrdinalIgnoreCase);
        }

        public static string SuggestZipPath(IList<string> paths, string outputDirectory)
        {
            if (paths == null || paths.Count == 0) throw new ArgumentException("No items were selected.");
            if (string.IsNullOrEmpty(outputDirectory)) outputDirectory = Environment.CurrentDirectory;

            string archiveBase;
            if (paths.Count == 1)
            {
                string leaf = GetLeafName(paths[0]);
                archiveBase = Directory.Exists(paths[0]) ? leaf : Path.GetFileNameWithoutExtension(leaf);
                if (string.IsNullOrEmpty(archiveBase)) archiveBase = "Archive";
            }
            else
            {
                archiveBase = "Archive";
            }

            return UniqueFilePath(Path.Combine(outputDirectory, archiveBase + ".zip"));
        }

        public static string SuggestExtractionDirectory(string zipPath, bool namedFolder)
        {
            string parent = Path.GetDirectoryName(zipPath);
            if (string.IsNullOrEmpty(parent)) parent = Environment.CurrentDirectory;
            if (!namedFolder) return parent;

            string name = Path.GetFileNameWithoutExtension(zipPath);
            if (string.IsNullOrEmpty(name)) name = "Extracted";
            return UniqueDirectoryPath(Path.Combine(parent, name));
        }

        private static string GetLeafName(string path)
        {
            string trimmed = (path ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return Path.GetFileName(trimmed);
        }

        private static string UniqueFilePath(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path)) return path;
            string dir = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dir)) dir = Environment.CurrentDirectory;
            string name = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);
            for (int i = 2; i < 10000; i++)
            {
                string candidate = Path.Combine(dir, name + " (" + i + ")" + ext);
                if (!File.Exists(candidate) && !Directory.Exists(candidate)) return candidate;
            }
            throw new IOException("Could not choose an unused archive name.");
        }

        private static string UniqueDirectoryPath(string path)
        {
            if (!Directory.Exists(path) && !File.Exists(path)) return path;
            for (int i = 2; i < 10000; i++)
            {
                string candidate = path + " (" + i + ")";
                if (!Directory.Exists(candidate) && !File.Exists(candidate)) return candidate;
            }
            throw new IOException("Could not choose an unused extraction folder name.");
        }
    }
}
