using System.Windows;

namespace StickyDo.Widget.Services;

/// <summary>
/// Abstraction for showing dialogs and messages.
/// Keeps ViewModels view-agnostic and enables testing.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows an information or error message dialog.
    /// </summary>
    Task ShowMessageAsync(string title, string message, MessageBoxImage icon = MessageBoxImage.None);

    /// <summary>
    /// Shows a confirmation dialog and returns the result.
    /// </summary>
    Task<bool> ShowConfirmationAsync(string title, string message);
}
