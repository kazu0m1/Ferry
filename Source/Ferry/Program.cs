using System;
using System.IO;
using System.Windows;

namespace Ferry
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            AppSettings settings = SettingsStore.Load();
            Logger.Configure(settings.DebugLogging);

            Application app = new Application();
            app.ShutdownMode = ShutdownMode.OnLastWindowClose;

            string initial = null;
            if (args != null && args.Length > 0 && Directory.Exists(args[0])) initial = args[0];
            MainWindow window = new MainWindow(settings, initial);
            app.Run(window);
        }
    }
}
