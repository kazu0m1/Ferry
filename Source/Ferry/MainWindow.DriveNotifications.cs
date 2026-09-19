using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Ferry
{
    internal sealed partial class MainWindow
    {
        private const int WmDeviceChange = 0x0219;
        private const int DbtDeviceArrival = 0x8000;
        private const int DbtDeviceQueryRemove = 0x8001;
        private const int DbtDeviceQueryRemoveFailed = 0x8002;
        private const int DbtDeviceRemovePending = 0x8003;
        private const int DbtDeviceRemoveComplete = 0x8004;
        private const int DbtDevTypVolume = 0x00000002;

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

        private HwndSource driveNotificationSource;
        private DispatcherTimer driveRefreshTimer;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            driveNotificationSource = PresentationSource.FromVisual(this) as HwndSource;
            if (driveNotificationSource != null)
                driveNotificationSource.AddHook(DriveNotificationWndProc);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (driveRefreshTimer != null)
            {
                driveRefreshTimer.Stop();
                driveRefreshTimer = null;
            }

            if (driveNotificationSource != null)
            {
                driveNotificationSource.RemoveHook(DriveNotificationWndProc);
                driveNotificationSource = null;
            }

            base.OnClosed(e);
        }

        private IntPtr DriveNotificationWndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (message != WmDeviceChange || lParam == IntPtr.Zero) return IntPtr.Zero;

            int change = wParam.ToInt32();

            try
            {
                DeviceBroadcastHeader header = (DeviceBroadcastHeader)Marshal.PtrToStructure(lParam, typeof(DeviceBroadcastHeader));
                if (header.DeviceType != DbtDevTypVolume) return IntPtr.Zero;

                DeviceBroadcastVolume volume = (DeviceBroadcastVolume)Marshal.PtrToStructure(lParam, typeof(DeviceBroadcastVolume));

                if (change == DbtDeviceQueryRemove || change == DbtDeviceRemovePending)
                {
                    PrepareTabsForVolumeRemoval(volume.UnitMask);
                    return IntPtr.Zero;
                }

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

        private void PrepareTabsForVolumeRemoval(uint unitMask)
        {
            string fallback = GetVolumeRemovalFallback(unitMask);
            if (string.IsNullOrEmpty(fallback)) return;

            foreach (TabViewContext ctx in contexts.Values)
            {
                if (ctx == null || ctx.State == null || ctx.State.IsRecycleBin) continue;
                if (!IsPathOnVolumeMask(ctx.State.CurrentPath, unitMask)) continue;

                // FileSystemWatcher owns a live handle to the watched volume. Dispose it
                // synchronously before the device-query message returns so Windows can eject.
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

        private string GetVolumeRemovalFallback(uint unitMask)
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
                if (IsPathOnVolumeMask(candidate, unitMask)) continue;
                try { if (Directory.Exists(candidate)) return candidate; } catch { }
            }

            return null;
        }

        private static bool IsPathOnVolumeMask(string path, uint unitMask)
        {
            if (string.IsNullOrEmpty(path) || unitMask == 0) return false;

            try
            {
                string root = Path.GetPathRoot(path);
                if (string.IsNullOrEmpty(root) || root.Length < 2 || root[1] != ':') return false;

                char drive = char.ToUpperInvariant(root[0]);
                int index = drive - 'A';
                if (index < 0 || index > 25) return false;

                uint bit = 1u << index;
                return (unitMask & bit) != 0;
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
                };
            }

            driveRefreshTimer.Stop();
            driveRefreshTimer.Start();
        }
    }
}
