namespace DesktopCommandCenter.Windows.Windows;

public sealed record WindowInfo(
    nint Handle,
    string Title,
    uint ProcessId,
    string ProcessName);
