using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using DesktopCommandCenter.Windows.Interop;

namespace DesktopCommandCenter.Windows.Windows;

public enum WindowSnapPosition
{
    Left,
    Right
}

public sealed class WindowService
{
    private readonly uint _currentProcessId = (uint)Environment.ProcessId;

    public IReadOnlyList<WindowInfo> GetWindows()
    {
        var windows = new List<WindowInfo>();

        NativeMethods.EnumWindows((handle, _) =>
        {
            var info = TryGetWindowInfo(handle);
            if (info is not null)
            {
                windows.Add(info);
            }

            return true;
        }, 0);

        return windows
            .OrderBy(window => window.ProcessName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(window => window.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public WindowInfo? GetForegroundWindowInfo()
    {
        var handle = NativeMethods.GetForegroundWindow();
        return handle == 0 ? null : TryGetWindowInfo(handle);
    }

    public WindowInfo? TryGetWindowInfo(nint handle)
    {
        if (!IsUserWindow(handle))
        {
            return null;
        }

        var length = NativeMethods.GetWindowTextLength(handle);
        if (length <= 0)
        {
            return null;
        }

        var titleBuilder = new StringBuilder(length + 1);
        _ = NativeMethods.GetWindowText(handle, titleBuilder, titleBuilder.Capacity);
        var title = titleBuilder.ToString().Trim();
        if (title.Length == 0)
        {
            return null;
        }

        _ = NativeMethods.GetWindowThreadProcessId(handle, out var processId);
        if (processId == 0 || processId == _currentProcessId)
        {
            return null;
        }

        return new WindowInfo(
            handle,
            title,
            processId,
            GetProcessName(processId));
    }

    public bool Activate(nint handle)
    {
        if (!NativeMethods.IsWindow(handle))
        {
            return false;
        }

        _ = NativeMethods.ShowWindow(handle, NativeMethods.SwRestore);
        return NativeMethods.SetForegroundWindow(handle);
    }

    public bool Minimize(nint handle) =>
        NativeMethods.IsWindow(handle) &&
        NativeMethods.ShowWindow(handle, NativeMethods.SwMinimize);

    public bool Maximize(nint handle) =>
        NativeMethods.IsWindow(handle) &&
        NativeMethods.ShowWindow(handle, NativeMethods.SwMaximize);

    public bool Restore(nint handle) =>
        NativeMethods.IsWindow(handle) &&
        NativeMethods.ShowWindow(handle, NativeMethods.SwRestore);

    public bool Close(nint handle) =>
        NativeMethods.IsWindow(handle) &&
        NativeMethods.PostMessage(handle, NativeMethods.WmClose, 0, 0);

    public bool IsAlwaysOnTop(nint handle)
    {
        if (!NativeMethods.IsWindow(handle))
        {
            return false;
        }

        var styles = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GwlExStyle).ToInt64();
        return (styles & NativeMethods.WsExTopmost) != 0;
    }

    public bool SetAlwaysOnTop(nint handle, bool enabled)
    {
        if (!NativeMethods.IsWindow(handle))
        {
            return false;
        }

        return NativeMethods.SetWindowPos(
            handle,
            enabled ? NativeMethods.HwndTopmost : NativeMethods.HwndNoTopmost,
            0,
            0,
            0,
            0,
            NativeMethods.SwpNoMove |
            NativeMethods.SwpNoSize |
            NativeMethods.SwpNoActivate);
    }

    public int GetOpacity(nint handle)
    {
        if (!NativeMethods.IsWindow(handle))
        {
            return 100;
        }

        var styles = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GwlExStyle).ToInt64();
        if ((styles & NativeMethods.WsExLayered) == 0)
        {
            return 100;
        }

        if (!NativeMethods.GetLayeredWindowAttributes(handle, out _, out var alpha, out var flags) ||
            (flags & NativeMethods.LwaAlpha) == 0)
        {
            return 100;
        }

        return Math.Clamp((int)Math.Round(alpha / 255d * 100d), 20, 100);
    }

    public bool SetOpacity(nint handle, int percent)
    {
        if (!NativeMethods.IsWindow(handle))
        {
            return false;
        }

        percent = Math.Clamp(percent, 20, 100);
        var styles = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GwlExStyle).ToInt64();
        if ((styles & NativeMethods.WsExLayered) == 0)
        {
            _ = NativeMethods.SetWindowLongPtr(
                handle,
                NativeMethods.GwlExStyle,
                (nint)(styles | NativeMethods.WsExLayered));
        }

        var alpha = (byte)Math.Clamp((int)Math.Round(percent / 100d * 255d), 51, 255);
        return NativeMethods.SetLayeredWindowAttributes(handle, 0, alpha, NativeMethods.LwaAlpha);
    }

    public bool Snap(nint handle, WindowSnapPosition position)
    {
        if (!NativeMethods.IsWindow(handle))
        {
            return false;
        }

        var monitor = NativeMethods.MonitorFromWindow(handle, NativeMethods.MonitorDefaultToNearest);
        if (monitor == 0)
        {
            return false;
        }

        var info = new NativeMethods.MonitorInfo
        {
            Size = (uint)Marshal.SizeOf<NativeMethods.MonitorInfo>()
        };

        if (!NativeMethods.GetMonitorInfo(monitor, ref info))
        {
            return false;
        }

        _ = NativeMethods.ShowWindow(handle, NativeMethods.SwRestore);

        var width = info.Work.Right - info.Work.Left;
        var height = info.Work.Bottom - info.Work.Top;
        var halfWidth = width / 2;
        var x = position == WindowSnapPosition.Left
            ? info.Work.Left
            : info.Work.Right - halfWidth;

        return NativeMethods.SetWindowPos(
            handle,
            0,
            x,
            info.Work.Top,
            halfWidth,
            height,
            NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow);
    }

    private bool IsUserWindow(nint handle)
    {
        if (handle == 0 ||
            !NativeMethods.IsWindow(handle) ||
            !NativeMethods.IsWindowVisible(handle))
        {
            return false;
        }

        _ = NativeMethods.GetWindowThreadProcessId(handle, out var processId);
        if (processId == 0 || processId == _currentProcessId)
        {
            return false;
        }

        var owner = NativeMethods.GetWindow(handle, NativeMethods.GwOwner);
        var exStyles = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GwlExStyle).ToInt64();
        if (owner != 0 || (exStyles & NativeMethods.WsExToolWindow) != 0)
        {
            return false;
        }

        try
        {
            if (NativeMethods.DwmGetWindowAttribute(
                    handle,
                    NativeMethods.DwmwaCloaked,
                    out var cloaked,
                    sizeof(int)) == 0 &&
                cloaked != 0)
            {
                return false;
            }
        }
        catch (DllNotFoundException)
        {
            // DWM is available on supported Windows versions; if not, visibility filtering is enough.
        }

        return NativeMethods.GetWindowTextLength(handle) > 0;
    }

    private static string GetProcessName(uint processId)
    {
        try
        {
            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch
        {
            return "App";
        }
    }
}
