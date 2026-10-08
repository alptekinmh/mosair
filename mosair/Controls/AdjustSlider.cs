using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;

namespace mosair.Controls
{
    // A slim slider for the Görsel Ayarları panel, like Photoshop's adjustment sliders: a thin track (plain or a
    // colour gradient) with a small triangle under it. Whole-number values.
    //   drag / click: set the value · Shift + drag: fine (a quarter of the speed) · wheel: ±1 (Ctrl ±10)
    //   arrows: ±1 (Shift ±10) · right-click or Delete: back to DefaultValue
    public class AdjustSlider : Control
    {
        public static readonly StyledProperty<double> MinimumProperty =
            AvaloniaProperty.Register<AdjustSlider, double>(nameof(Minimum), -100);
        public static readonly StyledProperty<double> MaximumProperty =
            AvaloniaProperty.Register<AdjustSlider, double>(nameof(Maximum), 100);
        public static readonly StyledProperty<double> ValueProperty =
            AvaloniaProperty.Register<AdjustSlider, double>(nameof(Value), 0, defaultBindingMode: BindingMode.TwoWay);
        public static readonly StyledProperty<double> DefaultValueProperty =
            AvaloniaProperty.Register<AdjustSlider, double>(nameof(DefaultValue), 0);
        public static readonly StyledProperty<IBrush?> TrackBrushProperty =
            AvaloniaProperty.Register<AdjustSlider, IBrush?>(nameof(TrackBrush));
        public static readonly StyledProperty<IBrush?> EmptyTrackBrushProperty =
            AvaloniaProperty.Register<AdjustSlider, IBrush?>(nameof(EmptyTrackBrush));
        public static readonly StyledProperty<IBrush?> ThumbBrushProperty =
            AvaloniaProperty.Register<AdjustSlider, IBrush?>(nameof(ThumbBrush));
        public static readonly StyledProperty<IBrush?> ThumbBorderBrushProperty =
            AvaloniaProperty.Register<AdjustSlider, IBrush?>(nameof(ThumbBorderBrush));

        public double Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
        public double Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
        public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
        public double DefaultValue { get => GetValue(DefaultValueProperty); set => SetValue(DefaultValueProperty, value); }
        public IBrush? TrackBrush { get => GetValue(TrackBrushProperty); set => SetValue(TrackBrushProperty, value); }
        // The plain track, for rows without a colour scale of their own.
        public IBrush? EmptyTrackBrush { get => GetValue(EmptyTrackBrushProperty); set => SetValue(EmptyTrackBrushProperty, value); }
        public IBrush? ThumbBrush { get => GetValue(ThumbBrushProperty); set => SetValue(ThumbBrushProperty, value); }
        public IBrush? ThumbBorderBrush { get => GetValue(ThumbBorderBrushProperty); set => SetValue(ThumbBorderBrushProperty, value); }

        private const double Pad = 6;         // keeps the triangle inside at both ends
        private const double TrackTop = 3, TrackHeight = 4;
        private const double ThumbHalf = 5, ThumbTop = 8, ThumbHeight = 8;

        private bool _dragging;
        private bool _fine;
        private double _fineStartX, _fineStartValue;

        static AdjustSlider()
        {
            AffectsRender<AdjustSlider>(ValueProperty, MinimumProperty, MaximumProperty, TrackBrushProperty, EmptyTrackBrushProperty,
                ThumbBrushProperty, ThumbBorderBrushProperty);
            FocusableProperty.OverrideDefaultValue<AdjustSlider>(true);
            CursorProperty.OverrideDefaultValue<AdjustSlider>(new Cursor(StandardCursorType.Hand));
        }

        protected override Size MeasureOverride(Size availableSize) =>
            new(double.IsInfinity(availableSize.Width) ? 120 : availableSize.Width, ThumbTop + ThumbHeight + 1);

        private double Fraction => Maximum > Minimum ? Math.Clamp((Value - Minimum) / (Maximum - Minimum), 0, 1) : 0;

        public override void Render(DrawingContext context)
        {
            double w = Math.Max(1, Bounds.Width - 2 * Pad);
            var track = new Rect(Pad, TrackTop, w, TrackHeight);
            context.DrawRectangle(TrackBrush ?? EmptyTrackBrush ?? Brushes.Gray, null, track, 2, 2);

            double x = Pad + Fraction * w;
            var geo = new StreamGeometry();
            using (var g = geo.Open())
            {
                g.BeginFigure(new Point(x, ThumbTop), true);
                g.LineTo(new Point(x + ThumbHalf, ThumbTop + ThumbHeight));
                g.LineTo(new Point(x - ThumbHalf, ThumbTop + ThumbHeight));
                g.EndFigure(true);
            }
            var pen = ThumbBorderBrush != null ? new Pen(ThumbBorderBrush, IsFocused ? 1.5 : 1) : null;
            context.DrawGeometry(ThumbBrush ?? Brushes.White, pen, geo);
        }

        private void SetFromX(double px)
        {
            double w = Math.Max(1, Bounds.Width - 2 * Pad);
            SetValueClamped(Minimum + Math.Clamp((px - Pad) / w, 0, 1) * (Maximum - Minimum));
        }

        private void SetValueClamped(double v) => Value = Math.Round(Math.Clamp(v, Minimum, Maximum));

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            var props = e.GetCurrentPoint(this).Properties;
            if (props.IsRightButtonPressed)
            {
                // Right-click: back to the default value.
                Focus();
                Value = DefaultValue;
                e.Handled = true;
                return;
            }
            if (!props.IsLeftButtonPressed) return;
            Focus();
            _dragging = true;
            _fine = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            double px = e.GetPosition(this).X;
            _fineStartX = px;
            _fineStartValue = Value;
            if (!_fine) SetFromX(px);
            e.Pointer.Capture(this);
            e.Handled = true;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (!_dragging) return;
            double px = e.GetPosition(this).X;
            if (_fine)
            {
                double w = Math.Max(1, Bounds.Width - 2 * Pad);
                SetValueClamped(_fineStartValue + (px - _fineStartX) / w * (Maximum - Minimum) / 4);
            }
            else SetFromX(px);
            e.Handled = true;
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (!_dragging) return;
            _dragging = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            if (!IsEnabled) return;
            double step = e.KeyModifiers.HasFlag(KeyModifiers.Control) ? 10 : 1;
            SetValueClamped(Value + Math.Sign(e.Delta.Y) * step);
            e.Handled = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            double step = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 10 : 1;
            switch (e.Key)
            {
                case Key.Left: case Key.Down: SetValueClamped(Value - step); break;
                case Key.Right: case Key.Up: SetValueClamped(Value + step); break;
                case Key.Home: Value = Minimum; break;
                case Key.End: Value = Maximum; break;
                case Key.Delete: case Key.Back: Value = DefaultValue; break;
                default: return;
            }
            e.Handled = true;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == IsFocusedProperty) InvalidateVisual();   // a thicker outline while focused
        }
    }
}
