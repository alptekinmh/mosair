using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace mosair.Controls;

// Status-bar background: two translucent sea waves that flow from left to right for as long as IsActive is true.
// The waves fade in when work starts and fade out when it ends, so very short jobs only give a brief shimmer.
public class ActivityWave : Control
{
    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<ActivityWave, bool>(nameof(IsActive));

    public static readonly StyledProperty<Color> WaveColorProperty =
        AvaloniaProperty.Register<ActivityWave, Color>(nameof(WaveColor), Color.FromRgb(0x2d, 0x6b, 0xd9));   // the accent; MainWindow sets AccentFill

    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public Color WaveColor
    {
        get => GetValue(WaveColorProperty);
        set => SetValue(WaveColorProperty, value);
    }

    private const double FadeSeconds = 0.35;
    private const double SpeedPxPerSecond = 90;   // how fast the crests travel to the right
    private const double Wavelength = 140;        // px between crests of the front wave

    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private DispatcherTimer? _timer;
    private double _level;          // 0 = invisible, 1 = full strength
    private double _lastTick;

    static ActivityWave()
    {
        AffectsRender<ActivityWave>(WaveColorProperty);
    }

    public ActivityWave()
    {
        IsHitTestVisible = false;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsActiveProperty && IsActive) Start();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (IsActive) Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Stop();
    }

    private void Start()
    {
        if (_timer != null) return;
        _lastTick = _clock.Elapsed.TotalSeconds;
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(33), DispatcherPriority.Render, OnTick);
        _timer.Start();
    }

    private void Stop()
    {
        _timer?.Stop();
        _timer = null;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        double now = _clock.Elapsed.TotalSeconds;
        double dt = Math.Min(0.1, now - _lastTick);
        _lastTick = now;
        double step = dt / FadeSeconds;
        _level = IsActive ? Math.Min(1, _level + step) : Math.Max(0, _level - step);
        if (!IsActive && _level <= 0) Stop();
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        if (_level <= 0) return;
        double w = Bounds.Width, h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        double t = _clock.Elapsed.TotalSeconds;
        var c = WaveColor;
        // Back wave: longer, slower, fainter, a little higher. Front wave: the main swell.
        DrawWave(context, w, h, t, Wavelength * 1.7, SpeedPxPerSecond * 0.6, h * 0.42, h * 0.16, (byte)(34 * _level), c, 1.3);
        DrawWave(context, w, h, t, Wavelength, SpeedPxPerSecond, h * 0.55, h * 0.18, (byte)(52 * _level), c, 0);
    }

    private static void DrawWave(DrawingContext context, double w, double h, double t, double wavelength,
        double speed, double baseline, double amplitude, byte alpha, Color c, double phaseOffset)
    {
        double k = 2 * Math.PI / wavelength;
        double shift = speed * t;   // crests move to the right
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(new Point(0, h), true);
            for (double x = 0; x <= w + 4; x += 4)
            {
                double xx = Math.Min(x, w);
                // A second, smaller harmonic keeps the swell from looking like a plain sine.
                double y = baseline
                           - amplitude * Math.Sin(k * (xx - shift) + phaseOffset)
                           - amplitude * 0.35 * Math.Sin(2.3 * k * (xx - shift * 1.4) + phaseOffset);
                g.LineTo(new Point(xx, Math.Clamp(y, 0, h)));
            }
            g.LineTo(new Point(w, h));
            g.EndFigure(true);
        }
        var brush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.FromArgb(alpha, c.R, c.G, c.B), 0),
                new GradientStop(Color.FromArgb((byte)(alpha / 3), c.R, c.G, c.B), 1)
            }
        };
        context.DrawGeometry(brush, null, geometry);
    }
}
