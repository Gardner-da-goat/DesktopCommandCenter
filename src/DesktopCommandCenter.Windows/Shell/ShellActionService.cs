using System.Diagnostics;

namespace DesktopCommandCenter.Windows.Shell;

public sealed class ShellActionService
{
    public bool OpenDownloads()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");

        return OpenPath(path);
    }

    public bool OpenTaskManager() => Start("taskmgr.exe");

    public bool OpenWindowsSettings() => Start("ms-settings:");

    public bool OpenTerminal()
    {
        if (Start("wt.exe"))
        {
            return true;
        }

        return Start("powershell.exe");
    }

    public bool OpenPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo(path)
            {
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool Start(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target)
            {
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }
}
