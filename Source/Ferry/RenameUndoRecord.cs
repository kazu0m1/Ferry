namespace Ferry
{
    internal sealed class RenameUndoRecord
    {
        public string OriginalPath; public string NewPath;
        public RenameUndoRecord(string original, string newer) { OriginalPath = original; NewPath = newer; }
    }
}
