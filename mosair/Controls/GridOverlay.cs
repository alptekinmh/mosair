using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace mosair.Controls;

public class GridOverlay : Control
{
    public static readonly StyledProperty<bool> ShowGridProperty =
        AvaloniaProperty.Register<GridOverlay, bool>(nameof(ShowGrid));

    public static readonly StyledProperty<int> StoneSizeProperty =
        AvaloniaProperty.Register<GridOverlay, int>(nameof(StoneSize), 20);

    public static readonly StyledProperty<int> BitmapWidthProperty =
        AvaloniaProperty.Register<GridOverlay, int>(nameof(BitmapWidth));

    public static readonly StyledProperty<int> BitmapHeightProperty =
        AvaloniaProperty.Register<GridOverlay, int>(nameof(BitmapHeight));

    public static readonly StyledProperty<int> StoneColumnsProperty =
        AvaloniaProperty.Register<GridOverlay, int>(nameof(StoneColumns));

    public static readonly StyledProperty<int> StoneRowsProperty =
        AvaloniaProperty.Register<GridOverlay, int>(nameof(StoneRows));

    public static readonly StyledProperty<Color> GridColorProperty =
        AvaloniaProperty.Register<GridOverlay, Color>(nameof(GridColor), Colors.Gray);

    public bool ShowGrid
    {
        get => GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    public int StoneSize
    {
        get => GetValue(StoneSizeProperty);
        set => SetValue(StoneSizeProperty, value);
    }

    public int BitmapWidth
    {
        get => GetValue(BitmapWidthProperty);
        set => SetValue(BitmapWidthProperty, value);
    }

    public int BitmapHeight
    {
        get => GetValue(BitmapHeightProperty);
        set => SetValue(BitmapHeightProperty, value);
    }

    public int StoneColumns
    {
        get => GetValue(StoneColumnsProperty);
        set => SetValue(StoneColumnsProperty, value);
    }

    public int StoneRows
    {
        get => GetValue(StoneRowsProperty);
        set => SetValue(StoneRowsProperty, value);
    }

    public Color GridColor
    {
        get => GetValue(GridColorProperty);
        set => SetValue(GridColorProperty, value);
    }

    static GridOverlay()
    {
        AffectsRender<GridOverlay>(
            ShowGridProperty, StoneSizeProperty,
            BitmapWidthProperty, BitmapHeightProperty,
            StoneColumnsProperty, StoneRowsProperty,
            GridColorProperty);
    }

    private ScrollViewer? _scroller;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _scroller = this.FindAncestorOfType<ScrollViewer>();
        if (_scroller != null) _scroller.ScrollChanged += OnScrollChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_scroller != null) _scroller.ScrollChanged -= OnScrollChanged;
        _scroller = null;
    }

    // Lines are only drawn inside the visible part, so redraw when it moves.
    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e) => InvalidateVisual();

    private Rect VisibleRect()
    {
        var all = new Rect(Bounds.Size);
        if (_scroller == null) return all;
        var p = this.TranslatePoint(new Point(0, 0), _scroller);
        if (p == null) return all;
        return all.Intersect(new Rect(-p.Value.X, -p.Value.Y, _scroller.Viewport.Width, _scroller.Viewport.Height));
    }

    public override void Render(DrawingContext context)
    {
        if (!ShowGrid || StoneSize <= 0 || BitmapWidth <= 0 || BitmapHeight <= 0)
            return;

        double scaleX = Bounds.Width / BitmapWidth;
        double scaleY = Bounds.Height / BitmapHeight;
        double cellScreen = StoneSize * Math.Min(scaleX, scaleY);

        if (cellScreen < 3) return;

        // Sub-sample: skip lines until effective on-screen spacing >= 20px
        int skip = 1;
        while (cellScreen * skip < 20)
            skip *= 2;

        int stepPx = StoneSize * skip;
        double effectiveCell = cellScreen * skip;
        double thickness = effectiveCell >= 40 ? 2 : 1;
        var pen = new Pen(new SolidColorBrush(GridColor), thickness);

        // Only the lines inside the visible part: a 20 m mosaic zoomed in has tens of thousands off screen.
        var vis = VisibleRect();
        if (vis.Width <= 0 || vis.Height <= 0) return;
        int pyFirst = Math.Max(stepPx, (int)Math.Floor(vis.Top / scaleY / stepPx) * stepPx);
        int pyLast = (int)Math.Min(BitmapHeight - 1, Math.Ceiling(vis.Bottom / scaleY));
        int pxFirst = Math.Max(stepPx, (int)Math.Floor(vis.Left / scaleX / stepPx) * stepPx);
        int pxLast = (int)Math.Min(BitmapWidth - 1, Math.Ceiling(vis.Right / scaleX));

        for (int py = pyFirst; py <= pyLast; py += stepPx)
        {
            double y = Math.Round(py * scaleY);
            context.DrawLine(pen, new Point(vis.Left, y), new Point(vis.Right, y));
        }

        for (int px = pxFirst; px <= pxLast; px += stepPx)
        {
            double x = Math.Round(px * scaleX);
            context.DrawLine(pen, new Point(x, vis.Top), new Point(x, vis.Bottom));
        }
    }
}
