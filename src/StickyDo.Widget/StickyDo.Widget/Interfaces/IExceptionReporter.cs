namespace StickyDo.Widget.Interfaces;

/// <summary>
/// Abstraction for logging and surfacing unhandled exceptions to the user.
/// Keeps <see cref="StickyDo.Widget.Services.GlobalExceptionHandler"/> testable without
/// popping real message boxes or writing to the console during test runs.
/// </summary>
public interface IExceptionReporter
{
    void LogException(Exception ex, string context);

    void LogError(string message);

    void ShowErrorDialog(string title, string message);
}
