using DesktopCommandCenter.Windows.Shell;

namespace DesktopCommandCenter.App.ViewModels;

public sealed class MusicViewModel : ObservableObject
{
    private readonly ShellActionService _shell;

    public MusicViewModel(ShellActionService shell)
    {
        _shell = shell;
        PlayPauseCommand = new RelayCommand(() => _ = _shell.PlayPause());
        PreviousTrackCommand = new RelayCommand(() => _ = _shell.PreviousTrack());
        NextTrackCommand = new RelayCommand(() => _ = _shell.NextTrack());
        ToggleMuteCommand = new RelayCommand(() => _ = _shell.ToggleMute());
        VolumeDownCommand = new RelayCommand(() => _ = _shell.VolumeDown());
        VolumeUpCommand = new RelayCommand(() => _ = _shell.VolumeUp());
        OpenMusicFolderCommand = new RelayCommand(() => _ = _shell.OpenMusicFolder());
    }

    public RelayCommand PlayPauseCommand { get; }
    public RelayCommand PreviousTrackCommand { get; }
    public RelayCommand NextTrackCommand { get; }
    public RelayCommand ToggleMuteCommand { get; }
    public RelayCommand VolumeDownCommand { get; }
    public RelayCommand VolumeUpCommand { get; }
    public RelayCommand OpenMusicFolderCommand { get; }
}
