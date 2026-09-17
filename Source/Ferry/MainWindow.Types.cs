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
