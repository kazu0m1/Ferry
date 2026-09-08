using System;
using System.IO;
using System.Text;

namespace Ferry
{
    internal static class Logger
    {
        private static bool enabled;
        private static string path;

        public static void Configure(bool value)
        {
            enabled = value;
            path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Ferry.log");
        }

        public static void Write(string message)
        {
            if (!enabled) return;
            try
            {
                if (File.Exists(path) && new FileInfo(path).Length > 2 * 1024 * 1024)
                    File.WriteAllText(path, "Ferry debug log truncated at 2 MB." + Environment.NewLine, Encoding.UTF8);
                File.AppendAllText(path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + message + Environment.NewLine, Encoding.UTF8);
            }
            catch { }
        }
    }
}
