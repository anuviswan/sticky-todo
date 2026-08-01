using System.Windows;
using StickyDo.Widget.Utilities;

namespace StickyDo.Widget.Services;

/// <summary>
/// Wires up process-wide unhandled exception handlers so that an exception thrown after
/// startup is logged and surfaced to the user instead of crashing the app silently.
/// Registered directly from <see cref="App"/> (before the DI container exists), so its
/// dependencies are plain delegates rather than injected services.
/// </summary>
public sealed class GlobalExceptionHandler
{
    private const string ErrorDialogTitle = "Unexpected Error";

    private readonly Action<string, string> _showErrorDialog;
    private readonly Action<Exception, string> _logException;
    private readonly Action<string> _logError;

    public GlobalExceptionHandler(
        Action<string, string>? showErrorDialog = null,
        Action<Exception, string>? logException = null,
        Action<string>? logError = null)
    {
        _showErrorDialog = showErrorDialog ?? ShowErrorDialog;
        _logException = logException ?? LoggerHelper.LogException;
        _logError = logError ?? LoggerHelper.LogError;
    }

    /// <summary>
    /// Subscribes to the UI-thread, non-UI-thread and unobserved-task exception sources.
    /// Must be called once, as early as possible during startup.
    /// </summary>
    public void Register(Application application)
    {
        application.DispatcherUnhandledException += (_, e) =>
        {
            // Recoverable: we've shown the user an error dialog, so the app can keep running.
            HandleDispatcherUnhandledException(e.Exception);
            e.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            HandleAppDomainUnhandledException(e.ExceptionObject);

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            HandleUnobservedTaskException(e.Exception);
            // Prevents the exception from re-throwing on the finalizer thread.
            e.SetObserved();
        };
    }

    public void HandleDispatcherUnhandledException(Exception ex) =>
        LogAndShowDialog(ex, "UI thread");

    public void HandleAppDomainUnhandledException(object exceptionObject)
    {
        if (exceptionObject is Exception ex)
        {
            // e.IsTerminating is true for the vast majority of these (the CLR is about to tear
            // down the process regardless of what we do here) - logging and showing a dialog is
            // best-effort so the crash isn't silent, not an attempt to keep the app alive.
            LogAndShowDialog(ex, "background thread");
        }
        else
        {
            _logError($"Unhandled non-exception error on a background thread: {exceptionObject}");
        }
    }

    /// <summary>
    /// Unobserved task exceptions are a background housekeeping event (a faulted task was
    /// garbage-collected without anyone observing its exception) rather than something the user
    /// is actively experiencing right now, and they no longer crash the process by default.
    /// Log only - popping a dialog here could surface a stale, confusing error at an arbitrary
    /// later time on the finalizer thread.
    /// </summary>
    public void HandleUnobservedTaskException(AggregateException ex) =>
        _logException(ex, "unobserved task exception");

    private void LogAndShowDialog(Exception ex, string source)
    {
        _logException(ex, source);
        _showErrorDialog(ErrorDialogTitle, $"An unexpected error occurred and has been logged.\n\n{ex.Message}");
    }

    private static void ShowErrorDialog(string title, string message)
    {
        void Show() => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
            dispatcher.Invoke(Show);
        else
            Show();
    }
}
