using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Ferry
{
    internal static class ShellFileOperations
    {
        private const uint FO_MOVE = 0x0001;
        private const uint FO_COPY = 0x0002;
        private const uint FO_DELETE = 0x0003;
        private const ushort FOF_MULTIDESTFILES = 0x0001;
        private const ushort FOF_SILENT = 0x0004;
        private const ushort FOF_RENAMEONCOLLISION = 0x0008;
        private const ushort FOF_NOCONFIRMATION = 0x0010;
        private const ushort FOF_ALLOWUNDO = 0x0040;
        private const ushort FOF_NOCONFIRMMKDIR = 0x0200;
        private const ushort FOF_NOERRORUI = 0x0400;
        private const ushort FOF_WANTNUKEWARNING = 0x4000;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public uint wFunc;
            [MarshalAs(UnmanagedType.LPWStr)] public string pFrom;
            [MarshalAs(UnmanagedType.LPWStr)] public string pTo;
            public ushort fFlags;
            [MarshalAs(UnmanagedType.Bool)] public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpszProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

        public static bool Copy(IList<string> paths, string destination)
        {
            return Execute(FO_COPY, paths, destination, FOF_ALLOWUNDO | FOF_NOCONFIRMMKDIR);
        }

        public static bool Move(IList<string> paths, string destination)
        {
            return Execute(FO_MOVE, paths, destination, FOF_ALLOWUNDO | FOF_NOCONFIRMMKDIR);
        }

        public static bool DeleteToRecycleBin(IList<string> paths)
        {
            return Execute(FO_DELETE, paths, null, FOF_ALLOWUNDO | FOF_NOCONFIRMMKDIR);
        }

        public static bool DeletePermanently(IList<string> paths)
        {
            return Execute(FO_DELETE, paths, null, FOF_WANTNUKEWARNING);
        }

        private static bool Execute(uint operation, IList<string> paths, string destination, ushort flags)
        {
            if (paths == null || paths.Count == 0) return true;
            SHFILEOPSTRUCT op = new SHFILEOPSTRUCT();
            op.wFunc = operation;
            op.pFrom = BuildMultiString(paths);
            op.pTo = string.IsNullOrEmpty(destination) ? null : destination + '\0' + '\0';
            op.fFlags = flags;
            int result = SHFileOperation(ref op);
            if (result != 0) throw new IOException("Windows file operation failed (0x" + result.ToString("X") + ").");
            return !op.fAnyOperationsAborted;
        }

        private static string BuildMultiString(IList<string> paths)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < paths.Count; i++)
            {
                if (string.IsNullOrEmpty(paths[i])) continue;
                sb.Append(paths[i]); sb.Append('\0');
            }
            sb.Append('\0');
            return sb.ToString();
        }
    }
}
