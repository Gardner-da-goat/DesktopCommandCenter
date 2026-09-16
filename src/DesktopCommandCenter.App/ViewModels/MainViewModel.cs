namespace DesktopCommandCenter.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private object _currentPage;
    private string _currentSection = "Home";

    public MainViewModel(
        HomeViewModel home,
        MusicViewModel music,
        FilesViewModel files,
        WindowsViewModel windows,
        SettingsViewModel settings)
    {
        Home = home;
        Music = music;
        Files = files;
        Windows = windows;
        Settings = settings;
        _currentPage = Home;

        ShowHomeCommand = new RelayCommand(ShowHome);
        ShowMusicCommand = new RelayCommand(ShowMusic);
        ShowFilesCommand = new RelayCommand(ShowFiles);
        ShowWindowsCommand = new RelayCommand(ShowWindows);
        ShowAmbienceCommand = new RelayCommand(ShowAmbience);
        ShowSettingsCommand = new RelayCommand(() => ShowSettings());

        Home.Search.SettingsNavigationRequested += (_, category) =>
            ShowSettings(category);
    }

    public HomeViewModel Home { get; }
    public MusicViewModel Music { get; }
    public FilesViewModel Files { get; }
    public WindowsViewModel Windows { get; }
    public SettingsViewModel Settings { get; }

    public RelayCommand ShowHomeCommand { get; }
    public RelayCommand ShowMusicCommand { get; }
    public RelayCommand ShowFilesCommand { get; }
    public RelayCommand ShowWindowsCommand { get; }
    public RelayCommand ShowAmbienceCommand { get; }
    public RelayCommand ShowSettingsCommand { get; }

    public object CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }

    public string CurrentSection
    {
        get => _currentSection;
        private set
        {
            if (!SetProperty(ref _currentSection, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsHomeSelected));
            OnPropertyChanged(nameof(IsMusicSelected));
            OnPropertyChanged(nameof(IsFilesSelected));
            OnPropertyChanged(nameof(IsWindowsSelected));
            OnPropertyChanged(nameof(IsAmbienceSelected));
            OnPropertyChanged(nameof(IsSettingsSelected));
        }
    }

    public bool IsHomeSelected => CurrentSection == "Home";
    public bool IsMusicSelected => CurrentSection == "Music";
    public bool IsFilesSelected => CurrentSection == "Files";
    public bool IsWindowsSelected => CurrentSection == "Windows";
    public bool IsAmbienceSelected => CurrentSection == "Ambience";
    public bool IsSettingsSelected => CurrentSection == "Settings";

    public void NavigateTo(string? section, string? settingsCategory = null)
    {
        switch (section?.Trim().ToLowerInvariant())
        {
            case "music":
                ShowMusic();
                break;
            case "files":
                ShowFiles();
                break;
            case "windows":
                ShowWindows();
                break;
            case "ambience":
                ShowAmbience();
                break;
            case "settings":
                ShowSettings(settingsCategory);
                break;
            default:
                ShowHome();
                break;
        }
    }

    public void ShowHome()
    {
        CurrentSection = "Home";
        CurrentPage = Home;
    }

    public void ShowMusic()
    {
        CurrentSection = "Music";
        CurrentPage = Music;
    }

    public void ShowFiles()
    {
        Files.Refresh();
        CurrentSection = "Files";
        CurrentPage = Files;
    }

    public void ShowWindows()
    {
        Windows.Refresh();
        CurrentSection = "Windows";
        CurrentPage = Windows;
    }

    public void ShowAmbience()
    {
        SelectSettingsCategory("Ambience");
        CurrentSection = "Ambience";
        CurrentPage = Settings;
    }

    public void ShowSettings(string? category = null)
    {
        SelectSettingsCategory(string.IsNullOrWhiteSpace(category) ? "General" : category);
        CurrentSection = "Settings";
        CurrentPage = Settings;
    }

    private void SelectSettingsCategory(string categoryName)
    {
        var category = Settings.Categories.FirstOrDefault(item =>
            item.Name.Equals(
                categoryName,
                StringComparison.CurrentCultureIgnoreCase));

        if (category is not null)
        {
            Settings.SelectedCategory = category;
        }
    }
}
