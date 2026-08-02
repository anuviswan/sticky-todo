namespace StickyDo.Widget.Interfaces;

/// <summary>
/// Abstraction over Windows file dialogs. Keeps ViewModels view-agnostic and enables testing.
/// </summary>
public interface IFilePickerService
{
    /// <summary>
    /// Shows a Save File dialog and returns the chosen path, or <see langword="null"/> if the
    /// user cancelled.
    /// </summary>
    /// <param name="defaultFileName">The file name pre-filled in the dialog.</param>
    /// <param name="filter">The dialog's file-type filter, e.g. "StickyDo Backup (*.json)|*.json".</param>
    /// <param name="initialDirectory">The folder the dialog should open to, if it exists.</param>
    string? ShowSaveFileDialog(string defaultFileName, string filter, string? initialDirectory = null);

    /// <summary>
    /// Shows an Open File dialog and returns the chosen path, or <see langword="null"/> if the
    /// user cancelled.
    /// </summary>
    /// <param name="filter">The dialog's file-type filter, e.g. "StickyDo Backup (*.zip)|*.zip".</param>
    /// <param name="initialDirectory">The folder the dialog should open to, if it exists.</param>
    string? ShowOpenFileDialog(string filter, string? initialDirectory = null);
}
