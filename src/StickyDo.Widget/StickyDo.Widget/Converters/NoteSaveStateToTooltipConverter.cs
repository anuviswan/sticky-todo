using System.Globalization;
using System.Windows.Data;
using StickyDo.Domain.Models;
using AppResources = StickyDo.Widget.Resources.Resources;

namespace StickyDo.Widget.Converters;

/// <summary>
/// Converts a <see cref="NoteSaveState"/> to the tooltip text shown for the Save Status icon.
/// </summary>
public class NoteSaveStateToTooltipConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is NoteSaveState state)
        {
            return state switch
            {
                NoteSaveState.Saved => AppResources.SaveStatus_Saved,
                NoteSaveState.Saving => AppResources.SaveStatus_Saving,
                NoteSaveState.NotSaved => AppResources.SaveStatus_NotSaved,
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
