using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace StickyDo.Widget;

public partial class ColorPalettePopup : UserControl
{
    public ColorPalettePopup()
    {
        InitializeComponent();
        this.Loaded += ColorPalettePopup_Loaded;
        this.DataContextChanged += ColorPalettePopup_DataContextChanged;
    }

    private void ColorPalettePopup_Loaded(object sender, RoutedEventArgs e)
    {
        if (this.DataContext is INotifyPropertyChanged viewModel)
        {
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            UpdateCheckmarks();
        }
    }

    private void ColorPalettePopup_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyPropertyChanged oldVm)
        {
            oldVm.PropertyChanged -= ViewModel_PropertyChanged;
        }
        if (e.NewValue is INotifyPropertyChanged newVm)
        {
            newVm.PropertyChanged += ViewModel_PropertyChanged;
            UpdateCheckmarks();
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "CurrentColor")
        {
            UpdateCheckmarks();
        }
    }

    private void UpdateCheckmarks()
    {
        if (ColorItemsControl.ItemsSource == null)
            return;

        // Get CurrentColor from DataContext via reflection
        var currentColorProperty = this.DataContext?.GetType().GetProperty("CurrentColor");
        if (currentColorProperty == null)
            return;

        var selectedColor = (uint?)currentColorProperty.GetValue(this.DataContext);
        if (selectedColor == null)
            return;

        // Update visibility of checkmarks based on selected color
        for (int i = 0; i < ColorItemsControl.Items.Count; i++)
        {
            var container = ColorItemsControl.ItemContainerGenerator.ContainerFromIndex(i);
            if (container is FrameworkElement element)
            {
                var colorGrid = element as Grid;
                if (colorGrid?.Children.Count > 1)
                {
                    var checkmarkBorder = colorGrid.Children[1] as Border;
                    if (checkmarkBorder != null)
                    {
                        var colorButton = colorGrid.Children[0] as Button;
                        var buttonColor = colorButton?.Tag as uint?;
                        checkmarkBorder.Visibility = buttonColor == selectedColor ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
            }
        }
    }
}
