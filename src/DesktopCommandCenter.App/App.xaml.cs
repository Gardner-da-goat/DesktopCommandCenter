using System.Windows;
using DesktopCommandCenter.App.Services;
using DesktopCommandCenter.App.ViewModels;
using DesktopCommandCenter.App.Views;
using DesktopCommandCenter.Core.Settings;
using DesktopCommandCenter.Windows.Apps;
using DesktopCommandCenter.Windows.Monitors;
using DesktopCommandCenter.Windows.Shell;
using DesktopCommandCenter.Windows.Windows;

namespace DesktopCommandCenter.App;

public partial class App : System.Windows.Application
{
    private SidebarWindow? _window;
    private TrayIconService? _trayIcon;
    private SettingsViewModel? _settingsViewModel;
    private WindowsViewModel? _windowsViewModel;
    private bool _isExiting;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settingsService = new SettingsService();
        var settings = settingsService.Load();
        var shellActions = new ShellActionService();
        var appLauncher = new AppLauncherService();
        var updatesViewModel = new UpdatesViewModel(new UpdateService());
        var windowService = new WindowService();

        var executablePath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(executablePath))
        {
            _ = shellActions.EnsureDesktopShortcut(executablePath);
        }

        var favoritesViewModel = new FavoritesViewModel(
            settings,
            settingsService,
            appLauncher);

        _windowsViewModel = new WindowsViewModel(windowService);

        var macrosViewModel = new MacrosViewModel(
            settings,
            settingsService,
            favoritesViewModel,
            _windowsViewModel,
            shellActions);

        _settingsViewModel = new SettingsViewModel(
            settings,
            settingsService,
            favoritesViewModel,
            macrosViewModel,
            updatesViewModel);

        var searchViewModel = new SearchViewModel(
            _windowsViewModel,
            appLauncher,
            shellActions,
            favoritesViewModel,
            macrosViewModel);

        var homeViewModel = new HomeViewModel(
            _windowsViewModel,
            searchViewModel,
            shellActions,
            _settingsViewModel);

        var sidebarViewModel = new SidebarViewModel(
            settings,
            homeViewModel,
            _windowsViewModel,
            _settingsViewModel);

        _window = new SidebarWindow(
            sidebarViewModel,
            new MonitorService(),
            windowService);

        _trayIcon = new TrayIconService(
            open: () => RunOnUi(() =>
            {
                sidebarViewModel.Expand();
                ActivateSidebar();
            }),
            collapse: () => RunOnUi(sidebarViewModel.Collapse),
            settings: () => RunOnUi(() =>
            {
                sidebarViewModel.ShowSettings();
                sidebarViewModel.Expand();
                ActivateSidebar();
            }),
            exit: () => RunOnUi(ExitApplication));

        _trayIcon.SetVisible(settings.ShowTrayIcon);
        _settingsViewModel.SettingsChanged += OnSettingsChanged;
        _settingsViewModel.Updates.RestartRequested += OnUpdateRestartRequested;

        _window.Show();

        if (settings.AutoCheckForUpdates)
        {
            _ = _settingsViewModel.Updates.CheckForUpdatesAsync();
        }
    }

    private void OnSettingsChanged(object? sender, SettingChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.ShowTrayIcon))
        {
            _trayIcon?.SetVisible(_settingsViewModel?.ShowTrayIcon == true);
        }
    }

    private void OnUpdateRestartRequested(object? sender, EventArgs e) => ExitApplication();

    private void ActivateSidebar()
    {
        if (_window is null)
        {
            return;
        }

        if (!_window.IsVisible)
        {
            _window.Show();
        }

        _window.Activate();
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

        _windowsViewModel?.Dispose();
        _trayIcon?.Dispose();
        _trayIcon = null;
        _window?.CloseForExit();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _windowsViewModel?.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
