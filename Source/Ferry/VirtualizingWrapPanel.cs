using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Ferry
{
    internal sealed class VirtualizingWrapPanel : VirtualizingPanel, IScrollInfo
    {
        public static readonly DependencyProperty ItemWidthProperty = DependencyProperty.Register("ItemWidth", typeof(double), typeof(VirtualizingWrapPanel), new FrameworkPropertyMetadata(164.0, FrameworkPropertyMetadataOptions.AffectsMeasure));
        public static readonly DependencyProperty ItemHeightProperty = DependencyProperty.Register("ItemHeight", typeof(double), typeof(VirtualizingWrapPanel), new FrameworkPropertyMetadata(145.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        private Size extent = new Size(0, 0);
        private Size viewport = new Size(0, 0);
        private Point offset;

        public double ItemWidth { get { return (double)GetValue(ItemWidthProperty); } set { SetValue(ItemWidthProperty, value); } }
        public double ItemHeight { get { return (double)GetValue(ItemHeightProperty); } set { SetValue(ItemHeightProperty, value); } }

        protected override Size MeasureOverride(Size availableSize)
        {
            ItemsControl owner = ItemsControl.GetItemsOwner(this);
            if (owner == null || owner.Items.Count == 0)
            {
                UpdateScrollInfo(availableSize, new Size(0, 0));
                return availableSize;
            }

            double width = double.IsInfinity(availableSize.Width) ? Math.Max(ItemWidth, viewport.Width) : availableSize.Width;
            double height = double.IsInfinity(availableSize.Height) ? viewport.Height : availableSize.Height;
            if (height <= 0 || double.IsInfinity(height)) height = ItemHeight * 4;
            int perRow = Math.Max(1, (int)Math.Floor(width / ItemWidth));
            int totalRows = (int)Math.Ceiling((double)owner.Items.Count / perRow);
            Size newExtent = new Size(width, totalRows * ItemHeight);
            UpdateScrollInfo(new Size(width, height), newExtent);

            int firstRow = Math.Max(0, (int)Math.Floor(VerticalOffset / ItemHeight));
            int visibleRows = Math.Max(1, (int)Math.Ceiling(height / ItemHeight) + 1);
            int startIndex = Math.Min(owner.Items.Count - 1, firstRow * perRow);
            int endIndex = Math.Min(owner.Items.Count - 1, ((firstRow + visibleRows) * perRow) - 1);

            ItemContainerGenerator concreteGenerator = owner.ItemContainerGenerator;
            IItemContainerGenerator generator = concreteGenerator;
            CleanupItems(concreteGenerator, generator, startIndex, endIndex);
            GeneratorPosition startPos = generator.GeneratorPositionFromIndex(startIndex);
            int childIndex = startPos.Offset == 0 ? startPos.Index : startPos.Index + 1;

            using (generator.StartAt(startPos, GeneratorDirection.Forward, true))
            {
                for (int itemIndex = startIndex; itemIndex <= endIndex; itemIndex++, childIndex++)
                {
                    bool newlyRealized;
                    UIElement child = generator.GenerateNext(out newlyRealized) as UIElement;
                    if (child == null) continue;
                    if (newlyRealized)
                    {
                        if (childIndex >= InternalChildren.Count) AddInternalChild(child); else InsertInternalChild(childIndex, child);
                        generator.PrepareItemContainer(child);
                    }
                    child.Measure(new Size(ItemWidth, ItemHeight));
                }
            }
            return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            ItemsControl owner = ItemsControl.GetItemsOwner(this);
            if (owner == null) return finalSize;
            int perRow = Math.Max(1, (int)Math.Floor(finalSize.Width / ItemWidth));
            ItemContainerGenerator generator = owner.ItemContainerGenerator;
            for (int i = 0; i < InternalChildren.Count; i++)
            {
                UIElement child = InternalChildren[i];
                int itemIndex = generator.IndexFromContainer(child);
                if (itemIndex < 0) continue;
                int row = itemIndex / perRow; int column = itemIndex % perRow;
                double x = column * ItemWidth - HorizontalOffset; double y = row * ItemHeight - VerticalOffset;
                child.Arrange(new Rect(x, y, ItemWidth, ItemHeight));
            }
            return finalSize;
        }

        private void CleanupItems(ItemContainerGenerator concreteGenerator, IItemContainerGenerator generator, int min, int max)
        {
            for (int i = InternalChildren.Count - 1; i >= 0; i--)
            {
                UIElement child = InternalChildren[i]; int itemIndex = concreteGenerator.IndexFromContainer(child);
                if (itemIndex < min || itemIndex > max)
                {
                    GeneratorPosition position = generator.GeneratorPositionFromIndex(itemIndex);
                    if (position.Index >= 0) generator.Remove(position, 1);
                    RemoveInternalChildRange(i, 1);
                }
            }
        }

        private void UpdateScrollInfo(Size newViewport, Size newExtent)
        {
            bool changed = viewport != newViewport || extent != newExtent;
            viewport = newViewport; extent = newExtent;
            if (VerticalOffset > Math.Max(0, ExtentHeight - ViewportHeight)) offset.Y = Math.Max(0, ExtentHeight - ViewportHeight);
            if (HorizontalOffset > Math.Max(0, ExtentWidth - ViewportWidth)) offset.X = Math.Max(0, ExtentWidth - ViewportWidth);
            if (changed && ScrollOwner != null) ScrollOwner.InvalidateScrollInfo();
        }

        public bool CanHorizontallyScroll { get; set; }
        public bool CanVerticallyScroll { get; set; }
        public double ExtentHeight { get { return extent.Height; } }
        public double ExtentWidth { get { return extent.Width; } }
        public double HorizontalOffset { get { return offset.X; } }
        public double VerticalOffset { get { return offset.Y; } }
        public double ViewportHeight { get { return viewport.Height; } }
        public double ViewportWidth { get { return viewport.Width; } }
        public ScrollViewer ScrollOwner { get; set; }

        public void LineUp() { SetVerticalOffset(VerticalOffset - Math.Max(16, ItemHeight / 3)); }
        public void LineDown() { SetVerticalOffset(VerticalOffset + Math.Max(16, ItemHeight / 3)); }
        public void LineLeft() { SetHorizontalOffset(HorizontalOffset - 16); }
        public void LineRight() { SetHorizontalOffset(HorizontalOffset + 16); }
        public void MouseWheelUp() { SetVerticalOffset(VerticalOffset - ItemHeight); }
        public void MouseWheelDown() { SetVerticalOffset(VerticalOffset + ItemHeight); }
        public void MouseWheelLeft() { SetHorizontalOffset(HorizontalOffset - ItemWidth); }
        public void MouseWheelRight() { SetHorizontalOffset(HorizontalOffset + ItemWidth); }
        public void PageUp() { SetVerticalOffset(VerticalOffset - ViewportHeight); }
        public void PageDown() { SetVerticalOffset(VerticalOffset + ViewportHeight); }
        public void PageLeft() { SetHorizontalOffset(HorizontalOffset - ViewportWidth); }
        public void PageRight() { SetHorizontalOffset(HorizontalOffset + ViewportWidth); }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            UIElement child = visual as UIElement;
            ItemsControl owner = ItemsControl.GetItemsOwner(this);
            if (child == null || owner == null) return rectangle;
            int index = owner.ItemContainerGenerator.IndexFromContainer(child);
            if (index < 0) return rectangle;
            int perRow = Math.Max(1, (int)Math.Floor(Math.Max(ItemWidth, ViewportWidth) / ItemWidth));
            int row = index / perRow; double top = row * ItemHeight; double bottom = top + ItemHeight;
            if (top < VerticalOffset) SetVerticalOffset(top); else if (bottom > VerticalOffset + ViewportHeight) SetVerticalOffset(bottom - ViewportHeight);
            return new Rect(0, top, ItemWidth, ItemHeight);
        }

        public void SetHorizontalOffset(double value)
        {
            value = Math.Max(0, Math.Min(value, Math.Max(0, ExtentWidth - ViewportWidth))); if (Math.Abs(value - offset.X) < 0.1) return; offset.X = value; InvalidateMeasure(); if (ScrollOwner != null) ScrollOwner.InvalidateScrollInfo();
        }
        public void SetVerticalOffset(double value)
        {
            value = Math.Max(0, Math.Min(value, Math.Max(0, ExtentHeight - ViewportHeight))); if (Math.Abs(value - offset.Y) < 0.1) return; offset.Y = value; InvalidateMeasure(); if (ScrollOwner != null) ScrollOwner.InvalidateScrollInfo();
        }
    }
}
