using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Ferry
{
    internal static class ArchiveHelper
    {
        public static bool IsZip(string path)
        {
            return File.Exists(path) && string.Equals(Path.GetExtension(path), ".zip", StringComparison.OrdinalIgnoreCase);
        }

        public static string CompressToZip(IList<string> paths, string outputDirectory)
        {
            if (paths == null || paths.Count == 0) throw new ArgumentException("No items were selected.");
            if (string.IsNullOrEmpty(outputDirectory) || !Directory.Exists(outputDirectory)) throw new DirectoryNotFoundException("The destination folder does not exist.");

            HashSet<string> rootNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < paths.Count; i++)
            {
                string path = paths[i];
                if (!File.Exists(path) && !Directory.Exists(path)) throw new FileNotFoundException("An item no longer exists.", path);
                string name = GetLeafName(path);
                if (!rootNames.Add(name)) throw new IOException("The selected items contain duplicate names from different folders. Compress them separately to avoid ambiguous ZIP entries.");
            }

            string archiveBase;
            if (paths.Count == 1)
            {
                string leaf = GetLeafName(paths[0]);
                archiveBase = Directory.Exists(paths[0]) ? leaf : Path.GetFileNameWithoutExtension(leaf);
                if (string.IsNullOrEmpty(archiveBase)) archiveBase = "Archive";
            }
            else archiveBase = "Archive";

            string archivePath = UniqueFilePath(Path.Combine(outputDirectory, archiveBase + ".zip"));
            StringBuilder args = new StringBuilder();
            args.Append("-a -c -f ").Append(Quote(archivePath));
            for (int i = 0; i < paths.Count; i++)
            {
                string full = Path.GetFullPath(paths[i]);
                string parent = Path.GetDirectoryName(full);
                if (string.IsNullOrEmpty(parent)) parent = outputDirectory;
                string leaf = GetLeafName(full);
                args.Append(" -C ").Append(Quote(parent)).Append(" ").Append(Quote(".\\" + leaf));
            }

            RunTar(args.ToString());
            if (!File.Exists(archivePath)) throw new IOException("Windows archive tool completed without creating the ZIP file.");
            return archivePath;
        }

        public static void ExtractHere(string zipPath)
        {
            if (!IsZip(zipPath)) throw new InvalidDataException("Extract is currently supported for ZIP files.");
            string parent = Path.GetDirectoryName(zipPath);
            if (string.IsNullOrEmpty(parent)) parent = Environment.CurrentDirectory;
            ExtractTo(zipPath, parent);
        }

        public static string ExtractToNamedFolder(string zipPath)
        {
            if (!IsZip(zipPath)) throw new InvalidDataException("Extract is currently supported for ZIP files.");
            string parent = Path.GetDirectoryName(zipPath);
            if (string.IsNullOrEmpty(parent)) parent = Environment.CurrentDirectory;
            string name = Path.GetFileNameWithoutExtension(zipPath);
            if (string.IsNullOrEmpty(name)) name = "Extracted";
            string destination = UniqueDirectoryPath(Path.Combine(parent, name));
            Directory.CreateDirectory(destination);
            try
            {
                ExtractTo(zipPath, destination);
                return destination;
            }
            catch
            {
                try { if (Directory.Exists(destination) && Directory.GetFileSystemEntries(destination).Length == 0) Directory.Delete(destination); } catch { }
                throw;
            }
        }

        private static void ExtractTo(string zipPath, string destination)
        {
            Directory.CreateDirectory(destination);
            string args = "-x -f " + Quote(Path.GetFullPath(zipPath)) + " -C " + Quote(Path.GetFullPath(destination));
            RunTar(args);
        }

        private static void RunTar(string arguments)
        {
            string exe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "tar.exe");
            if (!File.Exists(exe)) exe = "tar.exe";
            ProcessStartInfo psi = new ProcessStartInfo(exe, arguments);
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardError = true;
            using (Process p = Process.Start(psi))
            {
                string stderr = p.StandardError.ReadToEnd();
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    if (string.IsNullOrWhiteSpace(stderr)) stderr = "Windows archive tool failed with exit code " + p.ExitCode + ".";
                    throw new IOException(stderr.Trim());
                }
            }
        }

        private static string GetLeafName(string path)
        {
            string trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return Path.GetFileName(trimmed);
        }

        private static string UniqueFilePath(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path)) return path;
            string dir = Path.GetDirectoryName(path); if (string.IsNullOrEmpty(dir)) dir = Environment.CurrentDirectory;
            string name = Path.GetFileNameWithoutExtension(path); string ext = Path.GetExtension(path);
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

        private static string Quote(string value)
        {
            if (value == null) value = string.Empty;
            StringBuilder result = new StringBuilder(); result.Append('"');
            int slashes = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '\\') { slashes++; continue; }
                if (c == '"')
                {
                    result.Append('\\', slashes * 2 + 1); result.Append('"'); slashes = 0; continue;
                }
                if (slashes > 0) { result.Append('\\', slashes); slashes = 0; }
                result.Append(c);
            }
            if (slashes > 0) result.Append('\\', slashes * 2);
            result.Append('"'); return result.ToString();
        }
    }
}
