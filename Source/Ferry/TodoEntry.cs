using System;
using System.ComponentModel;
using System.Web.Script.Serialization;

namespace Ferry
{
    internal sealed class TodoEntry : INotifyPropertyChanged
    {
        private int number;
        private string text;
        private string memo;

        [ScriptIgnore]
        public int Number
        {
            get { return number; }
            set
            {
                if (number == value) return;
                number = value;
                OnPropertyChanged("Number");
            }
        }

        public string Text
        {
            get { return text ?? string.Empty; }
            set
            {
                string next = value ?? string.Empty;
                if (string.Equals(text, next, StringComparison.Ordinal)) return;
                text = next;
                OnPropertyChanged("Text");
            }
        }

        public string Memo
        {
            get { return memo ?? string.Empty; }
            set
            {
                string next = value ?? string.Empty;
                if (string.Equals(memo, next, StringComparison.Ordinal)) return;
                memo = next;
                OnPropertyChanged("Memo");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public TodoEntry()
        {
            text = string.Empty;
            memo = string.Empty;
        }

        private void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(name));
        }
    }
}
