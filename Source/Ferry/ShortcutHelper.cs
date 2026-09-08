using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Ferry
{
    internal static class ShortcutHelper
    {
        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLink { }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszName, int cch);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszDir, int cch);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszArgs, int cch);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszIconPath, int cch, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("0000010b-0000-0000-C000-000000000046")]
        private interface IPersistFile
        {
            void GetClassID(out Guid pClassID);
            [PreserveSig] int IsDirty();
            void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
            void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, bool fRemember);
            void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
            void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
        }


        public static string ResolveTarget(string shortcutPath)
        {
            if (string.IsNullOrEmpty(shortcutPath) || !File.Exists(shortcutPath)) return null;
            object obj = new ShellLink();
            try
            {
                ((IPersistFile)obj).Load(shortcutPath, 0);
                IShellLinkW shell = (IShellLinkW)obj;
                System.Text.StringBuilder target = new System.Text.StringBuilder(32768);
                shell.GetPath(target, target.Capacity, IntPtr.Zero, 0);
                string value = target.ToString();
                if (string.IsNullOrWhiteSpace(value)) return null;
                return Environment.ExpandEnvironmentVariables(value);
            }
            catch { return null; }
            finally { if (Marshal.IsComObject(obj)) Marshal.FinalReleaseComObject(obj); }
        }

        public static string Create(string target)
        {
            string dir = Path.GetDirectoryName(target);
            string name = Path.GetFileName(target.TrimEnd(Path.DirectorySeparatorChar));
            string baseName = Directory.Exists(target) ? name : Path.GetFileNameWithoutExtension(name);
            string link = Path.Combine(dir, baseName + " - Shortcut.lnk");
            int suffix = 2;
            while (File.Exists(link)) link = Path.Combine(dir, baseName + " - Shortcut (" + suffix++ + ").lnk");
            object obj = new ShellLink();
            try
            {
                IShellLinkW shell = (IShellLinkW)obj;
                shell.SetPath(target);
                shell.SetWorkingDirectory(Directory.Exists(target) ? target : dir);
                ((IPersistFile)obj).Save(link, true);
                return link;
            }
            finally { if (Marshal.IsComObject(obj)) Marshal.FinalReleaseComObject(obj); }
        }
    }
}
