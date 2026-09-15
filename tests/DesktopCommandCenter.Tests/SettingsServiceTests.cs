using DesktopCommandCenter.Core.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopCommandCenter.Tests;

[TestClass]
public sealed class SettingsServiceTests
{
    private string _directory = null!;
    private string _path = null!;

    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "DesktopCommandCenter.Tests", Guid.NewGuid().ToString("N"));
        _path = Path.Combine(_directory, "settings.json");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }

    [DataTestMethod]
    public void MissingSettingsFileReturnsDefaults()
    {
        var settings = new SettingsService(_path).Load();

        Assert.IsTrue(settings.StartCollapsed);
        Assert.AreEqual(360d, settings.SidebarWidth);
        Assert.IsTrue(settings.AlwaysOnTop);
        Assert.IsTrue(settings.AnimationsEnabled);
        Assert.IsTrue(settings.ShowTrayIcon);
        Assert.IsTrue(settings.ShowSearchModule);
        Assert.IsTrue(settings.ShowFavoritesModule);
        Assert.IsTrue(settings.ShowCurrentWindowModule);
        Assert.IsTrue(settings.ShowQuickActionsModule);
        Assert.IsTrue(settings.ShowMacrosModule);
    }

    [TestMethod]
    public void ValidSettingsLoadCorrectly()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_path, """
            { "settingsVersion": 2, "startCollapsed": false, "sidebarWidth": 420,
              "alwaysOnTop": false, "animationsEnabled": false, "showTrayIcon": false,
              "showSearchModule": false, "showFavoritesModule": true,
              "showCurrentWindowModule": true, "showQuickActionsModule": false,
              "showMacrosModule": true }
            """);

        var settings = new SettingsService(_path).Load();

        Assert.IsFalse(settings.StartCollapsed);
        Assert.AreEqual(420d, settings.SidebarWidth);
        Assert.IsFalse(settings.AlwaysOnTop);
        Assert.IsFalse(settings.AnimationsEnabled);
        Assert.IsFalse(settings.ShowTrayIcon);
        Assert.IsFalse(settings.ShowSearchModule);
        Assert.IsFalse(settings.ShowQuickActionsModule);
    }

    [TestMethod]
    public void SaveThenLoadRoundTripsValues()
    {
        var service = new SettingsService(_path);
        service.Save(new AppSettings
        {
            StartCollapsed = false,
            SidebarWidth = 488,
            AlwaysOnTop = false,
            AnimationsEnabled = false,
            ShowTrayIcon = false,
            ShowFavoritesModule = false,
            ShowMacrosModule = false
        });

        var settings = service.Load();

        Assert.IsFalse(settings.StartCollapsed);
        Assert.AreEqual(488d, settings.SidebarWidth);
        Assert.IsFalse(settings.AlwaysOnTop);
        Assert.IsFalse(settings.AnimationsEnabled);
        Assert.IsFalse(settings.ShowTrayIcon);
        Assert.IsFalse(settings.ShowFavoritesModule);
        Assert.IsFalse(settings.ShowMacrosModule);
    }

    [TestMethod]
    public void InvalidJsonReturnsDefaultsSafely()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_path, "{ definitely-not-json");

        var settings = new SettingsService(_path).Load();

        Assert.IsTrue(settings.StartCollapsed);
        Assert.AreEqual(AppSettings.DefaultSidebarWidth, settings.SidebarWidth);
        Assert.IsTrue(settings.ShowSearchModule);
    }

    [TestMethod]
    [DataRow(250d, 300d)]
    [DataRow(700d, 520d)]
    [DataRow(410d, 410d)]
    public void SidebarWidthClampsToValidRange(double requested, double expected)
    {
        var settings = new AppSettings { SidebarWidth = requested };
        Assert.AreEqual(expected, settings.SidebarWidth);
    }

    [TestMethod]
    public void SettingsVersionDefaultsCorrectly()
    {
        Assert.AreEqual(AppSettings.CurrentVersion, new AppSettings().SettingsVersion);
    }
}
