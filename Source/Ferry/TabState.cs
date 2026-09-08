using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace Ferry
{
    internal sealed class TabState
    {
        public const string RecycleBinPath = "Ferry::RecycleBin";
        public Guid Id { get; private set; }
        public string CurrentPath { get; set; }
        public string Title { get; set; }
        public ObservableCollection<FileItem> Items { get; private set; }
        public List<string> BackHistory { get; private set; }
        public List<string> ForwardHistory { get; private set; }
        public bool IsSearching { get; set; }
        public string SearchText { get; set; }
        public CancellationTokenSource LoadCancellation { get; set; }
        public CancellationTokenSource SearchCancellation { get; set; }
        public int RefreshGeneration;
        private readonly Dictionary<string, long> unsortedTailOrder = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        private long nextUnsortedTailOrder;
        public bool IsRecycleBin { get { return IsRecycleBinPath(CurrentPath); } }

        public TabState(string path)
        {
            Id = Guid.NewGuid();
            CurrentPath = path;
            Title = BuildTitle(path);
            Items = new ObservableCollection<FileItem>();
            BackHistory = new List<string>();
            ForwardHistory = new List<string>();
            SearchText = string.Empty;
        }


        public void MarkUnsortedTail(string path)
        {
            if (string.IsNullOrEmpty(path) || unsortedTailOrder.ContainsKey(path)) return;
            unsortedTailOrder[path] = ++nextUnsortedTailOrder;
        }

        public void RemoveUnsortedTail(string path)
        {
            if (!string.IsNullOrEmpty(path)) unsortedTailOrder.Remove(path);
        }

        public void ClearUnsortedTail()
        {
            unsortedTailOrder.Clear();
            nextUnsortedTailOrder = 0;
        }

        public bool TryGetUnsortedTailOrder(string path, out long order)
        {
            if (string.IsNullOrEmpty(path)) { order = 0; return false; }
            return unsortedTailOrder.TryGetValue(path, out order);
        }

        public static bool IsRecycleBinPath(string path)
        {
            return string.Equals(path, RecycleBinPath, StringComparison.OrdinalIgnoreCase);
        }

        public static string BuildTitle(string path)
        {
            if (IsRecycleBinPath(path)) return "Recycle Bin";
            try
            {
                string trimmed = path.TrimEnd('\\', '/');
                string name = System.IO.Path.GetFileName(trimmed);
                if (!string.IsNullOrEmpty(name)) return name;
                if (trimmed.EndsWith(":")) return trimmed + "\\";
            }
            catch { }
            return path;
        }

        public void CancelBackgroundWork()
        {
            try { if (LoadCancellation != null) LoadCancellation.Cancel(); } catch { }
            try { if (SearchCancellation != null) SearchCancellation.Cancel(); } catch { }
        }
    }
}
