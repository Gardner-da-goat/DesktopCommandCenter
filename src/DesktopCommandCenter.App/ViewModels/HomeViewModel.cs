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
        OpenScreenshotCommand = new RelayCommand(() => _ = shellActions.OpenScreenshot());
        ToggleMuteCommand = new RelayCommand(() => _ = shellActions.ToggleMute());
        VolumeUpCommand = new RelayCommand(() => _ = shellActions.VolumeUp());
        VolumeDownCommand = new RelayCommand(() => _ = shellActions.VolumeDown());
        OpenClipboardCommand = new RelayCommand(() => _ = shellActions.OpenClipboardHistory());
        PlayPauseCommand = new RelayCommand(() => _ = shellActions.PlayPause());
        NextTrackCommand = new RelayCommand(() => _ = shellActions.NextTrack());
        PreviousTrackCommand = new RelayCommand(() => _ = shellActions.PreviousTrack());
        ShowDesktopCommand = new RelayCommand(() => _ = shellActions.ShowDesktop());
        LockComputerCommand = new RelayCommand(() => _ = shellActions.LockComputer());
        _windowsViewModel.PropertyChanged += OnWindowsPropertyChanged;
        _settingsViewModel.SettingsChanged += OnSettingsChanged;
    }

    public IReadOnlyList<HomeModule> Modules { get; } =
    [
        new("search", "Search", true, 0),
        new("favorites", "Favorites", true, 1),
        new("current-window", "Current Window", true, 2),
        new("quick-actions", "Quick Actions", true, 3),
        new("macros", "Macros", true, 4),
        new("media", "Media", false, 5)
    ];

    public SearchViewModel Search { get; }
    public FavoritesViewModel Favorites => _settingsViewModel.Favorites;
    public MacrosViewModel Macros => _settingsViewModel.Macros;
    public RelayCommand OpenDownloadsCommand { get; }
    public RelayCommand OpenTaskManagerCommand { get; }
    public RelayCommand OpenSettingsCommand { get; }
    public RelayCommand OpenTerminalCommand { get; }
    public RelayCommand OpenScreenshotCommand { get; }
    public RelayCommand ToggleMuteCommand { get; }
    public RelayCommand VolumeUpCommand { get; }
    public RelayCommand VolumeDownCommand { get; }
    public RelayCommand OpenClipboardCommand { get; }
    public RelayCommand PlayPauseCommand { get; }
    public RelayCommand NextTrackCommand { get; }
    public RelayCommand PreviousTrackCommand { get; }
    public RelayCommand ShowDesktopCommand { get; }
    public RelayCommand LockComputerCommand { get; }

    public WindowItemViewModel? CurrentWindow => _windowsViewModel.CurrentWindow;
    public bool HasCurrentWindow => _windowsViewModel.HasCurrentWindow;

    public bool ShowSearchModule => _settingsViewModel.ShowSearchModule;
    public bool ShowFavoritesModule => _settingsViewModel.ShowFavoritesModule;
    public bool ShowCurrentWindowModule => _settingsViewModel.ShowCurrentWindowModule;
    public bool ShowQuickActionsModule => _settingsViewModel.ShowQuickActionsModule;
    public bool ShowMacrosModule => _settingsViewModel.ShowMacrosModule;
    public bool ShowMediaModule => _settingsViewModel.ShowMediaModule;

    public bool ShowDownloadsAction => _settingsViewModel.ShowDownloadsAction;
    public bool ShowTaskManagerAction => _settingsViewModel.ShowTaskManagerAction;
    public bool ShowWindowsSettingsAction => _settingsViewModel.ShowWindowsSettingsAction;
    public bool ShowTerminalAction => _settingsViewModel.ShowTerminalAction;
    public bool ShowScreenshotAction => _settingsViewModel.ShowScreenshotAction;
    public bool ShowMuteAction => _settingsViewModel.ShowMuteAction;
    public bool ShowVolumeUpAction => _settingsViewModel.ShowVolumeUpAction;
    public bool ShowVolumeDownAction => _settingsViewModel.ShowVolumeDownAction;
    public bool ShowClipboardAction => _settingsViewModel.ShowClipboardAction;
    public bool ShowPlayPauseAction => _settingsViewModel.ShowPlayPauseAction;
    public bool ShowDesktopAction => _settingsViewModel.ShowDesktopAction;
    public bool ShowLockAction => _settingsViewModel.ShowLockAction;

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
            case nameof(SettingsViewModel.ShowMediaModule):
                OnPropertyChanged(nameof(ShowMediaModule));
                break;
            case nameof(SettingsViewModel.ShowDownloadsAction):
                OnPropertyChanged(nameof(ShowDownloadsAction));
                break;
            case nameof(SettingsViewModel.ShowTaskManagerAction):
                OnPropertyChanged(nameof(ShowTaskManagerAction));
                break;
            case nameof(SettingsViewModel.ShowWindowsSettingsAction):
                OnPropertyChanged(nameof(ShowWindowsSettingsAction));
                break;
            case nameof(SettingsViewModel.ShowTerminalAction):
                OnPropertyChanged(nameof(ShowTerminalAction));
                break;
            case nameof(SettingsViewModel.ShowScreenshotAction):
                OnPropertyChanged(nameof(ShowScreenshotAction));
                break;
            case nameof(SettingsViewModel.ShowMuteAction):
                OnPropertyChanged(nameof(ShowMuteAction));
                break;
            case nameof(SettingsViewModel.ShowVolumeUpAction):
                OnPropertyChanged(nameof(ShowVolumeUpAction));
                break;
            case nameof(SettingsViewModel.ShowVolumeDownAction):
                OnPropertyChanged(nameof(ShowVolumeDownAction));
                break;
            case nameof(SettingsViewModel.ShowClipboardAction):
                OnPropertyChanged(nameof(ShowClipboardAction));
                break;
            case nameof(SettingsViewModel.ShowPlayPauseAction):
                OnPropertyChanged(nameof(ShowPlayPauseAction));
                break;
            case nameof(SettingsViewModel.ShowDesktopAction):
                OnPropertyChanged(nameof(ShowDesktopAction));
                break;
            case nameof(SettingsViewModel.ShowLockAction):
                OnPropertyChanged(nameof(ShowLockAction));
                break;
        }
    }
}
