using System;
using System.Runtime.InteropServices;

namespace Ferry
{
    internal static class KnownFolders
    {
        private static readonly Guid DownloadsId = new Guid("374DE290-123F-4565-9164-39C4925E467B");

        [DllImport("shell32.dll")]
        private static extern int SHGetKnownFolderPath(ref Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);

        public static string Home
        {
            get
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (!string.IsNullOrEmpty(path)) return path;
                return Environment.GetEnvironmentVariable("USERPROFILE") ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            }
        }

        public static string Downloads
        {
            get
            {
                IntPtr ptr = IntPtr.Zero;
                try
                {
                    Guid id = DownloadsId;
                    int hr = SHGetKnownFolderPath(ref id, 0, IntPtr.Zero, out ptr);
                    if (hr == 0 && ptr != IntPtr.Zero)
                    {
                        string path = Marshal.PtrToStringUni(ptr);
                        if (!string.IsNullOrEmpty(path)) return path;
                    }
                }
                catch { }
                finally { if (ptr != IntPtr.Zero) Marshal.FreeCoTaskMem(ptr); }
                return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
            }
        }
    }
}
