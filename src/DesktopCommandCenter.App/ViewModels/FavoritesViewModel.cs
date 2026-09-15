using System.Collections.ObjectModel;
using DesktopCommandCenter.Core.Settings;
using DesktopCommandCenter.Windows.Apps;

namespace DesktopCommandCenter.App.ViewModels;

public sealed class FavoritesViewModel : ObservableObject
{
    private readonly AppSettings _settings;
    private readonly ISettingsService _settingsService;
    private readonly AppLauncherService _appLauncher;
    private readonly RecentActivityViewModel _recent;

    public FavoritesViewModel(
        AppSettings settings,
        ISettingsService settingsService,
        AppLauncherService appLauncher,
        RecentActivityViewModel recent)
    {
        _settings = settings;
        _settingsService = settingsService;
        _appLauncher = appLauncher;
        _recent = recent;
        Items = new ObservableCollection<FavoriteItemViewModel>();

        foreach (var favorite in _settings.FavoriteApps)
        {
            AddViewModel(favorite);
        }
    }

    public ObservableCollection<FavoriteItemViewModel> Items { get; }
    public bool HasFavorites => Items.Count > 0;

    public bool AddFavorite(InstalledAppInfo app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (_settings.FavoriteApps.Any(item =>
                string.Equals(item.LaunchPath, app.LaunchPath, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var favorite = new FavoriteAppSetting
        {
            Name = app.Name,
            LaunchPath = app.LaunchPath
        };

        _settings.FavoriteApps.Add(favorite);
        AddViewModel(favorite);
        SaveAndNotify();
        return true;
    }

    private void AddViewModel(FavoriteAppSetting favorite)
    {
        FavoriteItemViewModel? item = null;
        item = new FavoriteItemViewModel(
            favorite.Name,
            favorite.LaunchPath,
            new RelayCommand(() =>
            {
                Action launch = () =>
                    _ = _appLauncher.Launch(new InstalledAppInfo(favorite.Name, favorite.LaunchPath));
                launch();
                _recent.Add(favorite.Name, "Favorite app", launch);
            }),
            new RelayCommand(() =>
            {
                if (item is not null)
                {
                    RemoveFavorite(item);
                }
            }));

        Items.Add(item);
    }

    private void RemoveFavorite(FavoriteItemViewModel item)
    {
        var setting = _settings.FavoriteApps.FirstOrDefault(candidate =>
            string.Equals(candidate.LaunchPath, item.LaunchPath, StringComparison.OrdinalIgnoreCase));

        if (setting is not null)
        {
            _settings.FavoriteApps.Remove(setting);
        }

        Items.Remove(item);
        SaveAndNotify();
    }

    private void SaveAndNotify()
    {
        _settingsService.Save(_settings);
        OnPropertyChanged(nameof(HasFavorites));
    }
}
