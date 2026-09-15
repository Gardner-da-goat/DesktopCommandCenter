using System.ComponentModel;
using DesktopCommandCenter.Core.Home;

namespace DesktopCommandCenter.App.ViewModels;

public sealed class HomeViewModel : ObservableObject
{
    private readonly WindowsViewModel _windowsViewModel;

    public HomeViewModel(WindowsViewModel windowsViewModel)
    {
        _windowsViewModel = windowsViewModel;
        _windowsViewModel.PropertyChanged += OnWindowsPropertyChanged;
    }

    public IReadOnlyList<HomeModule> Modules { get; } =
    [
        new("search", "Search", true, 0),
        new("favorites", "Favorites", true, 1),
        new("current-window", "Current Window", true, 2),
        new("quick-actions", "Quick Actions", true, 3),
        new("macros", "Macros", true, 4)
    ];

    public WindowItemViewModel? CurrentWindow => _windowsViewModel.CurrentWindow;
    public bool HasCurrentWindow => _windowsViewModel.HasCurrentWindow;

    private void OnWindowsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(WindowsViewModel.CurrentWindow) or nameof(WindowsViewModel.HasCurrentWindow))
        {
            OnPropertyChanged(nameof(CurrentWindow));
            OnPropertyChanged(nameof(HasCurrentWindow));
        }
    }
}
