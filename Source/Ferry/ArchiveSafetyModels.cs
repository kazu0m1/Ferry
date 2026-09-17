using System.Collections.Generic;

namespace Ferry
{
    public enum ArchiveSafetyIssueType
    {
        ExpandedSize,
        FileCount,
        CompressionRatio
    }

    public sealed class ArchiveThresholds
    {
        public long ExpandedSizeWarningBytes { get; set; }
        public int FileCountWarning { get; set; }
        public double CompressionRatioWarning { get; set; }

        public ArchiveThresholds()
        {
            ExpandedSizeWarningBytes = 20L * 1024L * 1024L * 1024L; // 20 GiB
            FileCountWarning = 50000;
            CompressionRatioWarning = 100.0;
        }
    }

    public sealed class ArchiveSafetyIssue
    {
        public ArchiveSafetyIssueType Type { get; set; }
        public string Message { get; set; }
    }

    public sealed class ArchiveSafetyReport
    {
        public long CompressedBytes { get; set; }
        public long ExpandedBytes { get; set; }
        public int FileCount { get; set; }
        public int DirectoryCount { get; set; }
        public double CompressionRatio { get; set; }
        public IList<ArchiveSafetyIssue> Issues { get; private set; }

        public bool RequiresConfirmation
        {
            get { return Issues.Count > 0; }
        }

        public ArchiveSafetyReport()
        {
            Issues = new List<ArchiveSafetyIssue>();
        }
    }
}
