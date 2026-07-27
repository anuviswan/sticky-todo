using System.Globalization;
using System.Windows.Data;
using StickyDo.Domain.Models;

namespace StickyDo.Widget.Converters;

/// <summary>
/// Converts a <see cref="NoteSaveState"/> to the glyph shown in the note footer's Save Status icon.
/// </summary>
public class NoteSaveStateToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is NoteSaveState state)
        {
            return state switch
            {
                NoteSaveState.Saved => "✓",
                NoteSaveState.Saving => "↻",
                NoteSaveState.NotSaved => "✎",
                _ => string.Empty
            };
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
