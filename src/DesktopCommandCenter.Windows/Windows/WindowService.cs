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

public sealed record WindowReflowSnapshot(
    nint Handle,
    int Left,
    int Top,
    int Width,
    int Height,
    bool WasMaximized,
    bool WasMinimized);

public sealed class WindowService
{
    private readonly uint _currentProcessId = (uint)Environment.ProcessId;

    public IReadOnlyList<WindowInfo> GetWindows()
    {
        var windows = new List<WindowInfo>();

        try
        {
            NativeMethods.EnumWindows((handle, _) =>
            {
                try
                {
                    var info = TryGetWindowInfo(handle);
                    if (info is not null)
                    {
                        windows.Add(info);
                    }
                }
                catch
                {
                    // One unusual/protected window must never take down the Windows page.
                }

                return true;
            }, 0);
        }
        catch
        {
            return [];
        }

        return windows
            .GroupBy(window => window.Handle)
            .Select(group => group.First())
            .OrderBy(window => window.ProcessName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(window => window.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public WindowInfo? GetForegroundWindowInfo()
    {
        try
        {
            var handle = NativeMethods.GetForegroundWindow();
            return handle == 0 ? null : TryGetWindowInfo(handle);
        }
        catch
        {
            return null;
        }
    }

    public WindowInfo? TryGetWindowInfo(nint handle)
    {
        try
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
        catch
        {
            return null;
        }
    }

    public bool Activate(nint handle) => SafeWindowAction(handle, () =>
    {
        _ = NativeMethods.ShowWindow(handle, NativeMethods.SwRestore);
        return NativeMethods.SetForegroundWindow(handle);
    });

    public bool Minimize(nint handle) =>
        SafeWindowAction(handle, () => NativeMethods.ShowWindow(handle, NativeMethods.SwMinimize));

    public bool Maximize(nint handle) =>
        SafeWindowAction(handle, () => NativeMethods.ShowWindow(handle, NativeMethods.SwMaximize));

    public bool Restore(nint handle) =>
        SafeWindowAction(handle, () => NativeMethods.ShowWindow(handle, NativeMethods.SwRestore));

    public bool Close(nint handle) =>
        SafeWindowAction(handle, () => NativeMethods.PostMessage(handle, NativeMethods.WmClose, 0, 0));

    public bool IsAlwaysOnTop(nint handle)
    {
        try
        {
            if (!NativeMethods.IsWindow(handle))
            {
                return false;
            }

            var styles = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GwlExStyle).ToInt64();
            return (styles & NativeMethods.WsExTopmost) != 0;
        }
        catch
        {
            return false;
        }
    }

    public bool SetAlwaysOnTop(nint handle, bool enabled) =>
        SafeWindowAction(handle, () =>
            NativeMethods.SetWindowPos(
                handle,
                enabled ? NativeMethods.HwndTopmost : NativeMethods.HwndNoTopmost,
                0,
                0,
                0,
                0,
                NativeMethods.SwpNoMove |
                NativeMethods.SwpNoSize |
                NativeMethods.SwpNoActivate));

    public int GetOpacity(nint handle)
    {
        try
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
        catch
        {
            return 100;
        }
    }

    public bool SetOpacity(nint handle, int percent) =>
        SafeWindowAction(handle, () =>
        {
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
        });

    public bool Snap(nint handle, WindowSnapPosition position)
    {
        try
        {
            if (!TryGetMonitorInfoForWindow(handle, out var info))
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
        catch
        {
            return false;
        }
    }

    public bool Center(nint handle)
    {
        try
        {
            if (!TryGetMonitorInfoForWindow(handle, out var info) ||
                !NativeMethods.GetWindowRect(handle, out var rect))
            {
                return false;
            }

            _ = NativeMethods.ShowWindow(handle, NativeMethods.SwRestore);

            var workWidth = info.Work.Right - info.Work.Left;
            var workHeight = info.Work.Bottom - info.Work.Top;
            var currentWidth = Math.Max(200, rect.Right - rect.Left);
            var currentHeight = Math.Max(120, rect.Bottom - rect.Top);
            var width = Math.Min(currentWidth, workWidth);
            var height = Math.Min(currentHeight, workHeight);
            var x = info.Work.Left + (workWidth - width) / 2;
            var y = info.Work.Top + (workHeight - height) / 2;

            return NativeMethods.SetWindowPos(
                handle,
                0,
                x,
                y,
                width,
                height,
                NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow);
        }
        catch
        {
            return false;
        }
    }

    public bool MoveToNextMonitor(nint handle)
    {
        try
        {
            if (!NativeMethods.IsWindow(handle) ||
                !NativeMethods.GetWindowRect(handle, out var rect))
            {
                return false;
            }

            var monitors = GetMonitors();
            if (monitors.Count < 2)
            {
                return false;
            }

            var currentMonitor = NativeMethods.MonitorFromWindow(handle, NativeMethods.MonitorDefaultToNearest);
            var currentIndex = monitors.FindIndex(monitor => monitor.Handle == currentMonitor);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            var target = monitors[(currentIndex + 1) % monitors.Count];
            _ = NativeMethods.ShowWindow(handle, NativeMethods.SwRestore);

            var width = Math.Min(Math.Max(200, rect.Right - rect.Left), target.Work.Right - target.Work.Left);
            var height = Math.Min(Math.Max(120, rect.Bottom - rect.Top), target.Work.Bottom - target.Work.Top);
            var x = target.Work.Left + ((target.Work.Right - target.Work.Left) - width) / 2;
            var y = target.Work.Top + ((target.Work.Bottom - target.Work.Top) - height) / 2;

            return NativeMethods.SetWindowPos(
                handle,
                0,
                x,
                y,
                width,
                height,
                NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow);
        }
        catch
        {
            return false;
        }
    }

    public IReadOnlyList<WindowReflowSnapshot> ReflowAllForSidebar(double sidebarWidthDip)
    {
        var snapshots = new List<WindowReflowSnapshot>();

        foreach (var window in GetWindows())
        {
            var snapshot = ReflowForSidebar(window.Handle, sidebarWidthDip);
            if (snapshot is not null)
            {
                snapshots.Add(snapshot);
            }
        }

        return snapshots;
    }

    public void RestoreReflows(IEnumerable<WindowReflowSnapshot> snapshots)
    {
        foreach (var snapshot in snapshots.Reverse())
        {
            _ = RestoreReflow(snapshot);
        }
    }

    public WindowReflowSnapshot? ReflowForSidebar(nint handle, double sidebarWidthDip)
    {
        try
        {
            if (!IsUserWindow(handle) ||
                NativeMethods.IsIconic(handle) ||
                !NativeMethods.GetWindowRect(handle, out var rect))
            {
                return null;
            }

            var primaryMonitor = NativeMethods.MonitorFromPoint(
                new NativeMethods.Point(0, 0),
                NativeMethods.MonitorDefaultToPrimary);

            var windowMonitor = NativeMethods.MonitorFromWindow(
                handle,
                NativeMethods.MonitorDefaultToNearest);

            if (primaryMonitor == 0 || windowMonitor != primaryMonitor)
            {
                return null;
            }

            var monitorInfo = new NativeMethods.MonitorInfo
            {
                Size = (uint)Marshal.SizeOf<NativeMethods.MonitorInfo>()
            };

            if (!NativeMethods.GetMonitorInfo(primaryMonitor, ref monitorInfo))
            {
                return null;
            }

            var wasMaximized = NativeMethods.IsZoomed(handle);
            var wasMinimized = NativeMethods.IsIconic(handle);

            var snapshot = new WindowReflowSnapshot(
                handle,
                rect.Left,
                rect.Top,
                Math.Max(1, rect.Right - rect.Left),
                Math.Max(1, rect.Bottom - rect.Top),
                wasMaximized,
                wasMinimized);

            var dpi = Math.Max(96u, NativeMethods.GetDpiForSystem());
            var sidebarWidthPixels = (int)Math.Ceiling(sidebarWidthDip * dpi / 96d);
            var availableLeft = monitorInfo.Work.Left;
            var availableTop = monitorInfo.Work.Top;
            var availableRight = monitorInfo.Work.Right - sidebarWidthPixels;
            var availableBottom = monitorInfo.Work.Bottom;
            var availableWidth = availableRight - availableLeft;
            var availableHeight = availableBottom - availableTop;

            if (availableWidth < 320 || availableHeight < 200)
            {
                return null;
            }

            var rightFrameInset = 0;
            try
            {
                if (NativeMethods.DwmGetWindowAttribute(
                        handle,
                        NativeMethods.DwmwaExtendedFrameBounds,
                        out NativeMethods.Rect frameRect,
                        Marshal.SizeOf<NativeMethods.Rect>()) == 0)
                {
                    rightFrameInset = Math.Clamp(rect.Right - frameRect.Right, 0, 32);
                }
            }
            catch
            {
                rightFrameInset = 0;
            }

            _ = NativeMethods.ShowWindow(handle, NativeMethods.SwRestore);

            int targetX;
            int targetY;
            int targetWidth;
            int targetHeight;

            if (wasMaximized)
            {
                targetX = availableLeft;
                targetY = availableTop;
                targetWidth = availableWidth + rightFrameInset;
                targetHeight = availableHeight;
            }
            else
            {
                targetWidth = Math.Min(snapshot.Width, availableWidth + rightFrameInset);
                targetHeight = Math.Min(snapshot.Height, availableHeight);

                var maximumLeft = availableRight + rightFrameInset - targetWidth;
                targetX = Math.Clamp(snapshot.Left, availableLeft, Math.Max(availableLeft, maximumLeft));
                targetY = Math.Clamp(
                    snapshot.Top,
                    availableTop,
                    Math.Max(availableTop, availableBottom - targetHeight));
            }

            if (!NativeMethods.SetWindowPos(
                    handle,
                    0,
                    targetX,
                    targetY,
                    targetWidth,
                    targetHeight,
                    NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow))
            {
                if (wasMaximized)
                {
                    _ = NativeMethods.ShowWindow(handle, NativeMethods.SwMaximize);
                }

                return null;
            }

            return snapshot;
        }
        catch
        {
            return null;
        }
    }

    public bool RestoreReflow(WindowReflowSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return true;
        }

        try
        {
            if (!NativeMethods.IsWindow(snapshot.Handle))
            {
                return false;
            }

            _ = NativeMethods.ShowWindow(snapshot.Handle, NativeMethods.SwRestore);

            var restored = NativeMethods.SetWindowPos(
                snapshot.Handle,
                0,
                snapshot.Left,
                snapshot.Top,
                snapshot.Width,
                snapshot.Height,
                NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow);

            if (snapshot.WasMaximized)
            {
                _ = NativeMethods.ShowWindow(snapshot.Handle, NativeMethods.SwMaximize);
            }
            else if (snapshot.WasMinimized)
            {
                _ = NativeMethods.ShowWindow(snapshot.Handle, NativeMethods.SwMinimize);
            }

            return restored;
        }
        catch
        {
            return false;
        }
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
        catch
        {
            // Visibility/title filtering is enough if DWM inspection is unavailable.
        }

        return NativeMethods.GetWindowTextLength(handle) > 0;
    }

    private static bool TryGetMonitorInfoForWindow(nint handle, out NativeMethods.MonitorInfo info)
    {
        info = new NativeMethods.MonitorInfo
        {
            Size = (uint)Marshal.SizeOf<NativeMethods.MonitorInfo>()
        };

        if (!NativeMethods.IsWindow(handle))
        {
            return false;
        }

        var monitor = NativeMethods.MonitorFromWindow(handle, NativeMethods.MonitorDefaultToNearest);
        return monitor != 0 && NativeMethods.GetMonitorInfo(monitor, ref info);
    }

    private static List<MonitorSnapshot> GetMonitors()
    {
        var monitors = new List<MonitorSnapshot>();

        NativeMethods.EnumDisplayMonitors(0, 0, (handle, _, _, _) =>
        {
            var info = new NativeMethods.MonitorInfo
            {
                Size = (uint)Marshal.SizeOf<NativeMethods.MonitorInfo>()
            };

            if (NativeMethods.GetMonitorInfo(handle, ref info))
            {
                monitors.Add(new MonitorSnapshot(handle, info.Work));
            }

            return true;
        }, 0);

        return monitors;
    }

    private static bool SafeWindowAction(nint handle, Func<bool> action)
    {
        try
        {
            return NativeMethods.IsWindow(handle) && action();
        }
        catch
        {
            return false;
        }
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

    private sealed record MonitorSnapshot(nint Handle, NativeMethods.Rect Work);
}
