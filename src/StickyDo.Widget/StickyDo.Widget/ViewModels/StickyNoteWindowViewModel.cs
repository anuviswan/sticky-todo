using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StickyDo.Domain.Constants;
using StickyDo.Domain.Models;
using StickyDo.Domain.Services;
using StickyDo.Widget.Interfaces;
using StickyDo.Widget.Services;
using StickyDo.Widget.Utilities;

namespace StickyDo.Widget.ViewModels;

/// <summary>
/// ViewModel for a floating sticky note window with task management.
/// Pure MVVM - communicates via services and observable properties, not callbacks.
/// </summary>
public partial class StickyNoteWindowViewModel : ObservableObject
{
    private readonly StickyNoteService _stickyNoteService;
    private readonly IDialogService _dialogService;
    private readonly IWindowService _windowService;
    private readonly IStickyNoteCreationService _creationService;
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
    private bool shouldFocusAddTaskInput = false;

    [ObservableProperty]
    private double windowX;

    [ObservableProperty]
    private double windowY;

    [ObservableProperty]
    private double windowWidth = 300;

    [ObservableProperty]
    private double windowHeight = 400;

    [ObservableProperty]
    private uint currentColor = ColorPalette.Colors[0];

    [ObservableProperty]
    private bool isColorPickerOpen = false;

    [ObservableProperty]
    private ObservableCollection<uint> availableColors = new(ColorPalette.Colors);

    partial void OnTitleChanged(string value)
    {
        if (_currentNote != null)
        {
            _currentNote.Title = value;
            _hasUnsavedChanges = true;
        }
    }

    public StickyNoteWindowViewModel(
        StickyNoteService stickyNoteService,
        IDialogService dialogService,
        IWindowService windowService,
        IStickyNoteCreationService creationService)
    {
        ArgumentNullException.ThrowIfNull(stickyNoteService);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(windowService);
        ArgumentNullException.ThrowIfNull(creationService);
        _stickyNoteService = stickyNoteService;
        _dialogService = dialogService;
        _windowService = windowService;
        _creationService = creationService;
    }

    /// <summary>
    /// Creates a new note window via the creation service.
    /// </summary>
    [RelayCommand]
    public async Task CreateNewNoteAsync()
    {
        try
        {
            await _creationService.CreateNewNoteAsync();
        }
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(CreateNewNoteAsync));
            await _dialogService.ShowMessageAsync(
                "Create Note Error",
                $"Error creating new note: {ex.Message}",
                System.Windows.MessageBoxImage.Error);
        }
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
                await _dialogService.ShowMessageAsync("Error", "Note not found.", System.Windows.MessageBoxImage.Error);
                return;
            }

            NoteId = _currentNote.Id;
            Title = _currentNote.Title;
            CurrentColor = _currentNote.ColorArgb ?? ColorPalette.GetDefaultColor();

            Tasks.Clear();

            // If no tasks exist, add a sample task for demonstration
            if (!_currentNote.Tasks.Any())
            {
                var sampleTaskId = await _stickyNoteService.CreateTaskAsync(_currentNote.Id, "First Task");
                var sampleTask = await _stickyNoteService.GetNoteByIdAsync(_currentNote.Id);
                if (sampleTask?.Tasks.FirstOrDefault(t => t.Id == sampleTaskId) is { } newTask)
                {
                    var taskVm = new StickyNoteTaskItemViewModel
                    {
                        Id = newTask.Id,
                        Title = newTask.Title,
                        IsCompleted = newTask.IsCompleted,
                        Order = newTask.Order,
                        CreatedAt = newTask.CreatedAt,
                        UpdatedAt = newTask.UpdatedAt
                    };
                    taskVm.SetCallbacks(UpdateTaskAsync, async (taskId) => await DeleteTaskAsync(taskId));
                    Tasks.Add(taskVm);
                }
            }
            else
            {
                foreach (var task in _currentNote.Tasks.OrderBy(t => t.Order))
                {
                    var taskVm = new StickyNoteTaskItemViewModel
                    {
                        Id = task.Id,
                        Title = task.Title,
                        IsCompleted = task.IsCompleted,
                        Order = task.Order,
                        CreatedAt = task.CreatedAt,
                        UpdatedAt = task.UpdatedAt
                    };
                    taskVm.SetCallbacks(UpdateTaskAsync, async (taskId) => await DeleteTaskAsync(taskId));
                    Tasks.Add(taskVm);
                }
            }

            _hasUnsavedChanges = false;
            NewTaskTitle = string.Empty;
        }
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(LoadNoteAsync));
            await _dialogService.ShowMessageAsync("Load Error", $"Error loading note: {ex.Message}", System.Windows.MessageBoxImage.Error);
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
                var taskVm = new StickyNoteTaskItemViewModel
                {
                    Id = newTask.Id,
                    Title = newTask.Title,
                    IsCompleted = newTask.IsCompleted,
                    Order = newTask.Order,
                    CreatedAt = newTask.CreatedAt,
                    UpdatedAt = newTask.UpdatedAt
                };
                taskVm.SetCallbacks(UpdateTaskAsync, async (taskId) => await DeleteTaskAsync(taskId));
                Tasks.Add(taskVm);

                _hasUnsavedChanges = true;
                NewTaskTitle = string.Empty;
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(AddTaskAsync));
            await _dialogService.ShowMessageAsync("Add Task Error", $"Error adding task: {ex.Message}", System.Windows.MessageBoxImage.Error);
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
            await _dialogService.ShowMessageAsync("Update Error", $"Error updating task: {ex.Message}", System.Windows.MessageBoxImage.Error);
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
            await _dialogService.ShowMessageAsync("Delete Error", $"Error deleting task: {ex.Message}", System.Windows.MessageBoxImage.Error);
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
                _currentNote.Status);

            _hasUnsavedChanges = false;
        }
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(SaveAsync));
            await _dialogService.ShowMessageAsync("Save Error", $"Error saving note: {ex.Message}", System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Auto-saves any unsaved changes without prompting the user.
    /// </summary>
    public async Task<bool> CanCloseWindowAsync()
    {
        if (_hasUnsavedChanges)
        {
            await SaveAsync();
        }

        return true;
    }

    /// <summary>
    /// Focuses the add task input field when placeholder is clicked.
    /// </summary>
    [RelayCommand]
    public void FocusAddTaskInput()
    {
        ShouldFocusAddTaskInput = true;
        // Reset after a brief delay so behavior can be triggered again
        Task.Delay(100).ContinueWith(_ => ShouldFocusAddTaskInput = false);
    }

    /// <summary>
    /// Opens the color picker overlay.
    /// </summary>
    [RelayCommand]
    public void OpenColorPicker()
    {
        IsColorPickerOpen = true;
    }

    /// <summary>
    /// Closes the color picker overlay without changing color.
    /// </summary>
    [RelayCommand]
    public void CloseColorPicker()
    {
        IsColorPickerOpen = false;
    }

    /// <summary>
    /// Selects a color and saves it to the database.
    /// </summary>
    [RelayCommand]
    public async Task SelectColorAsync(uint color)
    {
        if (_currentNote is null)
            return;

        try
        {
            CurrentColor = color;
            _currentNote.ColorArgb = color;

            await _stickyNoteService.UpdateNoteAsync(
                _currentNote.Id,
                _currentNote.Title,
                _currentNote.Status,
                color);

            IsColorPickerOpen = false;
        }
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(SelectColorAsync));
            await _dialogService.ShowMessageAsync("Color Error", $"Error changing color: {ex.Message}", System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Closes the window after checking for unsaved changes.
    /// </summary>
    [RelayCommand]
    public async Task CloseWindowAsync()
    {
        var canClose = await CanCloseWindowAsync();
        if (canClose)
        {
            _windowService.RequestClose();
        }
    }

}
