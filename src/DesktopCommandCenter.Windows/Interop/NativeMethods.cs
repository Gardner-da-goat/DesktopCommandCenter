using System.Runtime.InteropServices;
using System.Text;

namespace DesktopCommandCenter.Windows.Interop;

internal static class NativeMethods
{
    internal const uint MonitorDefaultToPrimary = 1;
    internal const uint MonitorDefaultToNearest = 2;
    internal const uint GwOwner = 4;
    internal const int GwlExStyle = -20;
    internal const long WsExTopmost = 0x00000008L;
    internal const long WsExToolWindow = 0x00000080L;
    internal const long WsExLayered = 0x00080000L;
    internal const uint LwaAlpha = 0x00000002;
    internal const uint WmClose = 0x0010;
    internal const int WmHotkey = 0x0312;
    internal const uint ModControl = 0x0002;
    internal const uint ModShift = 0x0004;
    internal const uint ModNoRepeat = 0x4000;
    internal const uint VkSpace = 0x20;
    internal const byte VkVolumeMute = 0xAD;
    internal const byte VkVolumeDown = 0xAE;
    internal const byte VkVolumeUp = 0xAF;
    internal const byte VkMediaPlayPause = 0xB3;
    internal const byte VkLWin = 0x5B;
    internal const byte VkV = 0x56;
    internal const uint KeyeventfKeyup = 0x0002;
    internal const int SwMinimize = 6;
    internal const int SwMaximize = 3;
    internal const int SwRestore = 9;
    internal const uint SwpNoSize = 0x0001;
    internal const uint SwpNoMove = 0x0002;
    internal const uint SwpNoActivate = 0x0010;
    internal const uint SwpShowWindow = 0x0040;
    internal const int DwmwaExtendedFrameBounds = 9;
    internal const int DwmwaCloaked = 14;

    internal static readonly nint HwndTopmost = new(-1);
    internal static readonly nint HwndNoTopmost = new(-2);

    internal delegate bool EnumWindowsProc(nint hWnd, nint lParam);
    internal delegate bool MonitorEnumProc(nint hMonitor, nint hdcMonitor, nint rect, nint data);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EnumDisplayMonitors(
        nint hdc,
        nint clipRect,
        MonitorEnumProc callback,
        nint data);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWindow(nint hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsZoomed(nint hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsIconic(nint hWnd);

    [DllImport("user32.dll")]
    internal static extern int GetWindowTextLength(nint hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    internal static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);

    [DllImport("user32.dll")]
    internal static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool PostMessage(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    internal static extern nint GetWindow(nint hWnd, uint uCmd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetWindowRect(nint hWnd, out Rect rect);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetWindowPos(
        nint hWnd,
        nint hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetLayeredWindowAttributes(
        nint hWnd,
        uint colorKey,
        byte alpha,
        uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetLayeredWindowAttributes(
        nint hWnd,
        out uint colorKey,
        out byte alpha,
        out uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnregisterHotKey(nint hWnd, int id);

    [DllImport("user32.dll")]
    internal static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, nuint extraInfo);

    [DllImport("user32.dll")]
    internal static extern nint MonitorFromPoint(Point point, uint flags);

    [DllImport("user32.dll")]
    internal static extern nint MonitorFromWindow(nint hWnd, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo monitorInfo);

    [DllImport("user32.dll")]
    internal static extern uint GetDpiForSystem();

    [DllImport("dwmapi.dll")]
    internal static extern int DwmGetWindowAttribute(
        nint hWnd,
        int attribute,
        out int value,
        int valueSize);

    [DllImport("dwmapi.dll")]
    internal static extern int DwmGetWindowAttribute(
        nint hWnd,
        int attribute,
        out Rect value,
        int valueSize);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr64(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong32(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint SetWindowLongPtr64(nint hWnd, int nIndex, nint newLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong32(nint hWnd, int nIndex, int newLong);

    internal static nint GetWindowLongPtr(nint hWnd, int nIndex) =>
        IntPtr.Size == 8
            ? GetWindowLongPtr64(hWnd, nIndex)
            : (nint)GetWindowLong32(hWnd, nIndex);

    internal static nint SetWindowLongPtr(nint hWnd, int nIndex, nint newLong) =>
        IntPtr.Size == 8
            ? SetWindowLongPtr64(hWnd, nIndex, newLong)
            : (nint)SetWindowLong32(hWnd, nIndex, (int)newLong);

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct Point
    {
        internal Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        internal int X { get; }
        internal int Y { get; }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct MonitorInfo
    {
        internal uint Size;
        internal Rect Monitor;
        internal Rect Work;
        internal uint Flags;
    }
}
