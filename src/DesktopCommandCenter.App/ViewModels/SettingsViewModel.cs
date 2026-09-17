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
    private string _settingsSearchText = string.Empty;

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
        OpenWindowsSettingsCommand = new RelayCommand(() => shellActions.OpenWindowsSettings());
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
        UpdateStableCommand = new RelayCommand(() => UpdateChannel = "Stable");
        UpdateBetaCommand = new RelayCommand(() => UpdateChannel = "Beta");
        ChangeToggleHotkeyCommand = new RelayCommand(CycleToggleHotkey);
        ChangeSearchHotkeyCommand = new RelayCommand(CycleSearchHotkey);
        ResetHotkeysCommand = new RelayCommand(ResetHotkeys);
        AmbienceAquariumCommand = new RelayCommand(() => AmbienceMode = "Aquarium");
        AmbienceFirefliesCommand = new RelayCommand(() => AmbienceMode = "Fireflies");
        AmbiencePetCommand = new RelayCommand(() => AmbienceMode = "Desktop Pet");
        AmbienceCoralReefCommand = new RelayCommand(() => AmbienceBackgroundPreset = "Coral Reef");
        AmbienceDeepOceanCommand = new RelayCommand(() => AmbienceBackgroundPreset = "Deep Ocean");
        AmbienceKelpForestCommand = new RelayCommand(() => AmbienceBackgroundPreset = "Kelp Forest");
        AmbienceFirefliesNightCommand = new RelayCommand(() => AmbienceBackgroundPreset = "Fireflies Night");
        AmbienceMinimalGradientCommand = new RelayCommand(() => AmbienceBackgroundPreset = "Minimal Gradient");
        AmbienceSpaceCommand = new RelayCommand(() => AmbienceBackgroundPreset = "Space");
        Categories = new ObservableCollection<SettingsCategory>
        {
            new("System", "Display, sound, notifications, power, storage, clipboard, and recovery.", "▣", "brightness hdr focus battery multitasking about"),
            new("Bluetooth & devices", "Bluetooth, printers, cameras, mouse, touchpad, AutoPlay, and USB.", "⌁", "phone pen scanner connected devices"),
            new("Network & internet", "Wi-Fi, Ethernet, VPN, hotspot, proxy, and adapter status.", "◎", "wifi ip airplane dial-up"),
            new("Personalization", "Windows background, colors, themes, lock screen, Start, and taskbar.", "✦", "fonts text input device usage"),
            new("Apps", "Installed apps, defaults, optional features, startup, and video playback.", "▦", "uninstall offline maps websites"),
            new("Accounts", "Your info, sign-in options, family, backup, users, work, and school.", "●", "email windows backup"),
            new("Time & language", "Date, time, language, region, typing, and speech.", "◷", "timezone keyboard"),
            new("Gaming", "Game Bar, captures, Game Mode, and future gaming workspace controls.", "♜", "performance launcher"),
            new("Accessibility", "Vision, hearing, captions, keyboard, mouse, speech, and interaction.", "♿", "narrator magnifier contrast filters"),
            new("Privacy & security", "Windows Security, device privacy, permissions, diagnostics, and search.", "◆", "camera microphone location developers encryption"),
            new("Windows Update", "Windows update status, history, advanced options, and Insider settings.", "↻", "optional updates"),
            new("Desktop Command Center", "Application settings, integrations, data, and power-user controls.", "⌘", "app hub"),
            new("General", "Startup, sidebar, and application behavior.", "⌂", "start minimized background exit language"),
            new("Customize Home", "Choose and arrange the modules on Home.", "▤", "dashboard widgets reorder"),
            new("Favorites", "Manage pinned apps, folders, and commands.", "★"),
            new("Quick Actions", "Choose the actions shown on Home.", "⚡"),
            new("Hotkeys", "Configure shortcuts for common actions.", "⌨", "keyboard shortcut conflicts"),
            new("Macros", "Build multi-step desktop routines.", "▶"),
            new("Commands", "Manage searchable commands.", ">_"),
            new("Search", "Choose search sources and behavior.", "⌕", "index history excluded locations"),
            new("Window management", "Configure window controls and defaults.", "▧", "snap opacity topmost"),
            new("Ambience", "Add passive desktop life like fish, fireflies, or a roaming pet.", "✧"),
            new("Appearance", "Theme, accent, transparency, and motion.", "◐", "dark light density scale sidebar"),
            new("Updates", "Control Desktop Command Center updates and release channel.", "⇩", "github version release automatic"),
            new("Advanced", "Diagnostics and advanced maintenance tools.", "⚙", "logs reset cache developer")
        };
        FilteredCategories = new ObservableCollection<SettingsCategory>(Categories);
        _selectedCategory = Categories[0];
    }

    public event EventHandler<SettingChangedEventArgs>? SettingsChanged;
    public ObservableCollection<SettingsCategory> Categories { get; }
    public ObservableCollection<SettingsCategory> FilteredCategories { get; }
    public FavoritesViewModel Favorites { get; }
    public MacrosViewModel Macros { get; }
    public CommandsViewModel Commands { get; }
    public UpdatesViewModel Updates { get; }
    public RelayCommand OpenSettingsFolderCommand { get; }
    public RelayCommand OpenWindowsSettingsCommand { get; }
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
    public RelayCommand UpdateStableCommand { get; }
    public RelayCommand UpdateBetaCommand { get; }
    public RelayCommand ChangeToggleHotkeyCommand { get; }
    public RelayCommand ChangeSearchHotkeyCommand { get; }
    public RelayCommand ResetHotkeysCommand { get; }
    public RelayCommand AmbienceAquariumCommand { get; }
    public RelayCommand AmbienceFirefliesCommand { get; }
    public RelayCommand AmbiencePetCommand { get; }
    public RelayCommand AmbienceCoralReefCommand { get; }
    public RelayCommand AmbienceDeepOceanCommand { get; }
    public RelayCommand AmbienceKelpForestCommand { get; }
    public RelayCommand AmbienceFirefliesNightCommand { get; }
    public RelayCommand AmbienceMinimalGradientCommand { get; }
    public RelayCommand AmbienceSpaceCommand { get; }

    public SettingsCategory SelectedCategory
    {
        get => _selectedCategory;
        set => SetProperty(ref _selectedCategory, value);
    }

    public string SettingsSearchText
    {
        get => _settingsSearchText;
        set
        {
            if (!SetProperty(ref _settingsSearchText, value ?? string.Empty))
            {
                return;
            }

            RefreshCategoryFilter();
        }
    }

    private void RefreshCategoryFilter()
    {
        var query = SettingsSearchText.Trim();
        var matches = string.IsNullOrWhiteSpace(query)
            ? Categories
            : new ObservableCollection<SettingsCategory>(Categories.Where(category =>
                category.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                category.Description.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                category.Keywords.Contains(query, StringComparison.CurrentCultureIgnoreCase)));

        FilteredCategories.Clear();
        foreach (var category in matches)
        {
            FilteredCategories.Add(category);
        }

        if (FilteredCategories.Count > 0 && !FilteredCategories.Contains(SelectedCategory))
        {
            SelectedCategory = FilteredCategories[0];
        }
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

    public bool NotificationsEnabled
    {
        get => _settings.NotificationsEnabled;
        set => SetBoolean(value, () => _settings.NotificationsEnabled, v => _settings.NotificationsEnabled = v);
    }

    public string UpdateChannel
    {
        get => string.Equals(_settings.UpdateChannel, "Beta", StringComparison.OrdinalIgnoreCase)
            ? "Beta"
            : "Stable";
        set
        {
            var normalized = string.Equals(value, "Beta", StringComparison.OrdinalIgnoreCase)
                ? "Beta"
                : "Stable";

            if (string.Equals(_settings.UpdateChannel, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _settings.UpdateChannel = normalized;
            SaveAndNotify();
            OnPropertyChanged();
        }
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
        get => NormalizeHotkeyText(_settings.ToggleHotkeyPreset, "Ctrl+Space");
        set => SetHotkeyText(
            value,
            () => _settings.ToggleHotkeyPreset,
            v => _settings.ToggleHotkeyPreset = v,
            "Ctrl+Space");
    }

    public string SearchHotkeyPreset
    {
        get => NormalizeHotkeyText(_settings.SearchHotkeyPreset, "Ctrl+Shift+Space");
        set => SetHotkeyText(
            value,
            () => _settings.SearchHotkeyPreset,
            v => _settings.SearchHotkeyPreset = v,
            "Ctrl+Shift+Space");
    }

    public string SnapLeftHotkey
    {
        get => NormalizeHotkeyText(_settings.SnapLeftHotkey, "Ctrl+Alt+Left");
        set => SetHotkeyText(value, () => _settings.SnapLeftHotkey, v => _settings.SnapLeftHotkey = v, "Ctrl+Alt+Left");
    }

    public string SnapRightHotkey
    {
        get => NormalizeHotkeyText(_settings.SnapRightHotkey, "Ctrl+Alt+Right");
        set => SetHotkeyText(value, () => _settings.SnapRightHotkey, v => _settings.SnapRightHotkey = v, "Ctrl+Alt+Right");
    }

    public string ToggleTopmostHotkey
    {
        get => NormalizeHotkeyText(_settings.ToggleTopmostHotkey, "Ctrl+Alt+T");
        set => SetHotkeyText(value, () => _settings.ToggleTopmostHotkey, v => _settings.ToggleTopmostHotkey = v, "Ctrl+Alt+T");
    }

    public string OpacityUpHotkey
    {
        get => NormalizeHotkeyText(_settings.OpacityUpHotkey, "Ctrl+Alt+Up");
        set => SetHotkeyText(value, () => _settings.OpacityUpHotkey, v => _settings.OpacityUpHotkey = v, "Ctrl+Alt+Up");
    }

    public string OpacityDownHotkey
    {
        get => NormalizeHotkeyText(_settings.OpacityDownHotkey, "Ctrl+Alt+Down");
        set => SetHotkeyText(value, () => _settings.OpacityDownHotkey, v => _settings.OpacityDownHotkey = v, "Ctrl+Alt+Down");
    }

    public bool ReflowWindowsOnSidebar
    {
        get => _settings.ReflowWindowsOnSidebar;
        set => SetBoolean(value, () => _settings.ReflowWindowsOnSidebar, v => _settings.ReflowWindowsOnSidebar = v);
    }

    public bool AmbienceEnabled
    {
        get => _settings.AmbienceEnabled;
        set => SetBoolean(value, () => _settings.AmbienceEnabled, v => _settings.AmbienceEnabled = v);
    }

    public string AmbienceMode
    {
        get => NormalizeAmbienceMode(_settings.AmbienceMode);
        set
        {
            var normalized = NormalizeAmbienceMode(value);
            if (string.Equals(_settings.AmbienceMode, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _settings.AmbienceMode = normalized;
            SaveAndNotify();
            OnPropertyChanged();
        }
    }

    public bool AmbienceBackgroundEnabled
    {
        get => _settings.AmbienceBackgroundEnabled;
        set => SetBoolean(value, () => _settings.AmbienceBackgroundEnabled, v => _settings.AmbienceBackgroundEnabled = v);
    }

    public bool AmbienceCreaturesEnabled
    {
        get => _settings.AmbienceCreaturesEnabled;
        set => SetBoolean(value, () => _settings.AmbienceCreaturesEnabled, v => _settings.AmbienceCreaturesEnabled = v);
    }

    public bool AmbienceEffectsEnabled
    {
        get => _settings.AmbienceEffectsEnabled;
        set => SetBoolean(value, () => _settings.AmbienceEffectsEnabled, v => _settings.AmbienceEffectsEnabled = v);
    }

    public string AmbienceBackgroundPreset
    {
        get => NormalizeAmbienceBackgroundPreset(_settings.AmbienceBackgroundPreset);
        set
        {
            var normalized = NormalizeAmbienceBackgroundPreset(value);
            if (string.Equals(_settings.AmbienceBackgroundPreset, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _settings.AmbienceBackgroundPreset = normalized;
            SaveAndNotify();
            OnPropertyChanged();
        }
    }

    public int AmbiencePopulation
    {
        get => Math.Clamp(_settings.AmbiencePopulation, 2, 30);
        set
        {
            var normalized = Math.Clamp(value, 2, 30);
            if (_settings.AmbiencePopulation == normalized)
            {
                return;
            }

            _settings.AmbiencePopulation = normalized;
            SaveAndNotify();
            OnPropertyChanged();
        }
    }

    public double AmbienceSpeed
    {
        get => NormalizeAmbienceDouble(_settings.AmbienceSpeed, 0.35, 2.5, 1);
        set => SetAmbienceDouble(
            value,
            0.35,
            2.5,
            1,
            () => _settings.AmbienceSpeed,
            v => _settings.AmbienceSpeed = v);
    }

    public double AmbienceOpacity
    {
        get => NormalizeAmbienceDouble(_settings.AmbienceOpacity, 0.15, 1, 0.78);
        set => SetAmbienceDouble(
            value,
            0.15,
            1,
            0.78,
            () => _settings.AmbienceOpacity,
            v => _settings.AmbienceOpacity = v);
    }

    public double AmbienceBackgroundOpacity
    {
        get => NormalizeAmbienceDouble(_settings.AmbienceBackgroundOpacity, 0, 1, 0.58);
        set => SetAmbienceDouble(
            value,
            0,
            1,
            0.58,
            () => _settings.AmbienceBackgroundOpacity,
            v => _settings.AmbienceBackgroundOpacity = v);
    }

    public double AmbienceBackgroundBrightness
    {
        get => NormalizeAmbienceDouble(_settings.AmbienceBackgroundBrightness, 0.4, 1.6, 1);
        set => SetAmbienceDouble(
            value,
            0.4,
            1.6,
            1,
            () => _settings.AmbienceBackgroundBrightness,
            v => _settings.AmbienceBackgroundBrightness = v);
    }

    public double AmbienceBackgroundMotion
    {
        get => NormalizeAmbienceDouble(_settings.AmbienceBackgroundMotion, 0, 1, 0.35);
        set => SetAmbienceDouble(
            value,
            0,
            1,
            0.35,
            () => _settings.AmbienceBackgroundMotion,
            v => _settings.AmbienceBackgroundMotion = v);
    }

    public double AmbienceCreatureSize
    {
        get => NormalizeAmbienceDouble(_settings.AmbienceCreatureSize, 0.6, 1.6, 1);
        set => SetAmbienceDouble(
            value,
            0.6,
            1.6,
            1,
            () => _settings.AmbienceCreatureSize,
            v => _settings.AmbienceCreatureSize = v);
    }

    public bool AmbienceRandomDirection
    {
        get => _settings.AmbienceRandomDirection;
        set => SetBoolean(value, () => _settings.AmbienceRandomDirection, v => _settings.AmbienceRandomDirection = v);
    }

    public bool AmbienceSchooling
    {
        get => _settings.AmbienceSchooling;
        set => SetBoolean(value, () => _settings.AmbienceSchooling, v => _settings.AmbienceSchooling = v);
    }

    public double AmbienceBubbleIntensity
    {
        get => NormalizeAmbienceDouble(_settings.AmbienceBubbleIntensity, 0, 1, 0.55);
        set => SetAmbienceDouble(
            value,
            0,
            1,
            0.55,
            () => _settings.AmbienceBubbleIntensity,
            v => _settings.AmbienceBubbleIntensity = v);
    }

    public double AmbienceParticleIntensity
    {
        get => NormalizeAmbienceDouble(_settings.AmbienceParticleIntensity, 0, 1, 0.4);
        set => SetAmbienceDouble(
            value,
            0,
            1,
            0.4,
            () => _settings.AmbienceParticleIntensity,
            v => _settings.AmbienceParticleIntensity = v);
    }

    public double AmbienceGlowIntensity
    {
        get => NormalizeAmbienceDouble(_settings.AmbienceGlowIntensity, 0, 1, 0.45);
        set => SetAmbienceDouble(
            value,
            0,
            1,
            0.45,
            () => _settings.AmbienceGlowIntensity,
            v => _settings.AmbienceGlowIntensity = v);
    }

    public bool AmbiencePerformanceMode
    {
        get => _settings.AmbiencePerformanceMode;
        set => SetBoolean(value, () => _settings.AmbiencePerformanceMode, v => _settings.AmbiencePerformanceMode = v);
    }

    public bool AmbienceAllMonitors
    {
        get => _settings.AmbienceAllMonitors;
        set => SetBoolean(value, () => _settings.AmbienceAllMonitors, v => _settings.AmbienceAllMonitors = v);
    }

    public bool AmbienceOverApps
    {
        get => _settings.AmbienceOverApps;
        set => SetBoolean(value, () => _settings.AmbienceOverApps, v => _settings.AmbienceOverApps = v);
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

    public bool ShowClipboardModule
    {
        get => _settings.ShowClipboardModule;
        set => SetBoolean(value, () => _settings.ShowClipboardModule, v => _settings.ShowClipboardModule = v);
    }

    public bool ShowMacrosModule
    {
        get => _settings.ShowMacrosModule;
        set => SetBoolean(value, () => _settings.ShowMacrosModule, v => _settings.ShowMacrosModule = v);
    }

    private static string NormalizeAmbienceBackgroundPreset(string? value) =>
        value switch
        {
            "Deep Ocean" => "Deep Ocean",
            "Kelp Forest" => "Kelp Forest",
            "Fireflies Night" => "Fireflies Night",
            "Minimal Gradient" => "Minimal Gradient",
            "Space" => "Space",
            _ => "Coral Reef"
        };

    private static double NormalizeAmbienceDouble(
        double value,
        double minimum,
        double maximum,
        double fallback) =>
        double.IsFinite(value)
            ? Math.Clamp(value, minimum, maximum)
            : fallback;

    private void SetAmbienceDouble(
        double value,
        double minimum,
        double maximum,
        double fallback,
        Func<double> getValue,
        Action<double> setValue,
        [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        var normalized = NormalizeAmbienceDouble(
            value,
            minimum,
            maximum,
            fallback);

        if (Math.Abs(getValue() - normalized) < 0.01)
        {
            return;
        }

        setValue(normalized);
        SaveAndNotify(propertyName);
        OnPropertyChanged(propertyName);
    }

    private static string NormalizeAmbienceMode(string? value) =>
        value switch
        {
            "Fireflies" => "Fireflies",
            "Desktop Pet" => "Desktop Pet",
            _ => "Aquarium"
        };

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

    private void ResetHotkeys()
    {
        ToggleHotkeyPreset = "Ctrl+Space";
        SearchHotkeyPreset = "Ctrl+Shift+Space";
        SnapLeftHotkey = "Ctrl+Alt+Left";
        SnapRightHotkey = "Ctrl+Alt+Right";
        ToggleTopmostHotkey = "Ctrl+Alt+T";
        OpacityUpHotkey = "Ctrl+Alt+Up";
        OpacityDownHotkey = "Ctrl+Alt+Down";
    }

    private void SetHotkeyText(
        string? value,
        Func<string> getValue,
        Action<string> setValue,
        string fallback,
        [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        var normalized = NormalizeHotkeyText(value, fallback);
        if (string.Equals(getValue(), normalized, StringComparison.Ordinal))
        {
            return;
        }

        setValue(normalized);
        SaveAndNotify(propertyName);
        OnPropertyChanged(propertyName);
    }

    private static string NormalizeHotkeyText(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var parts = value
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return parts.Length == 0
            ? fallback
            : string.Join("+", parts);
    }

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
