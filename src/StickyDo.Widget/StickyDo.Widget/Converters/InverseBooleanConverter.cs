using System.Globalization;
using System.Windows.Data;

namespace StickyDo.Widget.Converters;

/// <summary>
/// Inverts a boolean value for data binding scenarios (e.g. disabling a control while a note is pinned).
/// </summary>
public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool boolValue && !boolValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool boolValue && !boolValue;
    }
}
