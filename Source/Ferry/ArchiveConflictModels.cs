namespace Ferry
{
    public enum ArchiveOverwriteDecision
    {
        Overwrite,
        KeepBoth,
        Skip,
        Cancel
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
}
