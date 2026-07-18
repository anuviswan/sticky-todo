using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using StickyDo.Widget.ViewModels;

namespace StickyDo.Widget.Converters;

public class NavViewToBackgroundConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return Brushes.Transparent;

        if (values[0] is NavigationView currentView && values[1] is NavigationView buttonView)
        {
            return currentView == buttonView
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFACD"))
                : Brushes.Transparent;
        }

        return Brushes.Transparent;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
