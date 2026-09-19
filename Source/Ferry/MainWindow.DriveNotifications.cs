using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Win32.SafeHandles;

namespace Ferry
{
    internal sealed partial class MainWindow
    {
        private const int WmDeviceChange = 0x0219;
        private const int DbtDevNodesChanged = 0x0007;
        private const int DbtDeviceArrival = 0x8000;
        private const int DbtDeviceQueryRemove = 0x8001;
        private const int DbtDeviceQueryRemoveFailed = 0x8002;
        private const int DbtDeviceRemovePending = 0x8003;
        private const int DbtDeviceRemoveComplete = 0x8004;
        private const int DbtDevTypVolume = 0x00000002;
        private const int DbtDevTypHandle = 0x00000006;

        private const uint FileShareRead = 0x00000001;
        private const uint FileShareWrite = 0x00000002;
        private const uint FileShareDelete = 0x00000004;
        private const uint OpenExisting = 3;
        private const uint FileFlagBackupSemantics = 0x02000000;
        private const uint DeviceNotifyWindowHandle = 0x00000000;

        [StructLayout(LayoutKind.Sequential)]
        private struct DeviceBroadcastHeader
        {
            public int Size;
            public int DeviceType;
            public int Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DeviceBroadcastVolume
        {
            public int Size;
            public int DeviceType;
            public int Reserved;
            public uint UnitMask;
            public ushort Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DeviceBroadcastHandle
        {
            public int Size;
            public int DeviceType;
            public int Reserved;
            public IntPtr DeviceHandle;
            public IntPtr DeviceNotifyHandle;
            public Guid EventGuid;
            public int NameOffset;
        }

        private sealed class DriveNotificationRegistration
        {
            public string RootPath;
            public SafeFileHandle DeviceHandle;
            public IntPtr NotificationHandle;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(
            string fileName,
            uint desiredAccess,
            uint shareMode,
            IntPtr securityAttributes,
            uint creationDisposition,
            uint flagsAndAttributes,
            IntPtr templateFile);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotification(
            IntPtr recipient,
            ref DeviceBroadcastHandle notificationFilter,
            uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnregisterDeviceNotification(IntPtr notificationHandle);

        private HwndSource driveNotificationSource;
        private DispatcherTimer driveRefreshTimer;
        private readonly List<DriveNotificationRegistration> driveNotificationRegistrations =
            new List<DriveNotificationRegistration>();

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            driveNotificationSource = PresentationSource.FromVisual(this) as HwndSource;
            if (driveNotificationSource != null)
            {
                driveNotificationSource.AddHook(DriveNotificationWndProc);
                RefreshDriveNotificationRegistrations();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (driveRefreshTimer != null)
            {
                driveRefreshTimer.Stop();
                driveRefreshTimer = null;
            }

            ClearDriveNotificationRegistrations();

            if (driveNotificationSource != null)
            {
                driveNotificationSource.RemoveHook(DriveNotificationWndProc);
                driveNotificationSource = null;
            }

            base.OnClosed(e);
        }

        private IntPtr DriveNotificationWndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (message != WmDeviceChange) return IntPtr.Zero;

            int change = wParam.ToInt32();

            if (change == DbtDevNodesChanged)
            {
                ScheduleDriveSidebarRefresh();
                return IntPtr.Zero;
            }

            if (lParam == IntPtr.Zero) return IntPtr.Zero;

            try
            {
                DeviceBroadcastHeader header =
                    (DeviceBroadcastHeader)Marshal.PtrToStructure(lParam, typeof(DeviceBroadcastHeader));

                if (header.DeviceType == DbtDevTypHandle)
                {
                    if (change == DbtDeviceQueryRemove)
                    {
                        DeviceBroadcastHandle handleInfo =
                            (DeviceBroadcastHandle)Marshal.PtrToStructure(lParam, typeof(DeviceBroadcastHandle));
                        string root = RemoveDriveRegistration(handleInfo.DeviceHandle, handleInfo.DeviceNotifyHandle);
                        if (!string.IsNullOrEmpty(root))
                        {
                            PrepareTabsForDriveRemoval(root);
                            handled = true;
                            return new IntPtr(1);
                        }
                    }

                    return IntPtr.Zero;
                }

                if (header.DeviceType != DbtDevTypVolume) return IntPtr.Zero;

                DeviceBroadcastVolume volume =
                    (DeviceBroadcastVolume)Marshal.PtrToStructure(lParam, typeof(DeviceBroadcastVolume));

                if (change == DbtDeviceQueryRemove || change == DbtDeviceRemovePending)
                    PrepareTabsForVolumeRemoval(volume.UnitMask);

                if (change == DbtDeviceArrival ||
                    change == DbtDeviceQueryRemoveFailed ||
                    change == DbtDeviceRemoveComplete)
                {
                    ScheduleDriveSidebarRefresh();
                }
            }
            catch
            {
                // Device notifications are advisory. A malformed or transient notification
                // must never interfere with the normal Ferry window message loop.
            }

            return IntPtr.Zero;
        }

        private void RefreshDriveNotificationRegistrations()
        {
            if (driveNotificationSource == null) return;

            string systemRoot = null;
            try { systemRoot = Path.GetPathRoot(Environment.SystemDirectory); } catch { }

            HashSet<string> wantedRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                DriveInfo[] drives = DriveInfo.GetDrives();
                for (int i = 0; i < drives.Length; i++)
                {
                    DriveInfo drive = drives[i];
                    string root = null;
                    try
                    {
                        if (!drive.IsReady) continue;
                        if (drive.DriveType != DriveType.Removable && drive.DriveType != DriveType.Fixed) continue;
                        root = drive.RootDirectory.FullName;
                    }
                    catch { continue; }

                    if (string.IsNullOrEmpty(root)) continue;
                    if (!string.IsNullOrEmpty(systemRoot) &&
                        string.Equals(root, systemRoot, StringComparison.OrdinalIgnoreCase))
                        continue;

                    wantedRoots.Add(root);
                }
            }
            catch { }

            for (int i = driveNotificationRegistrations.Count - 1; i >= 0; i--)
            {
                DriveNotificationRegistration registration = driveNotificationRegistrations[i];
                if (registration == null || !wantedRoots.Contains(registration.RootPath))
                {
                    DisposeDriveRegistration(registration);
                    driveNotificationRegistrations.RemoveAt(i);
                }
            }

            foreach (string root in wantedRoots)
            {
                bool exists = false;
                for (int i = 0; i < driveNotificationRegistrations.Count; i++)
                {
                    if (string.Equals(driveNotificationRegistrations[i].RootPath, root, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }
                if (exists) continue;

                DriveNotificationRegistration registration = TryRegisterDrive(root);
                if (registration != null)
                    driveNotificationRegistrations.Add(registration);
            }
        }

        private DriveNotificationRegistration TryRegisterDrive(string root)
        {
            SafeFileHandle deviceHandle = null;
            try
            {
                deviceHandle = CreateFile(
                    root,
                    0,
                    FileShareRead | FileShareWrite | FileShareDelete,
                    IntPtr.Zero,
                    OpenExisting,
                    FileFlagBackupSemantics,
                    IntPtr.Zero);

                if (deviceHandle == null || deviceHandle.IsInvalid)
                {
                    if (deviceHandle != null) deviceHandle.Dispose();
                    return null;
                }

                DeviceBroadcastHandle filter = new DeviceBroadcastHandle();
                filter.Size = Marshal.SizeOf(typeof(DeviceBroadcastHandle));
                filter.DeviceType = DbtDevTypHandle;
                filter.DeviceHandle = deviceHandle.DangerousGetHandle();

                IntPtr notificationHandle = RegisterDeviceNotification(
                    driveNotificationSource.Handle,
                    ref filter,
                    DeviceNotifyWindowHandle);

                if (notificationHandle == IntPtr.Zero)
                {
                    deviceHandle.Dispose();
                    return null;
                }

                return new DriveNotificationRegistration
                {
                    RootPath = root,
                    DeviceHandle = deviceHandle,
                    NotificationHandle = notificationHandle
                };
            }
            catch
            {
                if (deviceHandle != null) deviceHandle.Dispose();
                return null;
            }
        }

        private string RemoveDriveRegistration(IntPtr deviceHandle, IntPtr notificationHandle)
        {
            for (int i = 0; i < driveNotificationRegistrations.Count; i++)
            {
                DriveNotificationRegistration registration = driveNotificationRegistrations[i];
                if (registration == null) continue;

                bool deviceMatch =
                    registration.DeviceHandle != null &&
                    !registration.DeviceHandle.IsInvalid &&
                    registration.DeviceHandle.DangerousGetHandle() == deviceHandle;
                bool notificationMatch =
                    registration.NotificationHandle != IntPtr.Zero &&
                    registration.NotificationHandle == notificationHandle;

                if (!deviceMatch && !notificationMatch) continue;

                string root = registration.RootPath;
                DisposeDriveRegistration(registration);
                driveNotificationRegistrations.RemoveAt(i);
                return root;
            }

            return null;
        }

        private static void DisposeDriveRegistration(DriveNotificationRegistration registration)
        {
            if (registration == null) return;

            if (registration.NotificationHandle != IntPtr.Zero)
            {
                try { UnregisterDeviceNotification(registration.NotificationHandle); } catch { }
                registration.NotificationHandle = IntPtr.Zero;
            }

            if (registration.DeviceHandle != null)
            {
                try { registration.DeviceHandle.Dispose(); } catch { }
                registration.DeviceHandle = null;
            }
        }

        private void ClearDriveNotificationRegistrations()
        {
            for (int i = driveNotificationRegistrations.Count - 1; i >= 0; i--)
                DisposeDriveRegistration(driveNotificationRegistrations[i]);
            driveNotificationRegistrations.Clear();
        }

        private void PrepareTabsForVolumeRemoval(uint unitMask)
        {
            if (unitMask == 0) return;

            for (int i = 0; i < 26; i++)
            {
                uint bit = 1u << i;
                if ((unitMask & bit) == 0) continue;
                string root = ((char)('A' + i)).ToString() + @":\";
                PrepareTabsForDriveRemoval(root);
            }
        }

        private void PrepareTabsForDriveRemoval(string root)
        {
            if (string.IsNullOrEmpty(root)) return;

            string fallback = GetDriveRemovalFallback(root);
            if (string.IsNullOrEmpty(fallback)) return;

            foreach (TabViewContext ctx in contexts.Values)
            {
                if (ctx == null || ctx.State == null || ctx.State.IsRecycleBin) continue;
                if (!IsPathOnDrive(ctx.State.CurrentPath, root)) continue;

                try { ctx.RefreshTimer.Stop(); } catch { }
                StopSearchDrain(ctx);
                CancelGridThumbnailLoad(ctx);
                ctx.State.CancelBackgroundWork();

                if (ctx.Watcher != null)
                {
                    try
                    {
                        ctx.Watcher.EnableRaisingEvents = false;
                        ctx.Watcher.Dispose();
                    }
                    catch { }
                    ctx.Watcher = null;
                }

                // Match Explorer's user-facing behavior: leave the removable volume before
                // Windows completes the eject request. Do not add the disappearing path to history.
                LoadFolder(ctx.State, fallback, false);
            }
        }

        private string GetDriveRemovalFallback(string removingRoot)
        {
            string[] candidates = new string[]
            {
                settings == null ? null : settings.HomePath,
                KnownFolders.Home,
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                Path.GetPathRoot(Environment.SystemDirectory)
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                string candidate = candidates[i];
                if (string.IsNullOrEmpty(candidate)) continue;
                if (IsPathOnDrive(candidate, removingRoot)) continue;
                try { if (Directory.Exists(candidate)) return candidate; } catch { }
            }

            return null;
        }

        private static bool IsPathOnDrive(string path, string root)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(root)) return false;

            try
            {
                string pathRoot = Path.GetPathRoot(path);
                return !string.IsNullOrEmpty(pathRoot) &&
                       string.Equals(pathRoot, root, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private void ScheduleDriveSidebarRefresh()
        {
            if (driveRefreshTimer == null)
            {
                driveRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
                driveRefreshTimer.Tick += delegate
                {
                    driveRefreshTimer.Stop();
                    if (sidebarPanel != null)
                        BuildSidebar();
                    RefreshDriveNotificationRegistrations();
                };
            }

            driveRefreshTimer.Stop();
            driveRefreshTimer.Start();
        }
    }
}
