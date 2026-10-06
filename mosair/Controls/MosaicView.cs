using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SkiaSharp;
using mosair.Services;

namespace mosair.Controls;

// Shows the stone-texture mosaic without ever building the whole image. Only the visible part is drawn, from
// 512-px tiles rendered in the background at the detail the zoom needs (N per stone: 2, 4, 8, 16, 32 … up to the
// N slider). Far out, when a stone is only a couple of screen pixels, the one-pixel-per-stone overview is drawn
// instead. While a sharper tile is being rendered, whatever is already cached (overview or other levels) fills
// its place, so the screen never goes blank. At full detail a tile is byte-identical to the same part of the
// whole-mosaic image (see MosaicRenderSource).
public class MosaicView : Control, ICustomHitTest
{
    public static readonly StyledProperty<MosaicRenderSource?> SourceProperty =
        AvaloniaProperty.Register<MosaicView, MosaicRenderSource?>(nameof(Source));
    public static readonly StyledProperty<Bitmap?> OverviewProperty =
        AvaloniaProperty.Register<MosaicView, Bitmap?>(nameof(Overview));
    public static readonly StyledProperty<int> StonePixelSizeProperty =
        AvaloniaProperty.Register<MosaicView, int>(nameof(StonePixelSize), 40);
    public static readonly StyledProperty<bool> ShowGridProperty =
        AvaloniaProperty.Register<MosaicView, bool>(nameof(ShowGrid));
    public static readonly StyledProperty<Color> GridColorProperty =
        AvaloniaProperty.Register<MosaicView, Color>(nameof(GridColor), Colors.Gray);

    public MosaicRenderSource? Source { get => GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    public Bitmap? Overview { get => GetValue(OverviewProperty); set => SetValue(OverviewProperty, value); }
    public int StonePixelSize { get => GetValue(StonePixelSizeProperty); set => SetValue(StonePixelSizeProperty, value); }
    public bool ShowGrid { get => GetValue(ShowGridProperty); set => SetValue(ShowGridProperty, value); }
    public Color GridColor { get => GetValue(GridColorProperty); set => SetValue(GridColorProperty, value); }

    private const int TilePx = 512;
    private const long CacheBudgetBytes = 256L * 1024 * 1024;
    private const double OverviewOnlyBelow = 2.5;   // screen px per stone below which textures are not drawn
    private static readonly int[] Levels = { 2, 4, 8, 16, 32, 64 };

    private readonly record struct TileKey(int Level, int Row, int Col);

    private sealed class Tile
    {
        public required Bitmap Bitmap;
        public required long Bytes;
        public long LastFrame;
    }

    private readonly Dictionary<TileKey, Tile> _cache = new();
    private readonly HashSet<TileKey> _pending = new();
    private long _cacheBytes;
    private long _frame;
    private int _generation;                       // bumped when anything that changes the pixels changes
    private volatile HashSet<TileKey> _wanted = new();
    private readonly List<(Bitmap bmp, DateTime at)> _retired = new();
    private static readonly SemaphoreSlim Workers = new(Math.Max(1, Environment.ProcessorCount - 1));
    private ScrollViewer? _scroller;

    static MosaicView()
    {
        AffectsRender<MosaicView>(OverviewProperty);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SourceProperty || change.Property == StonePixelSizeProperty ||
            change.Property == ShowGridProperty || change.Property == GridColorProperty)
            ClearTiles();
    }

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
        ClearTiles();
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e) => InvalidateVisual();

    // Clicks and the mouse wheel must reach this control even where nothing has been drawn yet.
    public bool HitTest(Point point) => new Rect(Bounds.Size).Contains(point);

    // A stone changed (pixel edit, variant choice): drop every cached tile that contains it.
    public void InvalidateStone(int row, int col)
    {
        if (row < 0 || col < 0) { ClearTiles(); return; }
        var drop = new List<TileKey>();
        foreach (var key in _cache.Keys)
        {
            int ts = TileStones(key.Level);
            if (key.Row == row / ts && key.Col == col / ts) drop.Add(key);
        }
        foreach (var key in drop) Retire(key);
        InvalidateVisual();
    }

    private void ClearTiles()
    {
        _generation++;
        foreach (var key in new List<TileKey>(_cache.Keys)) Retire(key);
        _pending.Clear();
        InvalidateVisual();
    }

    private void Retire(TileKey key)
    {
        if (!_cache.Remove(key, out var tile)) return;
        _cacheBytes -= tile.Bytes;
        // The compositor may still be drawing it from the last frame; free it a little later.
        _retired.Add((tile.Bitmap, DateTime.UtcNow));
    }

    private void DisposeRetired()
    {
        var now = DateTime.UtcNow;
        for (int i = _retired.Count - 1; i >= 0; i--)
            if ((now - _retired[i].at).TotalSeconds > 1.5)
            {
                _retired[i].bmp.Dispose();
                _retired.RemoveAt(i);
            }
    }

    private static int TileStones(int level) => Math.Max(1, TilePx / level);

    // The detail to render at: the smallest level that is at least as sharp as the screen needs, never above N.
    private static int ChooseLevel(double screenPxPerStone, int n)
    {
        foreach (int l in Levels)
            if (l < n && l >= screenPxPerStone) return l;
        return n;
    }

    // Grid baked into tiles: at full detail exactly as in the whole-mosaic image, coarser levels scale it down.
    private (int gw, SKColor color) GridFor(int level)
    {
        var c = GridColor;
        int n = StonePixelSize;
        int gw = !ShowGrid ? 0 : level == n ? Math.Max(1, n / 11) : level >= 8 ? Math.Max(1, level / 11) : 0;
        return (gw, new SKColor(c.R, c.G, c.B));
    }

    private Rect VisibleRect()
    {
        var all = new Rect(Bounds.Size);
        if (_scroller == null) return all;
        var p = this.TranslatePoint(new Point(0, 0), _scroller);
        if (p == null) return all;
        var view = new Rect(-p.Value.X, -p.Value.Y, _scroller.Viewport.Width, _scroller.Viewport.Height);
        return all.Intersect(view);
    }

    public override void Render(DrawingContext context)
    {
        DisposeRetired();
        var src = Source;
        double w = Bounds.Width, h = Bounds.Height;
        if (src == null || w <= 0 || h <= 0 || src.Cols <= 0) return;

        var vis = VisibleRect();
        if (vis.Width <= 0 || vis.Height <= 0) return;
        _frame++;

        double stonePx = w / src.Cols;                       // control px per stone
        double scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
        double screenPx = stonePx * scaling;                 // device px per stone

        // 1. Overview underneath everything: one pixel per stone, sharp when magnified.
        var overview = Overview;
        if (overview != null)
        {
            var srcRect = new Rect(vis.X / stonePx, vis.Y / stonePx, vis.Width / stonePx, vis.Height / stonePx);
            var mode = screenPx >= 1 ? BitmapInterpolationMode.None : BitmapInterpolationMode.MediumQuality;
            using (context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = mode }))
                context.DrawImage(overview, srcRect, vis);
        }

        if (screenPx < OverviewOnlyBelow || !src.HasTextures)
        {
            _wanted = new HashSet<TileKey>();
            return;
        }

        int n = StonePixelSize;
        int level = ChooseLevel(screenPx, n);
        int ts = TileStones(level);
        int r0 = Math.Max(0, (int)Math.Floor(vis.Y / stonePx / ts));
        int r1 = Math.Min((src.Rows - 1) / ts, (int)Math.Floor((vis.Bottom - 0.001) / stonePx / ts));
        int c0 = Math.Max(0, (int)Math.Floor(vis.X / stonePx / ts));
        int c1 = Math.Min((src.Cols - 1) / ts, (int)Math.Floor((vis.Right - 0.001) / stonePx / ts));

        using (context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.MediumQuality }))
        {
            // 2. Cached tiles of other levels that overlap the view (coarse first), as placeholders.
            var others = new List<(TileKey key, Tile tile)>();
            foreach (var kv in _cache)
                if (kv.Key.Level != level) others.Add((kv.Key, kv.Value));
            others.Sort((a, b) => a.key.Level.CompareTo(b.key.Level));
            foreach (var (key, tile) in others)
            {
                var dest = TileRect(src, key, stonePx);
                if (!dest.Intersects(vis)) continue;
                tile.LastFrame = _frame;
                context.DrawImage(tile.Bitmap, new Rect(tile.Bitmap.Size), dest);
            }

            // 3. Tiles at the wanted level; request the missing ones.
            var wanted = new HashSet<TileKey>();
            for (int r = r0; r <= r1; r++)
                for (int c = c0; c <= c1; c++)
                {
                    var key = new TileKey(level, r, c);
                    wanted.Add(key);
                    if (_cache.TryGetValue(key, out var tile))
                    {
                        tile.LastFrame = _frame;
                        context.DrawImage(tile.Bitmap, new Rect(tile.Bitmap.Size), TileRect(src, key, stonePx));
                    }
                }
            _wanted = wanted;
            foreach (var key in wanted)
                if (!_cache.ContainsKey(key)) Request(src, key);
        }

        Evict();
    }

    private static Rect TileRect(MosaicRenderSource src, TileKey key, double stonePx)
    {
        int ts = TileStones(key.Level);
        int rows = Math.Min(ts, src.Rows - key.Row * ts);
        int cols = Math.Min(ts, src.Cols - key.Col * ts);
        return new Rect(key.Col * ts * stonePx, key.Row * ts * stonePx, cols * stonePx, rows * stonePx);
    }

    private void Request(MosaicRenderSource src, TileKey key)
    {
        if (!_pending.Add(key)) return;
        int generation = _generation;
        var (gw, gridColor) = GridFor(key.Level);
        bool grid = ShowGrid;
        int ts = TileStones(key.Level);
        int row0 = key.Row * ts, col0 = key.Col * ts;
        int rows = Math.Min(ts, src.Rows - row0), cols = Math.Min(ts, src.Cols - col0);

        _ = Task.Run(async () =>
        {
            await Workers.WaitAsync();
            SKBitmap? bmp = null;
            try
            {
                // Skip work the view no longer needs (panned or zoomed away, or settings changed).
                if (generation == Volatile.Read(ref _generation) && _wanted.Contains(key))
                    bmp = src.RenderRegion(row0, col0, rows, cols, key.Level, grid, gw, gridColor);
            }
            catch (Exception)
            {
                bmp?.Dispose();
                bmp = null;
            }
            finally
            {
                Workers.Release();
            }

            Dispatcher.UIThread.Post(() =>
            {
                _pending.Remove(key);
                if (bmp == null) return;
                try
                {
                    if (generation != _generation || !ReferenceEquals(src, Source)) return;
                    var avalonia = ImageService.ToAvaloniaBitmap(bmp);
                    long bytes = (long)bmp.Width * bmp.Height * 4;
                    if (_cache.Remove(key, out var old)) { _cacheBytes -= old.Bytes; _retired.Add((old.Bitmap, DateTime.UtcNow)); }
                    _cache[key] = new Tile { Bitmap = avalonia, Bytes = bytes, LastFrame = _frame };
                    _cacheBytes += bytes;
                    InvalidateVisual();
                }
                finally
                {
                    bmp.Dispose();
                }
            }, DispatcherPriority.Background);
        });
    }

    // Least recently drawn tiles go first; tiles drawn in the current frame are kept.
    private void Evict()
    {
        if (_cacheBytes <= CacheBudgetBytes) return;
        var entries = new List<KeyValuePair<TileKey, Tile>>(_cache);
        entries.Sort((a, b) => a.Value.LastFrame.CompareTo(b.Value.LastFrame));
        foreach (var kv in entries)
        {
            if (_cacheBytes <= CacheBudgetBytes * 3 / 4) break;
            if (kv.Value.LastFrame == _frame) continue;
            Retire(kv.Key);
        }
    }
}
