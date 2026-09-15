using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;
using WpfApplication = System.Windows.Application;

namespace DesktopCommandCenter.App.Services;

public sealed class AppearanceService
{
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
}
