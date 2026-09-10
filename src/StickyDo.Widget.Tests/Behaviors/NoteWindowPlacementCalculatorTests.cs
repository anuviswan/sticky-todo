using System.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Widget.Behaviors;

namespace StickyDo.Widget.Tests.Behaviors;

/// <summary>
/// Covers the off-screen note recovery described in issue #183: a note whose saved position came
/// from a display layout that no longer exists must still open somewhere the user can see it.
/// </summary>
[TestClass]
public class NoteWindowPlacementCalculatorTests
{
    private static readonly Rect SingleMonitor = new(x: 0, y: 0, width: 1920, height: 1040);
    private static readonly Rect SecondMonitor = new(x: 1920, y: 0, width: 1920, height: 1040);
    private static readonly IReadOnlyList<Rect> OneScreen = [SingleMonitor];
    private static readonly IReadOnlyList<Rect> TwoScreens = [SingleMonitor, SecondMonitor];

    [TestMethod]
    public void FullyVisibleNote_IsLeftUntouched()
    {
        var saved = new Rect(x: 400, y: 300, width: 320, height: 400);

        var result = NoteWindowPlacementCalculator.EnsureVisible(saved, OneScreen);

        Assert.IsFalse(result.WasCorrected);
        Assert.AreEqual(saved, result.Bounds);
    }

    [TestMethod]
    public void NoteBeyondTheOnlyMonitor_IsPulledBackOnScreenAtSameSize()
    {
        // The reported case: a note last closed on a second monitor, reopened without it.
        var saved = new Rect(x: 2910, y: 690, width: 320, height: 224);

        var result = NoteWindowPlacementCalculator.EnsureVisible(saved, OneScreen);

        Assert.IsTrue(result.WasCorrected);
        Assert.AreEqual(saved.Width, result.Bounds.Width);
        Assert.AreEqual(saved.Height, result.Bounds.Height);
        Assert.IsTrue(SingleMonitor.Contains(result.Bounds), $"{result.Bounds} is not inside {SingleMonitor}");
    }

    [TestMethod]
    public void NoteAboveTheMonitor_IsPulledBackOnScreen()
    {
        var saved = new Rect(x: 400, y: -900, width: 320, height: 400);

        var result = NoteWindowPlacementCalculator.EnsureVisible(saved, OneScreen);

        Assert.IsTrue(result.WasCorrected);
        Assert.AreEqual(SingleMonitor.Top, result.Bounds.Top);
        Assert.AreEqual(saved.Left, result.Bounds.Left);
    }

    [TestMethod]
    public void NoteOverlappingEnoughToGrab_IsLeftUntouched()
    {
        // Hanging off the right edge but with well over the minimum visible strip still on screen.
        var saved = new Rect(x: 1700, y: 300, width: 320, height: 400);

        var result = NoteWindowPlacementCalculator.EnsureVisible(saved, OneScreen);

        Assert.IsFalse(result.WasCorrected);
        Assert.AreEqual(saved, result.Bounds);
    }

    [TestMethod]
    public void NoteWithOnlyASliverVisible_IsPulledBackOnScreen()
    {
        // Only 10 DIPs wide on screen - not enough of the title bar to find or drag.
        var saved = new Rect(x: 1910, y: 300, width: 320, height: 400);

        var result = NoteWindowPlacementCalculator.EnsureVisible(saved, OneScreen);

        Assert.IsTrue(result.WasCorrected);
        Assert.IsTrue(SingleMonitor.Contains(result.Bounds), $"{result.Bounds} is not inside {SingleMonitor}");
    }

    [TestMethod]
    public void NoteOnSecondMonitor_IsLeftUntouchedWhenThatMonitorIsConnected()
    {
        var saved = new Rect(x: 2910, y: 690, width: 320, height: 224);

        var result = NoteWindowPlacementCalculator.EnsureVisible(saved, TwoScreens);

        Assert.IsFalse(result.WasCorrected);
        Assert.AreEqual(saved, result.Bounds);
    }

    [TestMethod]
    public void NoteBeyondEveryMonitor_MovesToTheNearestOne()
    {
        var saved = new Rect(x: 5000, y: 300, width: 320, height: 400);

        var result = NoteWindowPlacementCalculator.EnsureVisible(saved, TwoScreens);

        Assert.IsTrue(result.WasCorrected);
        Assert.IsTrue(SecondMonitor.Contains(result.Bounds), $"{result.Bounds} is not inside {SecondMonitor}");
    }

    [TestMethod]
    public void NoteLargerThanTheWorkArea_IsShrunkToFit()
    {
        var saved = new Rect(x: 4000, y: 300, width: 2400, height: 1600);

        var result = NoteWindowPlacementCalculator.EnsureVisible(saved, OneScreen);

        Assert.IsTrue(result.WasCorrected);
        Assert.AreEqual(SingleMonitor.Width, result.Bounds.Width);
        Assert.AreEqual(SingleMonitor.Height, result.Bounds.Height);
        Assert.IsTrue(SingleMonitor.Contains(result.Bounds), $"{result.Bounds} is not inside {SingleMonitor}");
    }

    [TestMethod]
    public void NoScreensReported_LeavesBoundsUntouched()
    {
        // Screen enumeration failed; guessing a position would be worse than doing nothing.
        var saved = new Rect(x: 2910, y: 690, width: 320, height: 224);

        var result = NoteWindowPlacementCalculator.EnsureVisible(saved, []);

        Assert.IsFalse(result.WasCorrected);
        Assert.AreEqual(saved, result.Bounds);
    }

    [TestMethod]
    public void NonFiniteBounds_AreCenteredOnTheFirstMonitor()
    {
        var saved = new Rect(x: double.NaN, y: double.NaN, width: 320, height: 400);

        var result = NoteWindowPlacementCalculator.EnsureVisible(saved, OneScreen);

        Assert.IsTrue(result.WasCorrected);
        Assert.AreEqual(320, result.Bounds.Width);
        Assert.AreEqual(400, result.Bounds.Height);
        Assert.AreEqual((SingleMonitor.Width - 320) / 2, result.Bounds.Left);
        Assert.AreEqual((SingleMonitor.Height - 400) / 2, result.Bounds.Top);
    }

    [TestMethod]
    public void NonFiniteSize_FallsBackToTheDefaultNoteSize()
    {
        var saved = new Rect(x: 100, y: 100, width: double.NaN, height: double.NaN);

        var result = NoteWindowPlacementCalculator.EnsureVisible(saved, OneScreen);

        Assert.IsTrue(result.WasCorrected);
        Assert.AreEqual(320, result.Bounds.Width);
        Assert.AreEqual(400, result.Bounds.Height);
    }

    [TestMethod]
    public void NoteSmallerThanTheMinimumVisibleStrip_MustBeFullyVisible()
    {
        // A note narrower than the minimum strip only needs all of itself on screen; requiring the
        // full 120 DIPs would otherwise move a perfectly visible small note.
        var saved = new Rect(x: 400, y: 300, width: 80, height: 30);

        var result = NoteWindowPlacementCalculator.EnsureVisible(saved, OneScreen);

        Assert.IsFalse(result.WasCorrected);
        Assert.AreEqual(saved, result.Bounds);
    }

    [TestMethod]
    public void EnsureVisible_ThrowsWhenWorkAreasIsNull()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => NoteWindowPlacementCalculator.EnsureVisible(new Rect(0, 0, 320, 400), null!));
    }
}
