using System.Windows;
using System.Windows.Media;

namespace DesktopCommandCenter.App.Services;

public sealed class AppearanceService
{
    public void ApplyAccent(string? name)
    {
        var palette = name?.Trim().ToLowerInvariant() switch
        {
            "purple" => (
                Accent: Color.FromRgb(142, 93, 255),
                Hover: Color.FromRgb(164, 126, 255),
                Soft: Color.FromArgb(0x33, 142, 93, 255)),
            "green" => (
                Accent: Color.FromRgb(67, 182, 118),
                Hover: Color.FromRgb(91, 204, 140),
                Soft: Color.FromArgb(0x33, 67, 182, 118)),
            "orange" => (
                Accent: Color.FromRgb(242, 144, 64),
                Hover: Color.FromRgb(255, 166, 96),
                Soft: Color.FromArgb(0x33, 242, 144, 64)),
            _ => (
                Accent: Color.FromRgb(76, 141, 255),
                Hover: Color.FromRgb(104, 160, 255),
                Soft: Color.FromArgb(0x33, 76, 141, 255))
        };

        if (Application.Current is null)
        {
            return;
        }

        Application.Current.Resources["Accent"] = new SolidColorBrush(palette.Accent);
        Application.Current.Resources["AccentHover"] = new SolidColorBrush(palette.Hover);
        Application.Current.Resources["AccentSoft"] = new SolidColorBrush(palette.Soft);
    }
}
