using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;

namespace Ferry
{
    internal sealed class FileItem : INotifyPropertyChanged
    {
        private string itemCountText;
        private int? itemCount;
        private ImageSource icon;
        private ImageSource listIcon;
        private string typeName;
        private bool isRenaming;
        private string renameText;

        public string FullPath { get; set; }
        public string Name { get; set; }
        public bool IsDirectory { get; set; }
        public bool IsHidden { get; set; }
        public long? SizeBytes { get; set; }
        public DateTime Modified { get; set; }
        public DateTime Created { get; set; }
        public string TypeName
        {
            get { return typeName; }
            set { typeName = value; OnPropertyChanged("TypeName"); }
        }
        public string RelativeLocation { get; set; }
        public bool IsRecycleItem { get; set; }
        public string OriginalPath { get; set; }
        public string RecycleMetadataPath { get; set; }

        public int? ItemCount
        {
            get { return itemCount; }
            set { itemCount = value; OnPropertyChanged("ItemCount"); }
        }

        public string ItemCountText
        {
            get { return itemCountText; }
            set { itemCountText = value; OnPropertyChanged("ItemCountText"); }
        }

        // Grid view thumbnail. List view deliberately uses ListIcon so opening a
        // large folder never invokes thumbnail providers just to draw tiny rows.
        public ImageSource Icon
        {
            get { return icon; }
            set { icon = value; OnPropertyChanged("Icon"); }
        }

        public ImageSource ListIcon
        {
            get { return listIcon; }
            set { listIcon = value; OnPropertyChanged("ListIcon"); }
        }

        public bool IsRenaming
        {
            get { return isRenaming; }
            set { if (isRenaming == value) return; isRenaming = value; OnPropertyChanged("IsRenaming"); }
        }

        public string RenameText
        {
            get { return renameText ?? Name ?? string.Empty; }
            set { if (string.Equals(renameText, value, StringComparison.Ordinal)) return; renameText = value; OnPropertyChanged("RenameText"); }
        }

        public void BeginRename()
        {
            RenameText = Name ?? string.Empty;
            IsRenaming = true;
        }

        public void CancelRename()
        {
            RenameText = Name ?? string.Empty;
            IsRenaming = false;
        }

        public void ApplyRenameResult(string newPath)
        {
            FullPath = newPath;
            Name = Path.GetFileName((newPath ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            RenameText = Name ?? string.Empty;
            IsRenaming = false;
            OnPropertyChanged("FullPath");
            OnPropertyChanged("Name");
        }

        public string SizeText
        {
            get
            {
                if (IsDirectory || !SizeBytes.HasValue) return "—";
                return FormatBytes(SizeBytes.Value);
            }
        }

        public string ModifiedText { get { return Modified.ToString("yyyy/MM/dd HH:mm"); } }
        public string CreatedText { get { return Created.ToString("yyyy/MM/dd HH:mm"); } }
        public string DisplayCount { get { return IsDirectory ? (ItemCountText ?? "…") : "—"; } }

        public void UpdateBasicFrom(FileItem source)
        {
            if (source == null) return;
            bool nameChanged = !string.Equals(Name, source.Name, StringComparison.Ordinal);
            bool sizeChanged = SizeBytes != source.SizeBytes;
            bool modifiedChanged = Modified != source.Modified;
            bool createdChanged = Created != source.Created;
            bool hiddenChanged = IsHidden != source.IsHidden;
            bool directoryChanged = IsDirectory != source.IsDirectory;

            FullPath = source.FullPath;
            Name = source.Name;
            IsDirectory = source.IsDirectory;
            IsHidden = source.IsHidden;
            SizeBytes = source.SizeBytes;
            Modified = source.Modified;
            Created = source.Created;
            RelativeLocation = source.RelativeLocation;

            if (nameChanged) OnPropertyChanged("Name");
            if (directoryChanged) { OnPropertyChanged("IsDirectory"); OnPropertyChanged("DisplayCount"); }
            if (hiddenChanged) OnPropertyChanged("IsHidden");
            if (sizeChanged) { OnPropertyChanged("SizeBytes"); OnPropertyChanged("SizeText"); }
            if (modifiedChanged) { OnPropertyChanged("Modified"); OnPropertyChanged("ModifiedText"); }
            if (createdChanged) { OnPropertyChanged("Created"); OnPropertyChanged("CreatedText"); }
            OnPropertyChanged("RelativeLocation");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(name));
            if (name == "ItemCountText")
                if (handler != null) handler(this, new PropertyChangedEventArgs("DisplayCount"));
        }

        private static string FormatBytes(long bytes)
        {
            string[] units = new string[] { "B", "KB", "MB", "GB", "TB" };
            double value = bytes;
            int unit = 0;
            while (value >= 1024 && unit < units.Length - 1) { value /= 1024; unit++; }
            if (unit == 0) return bytes.ToString("N0") + " B";
            return value.ToString(value >= 100 ? "N0" : value >= 10 ? "N1" : "N2") + " " + units[unit];
        }

        public static FileItem FromPath(string path, string baseSearchPath)
        {
            FileAttributes attrs = File.GetAttributes(path);
            bool dir = (attrs & FileAttributes.Directory) != 0;
            FileSystemInfo fsi = dir ? (FileSystemInfo)new DirectoryInfo(path) : new FileInfo(path);
            FileItem item = new FileItem();
            item.FullPath = path;
            item.Name = fsi.Name;
            item.IsDirectory = dir;
            item.IsHidden = (attrs & FileAttributes.Hidden) != 0;
            item.Modified = fsi.LastWriteTime;
            item.Created = fsi.CreationTime;
            item.SizeBytes = dir ? (long?)null : ((FileInfo)fsi).Length;
            item.ItemCountText = dir ? "…" : null;
            item.TypeName = ShellInterop.GetTypeName(path, dir);
            if (!string.IsNullOrEmpty(baseSearchPath))
            {
                try
                {
                    string parent = Path.GetDirectoryName(path);
                    if (parent != null && parent.StartsWith(baseSearchPath, StringComparison.OrdinalIgnoreCase))
                    {
                        string rel = parent.Substring(baseSearchPath.Length).TrimStart(Path.DirectorySeparatorChar);
                        item.RelativeLocation = string.IsNullOrEmpty(rel) ? "." : rel;
                    }
                }
                catch { item.RelativeLocation = string.Empty; }
            }
            return item;
        }
    }
}
