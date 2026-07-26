using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace StickyDo.Widget.Converters;

/// <summary>
/// Compares a palette swatch's color to the note's current color, returning
/// Visible when they match. Used to show the selection indicator on the
/// currently applied color in the palette popup.
/// </summary>
public class ColorEqualityToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo cultureInfo)
    {
        if (values is [uint swatchColor, uint currentColor])
        {
            return swatchColor == currentColor ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo cultureInfo)
    {
        throw new NotSupportedException("ConvertBack is not supported for ColorEqualityToVisibilityConverter");
    }
}
