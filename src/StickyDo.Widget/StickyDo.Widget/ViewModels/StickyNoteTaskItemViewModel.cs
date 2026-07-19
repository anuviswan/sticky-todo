using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace StickyDo.Widget.ViewModels;

/// <summary>
/// ViewModel for an individual task within a sticky note window.
/// </summary>
public partial class StickyNoteTaskItemViewModel : ObservableObject
{
    private Func<Guid, string, bool, Task>? _onUpdateTask;
    private Func<Guid, Task>? _onDeleteTask;

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

    public void SetCallbacks(Func<Guid, string, bool, Task> onUpdateTask, Func<Guid, Task> onDeleteTask)
    {
        _onUpdateTask = onUpdateTask;
        _onDeleteTask = onDeleteTask;
    }

    partial void OnIsCompletedChanged(bool value)
    {
        _ = UpdateTaskInParent();
    }

    partial void OnTitleChanged(string value)
    {
        _ = UpdateTaskInParent();
    }

    /// <summary>
    /// Command to delete this task (handled by parent ViewModel).
    /// </summary>
    [RelayCommand]
    public async Task Delete()
    {
        if (_onDeleteTask != null)
        {
            await _onDeleteTask(Id);
        }
    }

    private async Task UpdateTaskInParent()
    {
        if (_onUpdateTask != null)
        {
            await _onUpdateTask(Id, Title, IsCompleted);
        }
    }
}
