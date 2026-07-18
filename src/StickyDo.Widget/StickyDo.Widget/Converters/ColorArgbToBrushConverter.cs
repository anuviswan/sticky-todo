using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace StickyDo.Widget.Converters;

/// <summary>
/// Converts a color ARGB value to a WPF Brush for data binding.
/// </summary>
public class ColorArgbToBrushConverter : IValueConverter
{
    private const uint DefaultColor = 0xFFFFCC07; // Yellow

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
    {
        if (value is uint colorArgb)
        {
            return GetBrushFromArgb(colorArgb);
        }

        return GetBrushFromArgb(DefaultColor);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo cultureInfo)
    {
        throw new NotSupportedException("ConvertBack is not supported for ColorArgbToBrushConverter");
    }

    private static Brush GetBrushFromArgb(uint argb)
    {
        var color = Color.FromArgb(
            (byte)((argb >> 24) & 0xFF),
            (byte)((argb >> 16) & 0xFF),
            (byte)((argb >> 8) & 0xFF),
            (byte)(argb & 0xFF)
        );
        return new SolidColorBrush(color);
    }
}
