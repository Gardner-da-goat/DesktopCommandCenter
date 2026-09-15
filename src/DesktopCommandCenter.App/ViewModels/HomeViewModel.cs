using DesktopCommandCenter.Core.Home;

namespace DesktopCommandCenter.App.ViewModels;

public sealed class HomeViewModel
{
    public IReadOnlyList<HomeModule> Modules { get; } =
    [
        new("search", "Search", true, 0),
        new("favorites", "Favorites", true, 1),
        new("current-window", "Current Window", true, 2),
        new("quick-actions", "Quick Actions", true, 3),
        new("macros", "Macros", true, 4)
    ];
}