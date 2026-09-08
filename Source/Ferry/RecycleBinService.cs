using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace Ferry
{
    internal sealed class RecycleBinEntry
    {
        public string RecycledPath { get; set; }
        public string MetadataPath { get; set; }
        public string OriginalPath { get; set; }
        public string Name { get; set; }
        public bool IsDirectory { get; set; }
        public long SizeBytes { get; set; }
        public DateTime DeletedAt { get; set; }
    }

    internal static class RecycleBinService
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, uint dwFlags);

        public static IList<RecycleBinEntry> EnumerateCurrentUser()
        {
            List<RecycleBinEntry> result = new List<RecycleBinEntry>();
            string sid = null;
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                if (identity != null && identity.User != null) sid = identity.User.Value;
            }
            catch { }
            if (string.IsNullOrEmpty(sid)) return result;

            DriveInfo[] drives;
            try { drives = DriveInfo.GetDrives(); }
            catch { return result; }

            for (int d = 0; d < drives.Length; d++)
            {
                try
                {
                    DriveInfo drive = drives[d];
                    if (!drive.IsReady) continue;
                    if (drive.DriveType != DriveType.Fixed && drive.DriveType != DriveType.Removable) continue;
                    string userBin = Path.Combine(drive.RootDirectory.FullName, "$Recycle.Bin", sid);
                    if (!Directory.Exists(userBin)) continue;
                    foreach (string metadataPath in Directory.EnumerateFiles(userBin, "$I*", SearchOption.TopDirectoryOnly))
                    {
                        RecycleBinEntry entry = ReadEntry(metadataPath);
                        if (entry != null) result.Add(entry);
                    }
                }
                catch { }
            }
            result.Sort(delegate(RecycleBinEntry a, RecycleBinEntry b) { return b.DeletedAt.CompareTo(a.DeletedAt); });
            return result;
        }

        private static RecycleBinEntry ReadEntry(string metadataPath)
        {
            try
            {
                byte[] data = File.ReadAllBytes(metadataPath);
                if (data.Length < 24) return null;
                long version = BitConverter.ToInt64(data, 0);
                long size = BitConverter.ToInt64(data, 8);
                long fileTime = BitConverter.ToInt64(data, 16);
                string original = string.Empty;
                if (version >= 2 && data.Length >= 28)
                {
                    int chars = BitConverter.ToInt32(data, 24);
                    int available = data.Length - 28;
                    int requested = chars > 0 && chars <= 32768 ? chars * 2 : available;
                    int byteCount = Math.Min(available, Math.Max(0, requested));
                    original = Encoding.Unicode.GetString(data, 28, byteCount).TrimEnd('\0');
                    if (string.IsNullOrEmpty(original) && available > 0) original = Encoding.Unicode.GetString(data, 28, available).TrimEnd('\0');
                }
                else
                {
                    original = Encoding.Unicode.GetString(data, 24, data.Length - 24).TrimEnd('\0');
                }
                if (string.IsNullOrWhiteSpace(original)) return null;

                string fileName = Path.GetFileName(metadataPath);
                if (string.IsNullOrEmpty(fileName) || fileName.Length < 3) return null;
                string recycledName = "$R" + fileName.Substring(2);
                string recycledPath = Path.Combine(Path.GetDirectoryName(metadataPath), recycledName);
                bool isDirectory = Directory.Exists(recycledPath);
                if (!isDirectory && !File.Exists(recycledPath)) return null;

                DateTime deletedAt = DateTime.MinValue;
                try { deletedAt = DateTime.FromFileTimeUtc(fileTime).ToLocalTime(); } catch { }
                return new RecycleBinEntry
                {
                    RecycledPath = recycledPath,
                    MetadataPath = metadataPath,
                    OriginalPath = original,
                    Name = SafeFileName(original, recycledPath),
                    IsDirectory = isDirectory,
                    SizeBytes = size < 0 ? 0 : size,
                    DeletedAt = deletedAt
                };
            }
            catch { return null; }
        }

        private static string SafeFileName(string originalPath, string recycledPath)
        {
            try
            {
                string trimmed = originalPath.TrimEnd('\\', '/');
                string name = Path.GetFileName(trimmed);
                if (!string.IsNullOrEmpty(name)) return name;
            }
            catch { }
            return Path.GetFileName(recycledPath);
        }

        public static void Empty(Window owner)
        {
            IntPtr hwnd = owner == null ? IntPtr.Zero : new WindowInteropHelper(owner).Handle;
            int hr = SHEmptyRecycleBin(hwnd, null, 0); // Keep the standard Windows confirmation/UI.
            int cancelled = unchecked((int)0x800704C7);
            if (hr != 0 && hr != 1 && hr != cancelled) throw new IOException("Windows could not empty the Recycle Bin (0x" + hr.ToString("X8") + ").");
        }

        public static void Restore(IList<FileItem> items)
        {
            if (items == null) return;
            for (int i = 0; i < items.Count; i++)
            {
                FileItem item = items[i];
                if (item == null || !item.IsRecycleItem || string.IsNullOrEmpty(item.OriginalPath)) continue;
                if (File.Exists(item.OriginalPath) || Directory.Exists(item.OriginalPath))
                    throw new IOException("Cannot restore because the original path already exists:\n" + item.OriginalPath);
                string parent = Path.GetDirectoryName(item.OriginalPath);
                if (!string.IsNullOrEmpty(parent) && !Directory.Exists(parent)) Directory.CreateDirectory(parent);
                if (item.IsDirectory) Directory.Move(item.FullPath, item.OriginalPath);
                else File.Move(item.FullPath, item.OriginalPath);
                TryDeleteMetadata(item.RecycleMetadataPath);
            }
        }

        public static void DeletePermanently(IList<FileItem> items)
        {
            if (items == null) return;
            for (int i = 0; i < items.Count; i++)
            {
                FileItem item = items[i];
                if (item == null || !item.IsRecycleItem) continue;
                try
                {
                    if (item.IsDirectory && Directory.Exists(item.FullPath)) Directory.Delete(item.FullPath, true);
                    else if (File.Exists(item.FullPath)) File.Delete(item.FullPath);
                }
                finally { TryDeleteMetadata(item.RecycleMetadataPath); }
            }
        }

        private static void TryDeleteMetadata(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                if (File.Exists(path))
                {
                    try { File.SetAttributes(path, FileAttributes.Normal); } catch { }
                    File.Delete(path);
                }
            }
            catch { }
        }
    }
}
