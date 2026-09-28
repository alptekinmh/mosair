using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace mosair.Converters;

public class InvariantDoubleConverter : IValueConverter
{
    public static readonly InvariantDoubleConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
            return d.ToString("F1", CultureInfo.InvariantCulture);
        return value?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            s = s.Replace(',', '.');
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                return result;
        }
        return 0.0;
    }
}
