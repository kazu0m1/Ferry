using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

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

        private static int transferActive;

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

        public static bool IsTransferActive
        {
            get { return Interlocked.CompareExchange(ref transferActive, 0, 0) != 0; }
        }

        public static bool Copy(IList<string> paths, string destination)
        {
            return ExecuteTransfer(FO_COPY, paths, destination, FOF_ALLOWUNDO | FOF_NOCONFIRMMKDIR);
        }

        public static bool Move(IList<string> paths, string destination)
        {
            return ExecuteTransfer(FO_MOVE, paths, destination, FOF_ALLOWUNDO | FOF_NOCONFIRMMKDIR);
        }

        public static bool DeleteToRecycleBin(IList<string> paths)
        {
            return Execute(FO_DELETE, paths, null, FOF_ALLOWUNDO | FOF_NOCONFIRMMKDIR);
        }

        public static bool DeletePermanently(IList<string> paths)
        {
            return Execute(FO_DELETE, paths, null, FOF_WANTNUKEWARNING);
        }

        public static bool DeleteAfterExternalMove(IList<string> paths)
        {
            // The destination already completed the data transfer and explicitly reported an
            // unoptimized MOVE.  This is source cleanup, not a user-requested standalone Delete,
            // so do not send the originals to Recycle Bin and do not ask for a second confirmation.
            return Execute(FO_DELETE, paths, null, FOF_NOCONFIRMATION | FOF_NOCONFIRMMKDIR);
        }

        private static bool ExecuteTransfer(uint operation, IList<string> paths, string destination, ushort flags)
        {
            if (paths == null || paths.Count == 0) return true;

            if (Interlocked.CompareExchange(ref transferActive, 1, 0) != 0)
                throw new IOException("Another file Copy/Move operation is already in progress.");

            try
            {
                List<string> stablePaths = new List<string>(paths);
                Application app = Application.Current;
                Dispatcher dispatcher = app == null ? null : app.Dispatcher;

                // Preserve the existing synchronous Shell-operation contract for callers, including
                // WPF drag/drop, while moving the expensive SHFileOperation call off the UI thread.
                // A nested DispatcherFrame keeps window messages, painting, minimize/restore and
                // normal browsing responsive until the Shell operation completes.
                if (dispatcher == null || !dispatcher.CheckAccess())
                    return Execute(operation, stablePaths, destination, flags);

                bool completed = false;
                Exception failure = null;
                DispatcherFrame frame = new DispatcherFrame();

                Thread worker = new Thread(delegate()
                {
                    try
                    {
                        completed = Execute(operation, stablePaths, destination, flags);
                    }
                    catch (Exception ex)
                    {
                        failure = ex;
                    }
                    finally
                    {
                        try
                        {
                            dispatcher.BeginInvoke(
                                DispatcherPriority.Send,
                                new Action(delegate { frame.Continue = false; }));
                        }
                        catch
                        {
                            frame.Continue = false;
                        }
                    }
                });

                worker.Name = operation == FO_MOVE ? "Ferry Shell Move" : "Ferry Shell Copy";
                worker.IsBackground = true;
                worker.SetApartmentState(ApartmentState.STA);
                worker.Start();

                Dispatcher.PushFrame(frame);
                worker.Join();

                if (failure != null) throw failure;
                return completed;
            }
            finally
            {
                Interlocked.Exchange(ref transferActive, 0);
            }
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
