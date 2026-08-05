using CommunityToolkit.Mvvm.ComponentModel;
using StickyDo.Domain.Models;
using StickyDo.Domain.Models.RichText;

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
    private NoteType type = NoteType.Todo;

    [ObservableProperty]
    private bool hasTasks;

    [ObservableProperty]
    private bool isFavorite;

    [ObservableProperty]
    private string firstTaskTitle = string.Empty;

    /// <summary>Formatting for <see cref="FirstTaskTitle"/>, remapped onto its (possibly truncated) offsets.</summary>
    [ObservableProperty]
    private RichTextFormatting? firstTaskTitleFormatting;

    /// <summary>Truncated preview of a free-form note's content, for display on its list card.</summary>
    [ObservableProperty]
    private string contentPreview = string.Empty;

    /// <summary>Formatting for <see cref="ContentPreview"/>, remapped onto its (possibly truncated) offsets.</summary>
    [ObservableProperty]
    private RichTextFormatting? contentPreviewFormatting;

    [ObservableProperty]
    private bool firstTaskCompleted;

    [ObservableProperty]
    private int remainingTaskCount;

    /// <summary>Titles of all tasks in the note, used to search notes by content.</summary>
    [ObservableProperty]
    private IReadOnlyList<string> taskTitles = [];
}
