using System.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Widget.Behaviors;

namespace StickyDo.Widget.Tests.Behaviors;

[TestClass]
public class WindowResizeCalculatorTests
{
    private static readonly Rect StartBounds = new(x: 100, y: 100, width: 300, height: 250);
    private const double MinWidth = 250;
    private const double MinHeight = 200;
    private const double MaxWidth = double.PositiveInfinity;
    private const double MaxHeight = double.PositiveInfinity;
    private static readonly Rect WorkArea = new(x: 0, y: 0, width: 1920, height: 1080);

    private static Rect Calculate(ResizeEdge edge, double deltaX, double deltaY, Rect? workArea = null) =>
        WindowResizeCalculator.Calculate(StartBounds, edge, deltaX, deltaY, MinWidth, MinHeight, MaxWidth, MaxHeight, workArea ?? WorkArea);

    [TestMethod]
    public void Right_IncreasesWidthOnly_LeftAndTopUnchanged()
    {
        var result = Calculate(ResizeEdge.Right, deltaX: 40, deltaY: 0);

        Assert.AreEqual(StartBounds.Left, result.Left);
        Assert.AreEqual(StartBounds.Top, result.Top);
        Assert.AreEqual(StartBounds.Width + 40, result.Width);
        Assert.AreEqual(StartBounds.Height, result.Height);
    }

    [TestMethod]
    public void Left_DraggingOutward_IncreasesWidthAndMovesLeftEdge()
    {
        var result = Calculate(ResizeEdge.Left, deltaX: -40, deltaY: 0);

        Assert.AreEqual(StartBounds.Left - 40, result.Left);
        Assert.AreEqual(StartBounds.Width + 40, result.Width);
        Assert.AreEqual(StartBounds.Top, result.Top);
        Assert.AreEqual(StartBounds.Height, result.Height);
    }

    [TestMethod]
    public void Bottom_IncreasesHeightOnly_LeftAndTopUnchanged()
    {
        var result = Calculate(ResizeEdge.Bottom, deltaX: 0, deltaY: 30);

        Assert.AreEqual(StartBounds.Top, result.Top);
        Assert.AreEqual(StartBounds.Height + 30, result.Height);
        Assert.AreEqual(StartBounds.Width, result.Width);
    }

    [TestMethod]
    public void Top_DraggingOutward_IncreasesHeightAndMovesTopEdge()
    {
        var result = Calculate(ResizeEdge.Top, deltaX: 0, deltaY: -30);

        Assert.AreEqual(StartBounds.Top - 30, result.Top);
        Assert.AreEqual(StartBounds.Height + 30, result.Height);
    }

    [TestMethod]
    public void BottomRight_ChangesBothWidthAndHeight()
    {
        var result = Calculate(ResizeEdge.BottomRight, deltaX: 20, deltaY: 15);

        Assert.AreEqual(StartBounds.Width + 20, result.Width);
        Assert.AreEqual(StartBounds.Height + 15, result.Height);
        Assert.AreEqual(StartBounds.Left, result.Left);
        Assert.AreEqual(StartBounds.Top, result.Top);
    }

    [TestMethod]
    public void TopLeft_ChangesBothDimensionsAndMovesLeftAndTop()
    {
        var result = Calculate(ResizeEdge.TopLeft, deltaX: -20, deltaY: -15);

        Assert.AreEqual(StartBounds.Left - 20, result.Left);
        Assert.AreEqual(StartBounds.Top - 15, result.Top);
        Assert.AreEqual(StartBounds.Width + 20, result.Width);
        Assert.AreEqual(StartBounds.Height + 15, result.Height);
    }

    [TestMethod]
    public void BottomLeft_ChangesBothDimensionsAndMovesLeftOnly()
    {
        var result = Calculate(ResizeEdge.BottomLeft, deltaX: -20, deltaY: 15);

        Assert.AreEqual(StartBounds.Left - 20, result.Left);
        Assert.AreEqual(StartBounds.Top, result.Top);
        Assert.AreEqual(StartBounds.Width + 20, result.Width);
        Assert.AreEqual(StartBounds.Height + 15, result.Height);
    }

    [TestMethod]
    public void Right_ShrinkingPastMinWidth_ClampsWidthToMinWidth()
    {
        var result = Calculate(ResizeEdge.Right, deltaX: -1000, deltaY: 0);

        Assert.AreEqual(MinWidth, result.Width);
    }

    [TestMethod]
    public void Left_ShrinkingPastMinWidth_ClampsWidthAndStopsLeftEdgeMoving()
    {
        var result = Calculate(ResizeEdge.Left, deltaX: 1000, deltaY: 0);

        Assert.AreEqual(MinWidth, result.Width);
        Assert.AreEqual(StartBounds.Right - MinWidth, result.Left);
    }

    [TestMethod]
    public void Bottom_ShrinkingPastMinHeight_ClampsHeightToMinHeight()
    {
        var result = Calculate(ResizeEdge.Bottom, deltaX: 0, deltaY: -1000);

        Assert.AreEqual(MinHeight, result.Height);
    }

    [TestMethod]
    public void Top_ShrinkingPastMinHeight_ClampsHeightAndStopsTopEdgeMoving()
    {
        var result = Calculate(ResizeEdge.Top, deltaX: 0, deltaY: 1000);

        Assert.AreEqual(MinHeight, result.Height);
        Assert.AreEqual(StartBounds.Bottom - MinHeight, result.Top);
    }

    [TestMethod]
    public void Right_GrowingPastWorkAreaRight_ClampsToWorkAreaBoundary()
    {
        var tightWorkArea = new Rect(x: 0, y: 0, width: 320, height: 1080);

        var result = Calculate(ResizeEdge.Right, deltaX: 1000, deltaY: 0, workArea: tightWorkArea);

        Assert.AreEqual(tightWorkArea.Right - StartBounds.Left, result.Width);
    }

    [TestMethod]
    public void Left_DraggingPastWorkAreaLeft_ClampsToWorkAreaBoundary()
    {
        var result = Calculate(ResizeEdge.Left, deltaX: -1000, deltaY: 0);

        Assert.AreEqual(WorkArea.Left, result.Left);
        Assert.AreEqual(StartBounds.Right - WorkArea.Left, result.Width);
    }

    [TestMethod]
    public void Bottom_GrowingPastWorkAreaBottom_ClampsToWorkAreaBoundary()
    {
        var tightWorkArea = new Rect(x: 0, y: 0, width: 1920, height: 300);

        var result = Calculate(ResizeEdge.Bottom, deltaX: 0, deltaY: 1000, workArea: tightWorkArea);

        Assert.AreEqual(tightWorkArea.Bottom - StartBounds.Top, result.Height);
    }

    [TestMethod]
    public void Top_DraggingPastWorkAreaTop_ClampsToWorkAreaBoundary()
    {
        var result = Calculate(ResizeEdge.Top, deltaX: 0, deltaY: -1000);

        Assert.AreEqual(WorkArea.Top, result.Top);
        Assert.AreEqual(StartBounds.Bottom - WorkArea.Top, result.Height);
    }
}
