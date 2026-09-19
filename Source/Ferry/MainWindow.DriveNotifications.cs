using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Ferry
{
    internal sealed partial class MainWindow
    {
        private const int WmDeviceChange = 0x0219;
        private const int DbtDeviceArrival = 0x8000;
        private const int DbtDeviceRemoveComplete = 0x8004;
        private const int DbtDevTypVolume = 0x00000002;

        [StructLayout(LayoutKind.Sequential)]
        private struct DeviceBroadcastHeader
        {
            public int Size;
            public int DeviceType;
            public int Reserved;
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
            if (message != WmDeviceChange) return IntPtr.Zero;

            int change = wParam.ToInt32();
            if (change != DbtDeviceArrival && change != DbtDeviceRemoveComplete) return IntPtr.Zero;
            if (lParam == IntPtr.Zero) return IntPtr.Zero;

            try
            {
                DeviceBroadcastHeader header = (DeviceBroadcastHeader)Marshal.PtrToStructure(lParam, typeof(DeviceBroadcastHeader));
                if (header.DeviceType == DbtDevTypVolume)
                    ScheduleDriveSidebarRefresh();
            }
            catch
            {
                // Device notifications are advisory. A malformed or transient notification
                // must never interfere with the normal Ferry window message loop.
            }

            return IntPtr.Zero;
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
