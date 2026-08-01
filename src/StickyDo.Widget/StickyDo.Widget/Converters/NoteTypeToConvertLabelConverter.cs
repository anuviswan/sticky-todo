using System.Globalization;
using System.Windows.Data;
using AppResources = StickyDo.Widget.Resources.Resources;

namespace StickyDo.Widget.Converters;

/// <summary>
/// Labels the "more options" menu's convert action based on the note's current type: a Todo
/// offers to convert to a Note and vice versa. Compares the bound value's string representation
/// so this converter doesn't need a dependency on the Domain project's enum type.
/// </summary>
public class NoteTypeToConvertLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return string.Equals(value?.ToString(), "Todo", StringComparison.Ordinal)
            ? AppResources.MoreOptionsPopup_ConvertToNote
            : AppResources.MoreOptionsPopup_ConvertToTodo;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("ConvertBack is not supported for NoteTypeToConvertLabelConverter");
    }
}
