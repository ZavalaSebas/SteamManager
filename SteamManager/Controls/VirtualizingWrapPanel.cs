using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace SteamManager.Controls;

public class VirtualizingWrapPanel : VirtualizingPanel, IScrollInfo
{
    private Size _extent = new(0, 0);
    private Size _viewport = new(0, 0);
    private Point _offset;
    private WrapPanelAbstraction? _abstractPanel;

    public VirtualizingWrapPanel()
    {
        CanHorizontallyScroll = false;
        CanVerticallyScroll = true;
    }

    public bool CanHorizontallyScroll { get; set; }
    public bool CanVerticallyScroll { get; set; }
    public double ExtentHeight => _extent.Height;
    public double ExtentWidth => _extent.Width;
    public double HorizontalOffset => _offset.X;
    public double VerticalOffset => _offset.Y;
    public double ViewportHeight => _viewport.Height;
    public double ViewportWidth => _viewport.Width;
    public ScrollViewer? ScrollOwner { get; set; }

    public void LineDown() => SetVerticalOffset(VerticalOffset + 20);
    public void LineUp() => SetVerticalOffset(VerticalOffset - 20);
    public void PageDown() => SetVerticalOffset(VerticalOffset + ViewportHeight);
    public void PageUp() => SetVerticalOffset(VerticalOffset - ViewportHeight);
    public void MouseWheelDown() => SetVerticalOffset(VerticalOffset + 40);
    public void MouseWheelUp() => SetVerticalOffset(VerticalOffset - 40);
    public void LineLeft() { }
    public void LineRight() { }
    public void PageLeft() { }
    public void PageRight() { }
    public void MouseWheelLeft() { }
    public void MouseWheelRight() { }
    public Rect MakeVisible(Visual visual, Rect rectangle) => rectangle;

    public void SetHorizontalOffset(double offset)
    {
        if (offset < 0 || ViewportWidth >= ExtentWidth) offset = 0;
        else if (offset + ViewportWidth >= ExtentWidth) offset = ExtentWidth - ViewportWidth;
        _offset.X = offset;
        ScrollOwner?.InvalidateScrollInfo();
        InvalidateMeasure();
    }

    public void SetVerticalOffset(double offset)
    {
        if (offset < 0 || ViewportHeight >= ExtentHeight) offset = 0;
        else if (offset + ViewportHeight >= ExtentHeight) offset = ExtentHeight - ViewportHeight;
        _offset.Y = offset;
        ScrollOwner?.InvalidateScrollInfo();
        InvalidateMeasure();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (InternalChildren.Count == 0 && ItemsControl.GetItemsOwner(this) is { } itemsControl)
        {
            _abstractPanel = new WrapPanelAbstraction(itemsControl.Items.Count);
        }
        _abstractPanel?.SetItemSize(availableSize.Width > 0 ? new Size(180, 105) : new Size(180, 105));
        // Simplified: estimate extent based on item count and wrap
        if (_abstractPanel != null && availableSize.Width > 0)
        {
            int itemsPerRow = Math.Max(1, (int)(availableSize.Width / 180));
            int rows = (int)Math.Ceiling((double)_abstractPanel.ItemCount / itemsPerRow);
            _extent = new Size(availableSize.Width, rows * 115);
            _viewport = availableSize;
            ScrollOwner?.InvalidateScrollInfo();
        }
        // Realize visible children
        var generator = ItemContainerGenerator;
        var start = GetStartIndex();
        var end = GetEndIndex(start, availableSize);
        // Clean up
        CleanUpItems(start, end);
        // Generate
        for (int i = start; i <= end && i < _abstractPanel?.ItemCount; i++)
        {
            bool newlyRealized;
            var child = generator.GenerateNext(out newlyRealized) as UIElement;
            if (newlyRealized)
            {
                if (child != null)
                {
                    AddInternalChild(child);
                    generator.PrepareItemContainer(child);
                }
            }
            else if (child != null && !InternalChildren.Contains(child))
            {
                AddInternalChild(child);
            }
            child?.Measure(new Size(180, 105));
        }
        return availableSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_abstractPanel == null) return finalSize;
        int itemsPerRow = Math.Max(1, (int)(finalSize.Width / 180));
        for (int i = 0; i < InternalChildren.Count; i++)
        {
            var child = InternalChildren[i];
            var itemIndex = GetStartIndex() + i;
            int row = itemIndex / itemsPerRow;
            int col = itemIndex % itemsPerRow;
            double x = col * 180 + col * 8;
            double y = row * 115 - VerticalOffset;
            child.Arrange(new Rect(x, y, 180, 105));
        }
        return finalSize;
    }

    private int GetStartIndex()
    {
        if (_abstractPanel == null) return 0;
        int itemsPerRow = Math.Max(1, (int)(_viewport.Width / 180));
        int startRow = (int)(VerticalOffset / 115);
        return startRow * itemsPerRow;
    }

    private int GetEndIndex(int start, Size availableSize)
    {
        if (_abstractPanel == null) return -1;
        int itemsPerRow = Math.Max(1, (int)(availableSize.Width / 180));
        int visibleRows = (int)Math.Ceiling(availableSize.Height / 115) + 1;
        int end = start + (visibleRows * itemsPerRow) - 1;
        return Math.Min(end, _abstractPanel.ItemCount - 1);
    }

    private void CleanUpItems(int start, int end)
    {
        var gen = (System.Windows.Controls.ItemContainerGenerator)ItemContainerGenerator;
        for (int i = InternalChildren.Count - 1; i >= 0; i--)
        {
            var child = InternalChildren[i];
            var itemIndex = gen.IndexFromContainer(child);
            if (itemIndex < start || itemIndex > end)
            {
                RemoveInternalChildRange(i, 1);
            }
        }
    }

    private class WrapPanelAbstraction
    {
        public int ItemCount { get; }
        public WrapPanelAbstraction(int count) => ItemCount = count;
        public void SetItemSize(Size s) { }
    }
}
