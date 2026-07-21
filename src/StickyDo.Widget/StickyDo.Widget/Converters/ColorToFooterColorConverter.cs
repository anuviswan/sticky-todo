using System.Globalization;
using System.Windows.Data;
using StickyDo.Domain.Utilities;

namespace StickyDo.Widget.Converters;

/// <summary>
/// Converts a selected color to its footer variant.
/// Formula: Footer = (Hue=Unchanged, Saturation x0.85, Lightness x 0.85)
/// </summary>
public class ColorToFooterColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is uint selectedColor)
        {
            uint footerColor = ColorUtility.GetFooterColor(selectedColor);
            return new ColorArgbToBrushConverter().Convert(footerColor, targetType, parameter, culture);
        }

        return new ColorArgbToBrushConverter().Convert(0xFFFFF9E6, targetType, parameter, culture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
