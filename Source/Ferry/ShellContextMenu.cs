using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Ferry
{
    internal static class ShellContextMenu
    {
        private const uint CMF_NORMAL = 0x00000000;
        private const uint CMF_EXTENDEDVERBS = 0x00000100;
        private const uint TPM_RETURNCMD = 0x0100;
        private const uint TPM_RIGHTBUTTON = 0x0002;
        private const int SW_SHOWNORMAL = 1;
        private const uint CMIC_MASK_UNICODE = 0x00004000;

        private const int WM_DRAWITEM = 0x002B;
        private const int WM_MEASUREITEM = 0x002C;
        private const int WM_INITMENUPOPUP = 0x0117;
        private const int WM_MENUCHAR = 0x0120;

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214E6-0000-0000-C000-000000000046")]
        private interface IShellFolder
        {
            [PreserveSig] int ParseDisplayName(IntPtr hwnd, IntPtr pbc, [MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName, ref uint pchEaten, out IntPtr ppidl, ref uint pdwAttributes);
            [PreserveSig] int EnumObjects(IntPtr hwnd, int grfFlags, out IntPtr ppenumIDList);
            [PreserveSig] int BindToObject(IntPtr pidl, IntPtr pbc, ref Guid riid, out IntPtr ppv);
            [PreserveSig] int BindToStorage(IntPtr pidl, IntPtr pbc, ref Guid riid, out IntPtr ppv);
            [PreserveSig] int CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2);
            [PreserveSig] int CreateViewObject(IntPtr hwndOwner, ref Guid riid, out IntPtr ppv);
            [PreserveSig] int GetAttributesOf(uint cidl, IntPtr apidl, ref uint rgfInOut);
            [PreserveSig] int GetUIObjectOf(IntPtr hwndOwner, uint cidl, IntPtr apidl, ref Guid riid, IntPtr rgfReserved, out IntPtr ppv);
            [PreserveSig] int GetDisplayNameOf(IntPtr pidl, uint uFlags, IntPtr lpName);
            [PreserveSig] int SetNameOf(IntPtr hwnd, IntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] string pszName, uint uFlags, out IntPtr ppidlOut);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214E4-0000-0000-C000-000000000046")]
        private interface IContextMenu
        {
            [PreserveSig] int QueryContextMenu(IntPtr hMenu, uint indexMenu, uint idCmdFirst, uint idCmdLast, uint uFlags);
            [PreserveSig] int InvokeCommand(ref CMINVOKECOMMANDINFOEX pici);
            [PreserveSig] int GetCommandString(UIntPtr idcmd, uint uflags, IntPtr reserved, IntPtr commandstring, int cch);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F4-0000-0000-C000-000000000046")]
        private interface IContextMenu2
        {
            [PreserveSig] int QueryContextMenu(IntPtr hMenu, uint indexMenu, uint idCmdFirst, uint idCmdLast, uint uFlags);
            [PreserveSig] int InvokeCommand(ref CMINVOKECOMMANDINFOEX pici);
            [PreserveSig] int GetCommandString(UIntPtr idcmd, uint uflags, IntPtr reserved, IntPtr commandstring, int cch);
            [PreserveSig] int HandleMenuMsg(uint uMsg, IntPtr wParam, IntPtr lParam);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("BCFCE0A0-EC17-11D0-8D10-00A0C90F2719")]
        private interface IContextMenu3
        {
            [PreserveSig] int QueryContextMenu(IntPtr hMenu, uint indexMenu, uint idCmdFirst, uint idCmdLast, uint uFlags);
            [PreserveSig] int InvokeCommand(ref CMINVOKECOMMANDINFOEX pici);
            [PreserveSig] int GetCommandString(UIntPtr idcmd, uint uflags, IntPtr reserved, IntPtr commandstring, int cch);
            [PreserveSig] int HandleMenuMsg(uint uMsg, IntPtr wParam, IntPtr lParam);
            [PreserveSig] int HandleMenuMsg2(uint uMsg, IntPtr wParam, IntPtr lParam, out IntPtr plResult);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DEFCONTEXTMENU
        {
            public IntPtr hwnd;
            public IntPtr pcmcb;
            public IntPtr pidlFolder;
            public IntPtr psf;
            public uint cidl;
            public IntPtr apidl;
            public IntPtr punkAssociationInfo;
            public uint cKeys;
            public IntPtr aKeys;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CMINVOKECOMMANDINFOEX
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;
            public IntPtr lpVerb;
            [MarshalAs(UnmanagedType.LPStr)] public string lpParameters;
            [MarshalAs(UnmanagedType.LPStr)] public string lpDirectory;
            public int nShow;
            public uint dwHotKey;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.LPStr)] public string lpTitle;
            public IntPtr lpVerbW;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpParametersW;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpDirectoryW;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpTitleW;
            public POINT ptInvoke;
        }

        [StructLayout(LayoutKind.Sequential)] private struct POINT { public int x; public int y; }

        [DllImport("shell32.dll")]
        private static extern int SHGetDesktopFolder([MarshalAs(UnmanagedType.Interface)] out IShellFolder ppshf);

        [DllImport("shell32.dll")]
        private static extern int SHCreateDefaultContextMenu(ref DEFCONTEXTMENU pdcm, ref Guid riid, out IntPtr ppv);

        [DllImport("shell32.dll")]
        private static extern int CDefFolderMenu_Create2(IntPtr pidlFolder, IntPtr hwnd, uint cidl, IntPtr apidl, IntPtr psf, IntPtr pfn, uint nKeys, IntPtr ahkeys, out IntPtr ppcm);

        [DllImport("user32.dll")]
        private static extern IntPtr CreatePopupMenu();
        [DllImport("user32.dll")]
        private static extern bool DestroyMenu(IntPtr hMenu);
        [DllImport("user32.dll")]
        private static extern uint TrackPopupMenuEx(IntPtr hmenu, uint fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

        public static bool Show(Window owner, IList<string> paths, Point screenPoint)
        {
            if (paths == null || paths.Count == 0) return false;

            string parentPath = GetParent(paths[0]);
            if (string.IsNullOrEmpty(parentPath)) return false;
            for (int i = 1; i < paths.Count; i++)
                if (!string.Equals(parentPath, GetParent(paths[i]), StringComparison.OrdinalIgnoreCase)) return false;

            IShellFolder desktopFolder = null;
            IShellFolder parentFolder = null;
            IntPtr parentPidl = IntPtr.Zero;
            IntPtr parentFolderUnknown = IntPtr.Zero;
            IntPtr parentFolderInterface = IntPtr.Zero;
            List<IntPtr> childPidls = new List<IntPtr>();
            IntPtr childArray = IntPtr.Zero;
            object menuObject = null;
            IntPtr menuHandle = IntPtr.Zero;
            HwndSource hwndSource = null;
            HwndSourceHook hook = null;

            try
            {
                IntPtr hwnd = owner == null ? IntPtr.Zero : new WindowInteropHelper(owner).Handle;
                Guid shellFolderGuid = typeof(IShellFolder).GUID;

                // RC13: obtain ONE parent IShellFolder, then parse every selected child through
                // that same parent. GetUIObjectOf requires apidl to be an array of child PIDLs
                // relative to the parent folder. Building each child via a separate
                // SHBindToParent call was reliable for one item but failed for multi-selection
                // on the public test machine.
                int hr = SHGetDesktopFolder(out desktopFolder);
                if (hr != 0 || desktopFolder == null)
                {
                    LogHr("SHGetDesktopFolder", hr);
                    return false;
                }

                uint eaten = 0;
                uint attrs = 0;
                hr = desktopFolder.ParseDisplayName(hwnd, IntPtr.Zero, parentPath, ref eaten, out parentPidl, ref attrs);
                if (hr != 0 || parentPidl == IntPtr.Zero)
                {
                    LogHr("Parse parent", hr);
                    return false;
                }

                hr = desktopFolder.BindToObject(parentPidl, IntPtr.Zero, ref shellFolderGuid, out parentFolderUnknown);
                if (hr != 0 || parentFolderUnknown == IntPtr.Zero)
                {
                    LogHr("Bind parent", hr);
                    return false;
                }
                parentFolder = (IShellFolder)Marshal.GetTypedObjectForIUnknown(parentFolderUnknown, typeof(IShellFolder));
                Marshal.Release(parentFolderUnknown);
                parentFolderUnknown = IntPtr.Zero;

                for (int i = 0; i < paths.Count; i++)
                {
                    string childName = GetLeafName(paths[i]);
                    if (string.IsNullOrEmpty(childName)) return false;

                    eaten = 0;
                    attrs = 0;
                    IntPtr childPidl;
                    hr = parentFolder.ParseDisplayName(hwnd, IntPtr.Zero, childName, ref eaten, out childPidl, ref attrs);
                    if (hr != 0 || childPidl == IntPtr.Zero)
                    {
                        LogHr("Parse child: " + childName, hr);
                        return false;
                    }
                    childPidls.Add(childPidl);
                }

                childArray = Marshal.AllocCoTaskMem(IntPtr.Size * childPidls.Count);
                for (int i = 0; i < childPidls.Count; i++)
                    Marshal.WriteIntPtr(childArray, i * IntPtr.Size, childPidls[i]);

                Guid contextGuid = typeof(IContextMenu).GUID;
                IntPtr ppv = IntPtr.Zero;

                hr = parentFolder.GetUIObjectOf(hwnd, (uint)childPidls.Count, childArray, ref contextGuid, IntPtr.Zero, out ppv);
                if (hr == 0 && ppv != IntPtr.Zero)
                {
                    try { menuObject = Marshal.GetObjectForIUnknown(ppv); }
                    finally { Marshal.Release(ppv); ppv = IntPtr.Zero; }
                }
                else
                {
                    LogHr("GetUIObjectOf(" + childPidls.Count + ")", hr);
                }

                IContextMenu context = menuObject as IContextMenu;
                if (!TryQueryContextMenu(context, out menuHandle))
                {
                    ReleaseMenuObject(ref menuObject);
                    if (menuHandle != IntPtr.Zero) { DestroyMenu(menuHandle); menuHandle = IntPtr.Zero; }

                    // First fallback: Shell's default context-menu factory, with both the
                    // absolute parent PIDL and the matching parent IShellFolder supplied.
                    parentFolderInterface = Marshal.GetComInterfaceForObject(parentFolder, typeof(IShellFolder));
                    DEFCONTEXTMENU dcm = new DEFCONTEXTMENU();
                    dcm.hwnd = hwnd;
                    dcm.pidlFolder = parentPidl;
                    dcm.psf = parentFolderInterface;
                    dcm.cidl = (uint)childPidls.Count;
                    dcm.apidl = childArray;

                    ppv = IntPtr.Zero;
                    hr = SHCreateDefaultContextMenu(ref dcm, ref contextGuid, out ppv);
                    if (hr == 0 && ppv != IntPtr.Zero)
                    {
                        try { menuObject = Marshal.GetObjectForIUnknown(ppv); }
                        finally { Marshal.Release(ppv); ppv = IntPtr.Zero; }
                        context = menuObject as IContextMenu;
                    }
                    else
                    {
                        LogHr("SHCreateDefaultContextMenu", hr);
                    }

                    if (!TryQueryContextMenu(context, out menuHandle))
                    {
                        ReleaseMenuObject(ref menuObject);
                        if (menuHandle != IntPtr.Zero) { DestroyMenu(menuHandle); menuHandle = IntPtr.Zero; }

                        // Second fallback: legacy folder-menu factory. It is specifically
                        // defined for a selected group of child items and remains available on
                        // supported Windows desktop versions.
                        ppv = IntPtr.Zero;
                        hr = CDefFolderMenu_Create2(parentPidl, hwnd, (uint)childPidls.Count, childArray, parentFolderInterface, IntPtr.Zero, 0, IntPtr.Zero, out ppv);
                        if (hr != 0 || ppv == IntPtr.Zero)
                        {
                            LogHr("CDefFolderMenu_Create2", hr);
                            return false;
                        }
                        try { menuObject = Marshal.GetObjectForIUnknown(ppv); }
                        finally { Marshal.Release(ppv); ppv = IntPtr.Zero; }
                        context = menuObject as IContextMenu;
                        if (!TryQueryContextMenu(context, out menuHandle)) return false;
                    }
                }

                IContextMenu3 context3 = menuObject as IContextMenu3;
                IContextMenu2 context2 = context3 == null ? menuObject as IContextMenu2 : null;
                if (hwnd != IntPtr.Zero && (context3 != null || context2 != null))
                {
                    hwndSource = HwndSource.FromHwnd(hwnd);
                    if (hwndSource != null)
                    {
                        hook = delegate(IntPtr hookHwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
                        {
                            if (msg != WM_INITMENUPOPUP && msg != WM_DRAWITEM && msg != WM_MEASUREITEM && msg != WM_MENUCHAR)
                                return IntPtr.Zero;
                            try
                            {
                                if (context3 != null)
                                {
                                    IntPtr result;
                                    int menuHr = context3.HandleMenuMsg2((uint)msg, wParam, lParam, out result);
                                    if (menuHr == 0) { handled = true; return result; }
                                }
                                else if (context2 != null)
                                {
                                    int menuHr = context2.HandleMenuMsg((uint)msg, wParam, lParam);
                                    if (menuHr == 0) handled = true;
                                }
                            }
                            catch { }
                            return IntPtr.Zero;
                        };
                        hwndSource.AddHook(hook);
                    }
                }

                uint command = TrackPopupMenuEx(menuHandle, TPM_RETURNCMD | TPM_RIGHTBUTTON, (int)screenPoint.X, (int)screenPoint.Y, hwnd, IntPtr.Zero);
                if (command == 0) return true;

                CMINVOKECOMMANDINFOEX invoke = new CMINVOKECOMMANDINFOEX();
                invoke.cbSize = Marshal.SizeOf(typeof(CMINVOKECOMMANDINFOEX));
                invoke.fMask = CMIC_MASK_UNICODE;
                invoke.hwnd = hwnd;
                invoke.lpVerb = (IntPtr)(command - 1);
                invoke.lpVerbW = (IntPtr)(command - 1);
                invoke.nShow = SW_SHOWNORMAL;
                invoke.ptInvoke = new POINT { x = (int)screenPoint.X, y = (int)screenPoint.Y };
                int invokeHr = context.InvokeCommand(ref invoke);
                if (invokeHr < 0) LogHr("InvokeCommand", invokeHr);
                return invokeHr >= 0;
            }
            catch (Exception ex)
            {
                Logger.Write("Shell context menu: " + ex.ToString());
                return false;
            }
            finally
            {
                if (hwndSource != null && hook != null) try { hwndSource.RemoveHook(hook); } catch { }
                if (menuHandle != IntPtr.Zero) DestroyMenu(menuHandle);
                ReleaseMenuObject(ref menuObject);
                if (parentFolderInterface != IntPtr.Zero) Marshal.Release(parentFolderInterface);
                if (childArray != IntPtr.Zero) Marshal.FreeCoTaskMem(childArray);
                for (int i = 0; i < childPidls.Count; i++)
                    if (childPidls[i] != IntPtr.Zero) Marshal.FreeCoTaskMem(childPidls[i]);
                if (parentFolderUnknown != IntPtr.Zero) Marshal.Release(parentFolderUnknown);
                if (parentFolder != null && Marshal.IsComObject(parentFolder)) Marshal.FinalReleaseComObject(parentFolder);
                if (parentPidl != IntPtr.Zero) Marshal.FreeCoTaskMem(parentPidl);
                if (desktopFolder != null && Marshal.IsComObject(desktopFolder)) Marshal.FinalReleaseComObject(desktopFolder);
            }
        }

        private static bool TryQueryContextMenu(IContextMenu context, out IntPtr menuHandle)
        {
            menuHandle = IntPtr.Zero;
            if (context == null) return false;
            menuHandle = CreatePopupMenu();
            if (menuHandle == IntPtr.Zero) return false;
            int queryHr = context.QueryContextMenu(menuHandle, 0, 1, 0x7FFF, CMF_NORMAL | CMF_EXTENDEDVERBS);
            if (queryHr >= 0) return true;
            LogHr("QueryContextMenu", queryHr);
            DestroyMenu(menuHandle);
            menuHandle = IntPtr.Zero;
            return false;
        }

        private static void ReleaseMenuObject(ref object menuObject)
        {
            if (menuObject != null && Marshal.IsComObject(menuObject))
            {
                try { Marshal.FinalReleaseComObject(menuObject); } catch { }
            }
            menuObject = null;
        }

        private static void LogHr(string operation, int hr)
        {
            try { Logger.Write("Shell context menu: " + operation + " failed, HRESULT=0x" + hr.ToString("X8")); } catch { }
        }

        private static string GetParent(string path)
        {
            try
            {
                string normalized = path == null ? null : path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return string.IsNullOrEmpty(normalized) ? null : Path.GetDirectoryName(normalized);
            }
            catch { return null; }
        }

        private static string GetLeafName(string path)
        {
            try
            {
                string normalized = path == null ? null : path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return string.IsNullOrEmpty(normalized) ? null : Path.GetFileName(normalized);
            }
            catch { return null; }
        }
    }
}
