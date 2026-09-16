using System.Collections.ObjectModel;
using DesktopCommandCenter.App.Services;
using DesktopCommandCenter.Windows.Shell;

namespace DesktopCommandCenter.App.ViewModels;

public sealed class MusicViewModel : ObservableObject
{
    private readonly ShellActionService _shell;
    private readonly PulseDeckIntegrationService _pulseDeck;
    private int _trackCount;
    private int _playlistCount;
    private int _favoriteCount;
    private bool _pulseDeckInstalled;
    private bool _pulseDeckDataFound;
    private string _libraryStatus = string.Empty;

    public MusicViewModel(
        ShellActionService shell,
        PulseDeckIntegrationService pulseDeck)
    {
        _shell = shell;
        _pulseDeck = pulseDeck;

        RecentTracks = [];

        PlayPauseCommand = new RelayCommand(() => _ = _shell.PlayPause());
        PreviousTrackCommand = new RelayCommand(() => _ = _shell.PreviousTrack());
        NextTrackCommand = new RelayCommand(() => _ = _shell.NextTrack());
        ToggleMuteCommand = new RelayCommand(() => _ = _shell.ToggleMute());
        VolumeDownCommand = new RelayCommand(() => _ = _shell.VolumeDown());
        VolumeUpCommand = new RelayCommand(() => _ = _shell.VolumeUp());
        OpenMusicFolderCommand = new RelayCommand(() => _ = _shell.OpenMusicFolder());
        OpenPulseDeckCommand = new RelayCommand(
            () => _ = _pulseDeck.LaunchPulseDeck(),
            () => PulseDeckInstalled);
        OpenPulseDeckDataCommand = new RelayCommand(
            () => _ = _pulseDeck.OpenPulseDeckDataFolder(),
            () => PulseDeckDataFound);
        RefreshLibraryCommand = new RelayCommand(Refresh);

        Refresh();
    }

    public ObservableCollection<PulseDeckTrackSummary> RecentTracks { get; }

    public RelayCommand PlayPauseCommand { get; }
    public RelayCommand PreviousTrackCommand { get; }
    public RelayCommand NextTrackCommand { get; }
    public RelayCommand ToggleMuteCommand { get; }
    public RelayCommand VolumeDownCommand { get; }
    public RelayCommand VolumeUpCommand { get; }
    public RelayCommand OpenMusicFolderCommand { get; }
    public RelayCommand OpenPulseDeckCommand { get; }
    public RelayCommand OpenPulseDeckDataCommand { get; }
    public RelayCommand RefreshLibraryCommand { get; }

    public int TrackCount
    {
        get => _trackCount;
        private set => SetProperty(ref _trackCount, value);
    }

    public int PlaylistCount
    {
        get => _playlistCount;
        private set => SetProperty(ref _playlistCount, value);
    }

    public int FavoriteCount
    {
        get => _favoriteCount;
        private set => SetProperty(ref _favoriteCount, value);
    }

    public bool PulseDeckInstalled
    {
        get => _pulseDeckInstalled;
        private set
        {
            if (SetProperty(ref _pulseDeckInstalled, value))
            {
                OpenPulseDeckCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool PulseDeckDataFound
    {
        get => _pulseDeckDataFound;
        private set
        {
            if (SetProperty(ref _pulseDeckDataFound, value))
            {
                OpenPulseDeckDataCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string LibraryStatus
    {
        get => _libraryStatus;
        private set => SetProperty(ref _libraryStatus, value);
    }

    public void Refresh()
    {
        var snapshot = _pulseDeck.ReadSnapshot();

        PulseDeckInstalled = _pulseDeck.IsPulseDeckInstalled();
        PulseDeckDataFound = snapshot.DataFound;
        TrackCount = snapshot.TrackCount;
        PlaylistCount = snapshot.PlaylistCount;
        FavoriteCount = snapshot.FavoriteCount;

        RecentTracks.Clear();
        foreach (var track in snapshot.RecentTracks)
        {
            RecentTracks.Add(track);
        }

        LibraryStatus = snapshot.DataFound
            ? "Connected to your PulseDeck library."
            : PulseDeckInstalled
                ? "PulseDeck is installed. Open it once so the hub can read its library."
                : "PulseDeck is not installed in a standard location yet.";
    }
}
