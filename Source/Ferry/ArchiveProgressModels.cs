using System;

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
}
