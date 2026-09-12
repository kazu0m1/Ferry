using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Ferry
{
    internal static class ShellInterop
    {
        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_TYPENAME = 0x000000400;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        private const uint SHGFI_SMALLICON = 0x000000001;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        private const int SEE_MASK_INVOKEIDLIST = 0x0000000C;
        private const int SW_SHOWNORMAL = 1;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpVerb;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpFile;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpParameters;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct OPENASINFO
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string pcszFile;
            [MarshalAs(UnmanagedType.LPWStr)] public string pcszClass;
            public uint oaifInFlags;
        }

        [ComImport]
        [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItemImageFactory
        {
            void GetImage(SIZE size, SIIGBF flags, out IntPtr phbm);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE { public int cx; public int cy; public SIZE(int x, int y) { cx = x; cy = y; } }

        [Flags]
        private enum SIIGBF
        {
            RESIZETOFIT = 0x00,
            BIGGERSIZEOK = 0x01,
            MEMORYONLY = 0x02,
            ICONONLY = 0x04,
            THUMBNAILONLY = 0x08,
            INCACHEONLY = 0x10
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateItemFromParsingName(string pszPath, IntPtr pbc, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHOpenWithDialog(IntPtr hwndParent, ref OPENASINFO poainfo);

        public static string GetTypeName(string path, bool isDirectory)
        {
            try
            {
                SHFILEINFO info = new SHFILEINFO();
                uint attr = isDirectory ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL;
                uint flags = SHGFI_TYPENAME;
                if (!File.Exists(path) && !Directory.Exists(path)) flags |= SHGFI_USEFILEATTRIBUTES;
                SHGetFileInfo(path, attr, ref info, (uint)Marshal.SizeOf(typeof(SHFILEINFO)), flags);
                if (!string.IsNullOrEmpty(info.szTypeName)) return info.szTypeName;
            }
            catch { }
            return isDirectory ? "File folder" : "File";
        }

        public static string GetTypeNameFast(string path, bool isDirectory)
        {
            try
            {
                SHFILEINFO info = new SHFILEINFO();
                uint attr = isDirectory ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL;
                SHGetFileInfo(path, attr, ref info, (uint)Marshal.SizeOf(typeof(SHFILEINFO)), SHGFI_TYPENAME | SHGFI_USEFILEATTRIBUTES);
                if (!string.IsNullOrEmpty(info.szTypeName)) return info.szTypeName;
            }
            catch { }
            return isDirectory ? "File folder" : "File";
        }

        public static ImageSource GetSmallTypeIcon(string path, bool isDirectory)
        {
            // List view intentionally asks Shell for the registered type icon only.
            // SHGFI_USEFILEATTRIBUTES avoids touching the actual file or invoking thumbnail
            // providers, so a folder full of PDFs cannot contend with Open/Enter operations.
            return TryGetSmallIcon(path, isDirectory, true);
        }

        public static ImageSource GetSmallIcon(string path, bool isDirectory)
        {
            ImageSource icon = TryGetSmallIcon(path, isDirectory, false);
            if (icon != null) return icon;
            return GetSmallTypeIcon(path, isDirectory);
        }

        private static ImageSource TryGetSmallIcon(string path, bool isDirectory, bool useFileAttributes)
        {
            IntPtr hIcon = IntPtr.Zero;
            try
            {
                SHFILEINFO info = new SHFILEINFO();
                uint attr = isDirectory ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL;
                uint flags = SHGFI_ICON | SHGFI_SMALLICON;
                if (useFileAttributes) flags |= SHGFI_USEFILEATTRIBUTES;
                IntPtr result = SHGetFileInfo(path, attr, ref info, (uint)Marshal.SizeOf(typeof(SHFILEINFO)), flags);
                hIcon = info.hIcon;
                if (result != IntPtr.Zero && hIcon != IntPtr.Zero)
                {
                    BitmapSource source = Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    source.Freeze();
                    return source;
                }
            }
            catch { }
            finally { if (hIcon != IntPtr.Zero) DestroyIcon(hIcon); }
            return null;
        }

        public static ImageSource GetThumbnailOrIcon(string path, int pixels)
        {
            object factoryObject = null;
            IntPtr hBitmap = IntPtr.Zero;
            try
            {
                Guid iid = typeof(IShellItemImageFactory).GUID;
                SHCreateItemFromParsingName(path, IntPtr.Zero, ref iid, out factoryObject);
                IShellItemImageFactory factory = (IShellItemImageFactory)factoryObject;
                factory.GetImage(new SIZE(pixels, pixels), SIIGBF.RESIZETOFIT, out hBitmap);
                if (hBitmap != IntPtr.Zero)
                {
                    BitmapSource image = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(pixels, pixels));
                    image.Freeze();
                    return image;
                }
            }
            catch { }
            finally
            {
                if (hBitmap != IntPtr.Zero) DeleteObject(hBitmap);
                if (factoryObject != null && Marshal.IsComObject(factoryObject)) Marshal.FinalReleaseComObject(factoryObject);
            }
            return GetSmallIcon(path, Directory.Exists(path));
        }

        public static void OpenPath(string path)
        {
            try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Ferry", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public static void OpenWith(Window owner, string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
            try
            {
                OPENASINFO info = new OPENASINFO();
                info.pcszFile = path;
                info.pcszClass = null;
                // OAIF_EXEC: after the user chooses an application, open this single file.
                info.oaifInFlags = 0x00000004;
                IntPtr hwnd = owner == null ? IntPtr.Zero : new WindowInteropHelper(owner).Handle;
                int hr = SHOpenWithDialog(hwnd, ref info);
                if (hr == 0) return;

                // Defensive fallback for systems where SHOpenWithDialog is unavailable/fails.
                SHELLEXECUTEINFO execute = new SHELLEXECUTEINFO();
                execute.cbSize = Marshal.SizeOf(typeof(SHELLEXECUTEINFO));
                execute.hwnd = hwnd;
                execute.lpVerb = "openas";
                execute.lpFile = path;
                execute.nShow = SW_SHOWNORMAL;
                if (!ShellExecuteEx(ref execute))
                    MessageBox.Show("Windows could not open the Open with dialog for this file.", "Ferry", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Ferry", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public static void ShowProperties(Window owner, string path)
        {
            try
            {
                SHELLEXECUTEINFO info = new SHELLEXECUTEINFO();
                info.cbSize = Marshal.SizeOf(typeof(SHELLEXECUTEINFO));
                info.fMask = SEE_MASK_INVOKEIDLIST;
                info.hwnd = owner == null ? IntPtr.Zero : new WindowInteropHelper(owner).Handle;
                info.lpVerb = "properties";
                info.lpFile = path;
                info.nShow = SW_SHOWNORMAL;
                ShellExecuteEx(ref info);
            }
            catch { }
        }

        public static void OpenExplorer(string path, bool select)
        {
            try
            {
                if (select && (File.Exists(path) || Directory.Exists(path)))
                    Process.Start("explorer.exe", "/select,\"" + path + "\"");
                else
                    Process.Start("explorer.exe", "\"" + path + "\"");
            }
            catch { }
        }

        public static void OpenRecycleBin()
        {
            try { Process.Start("explorer.exe", "shell:RecycleBinFolder"); }
            catch { }
        }

        public static void OpenTerminal(string command, string arguments, string workingDirectory)
        {
            if (string.IsNullOrWhiteSpace(workingDirectory) || !Directory.Exists(workingDirectory))
                workingDirectory = KnownFolders.Home;

            if (!string.IsNullOrWhiteSpace(command))
            {
                string expanded = Environment.ExpandEnvironmentVariables(command.Trim());
                if (TryStartTerminal(expanded, arguments ?? string.Empty, workingDirectory, true)) return;
                MessageBox.Show("Configured terminal command could not be started.\n\n" + expanded, "Ferry", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Public default: no machine-specific terminal path. Prefer Windows Terminal when
            // available, then fall back to Windows PowerShell and finally Command Prompt.
            string wtArgs = "-d \"" + workingDirectory.Replace("\"", "\\\"") + "\"";
            if (TryStartTerminal("wt.exe", wtArgs, workingDirectory, false)) return;
            if (TryStartTerminal("powershell.exe", "-NoLogo", workingDirectory, false)) return;
            if (TryStartTerminal("cmd.exe", string.Empty, workingDirectory, false)) return;

            MessageBox.Show("No supported terminal could be started.", "Ferry", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static bool TryStartTerminal(string command, string arguments, string workingDirectory, bool reportError)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(command, arguments ?? string.Empty);
                psi.WorkingDirectory = workingDirectory;
                psi.UseShellExecute = false;
                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                if (reportError) Logger.Write("Terminal launch failed: " + ex.Message);
                return false;
            }
        }
    }
}
