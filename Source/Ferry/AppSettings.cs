using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace Ferry
{
    internal sealed class AppSettings
    {
        public string HomePath { get; set; }
        public string DefaultView { get; set; }
        public bool ShowHidden { get; set; }
        public string SortKey { get; set; }
        public bool SortDescending { get; set; }
        public bool SortFoldersFirst { get; set; }
        public string SearchMode { get; set; }
        public double WindowWidth { get; set; }
        public double WindowHeight { get; set; }
        public double WindowLeft { get; set; }
        public double WindowTop { get; set; }
        public bool WindowMaximized { get; set; }
        public double SidebarWidth { get; set; }
        public bool SidebarVisible { get; set; }
        public double GridIconSize { get; set; }
        public double RubberBandAutoScrollSpeed { get; set; }
        public string TerminalCommand { get; set; }
        public string TerminalArguments { get; set; }
        public List<string> PinnedFolders { get; set; }
        public Dictionary<string, double> ColumnWidths { get; set; }
        public List<string> ColumnOrder { get; set; }
        public List<string> HiddenColumns { get; set; }
        public bool DebugLogging { get; set; }

        public AppSettings()
        {
            HomePath = KnownFolders.Home;
            DefaultView = "List";
            ShowHidden = false;
            SortKey = "Name";
            SortDescending = false;
            SortFoldersFirst = true;
            SearchMode = "Contains";
            WindowWidth = 1180;
            WindowHeight = 760;
            WindowLeft = -1;
            WindowTop = -1;
            WindowMaximized = false;
            SidebarWidth = 220;
            SidebarVisible = true;
            GridIconSize = 64;
            RubberBandAutoScrollSpeed = 100;
            TerminalCommand = string.Empty;
            TerminalArguments = string.Empty;
            PinnedFolders = new List<string>();
            ColumnWidths = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            ColumnWidths["Name"] = 420;
            ColumnWidths["Items"] = 90;
            ColumnWidths["Type"] = 160;
            ColumnWidths["Size"] = 110;
            ColumnWidths["Modified"] = 160;
            ColumnWidths["Created"] = 160;
            ColumnOrder = new List<string>(new string[] { "Name", "Items", "Type", "Size", "Modified", "Created" });
            HiddenColumns = new List<string>(new string[] { "Created" });
            DebugLogging = false;
        }
    }
}
