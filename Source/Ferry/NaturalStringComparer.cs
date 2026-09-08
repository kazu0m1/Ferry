using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Ferry
{
    internal sealed class NaturalStringComparer : IComparer<string>
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern int StrCmpLogicalW(string x, string y);

        public int Compare(string x, string y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            try { return StrCmpLogicalW(x, y); }
            catch { return string.Compare(x, y, StringComparison.CurrentCultureIgnoreCase); }
        }
    }
}
