using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StickyDo.Domain.Models.RichText;

namespace StickyDo.Widget.ViewModels;

/// <summary>
/// ViewModel for an individual task within a sticky note window.
/// </summary>
public partial class StickyNoteTaskItemViewModel : ObservableObject
{
    private Func<Guid, string, bool, RichTextFormatting?, Task>? _onUpdateTask;
    private Func<Guid, Task>? _onDeleteTask;
    private Action? _onSubmit;

    [ObservableProperty]
    private Guid id;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private RichTextFormatting? titleFormatting;

    [ObservableProperty]
    private bool isCompleted;

    [ObservableProperty]
    private int order;

    [ObservableProperty]
    private DateTime createdAt = DateTime.UtcNow;

    [ObservableProperty]
    private DateTime updatedAt = DateTime.UtcNow;

    public void SetCallbacks(Func<Guid, string, bool, RichTextFormatting?, Task> onUpdateTask, Func<Guid, Task> onDeleteTask, Action onSubmit)
    {
        _onUpdateTask = onUpdateTask;
        _onDeleteTask = onDeleteTask;
        _onSubmit = onSubmit;
    }

    /// <summary>
    /// Commits the in-progress edit (by moving focus away, which flushes the Title binding)
    /// and hands off to the parent note to focus the "Add a task..." input for the next line.
    /// </summary>
    [RelayCommand]
    public void SubmitEdit()
    {
        _onSubmit?.Invoke();
    }

    partial void OnIsCompletedChanged(bool value)
    {
        _ = UpdateTaskInParent();
    }

    partial void OnTitleChanged(string value)
    {
        _ = UpdateTaskInParent();
    }

    partial void OnTitleFormattingChanged(RichTextFormatting? value)
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
            await _onUpdateTask(Id, Title, IsCompleted, TitleFormatting);
        }
    }
}
