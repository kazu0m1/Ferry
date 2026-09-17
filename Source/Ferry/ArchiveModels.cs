using System;
using System.Collections.Generic;

namespace Ferry
{
    public enum ArchivePhase
    {
        Scanning,
        WaitingForConfirmation,
        Compressing,
        Extracting,
        Finalizing,
        Completed,
        Cancelled,
        Failed
    }

    public enum ArchiveOverwriteDecision
    {
        Overwrite,
        KeepBoth,
        Skip,
        Cancel
    }

    public enum ArchiveOperationStatus
    {
        Completed,
        Cancelled,
        Partial,
        Failed
    }

    public sealed class ArchiveProgressInfo
    {
        public ArchivePhase Phase { get; set; }
        public string CurrentItem { get; set; }
        public long ProcessedBytes { get; set; }
        public long TotalBytes { get; set; }
        public long CurrentItemProcessedBytes { get; set; }
        public long CurrentItemTotalBytes { get; set; }
        public int ProcessedFiles { get; set; }
        public int TotalFiles { get; set; }
        public double BytesPerSecond { get; set; }
        public TimeSpan? EstimatedRemaining { get; set; }

        public double OverallPercent
        {
            get
            {
                if (TotalBytes <= 0)
                    return TotalFiles == 0 ? 0.0 : Math.Min(100.0, ProcessedFiles * 100.0 / TotalFiles);
                return Math.Min(100.0, ProcessedBytes * 100.0 / TotalBytes);
            }
        }

        public double CurrentItemPercent
        {
            get
            {
                if (CurrentItemTotalBytes <= 0)
                    return 0.0;
                return Math.Min(100.0, CurrentItemProcessedBytes * 100.0 / CurrentItemTotalBytes);
            }
        }
    }

    public sealed class ArchiveOverwriteRequest
    {
        public string TargetPath { get; set; }
        public string ArchiveEntryName { get; set; }
        public bool ArchiveItemIsDirectory { get; set; }
        public bool ExistingItemIsDirectory { get; set; }
        public bool ExistingItemIsReparsePoint { get; set; }
        public string SuggestedKeepBothPath { get; set; }

        // Set only for file conflicts that occur under a folder the user chose to MERGE.
        // This is used only to scope the optional "apply to remaining file conflicts" choice.
        public string MergedFolderScopePath { get; set; }
        internal string MergedFolderScopeKey { get; set; }
    }

    public sealed class ArchiveConflictResolution
    {
        public ArchiveOverwriteDecision Decision { get; set; }

        // Available only for file conflicts under a user-selected MERGE scope.
        public bool ApplyToRemainingFileConflictsUnderMergedFolder { get; set; }

        public ArchiveConflictResolution()
        {
            Decision = ArchiveOverwriteDecision.Cancel;
        }
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
