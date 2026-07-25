using CommunityToolkit.Mvvm.ComponentModel;

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
    private DateTime lastModified = DateTime.UtcNow;

    [ObservableProperty]
    private uint colorArgb = 0xFFFFCC07;

    [ObservableProperty]
    private bool hasTasks;

    [ObservableProperty]
    private string firstTaskTitle = string.Empty;

    [ObservableProperty]
    private bool firstTaskCompleted;

    [ObservableProperty]
    private int remainingTaskCount;
}
