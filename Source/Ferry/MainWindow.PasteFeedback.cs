using System.Collections.Generic;
using System.Windows;

namespace Ferry
{
    internal sealed partial class MainWindow : Window
    {
        private sealed class PasteEntryStamp
        {
            public bool IsDirectory;
            public long Length;
            public long LastWriteUtcTicks;
            public long CreationUtcTicks;
        }

        private sealed class PasteFeedbackSession
        {
            public string Destination;
            public List<string> SourcePaths;
            public HashSet<string> SourceDirectoryPaths;
            public Dictionary<string, PasteEntryStamp> Before;
            public bool OperationFinished;
            public bool OperationCompleted;
        }
    }
}
