using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace StickyDo.Widget.ViewModels;

/// <summary>
/// ViewModel for an individual sticky note item in the list.
/// </summary>
public partial class StickyNoteItemViewModel : ObservableObject
{
    [ObservableProperty]
    private Guid id;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string content = string.Empty;

    [ObservableProperty]
    private string status = "Active";

    [ObservableProperty]
    private DateTime lastModified = DateTime.UtcNow;

    [ObservableProperty]
    private Brush colorBrush = new SolidColorBrush(Color.FromRgb(255, 193, 7));
}
