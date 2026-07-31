using System.Globalization;
using System.Windows.Data;

namespace StickyDo.Widget.Controls.Converters;

/// <summary>
/// Maps the active type filter (e.g. the notes list's TypeFilter) to the label for a
/// "Create New ..." tile, so the Todos section offers to create a Todo and the Notes
/// section offers to create a Note, while other sections fall back to a generic label.
/// Works off the bound value's string representation so this assembly need not reference
/// the enum's defining type.
/// </summary>
public class NoteTypeToCreateLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "Todo" => "Create New Todo",
            "Note" => "Create New Note",
            _ => "Create New Note"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("ConvertBack is not supported for NoteTypeToCreateLabelConverter");
    }
}
