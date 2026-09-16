using System.Windows;
using DesktopCommandCenter.App.Services;
using DesktopCommandCenter.App.ViewModels;
using DesktopCommandCenter.App.Views;
using DesktopCommandCenter.Core.Settings;
using DesktopCommandCenter.Windows.Apps;
using DesktopCommandCenter.Windows.Files;
using DesktopCommandCenter.Windows.Monitors;
using DesktopCommandCenter.Windows.Shell;
using DesktopCommandCenter.Windows.Windows;

namespace DesktopCommandCenter.App;

public partial class App : System.Windows.Application
{
    private SidebarWindow? _sidebarWindow;
    private MainWindow? _mainWindow;
    private MainViewModel? _mainViewModel;
    private TrayIconService? _trayIcon;
    private SettingsViewModel? _settingsViewModel;
    private WindowsViewModel? _windowsViewModel;
    private bool _isExiting;
    private AppearanceService? _appearanceService;
    private AmbienceService? _ambienceService;
    private AppSettings? _appSettings;
    private ShellActionService? _shellActions;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settingsService = new SettingsService();
        var settings = settingsService.Load();
        _appSettings = settings;
        _ambienceService = new AmbienceService();

        var shellActions = new ShellActionService();
        _shellActions = shellActions;
        var appLauncher = new AppLauncherService();
        var updatesViewModel = new UpdatesViewModel(new UpdateService(), settings);
        var recentViewModel = new RecentActivityViewModel();
        var windowService = new WindowService();
        var fileSearchService = new FileSearchService();
        fileSearchService.StartIndexing();

        _appearanceService = new AppearanceService();
        _appearanceService.Apply(settings.ThemeMode, settings.AccentName);

        var executablePath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(executablePath))
        {
            _ = shellActions.EnsureDesktopShortcut(executablePath);
        }

        var favoritesViewModel = new FavoritesViewModel(
            settings,
            settingsService,
            appLauncher,
            recentViewModel);

        _windowsViewModel = new WindowsViewModel(windowService);

        var commandsViewModel = new CommandsViewModel(
            settings,
            settingsService,
            shellActions,
            recentViewModel);

        var macrosViewModel = new MacrosViewModel(
            settings,
            settingsService,
            favoritesViewModel,
            _windowsViewModel,
            shellActions,
            commandsViewModel,
            recentViewModel);

        _settingsViewModel = new SettingsViewModel(
            settings,
            settingsService,
            favoritesViewModel,
            macrosViewModel,
            commandsViewModel,
            updatesViewModel,
            shellActions);

        var searchViewModel = new SearchViewModel(
            _windowsViewModel,
            appLauncher,
            shellActions,
            favoritesViewModel,
            macrosViewModel,
            commandsViewModel,
            _settingsViewModel,
            fileSearchService);

        var homeViewModel = new HomeViewModel(
            _windowsViewModel,
            searchViewModel,
            shellActions,
            _settingsViewModel,
            recentViewModel);

        var musicViewModel = new MusicViewModel(
            shellActions,
            new PulseDeckIntegrationService());
        var filesViewModel = new FilesViewModel(shellActions);

        _mainViewModel = new MainViewModel(
            homeViewModel,
            musicViewModel,
            filesViewModel,
            _windowsViewModel,
            _settingsViewModel);

        _mainWindow = new MainWindow(_mainViewModel);

        var sidebarViewModel = new SidebarViewModel(
            settings,
            homeViewModel,
            _windowsViewModel,
            _settingsViewModel,
            OpenHub);

        _sidebarWindow = new SidebarWindow(
            sidebarViewModel,
            new MonitorService(),
            windowService);

        _trayIcon = new TrayIconService(
            openHub: () => RunOnUi(() => OpenHub("Home", null)),
            openSidebar: () => RunOnUi(() =>
            {
                sidebarViewModel.Expand();
                ActivateSidebar();
            }),
            collapseSidebar: () => RunOnUi(sidebarViewModel.Collapse),
            settings: () => RunOnUi(() => OpenHub("Settings", "General")),
            exit: () => RunOnUi(ExitApplication));

        _trayIcon.SetVisible(settings.ShowTrayIcon);
        _settingsViewModel.SettingsChanged += OnSettingsChanged;
        _settingsViewModel.Updates.RestartRequested += OnUpdateRestartRequested;

        _mainWindow.Show();
        _sidebarWindow.Show();
        _ambienceService.Apply(settings);

        if (settings.AutoCheckForUpdates)
        {
            _ = CheckForUpdatesAndNotifyAsync();
        }
    }

    private async Task CheckForUpdatesAndNotifyAsync()
    {
        if (_settingsViewModel is null)
        {
            return;
        }

        await _settingsViewModel.Updates.CheckForUpdatesAsync();

        if (_settingsViewModel.NotificationsEnabled &&
            _settingsViewModel.Updates.IsUpdateAvailable)
        {
            _trayIcon?.ShowNotification(
                "Desktop Command Center update",
                $"Version {_settingsViewModel.Updates.LatestVersion} is ready to install.");
        }
    }

    private void OnSettingsChanged(object? sender, SettingChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.ShowTrayIcon))
        {
            _trayIcon?.SetVisible(_settingsViewModel?.ShowTrayIcon == true);
        }
        else if (e.PropertyName == nameof(SettingsViewModel.StartWithWindows) &&
                 _settingsViewModel is not null)
        {
            var executablePath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                _ = _shellActions?.SetStartWithWindows(
                    executablePath,
                    _settingsViewModel.StartWithWindows);
            }
        }
        else if (e.PropertyName is nameof(SettingsViewModel.AccentName)
                                  or nameof(SettingsViewModel.ThemeMode) &&
                 _settingsViewModel is not null)
        {
            _appearanceService?.Apply(
                _settingsViewModel.ThemeMode,
                _settingsViewModel.AccentName);
        }
        else if (e.PropertyName.StartsWith(
                     "Ambience",
                     StringComparison.Ordinal) &&
                 _appSettings is not null)
        {
            _ambienceService?.Apply(_appSettings);
        }
    }

    private void OnUpdateRestartRequested(object? sender, EventArgs e) =>
        ExitApplication();

    private void OpenHub(string section, string? settingsCategory)
    {
        if (_mainWindow is null || _mainViewModel is null)
        {
            return;
        }

        _mainViewModel.NavigateTo(section, settingsCategory);

        if (!_mainWindow.IsVisible)
        {
            _mainWindow.Show();
        }

        if (_mainWindow.WindowState == WindowState.Minimized)
        {
            _mainWindow.WindowState = WindowState.Normal;
        }

        _mainWindow.Activate();
    }

    private void ActivateSidebar()
    {
        if (_sidebarWindow is null)
        {
            return;
        }

        if (!_sidebarWindow.IsVisible)
        {
            _sidebarWindow.Show();
        }

        _sidebarWindow.Activate();
    }

    private void RunOnUi(Action action) => Dispatcher.Invoke(action);

    private void ExitApplication()
    {
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;

        if (_settingsViewModel is not null)
        {
            _settingsViewModel.SettingsChanged -= OnSettingsChanged;
            _settingsViewModel.Updates.RestartRequested -= OnUpdateRestartRequested;
        }

        _ambienceService?.Dispose();
        _ambienceService = null;
        _windowsViewModel?.Dispose();
        _trayIcon?.Dispose();
        _trayIcon = null;
        _sidebarWindow?.CloseForExit();
        _mainWindow?.CloseForExit();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _ambienceService?.Dispose();
        _windowsViewModel?.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
