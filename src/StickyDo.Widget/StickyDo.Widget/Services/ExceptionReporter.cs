using System.Windows;
using StickyDo.Widget.Interfaces;
using StickyDo.Widget.Utilities;

namespace StickyDo.Widget.Services;

/// <summary>
/// WPF implementation of <see cref="IExceptionReporter"/>, reusing the existing
/// <see cref="LoggerHelper"/> and a plain <see cref="MessageBox"/> for the error dialog.
/// </summary>
public sealed class ExceptionReporter : IExceptionReporter
{
    public void LogException(Exception ex, string context) => LoggerHelper.LogException(ex, context);

    public void LogError(string message) => LoggerHelper.LogError(message);

    public void ShowErrorDialog(string title, string message)
    {
        void Show() => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
            dispatcher.Invoke(Show);
        else
            Show();
    }
}
