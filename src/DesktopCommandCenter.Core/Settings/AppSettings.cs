namespace DesktopCommandCenter.Core.Settings;

public sealed class AppSettings
{
    public const int CurrentVersion = 25;
    public const double MinimumSidebarWidth = 300;
    public const double MaximumSidebarWidth = 520;
    public const double DefaultSidebarWidth = 360;

    private double _sidebarWidth = DefaultSidebarWidth;

    public int SettingsVersion { get; set; } = CurrentVersion;
    public bool StartCollapsed { get; set; } = true;
    public SidebarEdge SidebarEdge { get; set; } = SidebarEdge.Right;
    public SidebarHandlePosition HandlePosition { get; set; } = SidebarHandlePosition.Center;

    public double SidebarWidth
    {
        get => _sidebarWidth;
        set => _sidebarWidth = ClampSidebarWidth(value);
    }

    public bool AlwaysOnTop { get; set; } = true;
    public bool AnimationsEnabled { get; set; } = true;
    public bool ShowTrayIcon { get; set; } = true;
    public bool StartWithWindows { get; set; } = false;
    public bool AutoCheckForUpdates { get; set; } = true;
    public bool NotificationsEnabled { get; set; } = true;
    public string UpdateChannel { get; set; } = "Stable";
    public bool GlobalHotkeysEnabled { get; set; } = true;
    public bool WindowControlHotkeysEnabled { get; set; } = false;
    public string ToggleHotkeyPreset { get; set; } = "Ctrl+Space";
    public string SearchHotkeyPreset { get; set; } = "Ctrl+Shift+Space";
    public string SnapLeftHotkey { get; set; } = "Ctrl+Alt+Left";
    public string SnapRightHotkey { get; set; } = "Ctrl+Alt+Right";
    public string ToggleTopmostHotkey { get; set; } = "Ctrl+Alt+T";
    public string OpacityUpHotkey { get; set; } = "Ctrl+Alt+Up";
    public string OpacityDownHotkey { get; set; } = "Ctrl+Alt+Down";
    public bool ReflowWindowsOnSidebar { get; set; } = true;

    // Ambience
    public bool AmbienceEnabled { get; set; } = false;
    public string AmbienceMode { get; set; } = "Aquarium";
    public int AmbiencePopulation { get; set; } = 10;
    public bool AmbienceBreedingEnabled { get; set; } = false;
    public int AmbienceMaxPopulation { get; set; } = 24;
    public double AmbienceSpeed { get; set; } = 1;
    public double AmbienceOpacity { get; set; } = 0.78;
    public bool AmbienceAllMonitors { get; set; } = true;
    public bool AmbienceOverApps { get; set; } = false;

    public bool AmbienceBackgroundEnabled { get; set; } = true;
    public bool AmbienceCreaturesEnabled { get; set; } = true;
    public bool AmbienceEffectsEnabled { get; set; } = true;
    public string AmbienceBackgroundPreset { get; set; } = "Coral Reef";
    public double AmbienceBackgroundOpacity { get; set; } = 0.58;
    public double AmbienceBackgroundBrightness { get; set; } = 1;
    public double AmbienceBackgroundMotion { get; set; } = 0.35;
    public double AmbienceCreatureSize { get; set; } = 1;
    public bool AmbienceRandomDirection { get; set; } = true;
    public bool AmbienceSchooling { get; set; } = true;
    public double AmbienceBubbleIntensity { get; set; } = 0.55;
    public double AmbienceParticleIntensity { get; set; } = 0.4;
    public double AmbienceGlowIntensity { get; set; } = 0.45;
    public bool AmbiencePerformanceMode { get; set; } = true;

    public bool SearchAppsEnabled { get; set; } = true;
    public bool SearchWindowsEnabled { get; set; } = true;
    public bool SearchActionsEnabled { get; set; } = true;
    public bool SearchMacrosEnabled { get; set; } = true;
    public bool SearchSettingsEnabled { get; set; } = true;
    public bool SearchFilesEnabled { get; set; } = true;

    public bool ShowDownloadsAction { get; set; } = true;
    public bool ShowTaskManagerAction { get; set; } = true;
    public bool ShowWindowsSettingsAction { get; set; } = true;
    public bool ShowTerminalAction { get; set; } = true;
    public bool ShowScreenshotAction { get; set; } = true;
    public bool ShowMuteAction { get; set; } = true;
    public bool ShowVolumeUpAction { get; set; } = false;
    public bool ShowVolumeDownAction { get; set; } = false;
    public bool ShowClipboardAction { get; set; } = false;
    public bool ShowPlayPauseAction { get; set; } = false;
    public bool ShowDesktopAction { get; set; } = false;
    public bool ShowLockAction { get; set; } = false;

    public string ThemeMode { get; set; } = "Dark";
    public string AccentName { get; set; } = "Blue";

    public bool ShowSearchModule { get; set; } = true;
    public bool ShowFavoritesModule { get; set; } = true;
    public bool ShowCurrentWindowModule { get; set; } = true;
    public bool ShowQuickActionsModule { get; set; } = true;
    public bool ShowMacrosModule { get; set; } = true;
    public bool ShowMediaModule { get; set; } = false;
    public bool ShowRecentModule { get; set; } = false;
    public bool ShowClipboardModule { get; set; } = false;

    public List<FavoriteAppSetting> FavoriteApps { get; set; } = [];
    public List<MacroSetting> Macros { get; set; } = [];
    public List<CustomCommandSetting> CustomCommands { get; set; } = [];

    public static double ClampSidebarWidth(double width)
    {
        if (double.IsNaN(width) || double.IsInfinity(width))
        {
            return DefaultSidebarWidth;
        }

        return Math.Clamp(width, MinimumSidebarWidth, MaximumSidebarWidth);
    }

    public AppSettings Normalize()
    {
        SettingsVersion = CurrentVersion;
        SidebarWidth = SidebarWidth;

        AmbienceMode = AmbienceMode switch
        {
            "Fireflies" => "Fireflies",
            "Desktop Pet" => "Desktop Pet",
            _ => "Aquarium"
        };

        AmbienceBackgroundPreset = AmbienceBackgroundPreset switch
        {
            "Deep Ocean" => "Deep Ocean",
            "Kelp Forest" => "Kelp Forest",
            "Fireflies Night" => "Fireflies Night",
            "Minimal Gradient" => "Minimal Gradient",
            "Space" => "Space",
            _ => "Coral Reef"
        };

        AmbiencePopulation = Math.Clamp(AmbiencePopulation, 2, 30);
        AmbienceMaxPopulation = Math.Clamp(
            Math.Max(AmbienceMaxPopulation, AmbiencePopulation),
            2,
            60);
        AmbienceSpeed = NormalizeDouble(AmbienceSpeed, 0.35, 2.5, 1);
        AmbienceOpacity = NormalizeDouble(AmbienceOpacity, 0.15, 1, 0.78);
        AmbienceBackgroundOpacity = NormalizeDouble(
            AmbienceBackgroundOpacity,
            0,
            1,
            0.58);
        AmbienceBackgroundBrightness = NormalizeDouble(
            AmbienceBackgroundBrightness,
            0.4,
            1.6,
            1);
        AmbienceBackgroundMotion = NormalizeDouble(
            AmbienceBackgroundMotion,
            0,
            1,
            0.35);
        AmbienceCreatureSize = NormalizeDouble(
            AmbienceCreatureSize,
            0.6,
            1.6,
            1);
        AmbienceBubbleIntensity = NormalizeDouble(
            AmbienceBubbleIntensity,
            0,
            1,
            0.55);
        AmbienceParticleIntensity = NormalizeDouble(
            AmbienceParticleIntensity,
            0,
            1,
            0.4);
        AmbienceGlowIntensity = NormalizeDouble(
            AmbienceGlowIntensity,
            0,
            1,
            0.45);

        FavoriteApps ??= [];
        FavoriteApps = FavoriteApps
            .Where(item =>
                !string.IsNullOrWhiteSpace(item.Name) &&
                !string.IsNullOrWhiteSpace(item.LaunchPath))
            .GroupBy(item => item.LaunchPath, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        Macros ??= [];
        foreach (var macro in Macros)
        {
            if (string.IsNullOrWhiteSpace(macro.Id))
            {
                macro.Id = Guid.NewGuid().ToString("N");
            }
        }

        Macros = Macros
            .Where(macro =>
                !string.IsNullOrWhiteSpace(macro.Name) &&
                !string.IsNullOrWhiteSpace(macro.Script))
            .GroupBy(macro => macro.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        CustomCommands ??= [];
        foreach (var command in CustomCommands)
        {
            if (string.IsNullOrWhiteSpace(command.Id))
            {
                command.Id = Guid.NewGuid().ToString("N");
            }
        }

        CustomCommands = CustomCommands
            .Where(command =>
                !string.IsNullOrWhiteSpace(command.Name) &&
                !string.IsNullOrWhiteSpace(command.Target))
            .GroupBy(command => command.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        return this;
    }

    private static double NormalizeDouble(
        double value,
        double minimum,
        double maximum,
        double fallback) =>
        double.IsFinite(value)
            ? Math.Clamp(value, minimum, maximum)
            : fallback;
}
