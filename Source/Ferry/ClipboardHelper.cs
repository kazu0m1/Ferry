using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Windows;

namespace Ferry
{
    internal static class ClipboardHelper
    {
        private const string PreferredDropEffect = "Preferred DropEffect";

        public static void Copy(IList<string> paths, bool cut)
        {
            StringCollection collection = new StringCollection();
            for (int i = 0; i < paths.Count; i++) collection.Add(paths[i]);
            DataObject data = new DataObject();
            data.SetFileDropList(collection);
            byte[] effect = BitConverter.GetBytes(cut ? 2 : 1); // MOVE or COPY-compatible effect
            MemoryStream stream = new MemoryStream(effect);
            data.SetData(PreferredDropEffect, stream);
            Clipboard.SetDataObject(data, true);
        }

        public static bool CanPaste()
        {
            return Clipboard.ContainsFileDropList();
        }

        public static bool Paste(string destination)
        {
            if (!Clipboard.ContainsFileDropList()) return false;
            StringCollection files = Clipboard.GetFileDropList();
            List<string> paths = new List<string>();
            for (int i = 0; i < files.Count; i++) paths.Add(files[i]);
            bool move = IsCutOperation();
            bool completed = move ? ShellFileOperations.Move(paths, destination) : ShellFileOperations.Copy(paths, destination);
            if (completed && move)
            {
                try { Clipboard.Clear(); } catch { }
            }
            return completed;
        }

        private static bool IsCutOperation()
        {
            try
            {
                IDataObject obj = Clipboard.GetDataObject();
                if (obj == null || !obj.GetDataPresent(PreferredDropEffect)) return false;
                Stream stream = obj.GetData(PreferredDropEffect) as Stream;
                if (stream == null) return false;
                byte[] bytes = new byte[4];
                stream.Position = 0;
                stream.Read(bytes, 0, 4);
                int value = BitConverter.ToInt32(bytes, 0);
                return (value & 2) == 2;
            }
            catch { return false; }
        }
    }
}
