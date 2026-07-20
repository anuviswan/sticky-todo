using System.Globalization;
using System.Windows.Data;

namespace StickyDo.Widget.Converters;

public class CompareColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is uint color1 && parameter is uint color2)
        {
            return color1 == color2;
        }

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
