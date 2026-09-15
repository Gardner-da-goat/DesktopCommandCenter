using System.Windows;
using DesktopCommandCenter.App.Services;
using DesktopCommandCenter.App.ViewModels;
using DesktopCommandCenter.App.Views;
using DesktopCommandCenter.Core.Settings;
using DesktopCommandCenter.Windows.Monitors;

namespace DesktopCommandCenter.App;

public partial class App : System.Windows.Application
{
    private SidebarWindow? _window;
    private TrayIconService? _trayIcon;
    private SettingsViewModel? _settingsViewModel;
    private bool _isExiting;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settingsService = new SettingsService();
        var settings = settingsService.Load();
        _settingsViewModel = new SettingsViewModel(settings, settingsService);
        var sidebarViewModel = new SidebarViewModel(
            settings,
            new HomeViewModel(),
            _settingsViewModel);

        _window = new SidebarWindow(sidebarViewModel, new MonitorService());
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
        _window.Show();
    }

    private void OnSettingsChanged(object? sender, SettingChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.ShowTrayIcon))
        {
            _trayIcon?.SetVisible(_settingsViewModel?.ShowTrayIcon == true);
        }
    }

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
        _settingsViewModel!.SettingsChanged -= OnSettingsChanged;
        _trayIcon?.Dispose();
        _trayIcon = null;
        _window?.CloseForExit();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}