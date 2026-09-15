using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using DesktopCommandCenter.Windows.Interop;

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

    public bool OpenScreenshot()
    {
        if (Start("ms-screenclip:"))
        {
            return true;
        }

        return Start("snippingtool.exe");
    }

    public bool ToggleMute() => SendMediaKey(NativeMethods.VkVolumeMute);
    public bool VolumeUp() => SendMediaKey(NativeMethods.VkVolumeUp);
    public bool VolumeDown() => SendMediaKey(NativeMethods.VkVolumeDown);
    public bool PlayPause() => SendMediaKey(NativeMethods.VkMediaPlayPause);
    public bool NextTrack() => SendMediaKey(NativeMethods.VkMediaNextTrack);
    public bool PreviousTrack() => SendMediaKey(NativeMethods.VkMediaPreviousTrack);

    public bool OpenClipboardHistory()
    {
        try
        {
            NativeMethods.keybd_event(NativeMethods.VkLWin, 0, 0, 0);
            NativeMethods.keybd_event(NativeMethods.VkV, 0, 0, 0);
            NativeMethods.keybd_event(NativeMethods.VkV, 0, NativeMethods.KeyeventfKeyup, 0);
            NativeMethods.keybd_event(NativeMethods.VkLWin, 0, NativeMethods.KeyeventfKeyup, 0);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool SearchWeb(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        var url = "https://www.google.com/search?q=" + Uri.EscapeDataString(query.Trim());
        return OpenPath(url);
    }

    private static bool SendMediaKey(byte virtualKey)
    {
        try
        {
            NativeMethods.keybd_event(virtualKey, 0, 0, 0);
            NativeMethods.keybd_event(virtualKey, 0, NativeMethods.KeyeventfKeyup, 0);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool OpenTerminal()
    {
        if (Start("wt.exe"))
        {
            return true;
        }

        return Start("powershell.exe");
    }

    public bool EnsureDesktopShortcut(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
        {
            return false;
        }

        object? shell = null;
        object? shortcut = null;

        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (string.IsNullOrWhiteSpace(desktop))
            {
                return false;
            }

            Directory.CreateDirectory(desktop);
            var shortcutPath = Path.Combine(desktop, "Desktop Command Center.lnk");

            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null)
            {
                return false;
            }

            shell = Activator.CreateInstance(shellType);
            if (shell is null)
            {
                return false;
            }

            shortcut = shellType.InvokeMember(
                "CreateShortcut",
                BindingFlags.InvokeMethod,
                null,
                shell,
                [shortcutPath]);

            if (shortcut is null)
            {
                return false;
            }

            var shortcutType = shortcut.GetType();
            var workingDirectory = Path.GetDirectoryName(executablePath) ?? AppContext.BaseDirectory;

            shortcutType.InvokeMember(
                "TargetPath",
                BindingFlags.SetProperty,
                null,
                shortcut,
                [executablePath]);

            shortcutType.InvokeMember(
                "WorkingDirectory",
                BindingFlags.SetProperty,
                null,
                shortcut,
                [workingDirectory]);

            shortcutType.InvokeMember(
                "Description",
                BindingFlags.SetProperty,
                null,
                shortcut,
                ["Desktop Command Center"]);

            shortcutType.InvokeMember(
                "IconLocation",
                BindingFlags.SetProperty,
                null,
                shortcut,
                [$"{executablePath},0"]);

            shortcutType.InvokeMember(
                "Save",
                BindingFlags.InvokeMethod,
                null,
                shortcut,
                null);

            return File.Exists(shortcutPath);
        }
        catch
        {
            return false;
        }
        finally
        {
            if (shortcut is not null && Marshal.IsComObject(shortcut))
            {
                Marshal.FinalReleaseComObject(shortcut);
            }

            if (shell is not null && Marshal.IsComObject(shell))
            {
                Marshal.FinalReleaseComObject(shell);
            }
        }
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
