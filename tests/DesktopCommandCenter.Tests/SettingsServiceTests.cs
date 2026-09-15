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
        Assert.AreEqual(SidebarEdge.Right, settings.SidebarEdge);
        Assert.IsTrue(settings.AlwaysOnTop);
        Assert.IsTrue(settings.AnimationsEnabled);
        Assert.IsTrue(settings.ShowTrayIcon);
        Assert.IsTrue(settings.AutoCheckForUpdates);
        Assert.IsTrue(settings.GlobalHotkeysEnabled);
        Assert.IsTrue(settings.ShowSearchModule);
        Assert.IsTrue(settings.ShowFavoritesModule);
        Assert.IsTrue(settings.ShowCurrentWindowModule);
        Assert.IsTrue(settings.ShowQuickActionsModule);
        Assert.IsTrue(settings.ShowMacrosModule);
        Assert.AreEqual(0, settings.FavoriteApps.Count);
        Assert.AreEqual(0, settings.Macros.Count);
    }

    [TestMethod]
    public void ValidSettingsLoadCorrectly()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_path, """
            { "settingsVersion": 5, "startCollapsed": false, "sidebarWidth": 420,
              "alwaysOnTop": false, "animationsEnabled": false, "showTrayIcon": false,
              "autoCheckForUpdates": false,
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
        Assert.IsFalse(settings.AutoCheckForUpdates);
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
            SidebarEdge = SidebarEdge.Left,
            AlwaysOnTop = false,
            AnimationsEnabled = false,
            ShowTrayIcon = false,
            AutoCheckForUpdates = false,
            GlobalHotkeysEnabled = false,
            ShowFavoritesModule = false,
            ShowMacrosModule = false,
            FavoriteApps =
            [
                new FavoriteAppSetting { Name = "Example", LaunchPath = @"C:\Example.lnk" }
            ],
            Macros =
            [
                new MacroSetting { Id = "work", Name = "Work", Script = "terminal\ndelay 250" }
            ]
        });

        var settings = service.Load();

        Assert.IsFalse(settings.StartCollapsed);
        Assert.AreEqual(488d, settings.SidebarWidth);
        Assert.AreEqual(SidebarEdge.Left, settings.SidebarEdge);
        Assert.IsFalse(settings.AlwaysOnTop);
        Assert.IsFalse(settings.AnimationsEnabled);
        Assert.IsFalse(settings.ShowTrayIcon);
        Assert.IsFalse(settings.AutoCheckForUpdates);
        Assert.IsFalse(settings.GlobalHotkeysEnabled);
        Assert.IsFalse(settings.ShowFavoritesModule);
        Assert.IsFalse(settings.ShowMacrosModule);
        Assert.AreEqual(1, settings.FavoriteApps.Count);
        Assert.AreEqual("Example", settings.FavoriteApps[0].Name);
        Assert.AreEqual(1, settings.Macros.Count);
        Assert.AreEqual("Work", settings.Macros[0].Name);
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
