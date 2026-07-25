using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StickyDo.Domain.Constants;
using StickyDo.Domain.Models;
using StickyDo.Domain.Services;
using StickyDo.Widget.Interfaces;
using StickyDo.Widget.Messages;
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
    private readonly StickyNoteTaskService _stickyNoteTaskService;
    private readonly IDialogService _dialogService;
    private readonly IStickyNoteCreationService _creationService;
    private readonly PersistenceService _persistenceService;
    private readonly IMessenger _messenger;
    private StickyNote? _currentNote;
    private bool _hasUnsavedChanges;
    private CancellationTokenSource? _idleTimerCts;
    private const int IdleTimeoutMs = 5000;

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

    [ObservableProperty]
    private bool isPinned;

    partial void OnTitleChanged(string value)
    {
        if (_currentNote != null)
        {
            _currentNote.Title = value;
            _hasUnsavedChanges = true;
            OnEditingStarted();
        }
    }

    public StickyNoteWindowViewModel(
        StickyNoteService stickyNoteService,
        StickyNoteTaskService stickyNoteTaskService,
        IDialogService dialogService,
        IStickyNoteCreationService creationService,
        PersistenceService persistenceService,
        IMessenger messenger)
    {
        ArgumentNullException.ThrowIfNull(stickyNoteService);
        ArgumentNullException.ThrowIfNull(stickyNoteTaskService);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(creationService);
        ArgumentNullException.ThrowIfNull(persistenceService);
        ArgumentNullException.ThrowIfNull(messenger);
        _stickyNoteService = stickyNoteService;
        _stickyNoteTaskService = stickyNoteTaskService;
        _dialogService = dialogService;
        _creationService = creationService;
        _persistenceService = persistenceService;
        _messenger = messenger;
    }

    /// <summary>
    /// Raised when the user requests the window to close (e.g. via the close button),
    /// after any unsaved changes have been saved. The hosting service closes the actual
    /// Window instance, keeping this ViewModel view-agnostic.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Raised right after the note is pinned, so the hosting service can capture the window's
    /// current screen position/size and persist it, keeping this ViewModel view-agnostic.
    /// </summary>
    public event EventHandler? NotePinned;

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
            IsPinned = _currentNote.IsPinned;

            Tasks.Clear();

            // If no tasks exist, add a sample task for demonstration
            if (!_currentNote.Tasks.Any())
            {
                var sampleTaskId = await _stickyNoteTaskService.CreateTaskAsync(_currentNote.Id, "First Task");
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
                    taskVm.SetCallbacks(UpdateTaskAsync, async (taskId) => await DeleteTaskAsync(taskId), FocusAddTaskInput);
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
                    taskVm.SetCallbacks(UpdateTaskAsync, async (taskId) => await DeleteTaskAsync(taskId), FocusAddTaskInput);
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
            var taskId = await _stickyNoteTaskService.CreateTaskAsync(_currentNote.Id, NewTaskTitle);
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
                taskVm.SetCallbacks(UpdateTaskAsync, async (taskId) => await DeleteTaskAsync(taskId), FocusAddTaskInput);
                Tasks.Add(taskVm);

                _hasUnsavedChanges = true;
                NewTaskTitle = string.Empty;
                OnEditingStarted();
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
            await _stickyNoteTaskService.UpdateTaskAsync(_currentNote.Id, taskId, title, isCompleted);
            _hasUnsavedChanges = true;
            OnEditingStarted();
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
            await _stickyNoteTaskService.DeleteTaskAsync(_currentNote.Id, taskId);
            var taskVm = Tasks.FirstOrDefault(t => t.Id == taskId);
            if (taskVm != null)
            {
                Tasks.Remove(taskVm);
                _hasUnsavedChanges = true;
                OnEditingStarted();
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
            _messenger.Send(new StickyNoteChangedMessage(_currentNote.Id, StickyNoteChangeType.Updated));
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
            OnEditingStarted();
            _messenger.Send(new StickyNoteChangedMessage(_currentNote.Id, StickyNoteChangeType.Updated));
        }
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(SelectColorAsync));
            await _dialogService.ShowMessageAsync("Color Error", $"Error changing color: {ex.Message}", System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Toggles the pinned state of the note and persists it. A pinned note cannot be
    /// moved by dragging or closed until it is unpinned.
    /// </summary>
    [RelayCommand]
    public async Task TogglePinAsync()
    {
        if (_currentNote is null)
            return;

        try
        {
            IsPinned = !IsPinned;
            _currentNote.IsPinned = IsPinned;

            await _stickyNoteService.SetNotePinnedAsync(_currentNote.Id, IsPinned);
            _messenger.Send(new StickyNoteChangedMessage(_currentNote.Id, StickyNoteChangeType.Updated));

            if (IsPinned)
            {
                NotePinned?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(TogglePinAsync));
            await _dialogService.ShowMessageAsync("Pin Error", $"Error updating pin state: {ex.Message}", System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Closes the window after checking for unsaved changes. Pinned notes cannot be closed.
    /// </summary>
    [RelayCommand]
    public async Task CloseWindowAsync()
    {
        if (IsPinned)
            return;

        var canClose = await CanCloseWindowAsync();
        if (canClose)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Called whenever user starts editing. Starts auto-save timer and resets idle timeout.
    /// </summary>
    private void OnEditingStarted()
    {
        _persistenceService.StartAutoSave();
        ResetIdleTimer();
    }

    /// <summary>
    /// Resets the idle timer. If user stops editing for IdleTimeoutMs, auto-save stops.
    /// </summary>
    private void ResetIdleTimer()
    {
        _idleTimerCts?.Cancel();
        _idleTimerCts = new CancellationTokenSource();
        _ = StopAutoSaveAfterIdleAsync(_idleTimerCts.Token);
    }

    /// <summary>
    /// Stops auto-save after idle timeout with no new edits.
    /// </summary>
    private async Task StopAutoSaveAfterIdleAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(IdleTimeoutMs, cancellationToken);
            await _persistenceService.StopAutoSaveAsync();
            System.Diagnostics.Debug.WriteLine("Auto-save stopped due to inactivity");
        }
        catch (OperationCanceledException)
        {
            // Timer was reset due to new edit - this is expected
        }
    }

}
