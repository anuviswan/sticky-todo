using System.Windows;
using System.Windows.Interop;
using StickyDo.Widget.Interfaces;

namespace StickyDo.Widget.Services;

/// <summary>
/// WPF implementation of IDialogService using MessageBox.
/// </summary>
public class DialogService : IDialogService
{
    public Task ShowMessageAsync(string title, string message, MessageBoxImage icon = MessageBoxImage.None)
    {
        var owner = GetOwnerWindow();
        if (owner is not null)
            MessageBox.Show(owner, message, title, MessageBoxButton.OK, icon);
        else
            MessageBox.Show(message, title, MessageBoxButton.OK, icon);
        return Task.CompletedTask;
    }

    public Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var owner = GetOwnerWindow();
        var result = owner is not null
            ? MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
            : MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return Task.FromResult(result == MessageBoxResult.Yes);
    }

    /// <summary>
    /// Resolves the window the dialog should be owned by, so it gets correct z-order,
    /// activation, and centering instead of being an ownerless top-level window that has to
    /// independently win OS-level foreground activation.
    /// Only ever returns a window that has actually been shown: MessageBox throws when handed an
    /// owner with no window handle yet, which would turn a startup dialog into a silent no-op
    /// (the notes list stays hidden at startup whenever open notes are restored instead).
    /// </summary>
    private static Window? GetOwnerWindow()
    {
        if (Application.Current is null)
            return null;

        var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive && HasWindowHandle(w));
        if (activeWindow is not null)
            return activeWindow;

        return HasWindowHandle(Application.Current.MainWindow) ? Application.Current.MainWindow : null;
    }

    private static bool HasWindowHandle(Window? window) =>
        window is not null && new WindowInteropHelper(window).Handle != IntPtr.Zero;
}
