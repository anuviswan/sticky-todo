using System.Globalization;
using System.Windows.Data;
using StickyDo.Widget.ViewModels;

namespace StickyDo.Widget.Converters;

/// <summary>
/// Converts a <see cref="NoteSyncState"/> to the tooltip text shown for the Sync Status icon.
/// </summary>
public class NoteSyncStateToTooltipConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is NoteSyncState state)
        {
            return state switch
            {
                NoteSyncState.NotSynced => "Not Synced",
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
