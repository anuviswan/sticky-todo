using Microsoft.VisualStudio.TestTools.UnitTesting;
using StickyDo.Domain.Constants;
using StickyDo.Domain.Models;
using StickyDo.Domain.Repositories;
using StickyDo.Widget.ViewModels;

namespace StickyDo.Widget.Tests.ViewModels;

[TestClass]
public class SettingsViewModelTests
{
    private FakeSettingsRepository _repository = null!;
    private SettingsViewModel _viewModel = null!;

    [TestInitialize]
    public void Setup()
    {
        _repository = new FakeSettingsRepository();
        _viewModel = new SettingsViewModel(_repository);
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

    [TestMethod]
    public async Task InitializeAsync_PopulatesPropertiesFromRepository_WithoutSaving()
    {
        var color = ColorPalette.Colors[2];
        _repository.StoredSettings = new AppSettings { LaunchAtStartup = true, DefaultNoteColor = color };

        await _viewModel.InitializeAsync();

        Assert.IsTrue(_viewModel.LaunchAtStartup);
        Assert.AreEqual(color, _viewModel.SelectedDefaultColor);
        Assert.AreEqual(0, _repository.SaveCallCount);
    }

    [TestMethod]
    public void ChangingLaunchAtStartup_SavesSettingsAutomatically()
    {
        _viewModel.LaunchAtStartup = true;

        Assert.AreEqual(1, _repository.SaveCallCount);
        Assert.IsTrue(_repository.StoredSettings!.LaunchAtStartup);
    }

    [TestMethod]
    public void SelectDefaultColor_SavesSettingsAutomatically()
    {
        var color = ColorPalette.Colors[4];

        _viewModel.SelectDefaultColor(color);

        Assert.AreEqual(1, _repository.SaveCallCount);
        Assert.AreEqual(color, _repository.StoredSettings!.DefaultNoteColor);
    }

    private sealed class FakeSettingsRepository : ISettingsRepository
    {
        public AppSettings? StoredSettings { get; set; }
        public int SaveCallCount { get; private set; }

        public Task<AppSettings> LoadAsync() => Task.FromResult(StoredSettings ?? new AppSettings());

        public Task SaveAsync(AppSettings settings)
        {
            StoredSettings = settings;
            SaveCallCount++;
            return Task.CompletedTask;
        }
    }
}
