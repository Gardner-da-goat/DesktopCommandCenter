namespace DesktopCommandCenter.Core.Settings;

public sealed class AppSettings
{
    public const int CurrentVersion = 3;
    public const double MinimumSidebarWidth = 300;
    public const double MaximumSidebarWidth = 520;
    public const double DefaultSidebarWidth = 360;

    private double _sidebarWidth = DefaultSidebarWidth;

    public int SettingsVersion { get; set; } = CurrentVersion;
    public bool StartCollapsed { get; set; } = true;

    public double SidebarWidth
    {
        get => _sidebarWidth;
        set => _sidebarWidth = ClampSidebarWidth(value);
    }

    public bool AlwaysOnTop { get; set; } = true;
    public bool AnimationsEnabled { get; set; } = true;
    public bool ShowTrayIcon { get; set; } = true;

    public bool ShowSearchModule { get; set; } = true;
    public bool ShowFavoritesModule { get; set; } = true;
    public bool ShowCurrentWindowModule { get; set; } = true;
    public bool ShowQuickActionsModule { get; set; } = true;
    public bool ShowMacrosModule { get; set; } = true;

    public List<FavoriteAppSetting> FavoriteApps { get; set; } = [];

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
        return this;
    }
}
