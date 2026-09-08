using System.ComponentModel;

namespace Ferry
{
    internal sealed class RenameEntry : INotifyPropertyChanged
    {
        private string newName;
        private string status;
        private bool isValid;

        public string SourcePath { get; set; }
        public string CurrentName { get; set; }
        public string NewBaseName { get; set; }
        public string TargetPath { get; set; }

        public string NewName { get { return newName; } set { newName = value; OnChanged("NewName"); } }
        public string Status { get { return status; } set { status = value; OnChanged("Status"); } }
        public bool IsValid { get { return isValid; } set { isValid = value; OnChanged("IsValid"); } }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnChanged(string name) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name)); }
    }
}
