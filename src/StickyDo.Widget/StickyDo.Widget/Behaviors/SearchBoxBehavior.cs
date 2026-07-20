using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StickyDo.Widget.Behaviors;

/// <summary>
/// Attached behavior for search box placeholder text functionality.
/// </summary>
public static class SearchBoxBehavior
{
    private const string PlaceholderText = "Search tasks, notes, or labels...";

    public static readonly DependencyProperty IsSearchBoxProperty =
        DependencyProperty.RegisterAttached(
            "IsSearchBox",
            typeof(bool),
            typeof(SearchBoxBehavior),
            new PropertyMetadata(false, OnIsSearchBoxChanged));

    public static bool GetIsSearchBox(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsSearchBoxProperty);
    }

    public static void SetIsSearchBox(DependencyObject obj, bool value)
    {
        obj.SetValue(IsSearchBoxProperty, value);
    }

    private static void OnIsSearchBoxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox searchBox)
            return;

        if ((bool)e.NewValue)
        {
            searchBox.GotFocus += OnSearchBoxGotFocus;
            searchBox.LostFocus += OnSearchBoxLostFocus;
            searchBox.Loaded += OnSearchBoxLoaded;
        }
        else
        {
            searchBox.GotFocus -= OnSearchBoxGotFocus;
            searchBox.LostFocus -= OnSearchBoxLostFocus;
            searchBox.Loaded -= OnSearchBoxLoaded;
        }
    }

    private static void OnSearchBoxLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox searchBox && string.IsNullOrEmpty(searchBox.Text))
        {
            SetPlaceholder(searchBox);
        }
    }

    private static void OnSearchBoxGotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox searchBox && searchBox.Text == PlaceholderText)
        {
            searchBox.Text = string.Empty;
            searchBox.Foreground = Brushes.Black;
        }
    }

    private static void OnSearchBoxLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox searchBox && string.IsNullOrEmpty(searchBox.Text))
        {
            SetPlaceholder(searchBox);
        }
    }

    private static void SetPlaceholder(TextBox searchBox)
    {
        searchBox.Text = PlaceholderText;
        searchBox.Foreground = Brushes.LightGray;
    }
}
