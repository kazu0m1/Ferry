using System.Windows;

namespace Ferry
{
    internal sealed partial class MainWindow : Window
    {
        private sealed class PinnedSidebarItem
        {
            public PinnedSidebarItem(string name, string fullPath) { Name = name; FullPath = fullPath; }
            public string Name { get; private set; }
            public string FullPath { get; private set; }
        }
    }
}
