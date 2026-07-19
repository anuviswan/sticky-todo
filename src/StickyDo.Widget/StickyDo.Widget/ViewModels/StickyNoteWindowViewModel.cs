using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StickyDo.Domain.Models;
using StickyDo.Domain.Services;
using StickyDo.Widget.Utilities;

namespace StickyDo.Widget.ViewModels;

/// <summary>
/// ViewModel for a floating sticky note window with task management.
/// </summary>
public partial class StickyNoteWindowViewModel : ObservableObject
{
    private readonly StickyNoteService _stickyNoteService;
    private StickyNote? _currentNote;
    private bool _hasUnsavedChanges;

    [ObservableProperty]
    private Guid noteId;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private ObservableCollection<StickyNoteTaskItemViewModel> tasks = new();

    [ObservableProperty]
    private string newTaskTitle = string.Empty;

    [ObservableProperty]
    private double windowX;

    [ObservableProperty]
    private double windowY;

    [ObservableProperty]
    private double windowWidth = 300;

    [ObservableProperty]
    private double windowHeight = 400;

    partial void OnTitleChanged(string value)
    {
        if (_currentNote != null)
        {
            _currentNote.Title = value;
            _hasUnsavedChanges = true;
        }
    }

    public StickyNoteWindowViewModel(StickyNoteService stickyNoteService)
    {
        ArgumentNullException.ThrowIfNull(stickyNoteService);
        _stickyNoteService = stickyNoteService;
    }

    /// <summary>
    /// Loads a sticky note and its tasks into the view model.
    /// </summary>
    public async Task LoadNoteAsync(Guid noteId)
    {
        try
        {
            _currentNote = await _stickyNoteService.GetNoteByIdAsync(noteId);
            if (_currentNote is null)
            {
                MessageBox.Show("Note not found.", "Error");
                return;
            }

            NoteId = _currentNote.Id;
            Title = _currentNote.Title;

            Tasks.Clear();

            // If no tasks exist, add a sample task for demonstration
            if (!_currentNote.Tasks.Any())
            {
                var sampleTaskId = await _stickyNoteService.CreateTaskAsync(_currentNote.Id, "First Task");
                var sampleTask = await _stickyNoteService.GetNoteByIdAsync(_currentNote.Id);
                if (sampleTask?.Tasks.FirstOrDefault(t => t.Id == sampleTaskId) is { } newTask)
                {
                    Tasks.Add(new StickyNoteTaskItemViewModel
                    {
                        Id = newTask.Id,
                        Title = newTask.Title,
                        IsCompleted = newTask.IsCompleted,
                        Order = newTask.Order,
                        CreatedAt = newTask.CreatedAt,
                        UpdatedAt = newTask.UpdatedAt
                    });
                }
            }
            else
            {
                foreach (var task in _currentNote.Tasks.OrderBy(t => t.Order))
                {
                    Tasks.Add(new StickyNoteTaskItemViewModel
                    {
                        Id = task.Id,
                        Title = task.Title,
                        IsCompleted = task.IsCompleted,
                        Order = task.Order,
                        CreatedAt = task.CreatedAt,
                        UpdatedAt = task.UpdatedAt
                    });
                }
            }

            _hasUnsavedChanges = false;
            NewTaskTitle = string.Empty;
        }
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(LoadNoteAsync));
            MessageBox.Show($"Error loading note: {ex.Message}", "Load Error");
        }
    }

    /// <summary>
    /// Adds a new task to the current note.
    /// </summary>
    [RelayCommand]
    public async Task AddTaskAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTaskTitle) || _currentNote is null)
            return;

        try
        {
            var taskId = await _stickyNoteService.CreateTaskAsync(_currentNote.Id, NewTaskTitle);
            var task = await _stickyNoteService.GetNoteByIdAsync(_currentNote.Id);
            if (task?.Tasks.FirstOrDefault(t => t.Id == taskId) is { } newTask)
            {
                Tasks.Add(new StickyNoteTaskItemViewModel
                {
                    Id = newTask.Id,
                    Title = newTask.Title,
                    IsCompleted = newTask.IsCompleted,
                    Order = newTask.Order,
                    CreatedAt = newTask.CreatedAt,
                    UpdatedAt = newTask.UpdatedAt
                });

                _hasUnsavedChanges = true;
                NewTaskTitle = string.Empty;
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(AddTaskAsync));
            MessageBox.Show($"Error adding task: {ex.Message}", "Add Task Error");
        }
    }

    /// <summary>
    /// Updates an existing task (completion status or title).
    /// </summary>
    public async Task UpdateTaskAsync(Guid taskId, string title, bool isCompleted)
    {
        if (_currentNote is null)
            return;

        try
        {
            await _stickyNoteService.UpdateTaskAsync(_currentNote.Id, taskId, title, isCompleted);
            _hasUnsavedChanges = true;
        }
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(UpdateTaskAsync));
            MessageBox.Show($"Error updating task: {ex.Message}", "Update Error");
        }
    }

    /// <summary>
    /// Deletes a task from the current note.
    /// </summary>
    [RelayCommand]
    public async Task DeleteTaskAsync(Guid taskId)
    {
        if (_currentNote is null)
            return;

        try
        {
            await _stickyNoteService.DeleteTaskAsync(_currentNote.Id, taskId);
            var taskVm = Tasks.FirstOrDefault(t => t.Id == taskId);
            if (taskVm != null)
            {
                Tasks.Remove(taskVm);
                _hasUnsavedChanges = true;
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(DeleteTaskAsync));
            MessageBox.Show($"Error deleting task: {ex.Message}", "Delete Error");
        }
    }

    /// <summary>
    /// Saves all changes to the current note.
    /// </summary>
    [RelayCommand]
    public async Task SaveAsync()
    {
        if (_currentNote is null || !_hasUnsavedChanges)
            return;

        try
        {
            await _stickyNoteService.UpdateNoteAsync(
                _currentNote.Id,
                _currentNote.Title,
                _currentNote.Content,
                _currentNote.Status);

            _hasUnsavedChanges = false;
        }
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(SaveAsync));
            MessageBox.Show($"Error saving note: {ex.Message}", "Save Error");
        }
    }

    /// <summary>
    /// Checks if there are unsaved changes and prompts the user if needed.
    /// </summary>
    public bool CanCloseWindow()
    {
        if (!_hasUnsavedChanges)
            return true;

        var result = MessageBox.Show(
            "You have unsaved changes. Do you want to save before closing?",
            "Unsaved Changes",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            SaveCommand.Execute(null);
            return true;
        }

        return result == MessageBoxResult.No;
    }
}
