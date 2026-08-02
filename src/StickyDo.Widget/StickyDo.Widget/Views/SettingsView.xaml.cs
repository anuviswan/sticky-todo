using System.Windows.Controls;

namespace StickyDo.Widget.Views;

/// <summary>
/// Settings page shown within the main window's content area, grouped into General,
/// Data Management, and Application Information sections. Pure MVVM - all interactions
/// through bindings and commands.
/// </summary>
public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }
}
