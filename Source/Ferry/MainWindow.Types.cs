using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace Ferry
{
    internal sealed partial class MainWindow : Window
    {
        private sealed class PinnedSidebarItem
        {
            public PinnedSidebarItem(string name, string fullPath) { Name = name; FullPath = fullPath; }
            public string Name { get; private set; }
            public string FullPath { get; private set; }
        }

        private sealed class InverseBooleanToVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                return value is bool && (bool)value ? Visibility.Collapsed : Visibility.Visible;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class PasteEntryStamp
        {
            public bool IsDirectory;
            public long Length;
            public long LastWriteUtcTicks;
            public long CreationUtcTicks;
        }

        private sealed class PasteFeedbackSession
        {
            public string Destination;
            public List<string> SourcePaths;
            public HashSet<string> SourceDirectoryPaths;
            public Dictionary<string, PasteEntryStamp> Before;
            public bool OperationFinished;
            public bool OperationCompleted;
        }

        private sealed class TabViewContext
        {
            public TabState State; public TabItem TabItem; public Grid Container; public ListView ListView; public ListBox GridView; public GridView ListGrid; public FileSystemWatcher Watcher; public DispatcherTimer RefreshTimer; public DispatcherTimer SearchDrainTimer; public CancellationTokenSource GridThumbnailCancellation;
            public PasteFeedbackSession PasteFeedback;
            public bool IsReconciling;
            public List<FileItem> PreSearchItems; public HashSet<string> PreSearchSelection;
            public Control DropTargetContainer; public object DropTargetBackgroundLocal = DependencyProperty.UnsetValue; public object DropTargetBorderBrushLocal = DependencyProperty.UnsetValue; public object DropTargetBorderThicknessLocal = DependencyProperty.UnsetValue;
            public bool ItemDragArmed; public bool PendingMultiSelectionClick; public Selector PendingMultiSelectionView; public FileItem PendingMultiSelectionItem; public bool PendingMultiSelectionDragStarted;
            public Canvas SelectionOverlay; public System.Windows.Shapes.Rectangle RubberBandRectangle; public System.Windows.Shapes.Rectangle SelectionAnchorRectangle;
            public bool RubberBandPending; public bool RubberBandActive; public Selector RubberBandView; public Point RubberBandStart; public FileItem RubberBandStartItem; public FileItem RubberBandBackgroundFocusItem;
            public ModifierKeys RubberBandModifiers; public HashSet<FileItem> RubberBandInitialSelection; public bool RubberBandPassThroughPending;
            public Point RubberBandCurrentPoint; public HashSet<FileItem> RubberBandCurrentHitSet; public int RubberBandLastVerticalDirection;
            public FileItem RubberBandSelectionAnchor; public HashSet<FileItem> RubberBandCtrlShiftBaseSelection;
            public bool RubberBandCtrlShiftTrueBackgroundMode;
            public bool RubberBandTransitionMode; public bool RubberBandCtrlShiftShiftReleasedMode;
            public HashSet<FileItem> RubberBandCtrlShiftShiftReleasedToggleSet;
            public FileItem SelectionAnchorItem;
            public FileItem KeyboardNavigationItem;
            public DispatcherTimer RubberBandAutoScrollTimer; public double RubberBandAutoScrollAccumulator;
        }
    }
}
