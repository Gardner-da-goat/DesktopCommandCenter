namespace DesktopCommandCenter.Core.Settings;

public sealed class AppSettings
{
    public const int CurrentVersion = 18;
    public const double MinimumSidebarWidth = 300;
    public const double MaximumSidebarWidth = 520;
    public const double DefaultSidebarWidth = 360;

    private double _sidebarWidth = DefaultSidebarWidth;

    public int SettingsVersion { get; set; } = CurrentVersion;
    public bool StartCollapsed { get; set; } = true;
    public SidebarEdge SidebarEdge { get; set; } = SidebarEdge.Right;

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
    public bool GlobalHotkeysEnabled { get; set; } = true;
    public string ToggleHotkeyPreset { get; set; } = "Ctrl+Space";
    public string SearchHotkeyPreset { get; set; } = "Ctrl+Shift+Space";
    public bool ReflowWindowsOnSidebar { get; set; } = true;

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
}
