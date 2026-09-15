using DesktopCommandCenter.Windows.Interop;

namespace DesktopCommandCenter.Windows.Hotkeys;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int ToggleSidebarId = 0x4443;
    private const int FocusSearchId = 0x4444;

    private nint _windowHandle;
    private bool _toggleRegistered;
    private bool _focusRegistered;

    public void RegisterDefaults(nint windowHandle)
    {
        if (windowHandle == 0)
        {
            return;
        }

        Unregister();
        _windowHandle = windowHandle;

        _toggleRegistered = NativeMethods.RegisterHotKey(
            _windowHandle,
            ToggleSidebarId,
            NativeMethods.ModControl | NativeMethods.ModNoRepeat,
            NativeMethods.VkSpace);

        _focusRegistered = NativeMethods.RegisterHotKey(
            _windowHandle,
            FocusSearchId,
            NativeMethods.ModControl | NativeMethods.ModShift | NativeMethods.ModNoRepeat,
            NativeMethods.VkSpace);
    }

    public void Unregister()
    {
        if (_windowHandle != 0)
        {
            if (_toggleRegistered)
            {
                _ = NativeMethods.UnregisterHotKey(_windowHandle, ToggleSidebarId);
            }

            if (_focusRegistered)
            {
                _ = NativeMethods.UnregisterHotKey(_windowHandle, FocusSearchId);
            }
        }

        _toggleRegistered = false;
        _focusRegistered = false;
        _windowHandle = 0;
    }

    public bool IsToggleSidebarMessage(int message, nint wParam) =>
        message == NativeMethods.WmHotkey && wParam == ToggleSidebarId;

    public bool IsFocusSearchMessage(int message, nint wParam) =>
        message == NativeMethods.WmHotkey && wParam == FocusSearchId;

    public void Dispose() => Unregister();
}
