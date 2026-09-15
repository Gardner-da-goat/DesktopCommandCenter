namespace DesktopCommandCenter.App.Services;

public sealed record UpdateInfo(
    Version Version,
    string VersionText,
    string DownloadUrl,
    string ReleasePageUrl,
    string ReleaseNotes);
