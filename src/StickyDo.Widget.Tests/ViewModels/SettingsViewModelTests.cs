using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Domain.Constants;
using StickyDo.Widget.ViewModels;

namespace StickyDo.Widget.Tests.ViewModels;

[TestClass]
public class SettingsViewModelTests
{
    private SettingsViewModel _viewModel = null!;

    [TestInitialize]
    public void Setup()
    {
        _viewModel = new SettingsViewModel();
    }

    [TestMethod]
    public void Constructor_DefaultsSelectedColorToPaletteDefault()
    {
        Assert.AreEqual(ColorPalette.GetDefaultColor(), _viewModel.SelectedDefaultColor);
    }

    [TestMethod]
    public void Constructor_LaunchAtStartupDefaultsToFalse()
    {
        Assert.IsFalse(_viewModel.LaunchAtStartup);
    }

    [TestMethod]
    public void SelectDefaultColor_UpdatesSelectedDefaultColor()
    {
        var color = ColorPalette.Colors[3];

        _viewModel.SelectDefaultColor(color);

        Assert.AreEqual(color, _viewModel.SelectedDefaultColor);
    }

    [TestMethod]
    public void Close_RaisesCloseRequested()
    {
        var raised = false;
        _viewModel.CloseRequested += (s, e) => raised = true;

        _viewModel.Close();

        Assert.IsTrue(raised);
    }
}
