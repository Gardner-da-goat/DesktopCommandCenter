using Microsoft.Win32;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;
using WpfApplication = System.Windows.Application;

namespace DesktopCommandCenter.App.Services;

public sealed class AppearanceService
{
    public void Apply(string? themeMode, string? accentName)
    {
        ApplyTheme(themeMode);
        ApplyAccent(accentName);
    }

    public void ApplyTheme(string? mode)
    {
        var useLight = mode?.Trim().ToLowerInvariant() switch
        {
            "light" => true,
            "system" => IsWindowsLightTheme(),
            _ => false
        };

        if (WpfApplication.Current is null)
        {
            return;
        }

        var resources = WpfApplication.Current.Resources;

        if (useLight)
        {
            resources["WindowBackground"] = Brush("#FFF5F7FA");
            resources["PanelBackground"] = Brush("#FFF8FAFC");
            resources["NavigationBackground"] = Brush("#FFF0F3F7");
            resources["CardBackground"] = Brush("#FFFFFFFF");
            resources["CardHoverBackground"] = Brush("#FFE9EEF5");
            resources["BorderSubtle"] = Brush("#FFD6DCE5");
            resources["TextPrimary"] = Brush("#FF1C222A");
            resources["TextSecondary"] = Brush("#FF4F5966");
            resources["TextMuted"] = Brush("#FF7B8794");
            resources["Danger"] = Brush("#FFD84B52");
            resources["Success"] = Brush("#FF2E9D63");
        }
        else
        {
            resources["WindowBackground"] = Brush("#FF14181D");
            resources["PanelBackground"] = Brush("#FF181C22");
            resources["NavigationBackground"] = Brush("#FF12151A");
            resources["CardBackground"] = Brush("#FF22272F");
            resources["CardHoverBackground"] = Brush("#FF2A3039");
            resources["BorderSubtle"] = Brush("#FF343A44");
            resources["TextPrimary"] = Brush("#FFF1F4F8");
            resources["TextSecondary"] = Brush("#FFB6BEC9");
            resources["TextMuted"] = Brush("#FF7F8996");
            resources["Danger"] = Brush("#FFFF6B72");
            resources["Success"] = Brush("#FF57C785");
        }
    }

    public void ApplyAccent(string? name)
    {
        var palette = name?.Trim().ToLowerInvariant() switch
        {
            "purple" => (
                Accent: MediaColor.FromRgb(142, 93, 255),
                Hover: MediaColor.FromRgb(164, 126, 255),
                Soft: MediaColor.FromArgb(0x33, 142, 93, 255)),
            "green" => (
                Accent: MediaColor.FromRgb(67, 182, 118),
                Hover: MediaColor.FromRgb(91, 204, 140),
                Soft: MediaColor.FromArgb(0x33, 67, 182, 118)),
            "orange" => (
                Accent: MediaColor.FromRgb(242, 144, 64),
                Hover: MediaColor.FromRgb(255, 166, 96),
                Soft: MediaColor.FromArgb(0x33, 242, 144, 64)),
            _ => (
                Accent: MediaColor.FromRgb(76, 141, 255),
                Hover: MediaColor.FromRgb(104, 160, 255),
                Soft: MediaColor.FromArgb(0x33, 76, 141, 255))
        };

        if (WpfApplication.Current is null)
        {
            return;
        }

        WpfApplication.Current.Resources["Accent"] = new SolidColorBrush(palette.Accent);
        WpfApplication.Current.Resources["AccentHover"] = new SolidColorBrush(palette.Hover);
        WpfApplication.Current.Resources["AccentSoft"] = new SolidColorBrush(palette.Soft);
    }

    private static SolidColorBrush Brush(string color) =>
        new((MediaColor)System.Windows.Media.ColorConverter.ConvertFromString(color));

    private static bool IsWindowsLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int intValue && intValue != 0;
        }
        catch
        {
            return false;
        }
    }
}
