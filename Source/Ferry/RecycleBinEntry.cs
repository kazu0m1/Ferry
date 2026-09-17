using System;

namespace Ferry
{
    internal sealed class RecycleBinEntry
    {
        public string RecycledPath { get; set; }
        public string MetadataPath { get; set; }
        public string OriginalPath { get; set; }
        public string Name { get; set; }
        public bool IsDirectory { get; set; }
        public long SizeBytes { get; set; }
        public DateTime DeletedAt { get; set; }
    }
}
