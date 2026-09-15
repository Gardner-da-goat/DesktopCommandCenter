using System.Collections.ObjectModel;
using DesktopCommandCenter.Core.Settings;

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
        UpdatesViewModel updates)
    {
        _settings = settings;
        _settingsService = settingsService;
        Favorites = favorites;
        Macros = macros;
        Updates = updates;
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
    public UpdatesViewModel Updates { get; }

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

    public bool AutoCheckForUpdates
    {
        get => _settings.AutoCheckForUpdates;
        set => SetBoolean(value, () => _settings.AutoCheckForUpdates, v => _settings.AutoCheckForUpdates = v);
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

    public bool ShowMacrosModule
    {
        get => _settings.ShowMacrosModule;
        set => SetBoolean(value, () => _settings.ShowMacrosModule, v => _settings.ShowMacrosModule = v);
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
