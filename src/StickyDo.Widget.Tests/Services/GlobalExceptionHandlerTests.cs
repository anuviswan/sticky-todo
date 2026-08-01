using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Widget.Interfaces;
using StickyDo.Widget.Services;

namespace StickyDo.Widget.Tests.Services;

[TestClass]
public class GlobalExceptionHandlerTests
{
    [TestMethod]
    public void HandleDispatcherUnhandledException_LogsAndShowsDialog()
    {
        var reporter = new FakeExceptionReporter();
        var handler = new GlobalExceptionHandler(reporter);
        var thrown = new InvalidOperationException("boom");

        handler.HandleDispatcherUnhandledException(thrown);

        Assert.AreSame(thrown, reporter.LoggedException);
        Assert.IsNotNull(reporter.LoggedContext);
        Assert.IsNotNull(reporter.DialogTitle);
        Assert.IsTrue(reporter.DialogMessage!.Contains("boom"));
    }

    [TestMethod]
    public void HandleAppDomainUnhandledException_WithException_LogsAndShowsDialog()
    {
        var reporter = new FakeExceptionReporter();
        var handler = new GlobalExceptionHandler(reporter);
        var thrown = new InvalidOperationException("background failure");

        handler.HandleAppDomainUnhandledException(thrown);

        Assert.AreSame(thrown, reporter.LoggedException);
        Assert.IsTrue(reporter.DialogShown);
    }

    [TestMethod]
    public void HandleAppDomainUnhandledException_WithNonException_LogsErrorWithoutDialog()
    {
        var reporter = new FakeExceptionReporter();
        var handler = new GlobalExceptionHandler(reporter);

        handler.HandleAppDomainUnhandledException("not an exception");

        Assert.IsNotNull(reporter.LoggedError);
        Assert.IsTrue(reporter.LoggedError!.Contains("not an exception"));
        Assert.IsFalse(reporter.DialogShown);
    }

    [TestMethod]
    public void HandleUnobservedTaskException_LogsWithoutShowingDialog()
    {
        var reporter = new FakeExceptionReporter();
        var handler = new GlobalExceptionHandler(reporter);
        var thrown = new AggregateException(new InvalidOperationException("unobserved"));

        handler.HandleUnobservedTaskException(thrown);

        Assert.AreSame(thrown, reporter.LoggedException);
        Assert.IsFalse(reporter.DialogShown);
    }

    private sealed class FakeExceptionReporter : IExceptionReporter
    {
        public Exception? LoggedException { get; private set; }
        public string? LoggedContext { get; private set; }
        public string? LoggedError { get; private set; }
        public bool DialogShown { get; private set; }
        public string? DialogTitle { get; private set; }
        public string? DialogMessage { get; private set; }

        public void LogException(Exception ex, string context)
        {
            LoggedException = ex;
            LoggedContext = context;
        }

        public void LogError(string message) => LoggedError = message;

        public void ShowErrorDialog(string title, string message)
        {
            DialogShown = true;
            DialogTitle = title;
            DialogMessage = message;
        }
    }
}
