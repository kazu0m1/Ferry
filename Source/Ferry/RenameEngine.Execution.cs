using System;
using System.Collections.Generic;
using System.IO;

namespace Ferry
{
    internal static partial class RenameEngine
    {
        public static List<RenameUndoRecord> ExecuteRename(IList<RenameEntry> entries)
        {
            List<MoveRecord> temp = new List<MoveRecord>();
            List<MoveRecord> final = new List<MoveRecord>();
            List<RenameUndoRecord> undo = new List<RenameUndoRecord>();
            try
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    RenameEntry entry = entries[i];
                    if (PathsExactlyEqual(entry.SourcePath, entry.TargetPath)) continue;
                    string dir = Path.GetDirectoryName(entry.SourcePath); if (string.IsNullOrEmpty(dir)) dir = Environment.CurrentDirectory;
                    string tmp;
                    do { tmp = Path.Combine(dir, ".~Ferry-rename-" + Guid.NewGuid().ToString("N") + ".tmp"); }
                    while (File.Exists(tmp) || Directory.Exists(tmp));
                    MovePath(entry.SourcePath, tmp);
                    temp.Add(new MoveRecord(entry.SourcePath, tmp, entry.TargetPath));
                }
                for (int i = 0; i < temp.Count; i++)
                {
                    MoveRecord record = temp[i]; MovePath(record.TempPath, record.TargetPath); final.Add(record);
                    undo.Add(new RenameUndoRecord(record.SourcePath, record.TargetPath));
                }
                return undo;
            }
            catch { Rollback(temp, final); throw; }
        }

        public static void Undo(IList<RenameUndoRecord> records)
        {
            List<RenameEntry> entries = new List<RenameEntry>();
            for (int i = 0; i < records.Count; i++)
            {
                RenameUndoRecord r = records[i];
                if (!File.Exists(r.NewPath) && !Directory.Exists(r.NewPath)) throw new IOException("Cannot undo because an item no longer exists: " + r.NewPath);
                entries.Add(new RenameEntry { SourcePath = r.NewPath, NewBaseName = GetBaseName(r.OriginalPath), TargetPath = r.OriginalPath, CurrentName = Path.GetFileName(r.NewPath), NewName = Path.GetFileName(r.OriginalPath), IsValid = true });
            }
            string error = ValidateBatch(entries); if (error != null) throw new IOException(error);
            ExecuteRename(entries);
        }

        private static void MovePath(string source, string target)
        {
            if (Directory.Exists(source)) Directory.Move(source, target); else File.Move(source, target);
        }

        private static void Rollback(List<MoveRecord> temp, List<MoveRecord> final)
        {
            for (int i = final.Count - 1; i >= 0; i--) try { if (File.Exists(final[i].TargetPath) || Directory.Exists(final[i].TargetPath)) MovePath(final[i].TargetPath, final[i].TempPath); } catch { }
            for (int i = temp.Count - 1; i >= 0; i--) try { if (File.Exists(temp[i].TempPath) || Directory.Exists(temp[i].TempPath)) MovePath(temp[i].TempPath, temp[i].SourcePath); } catch { }
        }

        private static bool PathsExactlyEqual(string a, string b) { return string.Equals(Normalize(a), Normalize(b), StringComparison.Ordinal); }

        private sealed class MoveRecord
        {
            public string SourcePath; public string TempPath; public string TargetPath;
            public MoveRecord(string s, string t, string d) { SourcePath = s; TempPath = t; TargetPath = d; }
        }
    }
}
