using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Widget.Services;

namespace StickyDo.Widget.Tests.Services;

[TestClass]
public class WindowManagerTests
{
    [TestMethod]
    public void IsApplicationExiting_DefaultsToFalse()
    {
        var windowManager = new WindowManager();

        Assert.IsFalse(windowManager.IsApplicationExiting);
    }

    [TestMethod]
    public void MarkApplicationExiting_SetsIsApplicationExitingToTrue()
    {
        var windowManager = new WindowManager();

        windowManager.MarkApplicationExiting();

        Assert.IsTrue(windowManager.IsApplicationExiting);
    }
}
