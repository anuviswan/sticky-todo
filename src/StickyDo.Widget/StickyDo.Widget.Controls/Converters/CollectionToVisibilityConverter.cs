using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace StickyDo.Widget.Controls;

/// <summary>
/// Converts a collection to Visibility: shows content when collection has items, hides when empty.
/// Use parameter="inverted" to show when empty.
/// </summary>
public class CollectionToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        var hasItems = (value is ICollection collection && collection.Count > 0) ||
                       (value is IEnumerable enumerable && enumerable.Cast<object>().Any());

        var inverted = parameter as string == "inverted";
        return (hasItems && !inverted) || (!hasItems && inverted)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        throw new NotImplementedException();
    }
}
