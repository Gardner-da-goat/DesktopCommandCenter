namespace DesktopCommandCenter.Core.Settings;

public sealed class AppSettings
{
    public const int CurrentVersion = 8;
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
    public bool AutoCheckForUpdates { get; set; } = true;
    public bool GlobalHotkeysEnabled { get; set; } = true;

    public bool ShowSearchModule { get; set; } = true;
    public bool ShowFavoritesModule { get; set; } = true;
    public bool ShowCurrentWindowModule { get; set; } = true;
    public bool ShowQuickActionsModule { get; set; } = true;
    public bool ShowMacrosModule { get; set; } = true;

    public List<FavoriteAppSetting> FavoriteApps { get; set; } = [];
    public List<MacroSetting> Macros { get; set; } = [];

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

        return this;
    }
}
