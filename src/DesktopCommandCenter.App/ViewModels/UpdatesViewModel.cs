using DesktopCommandCenter.App.Services;
using DesktopCommandCenter.Core.Settings;

namespace DesktopCommandCenter.App.ViewModels;

public sealed class UpdatesViewModel : ObservableObject
{
    private readonly UpdateService _updateService;
    private readonly AppSettings _settings;
    private UpdateInfo? _availableUpdate;
    private string _statusMessage = "Ready to check for updates.";
    private string _latestVersion = "—";
    private bool _isChecking;
    private bool _isDownloading;
    private double _downloadProgress;

    public UpdatesViewModel(
        UpdateService updateService,
        AppSettings settings)
    {
        _updateService = updateService;
        _settings = settings;
        CurrentVersion = _updateService.CurrentVersion.ToString(3);
        CheckCommand = new RelayCommand(() => _ = CheckForUpdatesAsync());
        UpdateAndRestartCommand = new RelayCommand(() => _ = UpdateAndRestartAsync());
    }

    public event EventHandler? RestartRequested;

    public string CurrentVersion { get; }
    public RelayCommand CheckCommand { get; }
    public RelayCommand UpdateAndRestartCommand { get; }

    public string LatestVersion
    {
        get => _latestVersion;
        private set => SetProperty(ref _latestVersion, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsChecking
    {
        get => _isChecking;
        private set => SetProperty(ref _isChecking, value);
    }

    public bool IsDownloading
    {
        get => _isDownloading;
        private set => SetProperty(ref _isDownloading, value);
    }

    public double DownloadProgress
    {
        get => _downloadProgress;
        private set => SetProperty(ref _downloadProgress, value);
    }

    public bool IsUpdateAvailable =>
        _availableUpdate is not null &&
        _availableUpdate.Version > _updateService.CurrentVersion;

    public async Task CheckForUpdatesAsync()
    {
        if (IsChecking || IsDownloading)
        {
            return;
        }

        IsChecking = true;
        StatusMessage = $"Checking {_settings.UpdateChannel} releases…";

        try
        {
            _availableUpdate = await _updateService.CheckForUpdateAsync(
                _settings.UpdateChannel);

            LatestVersion = _availableUpdate?.VersionText ?? "—";
            OnPropertyChanged(nameof(IsUpdateAvailable));

            if (_availableUpdate is null)
            {
                StatusMessage = "No published release is available yet.";
            }
            else if (IsUpdateAvailable)
            {
                StatusMessage = $"Version {_availableUpdate.VersionText} is available.";
            }
            else
            {
                StatusMessage = "You're up to date.";
            }
        }
        catch (Exception ex)
        {
            _availableUpdate = null;
            OnPropertyChanged(nameof(IsUpdateAvailable));
            StatusMessage = $"Update check failed: {ex.Message}";
        }
        finally
        {
            IsChecking = false;
        }
    }

    private async Task UpdateAndRestartAsync()
    {
        if (!IsUpdateAvailable || _availableUpdate is null || IsDownloading)
        {
            return;
        }

        IsDownloading = true;
        DownloadProgress = 0;
        StatusMessage = $"Downloading {_availableUpdate.VersionText}…";

        try
        {
            var progress = new Progress<double>(value =>
            {
                DownloadProgress = Math.Clamp(value, 0, 1);
                StatusMessage = $"Downloading {_availableUpdate.VersionText}… {DownloadProgress:P0}";
            });

            var zipPath = await _updateService.DownloadUpdateAsync(
                _availableUpdate,
                progress);

            StatusMessage = "Preparing update and restart…";
            if (!_updateService.LaunchUpdater(zipPath))
            {
                StatusMessage = "Could not start the updater. Make sure the app folder is writable.";
                return;
            }

            RestartRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Update failed: {ex.Message}";
        }
        finally
        {
            IsDownloading = false;
        }
    }
}
