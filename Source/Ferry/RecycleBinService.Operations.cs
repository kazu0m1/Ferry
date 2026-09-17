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
    internal static partial class RecycleBinService
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, uint dwFlags);

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
