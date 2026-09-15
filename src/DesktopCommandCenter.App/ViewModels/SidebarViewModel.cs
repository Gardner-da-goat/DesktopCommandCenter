using System.Windows.Input;
using DesktopCommandCenter.Core.Settings;
using DesktopCommandCenter.Core.State;

namespace DesktopCommandCenter.App.ViewModels;

public sealed class SidebarViewModel : ObservableObject
{
    private readonly SidebarState _state;
    private object _currentPage;

    public SidebarViewModel(AppSettings settings, HomeViewModel home, SettingsViewModel settingsViewModel)
    {
        _state = new SidebarState(settings);
        Home = home;
        Settings = settingsViewModel;
        _currentPage = Home;

        ToggleCommand = new RelayCommand(Toggle);
        ExpandCommand = new RelayCommand(Expand);
        CollapseCommand = new RelayCommand(Collapse);
        ShowHomeCommand = new RelayCommand(ShowHome);
        ShowSettingsCommand = new RelayCommand(ShowSettings);

        Settings.SettingsChanged += OnSettingsChanged;
    }

    public HomeViewModel Home { get; }
    public SettingsViewModel Settings { get; }
    public ICommand ToggleCommand { get; }
    public ICommand ExpandCommand { get; }
    public ICommand CollapseCommand { get; }
    public ICommand ShowHomeCommand { get; }
    public ICommand ShowSettingsCommand { get; }

    public object CurrentPage
    {
        get => _currentPage;
        private set
        {
            if (!SetProperty(ref _currentPage, value)) return;
            OnPropertyChanged(nameof(IsHomeSelected));
            OnPropertyChanged(nameof(IsSettingsSelected));
        }
    }

    public bool IsExpanded => _state.IsExpanded;
    public bool IsHomeSelected => ReferenceEquals(CurrentPage, Home);
    public bool IsSettingsSelected => ReferenceEquals(CurrentPage, Settings);
    public double SidebarWidth => _state.ExpandedWidth;
    public bool AnimationsEnabled => _state.AnimationsEnabled;
    public bool AlwaysOnTop => Settings.AlwaysOnTop;

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
    public void ShowSettings() => CurrentPage = Settings;

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
    }
}