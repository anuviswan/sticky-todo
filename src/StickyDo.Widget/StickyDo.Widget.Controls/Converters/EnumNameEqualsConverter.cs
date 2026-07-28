using System.Globalization;
using System.Windows.Data;

namespace StickyDo.Widget.Controls.Converters;

/// <summary>
/// Returns true when the bound value's string representation matches the converter
/// parameter. Lets a Style.Trigger react to a ViewModel enum (e.g. the selected
/// navigation view) without this assembly referencing the enum's defining type.
/// </summary>
public class EnumNameEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return string.Equals(value?.ToString(), parameter as string, StringComparison.Ordinal);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("ConvertBack is not supported for EnumNameEqualsConverter");
    }
}
