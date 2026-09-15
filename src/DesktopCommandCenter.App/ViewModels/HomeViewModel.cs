using System.ComponentModel;
using DesktopCommandCenter.Core.Home;
using DesktopCommandCenter.Windows.Shell;

namespace DesktopCommandCenter.App.ViewModels;

public sealed class HomeViewModel : ObservableObject
{
    private readonly WindowsViewModel _windowsViewModel;
    private readonly SettingsViewModel _settingsViewModel;

    public HomeViewModel(
        WindowsViewModel windowsViewModel,
        SearchViewModel search,
        ShellActionService shellActions,
        SettingsViewModel settingsViewModel)
    {
        _windowsViewModel = windowsViewModel;
        _settingsViewModel = settingsViewModel;
        Search = search;
        OpenDownloadsCommand = new RelayCommand(() => _ = shellActions.OpenDownloads());
        OpenTaskManagerCommand = new RelayCommand(() => _ = shellActions.OpenTaskManager());
        OpenSettingsCommand = new RelayCommand(() => _ = shellActions.OpenWindowsSettings());
        OpenTerminalCommand = new RelayCommand(() => _ = shellActions.OpenTerminal());
        _windowsViewModel.PropertyChanged += OnWindowsPropertyChanged;
        _settingsViewModel.SettingsChanged += OnSettingsChanged;
    }

    public IReadOnlyList<HomeModule> Modules { get; } =
    [
        new("search", "Search", true, 0),
        new("favorites", "Favorites", true, 1),
        new("current-window", "Current Window", true, 2),
        new("quick-actions", "Quick Actions", true, 3),
        new("macros", "Macros", true, 4)
    ];

    public SearchViewModel Search { get; }
    public FavoritesViewModel Favorites => _settingsViewModel.Favorites;
    public MacrosViewModel Macros => _settingsViewModel.Macros;
    public RelayCommand OpenDownloadsCommand { get; }
    public RelayCommand OpenTaskManagerCommand { get; }
    public RelayCommand OpenSettingsCommand { get; }
    public RelayCommand OpenTerminalCommand { get; }

    public WindowItemViewModel? CurrentWindow => _windowsViewModel.CurrentWindow;
    public bool HasCurrentWindow => _windowsViewModel.HasCurrentWindow;

    public bool ShowSearchModule => _settingsViewModel.ShowSearchModule;
    public bool ShowFavoritesModule => _settingsViewModel.ShowFavoritesModule;
    public bool ShowCurrentWindowModule => _settingsViewModel.ShowCurrentWindowModule;
    public bool ShowQuickActionsModule => _settingsViewModel.ShowQuickActionsModule;
    public bool ShowMacrosModule => _settingsViewModel.ShowMacrosModule;

    private void OnWindowsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(WindowsViewModel.CurrentWindow) or nameof(WindowsViewModel.HasCurrentWindow))
        {
            OnPropertyChanged(nameof(CurrentWindow));
            OnPropertyChanged(nameof(HasCurrentWindow));
        }
    }

    private void OnSettingsChanged(object? sender, SettingChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SettingsViewModel.ShowSearchModule):
                OnPropertyChanged(nameof(ShowSearchModule));
                break;
            case nameof(SettingsViewModel.ShowFavoritesModule):
                OnPropertyChanged(nameof(ShowFavoritesModule));
                break;
            case nameof(SettingsViewModel.ShowCurrentWindowModule):
                OnPropertyChanged(nameof(ShowCurrentWindowModule));
                break;
            case nameof(SettingsViewModel.ShowQuickActionsModule):
                OnPropertyChanged(nameof(ShowQuickActionsModule));
                break;
            case nameof(SettingsViewModel.ShowMacrosModule):
                OnPropertyChanged(nameof(ShowMacrosModule));
                break;
        }
    }
}
