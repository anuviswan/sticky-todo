using System.Globalization;
using System.Windows.Data;
using StickyDo.Domain.Constants;

namespace StickyDo.Widget.Converters;

public class ColorNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is uint color)
        {
            return ColorPalette.GetColorName(color);
        }

        return "Color";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
