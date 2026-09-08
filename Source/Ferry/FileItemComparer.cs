using System;
using System.Collections;

namespace Ferry
{
    internal sealed class FileItemComparer : IComparer
    {
        private readonly string key;
        private readonly bool descending;
        private readonly bool sortFoldersFirst;
        private readonly TabState state;
        private readonly bool keepUnsortedTail;
        private readonly NaturalStringComparer natural = new NaturalStringComparer();

        public FileItemComparer(string key, bool descending, bool sortFoldersFirst)
            : this(key, descending, sortFoldersFirst, null, false)
        {
        }

        public FileItemComparer(string key, bool descending, bool sortFoldersFirst, TabState state, bool keepUnsortedTail)
        {
            this.key = string.IsNullOrEmpty(key) ? "Name" : key;
            this.descending = descending;
            this.sortFoldersFirst = sortFoldersFirst;
            this.state = state;
            this.keepUnsortedTail = keepUnsortedTail;
        }

        public int Compare(object x, object y)
        {
            FileItem a = x as FileItem; FileItem b = y as FileItem;
            if (a == null && b == null) return 0; if (a == null) return -1; if (b == null) return 1;

            // RC14: file-system additions stay in a stable unsorted tail until the user explicitly
            // requests sorting (F5, Refresh, a column header, or settings-driven reload). This keeps
            // frequently-changing downloads from moving item containers under the mouse.
            if (keepUnsortedTail && state != null)
            {
                long aOrder; long bOrder;
                bool aTail = state.TryGetUnsortedTailOrder(a.FullPath, out aOrder);
                bool bTail = state.TryGetUnsortedTailOrder(b.FullPath, out bOrder);
                if (aTail != bTail) return aTail ? 1 : -1;
                if (aTail && bTail) return aOrder.CompareTo(bOrder);
            }

            // "Items" exists only for folders, so folders are always the meaningful group there.
            if (string.Equals(key, "Items", StringComparison.OrdinalIgnoreCase) && a.IsDirectory != b.IsDirectory)
                return a.IsDirectory ? -1 : 1;

            // For comparable columns, Ferry can follow Nautilus-style mixed sorting unless the user
            // explicitly enables "Sort folders before files".
            if (sortFoldersFirst && a.IsDirectory != b.IsDirectory)
                return a.IsDirectory ? -1 : 1;

            // Ferry intentionally does not calculate recursive folder sizes. With folder-first OFF,
            // files sort by actual size and folders form a stable name-sorted group after them.
            if (string.Equals(key, "Size", StringComparison.OrdinalIgnoreCase) && a.IsDirectory != b.IsDirectory)
                return a.IsDirectory ? 1 : -1;

            int result = CompareCore(a, b);
            if (result == 0) result = natural.Compare(a.Name, b.Name);
            return descending ? -result : result;
        }

        private int CompareCore(FileItem a, FileItem b)
        {
            if (string.Equals(key, "Items", StringComparison.OrdinalIgnoreCase))
            {
                if (!a.IsDirectory || !b.IsDirectory) return natural.Compare(a.Name, b.Name);
                int av = a.ItemCount.HasValue ? a.ItemCount.Value : -1;
                int bv = b.ItemCount.HasValue ? b.ItemCount.Value : -1;
                return av.CompareTo(bv);
            }
            if (string.Equals(key, "Type", StringComparison.OrdinalIgnoreCase))
                return string.Compare(a.TypeName, b.TypeName, StringComparison.CurrentCultureIgnoreCase);
            if (string.Equals(key, "Size", StringComparison.OrdinalIgnoreCase))
            {
                if (a.IsDirectory && b.IsDirectory) return natural.Compare(a.Name, b.Name);
                long av = a.SizeBytes.HasValue ? a.SizeBytes.Value : -1;
                long bv = b.SizeBytes.HasValue ? b.SizeBytes.Value : -1;
                return av.CompareTo(bv);
            }
            if (string.Equals(key, "Modified", StringComparison.OrdinalIgnoreCase)) return a.Modified.CompareTo(b.Modified);
            if (string.Equals(key, "Created", StringComparison.OrdinalIgnoreCase)) return a.Created.CompareTo(b.Created);
            return natural.Compare(a.Name, b.Name);
        }
    }
}
