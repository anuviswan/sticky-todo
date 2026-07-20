using System.Windows;
using StickyDo.Widget.Interfaces;

namespace StickyDo.Widget.Services;

/// <summary>
/// WPF implementation of IDialogService using MessageBox.
/// </summary>
public class DialogService : IDialogService
{
    public Task ShowMessageAsync(string title, string message, MessageBoxImage icon = MessageBoxImage.None)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, icon);
        return Task.CompletedTask;
    }

    public Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return Task.FromResult(result == MessageBoxResult.Yes);
    }
}
