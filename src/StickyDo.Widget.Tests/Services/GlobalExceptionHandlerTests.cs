using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Widget.Services;

namespace StickyDo.Widget.Tests.Services;

[TestClass]
public class GlobalExceptionHandlerTests
{
    [TestMethod]
    public void HandleDispatcherUnhandledException_LogsAndShowsDialog()
    {
        Exception? loggedException = null;
        string? loggedSource = null;
        string? dialogTitle = null;
        string? dialogMessage = null;

        var handler = new GlobalExceptionHandler(
            showErrorDialog: (title, message) => { dialogTitle = title; dialogMessage = message; },
            logException: (ex, source) => { loggedException = ex; loggedSource = source; });

        var thrown = new InvalidOperationException("boom");

        handler.HandleDispatcherUnhandledException(thrown);

        Assert.AreSame(thrown, loggedException);
        Assert.IsNotNull(loggedSource);
        Assert.IsNotNull(dialogTitle);
        Assert.IsTrue(dialogMessage!.Contains("boom"));
    }

    [TestMethod]
    public void HandleAppDomainUnhandledException_WithException_LogsAndShowsDialog()
    {
        var dialogShown = false;
        Exception? loggedException = null;

        var handler = new GlobalExceptionHandler(
            showErrorDialog: (_, _) => dialogShown = true,
            logException: (ex, _) => loggedException = ex);

        var thrown = new InvalidOperationException("background failure");

        handler.HandleAppDomainUnhandledException(thrown);

        Assert.AreSame(thrown, loggedException);
        Assert.IsTrue(dialogShown);
    }

    [TestMethod]
    public void HandleAppDomainUnhandledException_WithNonException_LogsErrorWithoutDialog()
    {
        var dialogShown = false;
        string? loggedError = null;

        var handler = new GlobalExceptionHandler(
            showErrorDialog: (_, _) => dialogShown = true,
            logError: message => loggedError = message);

        handler.HandleAppDomainUnhandledException("not an exception");

        Assert.IsNotNull(loggedError);
        Assert.IsTrue(loggedError!.Contains("not an exception"));
        Assert.IsFalse(dialogShown);
    }

    [TestMethod]
    public void HandleUnobservedTaskException_LogsWithoutShowingDialog()
    {
        var dialogShown = false;
        Exception? loggedException = null;

        var handler = new GlobalExceptionHandler(
            showErrorDialog: (_, _) => dialogShown = true,
            logException: (ex, _) => loggedException = ex);

        var thrown = new AggregateException(new InvalidOperationException("unobserved"));

        handler.HandleUnobservedTaskException(thrown);

        Assert.AreSame(thrown, loggedException);
        Assert.IsFalse(dialogShown);
    }
}
