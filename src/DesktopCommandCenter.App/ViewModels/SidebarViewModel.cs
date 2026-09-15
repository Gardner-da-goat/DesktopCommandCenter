using System.Windows.Input;
using DesktopCommandCenter.Core.Settings;
using DesktopCommandCenter.Core.State;

namespace DesktopCommandCenter.App.ViewModels;

public sealed class SidebarViewModel : ObservableObject
{
    private readonly SidebarState _state;
    private object _currentPage;

    public SidebarViewModel(
        AppSettings settings,
        HomeViewModel home,
        WindowsViewModel windows,
        SettingsViewModel settingsViewModel)
    {
        _state = new SidebarState(settings);
        Home = home;
        Windows = windows;
        Settings = settingsViewModel;
        _currentPage = Home;

        ToggleCommand = new RelayCommand(Toggle);
        ExpandCommand = new RelayCommand(Expand);
        CollapseCommand = new RelayCommand(Collapse);
        ShowHomeCommand = new RelayCommand(ShowHome);
        ShowWindowsCommand = new RelayCommand(ShowWindows);
        ShowSettingsCommand = new RelayCommand(ShowSettings);

        Settings.SettingsChanged += OnSettingsChanged;
        Home.Search.SettingsNavigationRequested += OnSettingsNavigationRequested;
    }

    public HomeViewModel Home { get; }
    public WindowsViewModel Windows { get; }
    public SettingsViewModel Settings { get; }
    public ICommand ToggleCommand { get; }
    public ICommand ExpandCommand { get; }
    public ICommand CollapseCommand { get; }
    public ICommand ShowHomeCommand { get; }
    public ICommand ShowWindowsCommand { get; }
    public ICommand ShowSettingsCommand { get; }

    public object CurrentPage
    {
        get => _currentPage;
        private set
        {
            if (!SetProperty(ref _currentPage, value)) return;
            OnPropertyChanged(nameof(IsHomeSelected));
            OnPropertyChanged(nameof(IsWindowsSelected));
            OnPropertyChanged(nameof(IsSettingsSelected));
        }
    }

    public bool IsExpanded => _state.IsExpanded;
    public bool IsHomeSelected => ReferenceEquals(CurrentPage, Home);
    public bool IsWindowsSelected => ReferenceEquals(CurrentPage, Windows);
    public bool IsSettingsSelected => ReferenceEquals(CurrentPage, Settings);
    public double SidebarWidth => _state.ExpandedWidth;
    public bool AnimationsEnabled => _state.AnimationsEnabled;
    public bool AlwaysOnTop => Settings.AlwaysOnTop;
    public bool GlobalHotkeysEnabled => Settings.GlobalHotkeysEnabled;
    public bool WindowControlHotkeysEnabled => Settings.WindowControlHotkeysEnabled;
    public string ToggleHotkeyPreset => Settings.ToggleHotkeyPreset;
    public string SearchHotkeyPreset => Settings.SearchHotkeyPreset;
    public bool ReflowWindowsOnSidebar => Settings.ReflowWindowsOnSidebar;
    public SidebarEdge SidebarEdge => Settings.SidebarEdge;
    public SidebarHandlePosition HandlePosition => Settings.HandlePosition;
    public bool IsSidebarOnLeft => SidebarEdge == SidebarEdge.Left;
    public string HandleArrowGlyph => IsSidebarOnLeft ? "❯" : "❮";
    public string CollapseArrowGlyph => IsSidebarOnLeft ? "❮" : "❯";

    public void Expand()
    {
        if (IsExpanded) return;
        _state.Expand();
        OnPropertyChanged(nameof(IsExpanded));
    }

    public void Collapse()
    {
        if (!IsExpanded) return;
        _state.Collapse();
        OnPropertyChanged(nameof(IsExpanded));
    }

    public void Toggle()
    {
        _state.Toggle();
        OnPropertyChanged(nameof(IsExpanded));
    }

    public void ShowHome() => CurrentPage = Home;

    public void ShowWindows()
    {
        Windows.Refresh();
        CurrentPage = Windows;
    }

    public void ShowSettings() => CurrentPage = Settings;

    private void OnSettingsNavigationRequested(object? sender, string categoryName)
    {
        var category = Settings.Categories.FirstOrDefault(item =>
            item.Name.Equals(categoryName, StringComparison.CurrentCultureIgnoreCase));

        if (category is not null)
        {
            Settings.SelectedCategory = category;
        }

        CurrentPage = Settings;
        Expand();
    }

    private void OnSettingsChanged(object? sender, SettingChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.SidebarWidth))
        {
            _state.SetWidth(Settings.SidebarWidth);
            OnPropertyChanged(nameof(SidebarWidth));
        }
        else if (e.PropertyName == nameof(SettingsViewModel.AnimationsEnabled))
        {
            _state.SetAnimationsEnabled(Settings.AnimationsEnabled);
            OnPropertyChanged(nameof(AnimationsEnabled));
        }
        else if (e.PropertyName == nameof(SettingsViewModel.AlwaysOnTop))
        {
            OnPropertyChanged(nameof(AlwaysOnTop));
        }
        else if (e.PropertyName == nameof(SettingsViewModel.GlobalHotkeysEnabled))
        {
            OnPropertyChanged(nameof(GlobalHotkeysEnabled));
        }
        else if (e.PropertyName == nameof(SettingsViewModel.WindowControlHotkeysEnabled))
        {
            OnPropertyChanged(nameof(WindowControlHotkeysEnabled));
        }
        else if (e.PropertyName == nameof(SettingsViewModel.ToggleHotkeyPreset))
        {
            OnPropertyChanged(nameof(ToggleHotkeyPreset));
        }
        else if (e.PropertyName == nameof(SettingsViewModel.SearchHotkeyPreset))
        {
            OnPropertyChanged(nameof(SearchHotkeyPreset));
        }
        else if (e.PropertyName == nameof(SettingsViewModel.ReflowWindowsOnSidebar))
        {
            OnPropertyChanged(nameof(ReflowWindowsOnSidebar));
        }
        else if (e.PropertyName == nameof(SettingsViewModel.SidebarEdge))
        {
            OnPropertyChanged(nameof(SidebarEdge));
            OnPropertyChanged(nameof(IsSidebarOnLeft));
            OnPropertyChanged(nameof(HandleArrowGlyph));
            OnPropertyChanged(nameof(CollapseArrowGlyph));
        }
        else if (e.PropertyName == nameof(SettingsViewModel.HandlePosition))
        {
            OnPropertyChanged(nameof(HandlePosition));
        }
    }
}
