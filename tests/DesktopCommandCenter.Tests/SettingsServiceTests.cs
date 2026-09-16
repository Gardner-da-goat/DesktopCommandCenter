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
        Assert.AreEqual(SidebarHandlePosition.Center, settings.HandlePosition);
        Assert.IsTrue(settings.AlwaysOnTop);
        Assert.IsTrue(settings.AnimationsEnabled);
        Assert.IsTrue(settings.ShowTrayIcon);
        Assert.IsFalse(settings.StartWithWindows);
        Assert.IsTrue(settings.AutoCheckForUpdates);
        Assert.IsTrue(settings.NotificationsEnabled);
        Assert.AreEqual("Stable", settings.UpdateChannel);
        Assert.IsTrue(settings.GlobalHotkeysEnabled);
        Assert.IsFalse(settings.WindowControlHotkeysEnabled);
        Assert.AreEqual("Ctrl+Space", settings.ToggleHotkeyPreset);
        Assert.AreEqual("Ctrl+Shift+Space", settings.SearchHotkeyPreset);
        Assert.AreEqual("Ctrl+Alt+Left", settings.SnapLeftHotkey);
        Assert.AreEqual("Ctrl+Alt+Right", settings.SnapRightHotkey);
        Assert.AreEqual("Ctrl+Alt+T", settings.ToggleTopmostHotkey);
        Assert.AreEqual("Ctrl+Alt+Up", settings.OpacityUpHotkey);
        Assert.AreEqual("Ctrl+Alt+Down", settings.OpacityDownHotkey);
        Assert.IsTrue(settings.ReflowWindowsOnSidebar);
        Assert.IsFalse(settings.AmbienceEnabled);
        Assert.AreEqual("Aquarium", settings.AmbienceMode);
        Assert.AreEqual(10, settings.AmbiencePopulation);
        Assert.IsFalse(settings.AmbienceBreedingEnabled);
        Assert.AreEqual(24, settings.AmbienceMaxPopulation);
        Assert.AreEqual(1d, settings.AmbienceSpeed);
        Assert.AreEqual(0.78d, settings.AmbienceOpacity);
        Assert.IsTrue(settings.AmbienceAllMonitors);
        Assert.IsFalse(settings.AmbienceOverApps);
        Assert.IsTrue(settings.AmbienceBackgroundEnabled);
        Assert.IsTrue(settings.AmbienceCreaturesEnabled);
        Assert.IsTrue(settings.AmbienceEffectsEnabled);
        Assert.AreEqual("Coral Reef", settings.AmbienceBackgroundPreset);
        Assert.AreEqual(0.58d, settings.AmbienceBackgroundOpacity);
        Assert.AreEqual(1d, settings.AmbienceBackgroundBrightness);
        Assert.AreEqual(0.35d, settings.AmbienceBackgroundMotion);
        Assert.AreEqual(1d, settings.AmbienceCreatureSize);
        Assert.IsTrue(settings.AmbienceRandomDirection);
        Assert.IsTrue(settings.AmbienceSchooling);
        Assert.AreEqual(0.55d, settings.AmbienceBubbleIntensity);
        Assert.AreEqual(0.4d, settings.AmbienceParticleIntensity);
        Assert.AreEqual(0.45d, settings.AmbienceGlowIntensity);
        Assert.IsTrue(settings.AmbiencePerformanceMode);
        Assert.IsTrue(settings.SearchAppsEnabled);
        Assert.IsTrue(settings.SearchWindowsEnabled);
        Assert.IsTrue(settings.SearchActionsEnabled);
        Assert.IsTrue(settings.SearchMacrosEnabled);
        Assert.IsTrue(settings.SearchSettingsEnabled);
        Assert.IsTrue(settings.SearchFilesEnabled);
        Assert.IsTrue(settings.ShowScreenshotAction);
        Assert.IsTrue(settings.ShowMuteAction);
        Assert.IsFalse(settings.ShowVolumeUpAction);
        Assert.IsFalse(settings.ShowVolumeDownAction);
        Assert.IsFalse(settings.ShowClipboardAction);
        Assert.IsFalse(settings.ShowPlayPauseAction);
        Assert.IsFalse(settings.ShowDesktopAction);
        Assert.IsFalse(settings.ShowLockAction);
        Assert.AreEqual("Dark", settings.ThemeMode);
        Assert.AreEqual("Blue", settings.AccentName);
        Assert.IsTrue(settings.ShowSearchModule);
        Assert.IsTrue(settings.ShowFavoritesModule);
        Assert.IsTrue(settings.ShowCurrentWindowModule);
        Assert.IsTrue(settings.ShowQuickActionsModule);
        Assert.IsTrue(settings.ShowMacrosModule);
        Assert.IsFalse(settings.ShowMediaModule);
        Assert.IsFalse(settings.ShowRecentModule);
        Assert.IsFalse(settings.ShowClipboardModule);
        Assert.AreEqual(0, settings.FavoriteApps.Count);
        Assert.AreEqual(0, settings.Macros.Count);
        Assert.AreEqual(0, settings.CustomCommands.Count);
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
        Assert.IsFalse(settings.StartWithWindows);
        Assert.IsFalse(settings.AutoCheckForUpdates);
        Assert.IsTrue(settings.NotificationsEnabled);
        Assert.AreEqual("Stable", settings.UpdateChannel);
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
            HandlePosition = SidebarHandlePosition.Bottom,
            AlwaysOnTop = false,
            AnimationsEnabled = false,
            ShowTrayIcon = false,
            StartWithWindows = true,
            AutoCheckForUpdates = false,
            NotificationsEnabled = false,
            UpdateChannel = "Beta",
            GlobalHotkeysEnabled = false,
            WindowControlHotkeysEnabled = true,
            ToggleHotkeyPreset = "Win+Shift+D",
            SearchHotkeyPreset = "Alt+F8",
            SnapLeftHotkey = "Ctrl+Shift+Left",
            SnapRightHotkey = "Ctrl+Shift+Right",
            ToggleTopmostHotkey = "Ctrl+Shift+T",
            OpacityUpHotkey = "Alt+PageUp",
            OpacityDownHotkey = "Alt+PageDown",
            ReflowWindowsOnSidebar = false,
            AmbienceEnabled = true,
            AmbienceMode = "Fireflies",
            AmbiencePopulation = 18,
            AmbienceBreedingEnabled = false,
            AmbienceMaxPopulation = 36,
            AmbienceSpeed = 1.6,
            AmbienceOpacity = 0.55,
            AmbienceAllMonitors = false,
            AmbienceOverApps = true,
            AmbienceBackgroundEnabled = true,
            AmbienceCreaturesEnabled = false,
            AmbienceEffectsEnabled = true,
            AmbienceBackgroundPreset = "Kelp Forest",
            AmbienceBackgroundOpacity = 0.44,
            AmbienceBackgroundBrightness = 1.2,
            AmbienceBackgroundMotion = 0.7,
            AmbienceCreatureSize = 1.25,
            AmbienceRandomDirection = false,
            AmbienceSchooling = false,
            AmbienceBubbleIntensity = 0.8,
            AmbienceParticleIntensity = 0.2,
            AmbienceGlowIntensity = 0.65,
            AmbiencePerformanceMode = false,
            SearchAppsEnabled = false,
            SearchSettingsEnabled = false,
            SearchFilesEnabled = false,
            ShowScreenshotAction = false,
            ShowVolumeUpAction = true,
            ShowDesktopAction = true,
            ThemeMode = "Light",
            AccentName = "Purple",
            ShowFavoritesModule = false,
            ShowMacrosModule = false,
            ShowMediaModule = true,
            ShowRecentModule = true,
            ShowClipboardModule = true,
            FavoriteApps =
            [
                new FavoriteAppSetting { Name = "Example", LaunchPath = @"C:\Example.lnk" }
            ],
            Macros =
            [
                new MacroSetting { Id = "work", Name = "Work", Script = "terminal\ndelay 250" }
            ],
            CustomCommands =
            [
                new CustomCommandSetting { Id = "docs", Name = "Docs", Target = "https://example.com" }
            ]
        });

        var settings = service.Load();

        Assert.IsFalse(settings.StartCollapsed);
        Assert.AreEqual(488d, settings.SidebarWidth);
        Assert.AreEqual(SidebarEdge.Left, settings.SidebarEdge);
        Assert.AreEqual(SidebarHandlePosition.Bottom, settings.HandlePosition);
        Assert.IsFalse(settings.AlwaysOnTop);
        Assert.IsFalse(settings.AnimationsEnabled);
        Assert.IsFalse(settings.ShowTrayIcon);
        Assert.IsTrue(settings.StartWithWindows);
        Assert.IsFalse(settings.AutoCheckForUpdates);
        Assert.IsFalse(settings.GlobalHotkeysEnabled);
        Assert.IsTrue(settings.WindowControlHotkeysEnabled);
        Assert.AreEqual("Win+Shift+D", settings.ToggleHotkeyPreset);
        Assert.AreEqual("Alt+F8", settings.SearchHotkeyPreset);
        Assert.AreEqual("Ctrl+Shift+Left", settings.SnapLeftHotkey);
        Assert.AreEqual("Ctrl+Shift+Right", settings.SnapRightHotkey);
        Assert.AreEqual("Ctrl+Shift+T", settings.ToggleTopmostHotkey);
        Assert.AreEqual("Alt+PageUp", settings.OpacityUpHotkey);
        Assert.AreEqual("Alt+PageDown", settings.OpacityDownHotkey);
        Assert.IsFalse(settings.ReflowWindowsOnSidebar);
        Assert.IsTrue(settings.AmbienceEnabled);
        Assert.AreEqual("Fireflies", settings.AmbienceMode);
        Assert.AreEqual(18, settings.AmbiencePopulation);
        Assert.IsFalse(settings.AmbienceBreedingEnabled);
        Assert.AreEqual(36, settings.AmbienceMaxPopulation);
        Assert.AreEqual(1.6d, settings.AmbienceSpeed);
        Assert.AreEqual(0.55d, settings.AmbienceOpacity);
        Assert.IsFalse(settings.AmbienceAllMonitors);
        Assert.IsTrue(settings.AmbienceOverApps);
        Assert.IsTrue(settings.AmbienceBackgroundEnabled);
        Assert.IsFalse(settings.AmbienceCreaturesEnabled);
        Assert.IsTrue(settings.AmbienceEffectsEnabled);
        Assert.AreEqual("Kelp Forest", settings.AmbienceBackgroundPreset);
        Assert.AreEqual(0.44d, settings.AmbienceBackgroundOpacity);
        Assert.AreEqual(1.2d, settings.AmbienceBackgroundBrightness);
        Assert.AreEqual(0.7d, settings.AmbienceBackgroundMotion);
        Assert.AreEqual(1.25d, settings.AmbienceCreatureSize);
        Assert.IsFalse(settings.AmbienceRandomDirection);
        Assert.IsFalse(settings.AmbienceSchooling);
        Assert.AreEqual(0.8d, settings.AmbienceBubbleIntensity);
        Assert.AreEqual(0.2d, settings.AmbienceParticleIntensity);
        Assert.AreEqual(0.65d, settings.AmbienceGlowIntensity);
        Assert.IsFalse(settings.AmbiencePerformanceMode);
        Assert.IsFalse(settings.SearchAppsEnabled);
        Assert.IsFalse(settings.SearchSettingsEnabled);
        Assert.IsFalse(settings.SearchFilesEnabled);
        Assert.IsFalse(settings.ShowScreenshotAction);
        Assert.IsTrue(settings.ShowVolumeUpAction);
        Assert.IsTrue(settings.ShowDesktopAction);
        Assert.AreEqual("Light", settings.ThemeMode);
        Assert.AreEqual("Purple", settings.AccentName);
        Assert.IsFalse(settings.ShowFavoritesModule);
        Assert.IsFalse(settings.ShowMacrosModule);
        Assert.IsTrue(settings.ShowMediaModule);
        Assert.IsTrue(settings.ShowRecentModule);
        Assert.IsTrue(settings.ShowClipboardModule);
        Assert.AreEqual(1, settings.FavoriteApps.Count);
        Assert.AreEqual("Example", settings.FavoriteApps[0].Name);
        Assert.AreEqual(1, settings.Macros.Count);
        Assert.AreEqual("Work", settings.Macros[0].Name);
        Assert.AreEqual(1, settings.CustomCommands.Count);
        Assert.AreEqual("Docs", settings.CustomCommands[0].Name);
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
    public void AmbienceSettingsNormalizeSafely()
    {
        var settings = new AppSettings
        {
            AmbienceMode = "unknown",
            AmbienceBackgroundPreset = "unknown",
            AmbiencePopulation = 99,
            AmbienceMaxPopulation = 3,
            AmbienceSpeed = 99,
            AmbienceOpacity = -2
        }.Normalize();

        Assert.AreEqual("Aquarium", settings.AmbienceMode);
        Assert.AreEqual("Coral Reef", settings.AmbienceBackgroundPreset);
        Assert.AreEqual(30, settings.AmbiencePopulation);
        Assert.AreEqual(30, settings.AmbienceMaxPopulation);
        Assert.AreEqual(2.5d, settings.AmbienceSpeed);
        Assert.AreEqual(0.15d, settings.AmbienceOpacity);
    }

    [TestMethod]
    public void SettingsVersionDefaultsCorrectly()
    {
        Assert.AreEqual(AppSettings.CurrentVersion, new AppSettings().SettingsVersion);
    }
}
