using System.Globalization;
using System.Windows;
using System.Windows.Data;
using StickyDo.Widget.ViewModels;

namespace StickyDo.Widget.Converters;

/// <summary>
/// Shows the footer's clock badge (overlaid on the cloud icon) only while the note is
/// <see cref="NoteSyncState.NotSynced"/>, so a future "Synced" state can hide it without
/// any layout changes.
/// </summary>
public class NoteSyncStateToPendingVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is NoteSyncState.NotSynced ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
