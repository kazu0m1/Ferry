using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Ferry
{
    internal static class ShellFolderPicker
    {
        private const uint FOS_PICKFOLDERS = 0x00000020;
        private const uint FOS_FORCEFILESYSTEM = 0x00000040;
        private const uint FOS_PATHMUSTEXIST = 0x00000800;
        private const uint FOS_NOCHANGEDIR = 0x00000008;
        private const uint SIGDN_FILESYSPATH = 0x80058000;
        private const int ERROR_CANCELLED_HRESULT = unchecked((int)0x800704C7);

        public static bool TryPickFolder(Window owner, string title, out string selectedPath)
        {
            selectedPath = null;
            IFileDialog dialog = null;
            IShellItem item = null;

            try
            {
                dialog = (IFileDialog)new FileOpenDialogRCW();

                uint options;
                dialog.GetOptions(out options);
                options |= FOS_PICKFOLDERS | FOS_FORCEFILESYSTEM | FOS_PATHMUSTEXIST | FOS_NOCHANGEDIR;
                dialog.SetOptions(options);

                if (!String.IsNullOrWhiteSpace(title))
                    dialog.SetTitle(title);

                IntPtr ownerHandle = owner == null ? IntPtr.Zero : new WindowInteropHelper(owner).Handle;
                int hr = dialog.Show(ownerHandle);
                if (hr == ERROR_CANCELLED_HRESULT)
                    return false;
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                dialog.GetResult(out item);
                IntPtr displayName;
                item.GetDisplayName(SIGDN_FILESYSPATH, out displayName);
                try
                {
                    selectedPath = Marshal.PtrToStringUni(displayName);
                }
                finally
                {
                    Marshal.FreeCoTaskMem(displayName);
                }

                return !String.IsNullOrWhiteSpace(selectedPath);
            }
            finally
            {
                if (item != null && Marshal.IsComObject(item))
                    Marshal.FinalReleaseComObject(item);
                if (dialog != null && Marshal.IsComObject(dialog))
                    Marshal.FinalReleaseComObject(dialog);
            }
        }

        [ComImport]
        [Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
        private class FileOpenDialogRCW
        {
        }

        [ComImport]
        [Guid("42F85136-DB7E-439C-85F1-E4075D135FC8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileDialog
        {
            [PreserveSig]
            int Show(IntPtr parent);

            void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
            void SetFileTypeIndex(uint iFileType);
            void GetFileTypeIndex(out uint piFileType);
            void Advise(IntPtr pfde, out uint pdwCookie);
            void Unadvise(uint dwCookie);
            void SetOptions(uint fos);
            void GetOptions(out uint pfos);
            void SetDefaultFolder([MarshalAs(UnmanagedType.Interface)] IShellItem psi);
            void SetFolder([MarshalAs(UnmanagedType.Interface)] IShellItem psi);
            void GetFolder([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
            void GetCurrentSelection([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            void GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
        }

        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
            void GetParent([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
            void GetDisplayName(uint sigdnName, out IntPtr ppszName);
            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
            void Compare([MarshalAs(UnmanagedType.Interface)] IShellItem psi, uint hint, out int piOrder);
        }
    }
}
