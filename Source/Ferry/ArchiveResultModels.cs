using System;
using System.Collections.Generic;

namespace Ferry
{
    public enum ArchiveOperationStatus
    {
        Completed,
        Cancelled,
        Partial,
        Failed
    }

    public sealed class ArchiveOperationResult
    {
        public ArchiveOperationStatus Status { get; set; }
        public int CompletedFiles { get; set; }
        public int SkippedFiles { get; set; }
        public int KeptBothConflicts { get; set; }
        public int TotalFiles { get; set; }
        public string ErrorMessage { get; set; }
        public IList<string> CompletedPaths { get; private set; }

        public ArchiveOperationResult()
        {
            CompletedPaths = new List<string>();
        }
    }
}
