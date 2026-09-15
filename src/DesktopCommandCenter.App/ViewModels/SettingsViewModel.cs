using System.IO;
using System.Collections.ObjectModel;
using DesktopCommandCenter.Core.Settings;
using DesktopCommandCenter.Windows.Shell;

namespace DesktopCommandCenter.App.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly AppSettings _settings;
    private readonly ISettingsService _settingsService;
    private SettingsCategory _selectedCategory;

    public SettingsViewModel(
        AppSettings settings,
        ISettingsService settingsService,
        FavoritesViewModel favorites,
        MacrosViewModel macros,
        CommandsViewModel commands,
        UpdatesViewModel updates,
        ShellActionService shellActions)
    {
        _settings = settings;
        _settingsService = settingsService;
        Favorites = favorites;
        Macros = macros;
        Commands = commands;
        Updates = updates;
        OpenSettingsFolderCommand = new RelayCommand(() =>
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DesktopCommandCenter");
            Directory.CreateDirectory(folder);
            _ = shellActions.OpenPath(folder);
        });
        DecreaseSidebarWidthCommand = new RelayCommand(() => SidebarWidth -= 20);
        IncreaseSidebarWidthCommand = new RelayCommand(() => SidebarWidth += 20);
        Width320Command = new RelayCommand(() => SidebarWidth = 320);
        Width360Command = new RelayCommand(() => SidebarWidth = 360);
        Width420Command = new RelayCommand(() => SidebarWidth = 420);
        Width480Command = new RelayCommand(() => SidebarWidth = 480);
        ThemeDarkCommand = new RelayCommand(() => ThemeMode = "Dark");
        ThemeLightCommand = new RelayCommand(() => ThemeMode = "Light");
        ThemeSystemCommand = new RelayCommand(() => ThemeMode = "System");
        AccentBlueCommand = new RelayCommand(() => AccentName = "Blue");
        AccentPurpleCommand = new RelayCommand(() => AccentName = "Purple");
        AccentGreenCommand = new RelayCommand(() => AccentName = "Green");
        AccentOrangeCommand = new RelayCommand(() => AccentName = "Orange");
        ChangeToggleHotkeyCommand = new RelayCommand(CycleToggleHotkey);
        ChangeSearchHotkeyCommand = new RelayCommand(CycleSearchHotkey);
        Categories = new ObservableCollection<SettingsCategory>
        {
            new("General", "Startup, sidebar, and application behavior."),
            new("Customize Home", "Choose and arrange the modules on Home."),
            new("Favorites", "Manage pinned apps, folders, and commands."),
            new("Quick Actions", "Choose the actions shown on Home."),
            new("Hotkeys", "Configure shortcuts for common actions."),
            new("Macros", "Build multi-step desktop routines."),
            new("Commands", "Manage searchable commands."),
            new("Search", "Choose search sources and behavior."),
            new("Windows", "Configure window controls and defaults."),
            new("Appearance", "Theme, accent, transparency, and motion."),
            new("Updates", "Control app updates and release channel."),
            new("Advanced", "Diagnostics and advanced maintenance tools.")
        };
        _selectedCategory = Categories[0];
    }

    public event EventHandler<SettingChangedEventArgs>? SettingsChanged;
    public ObservableCollection<SettingsCategory> Categories { get; }
    public FavoritesViewModel Favorites { get; }
    public MacrosViewModel Macros { get; }
    public CommandsViewModel Commands { get; }
    public UpdatesViewModel Updates { get; }
    public RelayCommand OpenSettingsFolderCommand { get; }
    public RelayCommand DecreaseSidebarWidthCommand { get; }
    public RelayCommand IncreaseSidebarWidthCommand { get; }
    public RelayCommand Width320Command { get; }
    public RelayCommand Width360Command { get; }
    public RelayCommand Width420Command { get; }
    public RelayCommand Width480Command { get; }
    public RelayCommand ThemeDarkCommand { get; }
    public RelayCommand ThemeLightCommand { get; }
    public RelayCommand ThemeSystemCommand { get; }
    public RelayCommand AccentBlueCommand { get; }
    public RelayCommand AccentPurpleCommand { get; }
    public RelayCommand AccentGreenCommand { get; }
    public RelayCommand AccentOrangeCommand { get; }
    public RelayCommand ChangeToggleHotkeyCommand { get; }
    public RelayCommand ChangeSearchHotkeyCommand { get; }

    public SettingsCategory SelectedCategory
    {
        get => _selectedCategory;
        set => SetProperty(ref _selectedCategory, value);
    }

    public bool StartCollapsed
    {
        get => _settings.StartCollapsed;
        set
        {
            if (_settings.StartCollapsed == value) return;
            _settings.StartCollapsed = value;
            SaveAndNotify();
            OnPropertyChanged();
        }
    }

    public double SidebarWidth
    {
        get => _settings.SidebarWidth;
        set
        {
            var clamped = AppSettings.ClampSidebarWidth(value);
            if (Math.Abs(_settings.SidebarWidth - clamped) < 0.1) return;
            _settings.SidebarWidth = clamped;
            SaveAndNotify();
            OnPropertyChanged();
        }
    }

    public SidebarEdge SidebarEdge
    {
        get => _settings.SidebarEdge;
        set
        {
            if (_settings.SidebarEdge == value) return;
            _settings.SidebarEdge = value;
            SaveAndNotify();
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSidebarOnLeft));
            OnPropertyChanged(nameof(IsSidebarOnRight));
        }
    }

    public bool IsSidebarOnLeft
    {
        get => SidebarEdge == SidebarEdge.Left;
        set
        {
            if (value)
            {
                SidebarEdge = SidebarEdge.Left;
            }
        }
    }

    public bool IsSidebarOnRight
    {
        get => SidebarEdge == SidebarEdge.Right;
        set
        {
            if (value)
            {
                SidebarEdge = SidebarEdge.Right;
            }
        }
    }

    public SidebarHandlePosition HandlePosition
    {
        get => _settings.HandlePosition;
        set
        {
            if (_settings.HandlePosition == value) return;
            _settings.HandlePosition = value;
            SaveAndNotify();
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsHandleAtTop));
            OnPropertyChanged(nameof(IsHandleAtCenter));
            OnPropertyChanged(nameof(IsHandleAtBottom));
        }
    }

    public bool IsHandleAtTop
    {
        get => HandlePosition == SidebarHandlePosition.Top;
        set { if (value) HandlePosition = SidebarHandlePosition.Top; }
    }

    public bool IsHandleAtCenter
    {
        get => HandlePosition == SidebarHandlePosition.Center;
        set { if (value) HandlePosition = SidebarHandlePosition.Center; }
    }

    public bool IsHandleAtBottom
    {
        get => HandlePosition == SidebarHandlePosition.Bottom;
        set { if (value) HandlePosition = SidebarHandlePosition.Bottom; }
    }

    public bool AlwaysOnTop
    {
        get => _settings.AlwaysOnTop;
        set => SetBoolean(value, () => _settings.AlwaysOnTop, v => _settings.AlwaysOnTop = v);
    }

    public bool AnimationsEnabled
    {
        get => _settings.AnimationsEnabled;
        set => SetBoolean(value, () => _settings.AnimationsEnabled, v => _settings.AnimationsEnabled = v);
    }

    public bool ShowTrayIcon
    {
        get => _settings.ShowTrayIcon;
        set => SetBoolean(value, () => _settings.ShowTrayIcon, v => _settings.ShowTrayIcon = v);
    }

    public bool StartWithWindows
    {
        get => _settings.StartWithWindows;
        set => SetBoolean(value, () => _settings.StartWithWindows, v => _settings.StartWithWindows = v);
    }

    public bool AutoCheckForUpdates
    {
        get => _settings.AutoCheckForUpdates;
        set => SetBoolean(value, () => _settings.AutoCheckForUpdates, v => _settings.AutoCheckForUpdates = v);
    }

    public bool GlobalHotkeysEnabled
    {
        get => _settings.GlobalHotkeysEnabled;
        set => SetBoolean(value, () => _settings.GlobalHotkeysEnabled, v => _settings.GlobalHotkeysEnabled = v);
    }

    public bool WindowControlHotkeysEnabled
    {
        get => _settings.WindowControlHotkeysEnabled;
        set => SetBoolean(
            value,
            () => _settings.WindowControlHotkeysEnabled,
            v => _settings.WindowControlHotkeysEnabled = v);
    }

    public string ToggleHotkeyPreset
    {
        get => NormalizeToggleHotkey(_settings.ToggleHotkeyPreset);
        set
        {
            var normalized = NormalizeToggleHotkey(value);
            if (string.Equals(_settings.ToggleHotkeyPreset, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _settings.ToggleHotkeyPreset = normalized;
            SaveAndNotify();
            OnPropertyChanged();
        }
    }

    public string SearchHotkeyPreset
    {
        get => NormalizeSearchHotkey(_settings.SearchHotkeyPreset);
        set
        {
            var normalized = NormalizeSearchHotkey(value);
            if (string.Equals(_settings.SearchHotkeyPreset, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _settings.SearchHotkeyPreset = normalized;
            SaveAndNotify();
            OnPropertyChanged();
        }
    }

    public bool ReflowWindowsOnSidebar
    {
        get => _settings.ReflowWindowsOnSidebar;
        set => SetBoolean(value, () => _settings.ReflowWindowsOnSidebar, v => _settings.ReflowWindowsOnSidebar = v);
    }

    public bool SearchAppsEnabled
    {
        get => _settings.SearchAppsEnabled;
        set => SetBoolean(value, () => _settings.SearchAppsEnabled, v => _settings.SearchAppsEnabled = v);
    }

    public bool SearchWindowsEnabled
    {
        get => _settings.SearchWindowsEnabled;
        set => SetBoolean(value, () => _settings.SearchWindowsEnabled, v => _settings.SearchWindowsEnabled = v);
    }

    public bool SearchActionsEnabled
    {
        get => _settings.SearchActionsEnabled;
        set => SetBoolean(value, () => _settings.SearchActionsEnabled, v => _settings.SearchActionsEnabled = v);
    }

    public bool SearchMacrosEnabled
    {
        get => _settings.SearchMacrosEnabled;
        set => SetBoolean(value, () => _settings.SearchMacrosEnabled, v => _settings.SearchMacrosEnabled = v);
    }

    public bool SearchSettingsEnabled
    {
        get => _settings.SearchSettingsEnabled;
        set => SetBoolean(value, () => _settings.SearchSettingsEnabled, v => _settings.SearchSettingsEnabled = v);
    }

    public bool SearchFilesEnabled
    {
        get => _settings.SearchFilesEnabled;
        set => SetBoolean(value, () => _settings.SearchFilesEnabled, v => _settings.SearchFilesEnabled = v);
    }

    public bool ShowDownloadsAction
    {
        get => _settings.ShowDownloadsAction;
        set => SetBoolean(value, () => _settings.ShowDownloadsAction, v => _settings.ShowDownloadsAction = v);
    }

    public bool ShowTaskManagerAction
    {
        get => _settings.ShowTaskManagerAction;
        set => SetBoolean(value, () => _settings.ShowTaskManagerAction, v => _settings.ShowTaskManagerAction = v);
    }

    public bool ShowWindowsSettingsAction
    {
        get => _settings.ShowWindowsSettingsAction;
        set => SetBoolean(value, () => _settings.ShowWindowsSettingsAction, v => _settings.ShowWindowsSettingsAction = v);
    }

    public bool ShowTerminalAction
    {
        get => _settings.ShowTerminalAction;
        set => SetBoolean(value, () => _settings.ShowTerminalAction, v => _settings.ShowTerminalAction = v);
    }

    public bool ShowScreenshotAction
    {
        get => _settings.ShowScreenshotAction;
        set => SetBoolean(value, () => _settings.ShowScreenshotAction, v => _settings.ShowScreenshotAction = v);
    }

    public bool ShowMuteAction
    {
        get => _settings.ShowMuteAction;
        set => SetBoolean(value, () => _settings.ShowMuteAction, v => _settings.ShowMuteAction = v);
    }

    public bool ShowVolumeUpAction
    {
        get => _settings.ShowVolumeUpAction;
        set => SetBoolean(value, () => _settings.ShowVolumeUpAction, v => _settings.ShowVolumeUpAction = v);
    }

    public bool ShowVolumeDownAction
    {
        get => _settings.ShowVolumeDownAction;
        set => SetBoolean(value, () => _settings.ShowVolumeDownAction, v => _settings.ShowVolumeDownAction = v);
    }

    public bool ShowClipboardAction
    {
        get => _settings.ShowClipboardAction;
        set => SetBoolean(value, () => _settings.ShowClipboardAction, v => _settings.ShowClipboardAction = v);
    }

    public bool ShowPlayPauseAction
    {
        get => _settings.ShowPlayPauseAction;
        set => SetBoolean(value, () => _settings.ShowPlayPauseAction, v => _settings.ShowPlayPauseAction = v);
    }

    public bool ShowDesktopAction
    {
        get => _settings.ShowDesktopAction;
        set => SetBoolean(value, () => _settings.ShowDesktopAction, v => _settings.ShowDesktopAction = v);
    }

    public bool ShowLockAction
    {
        get => _settings.ShowLockAction;
        set => SetBoolean(value, () => _settings.ShowLockAction, v => _settings.ShowLockAction = v);
    }

    public string ThemeMode
    {
        get => NormalizeThemeMode(_settings.ThemeMode);
        set
        {
            var normalized = NormalizeThemeMode(value);
            if (string.Equals(_settings.ThemeMode, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _settings.ThemeMode = normalized;
            SaveAndNotify();
            OnPropertyChanged();
        }
    }

    public string AccentName
    {
        get => string.IsNullOrWhiteSpace(_settings.AccentName) ? "Blue" : _settings.AccentName;
        set
        {
            var normalized = value switch
            {
                "Purple" => "Purple",
                "Green" => "Green",
                "Orange" => "Orange",
                _ => "Blue"
            };

            if (string.Equals(_settings.AccentName, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _settings.AccentName = normalized;
            SaveAndNotify();
            OnPropertyChanged();
        }
    }

    public bool ShowSearchModule
    {
        get => _settings.ShowSearchModule;
        set => SetBoolean(value, () => _settings.ShowSearchModule, v => _settings.ShowSearchModule = v);
    }

    public bool ShowFavoritesModule
    {
        get => _settings.ShowFavoritesModule;
        set => SetBoolean(value, () => _settings.ShowFavoritesModule, v => _settings.ShowFavoritesModule = v);
    }

    public bool ShowCurrentWindowModule
    {
        get => _settings.ShowCurrentWindowModule;
        set => SetBoolean(value, () => _settings.ShowCurrentWindowModule, v => _settings.ShowCurrentWindowModule = v);
    }

    public bool ShowQuickActionsModule
    {
        get => _settings.ShowQuickActionsModule;
        set => SetBoolean(value, () => _settings.ShowQuickActionsModule, v => _settings.ShowQuickActionsModule = v);
    }

    public bool ShowMediaModule
    {
        get => _settings.ShowMediaModule;
        set => SetBoolean(value, () => _settings.ShowMediaModule, v => _settings.ShowMediaModule = v);
    }

    public bool ShowRecentModule
    {
        get => _settings.ShowRecentModule;
        set => SetBoolean(value, () => _settings.ShowRecentModule, v => _settings.ShowRecentModule = v);
    }

    public bool ShowMacrosModule
    {
        get => _settings.ShowMacrosModule;
        set => SetBoolean(value, () => _settings.ShowMacrosModule, v => _settings.ShowMacrosModule = v);
    }

    private static string NormalizeThemeMode(string? value) =>
        value switch
        {
            "Light" => "Light",
            "System" => "System",
            _ => "Dark"
        };

    private void CycleToggleHotkey()
    {
        ToggleHotkeyPreset = ToggleHotkeyPreset switch
        {
            "Ctrl+Space" => "Ctrl+Alt+Space",
            "Ctrl+Alt+Space" => "Ctrl+Alt+D",
            _ => "Ctrl+Space"
        };
    }

    private void CycleSearchHotkey()
    {
        SearchHotkeyPreset = SearchHotkeyPreset switch
        {
            "Ctrl+Shift+Space" => "Ctrl+Shift+F",
            "Ctrl+Shift+F" => "Ctrl+Alt+F",
            _ => "Ctrl+Shift+Space"
        };
    }

    private static string NormalizeToggleHotkey(string? value) =>
        value switch
        {
            "Ctrl+Alt+Space" => "Ctrl+Alt+Space",
            "Ctrl+Alt+D" => "Ctrl+Alt+D",
            _ => "Ctrl+Space"
        };

    private static string NormalizeSearchHotkey(string? value) =>
        value switch
        {
            "Ctrl+Shift+F" => "Ctrl+Shift+F",
            "Ctrl+Alt+F" => "Ctrl+Alt+F",
            _ => "Ctrl+Shift+Space"
        };

    private void SetBoolean(
        bool value,
        Func<bool> getValue,
        Action<bool> setValue,
        [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (getValue() == value) return;
        setValue(value);
        SaveAndNotify(propertyName);
        OnPropertyChanged(propertyName);
    }

    private void SaveAndNotify([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        _settingsService.Save(_settings);
        SettingsChanged?.Invoke(this, new SettingChangedEventArgs(propertyName ?? string.Empty));
    }
}

public sealed class SettingChangedEventArgs(string propertyName) : EventArgs
{
    public string PropertyName { get; } = propertyName;
}
