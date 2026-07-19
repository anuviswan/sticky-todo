using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace StickyDo.Widget.ViewModels;

/// <summary>
/// ViewModel for an individual task within a sticky note window.
/// </summary>
public partial class StickyNoteTaskItemViewModel : ObservableObject
{
    [ObservableProperty]
    private Guid id;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private bool isCompleted;

    [ObservableProperty]
    private int order;

    [ObservableProperty]
    private DateTime createdAt = DateTime.UtcNow;

    [ObservableProperty]
    private DateTime updatedAt = DateTime.UtcNow;

    /// <summary>
    /// Command to toggle the completion status of the task.
    /// </summary>
    [RelayCommand]
    public void ToggleCompletion()
    {
        IsCompleted = !IsCompleted;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Command to delete this task (handled by parent ViewModel).
    /// </summary>
    [RelayCommand]
    public void Delete()
    {
        // Parent ViewModel will handle deletion
    }
}
