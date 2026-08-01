using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace StickyDo.Widget.Converters;

/// <summary>
/// Shows an element only when the bound <c>NoteType</c> matches the <c>ConverterParameter</c>
/// ("Todo" or "Note"). Compares string representations so this converter doesn't need a
/// dependency on the Domain project's enum type.
/// </summary>
public class NoteTypeToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.Ordinal)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("ConvertBack is not supported for NoteTypeToVisibilityConverter");
    }
}
