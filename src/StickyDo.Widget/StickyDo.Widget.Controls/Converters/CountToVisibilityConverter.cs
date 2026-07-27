using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace StickyDo.Widget.Controls;

/// <summary>
/// Converts an integer count to Visibility: shows content when count is greater than zero, otherwise
/// hides it via Hidden (not Collapsed) so the element keeps reserving its layout space - e.g. so a
/// sibling container sizes the same whether or not the count text is actually displayed.
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        var count = value is int i ? i : 0;
        return count > 0 ? Visibility.Visible : Visibility.Hidden;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        throw new NotImplementedException();
    }
}
