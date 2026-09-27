using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

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

        for (int py = stepPx; py < BitmapHeight; py += stepPx)
        {
            double y = Math.Round(py * scaleY);
            context.DrawLine(pen, new Point(0, y), new Point(Bounds.Width, y));
        }

        for (int px = stepPx; px < BitmapWidth; px += stepPx)
        {
            double x = Math.Round(px * scaleX);
            context.DrawLine(pen, new Point(x, 0), new Point(x, Bounds.Height));
        }
    }
}
