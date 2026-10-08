using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using mosair.Services;

namespace mosair.ViewModels
{
    // One slider row of the Görsel Ayarları panel: a label, a number box and an AdjustSlider. Value is a whole
    // number; Divisor 100 shows it with two decimals (exposure in EV). Changing Value calls Changed.
    public sealed class AdjustParam : INotifyPropertyChanged
    {
        public AdjustParam(string labelKey, int min, int max, int divisor = 1)
        {
            LabelKey = labelKey;
            Min = min;
            Max = max;
            Divisor = divisor;
        }

        public string LabelKey { get; }
        public string Label => Loc.Get(LabelKey) + ":";
        public int Min { get; private set; }
        public int Max { get; private set; }
        public int Divisor { get; }
        public int Default { get; private set; }

        public Action<AdjustParam>? Changed { get; set; }

        private int _value;
        public int Value
        {
            get => _value;
            set
            {
                int v = Math.Clamp(value, Min, Max);
                if (v == _value) return;
                _value = v;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Text));
                Changed?.Invoke(this);
            }
        }

        // The slider binds a double.
        public double SliderValue
        {
            get => _value;
            set => Value = (int)Math.Round(value);
        }

        // The number box: "+17", "-5", "0"; with two decimals for Divisor 100 ("+0,25"). Accepts "," or "."
        // and a leading "+"; anything else is ignored and the box shows the current value again.
        public string Text
        {
            get
            {
                if (Divisor == 1) return _value > 0 && Min < 0 ? "+" + _value : _value.ToString(CultureInfo.CurrentCulture);
                double d = _value / (double)Divisor;
                return (d > 0 ? "+" : "") + d.ToString("0.00", CultureInfo.CurrentCulture);
            }
            set
            {
                string t = (value ?? "").Trim().Replace('+', ' ').Trim().Replace(',', '.');
                if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                    Value = (int)Math.Round(d * Divisor);
                OnPropertyChanged();
            }
        }

        private IBrush? _track;
        public IBrush? Track
        {
            get => _track;
            set { _track = value; OnPropertyChanged(); }
        }

        // Shows another value without calling Changed (switching colour range, loading a project, reset).
        public void SetSilently(int value, int? min = null, int? max = null, int? def = null)
        {
            if (min.HasValue) Min = min.Value;
            if (max.HasValue) Max = max.Value;
            if (def.HasValue) Default = def.Value;
            _value = Math.Clamp(value, Min, Max);
            OnPropertyChanged(nameof(Min)); OnPropertyChanged(nameof(Max)); OnPropertyChanged(nameof(Default));
            OnPropertyChanged(nameof(Value)); OnPropertyChanged(nameof(SliderValue)); OnPropertyChanged(nameof(Text));
        }

        public void RefreshLabel() => OnPropertyChanged(nameof(Label));

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            if (name == nameof(Value)) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SliderValue)));
        }
    }

    // A colour-range circle of the Ton/Doygunluk part (Ana, Kırmızılar, …).
    public sealed class AdjustRange : INotifyPropertyChanged
    {
        public AdjustRange(int index, string labelKey, IBrush swatch)
        {
            Index = index;
            LabelKey = labelKey;
            Swatch = swatch;
        }

        public int Index { get; }
        public string LabelKey { get; }
        public string Label => Loc.Get(LabelKey);
        public IBrush Swatch { get; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); }
        }

        private bool _isUsed;
        // A dot when this range has its own changes.
        public bool IsUsed
        {
            get => _isUsed;
            set { _isUsed = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsUsed))); }
        }

        public void RefreshLabel() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Label)));

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
