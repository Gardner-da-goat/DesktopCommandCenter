using DesktopCommandCenter.Windows.Interop;

namespace DesktopCommandCenter.Windows.Hotkeys;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int ToggleSidebarId = 0x4443;
    private nint _windowHandle;
    private bool _registered;

    public bool RegisterToggleSidebar(nint windowHandle)
    {
        if (windowHandle == 0)
        {
            return false;
        }

        DisposeRegistration();

        _windowHandle = windowHandle;
        _registered = NativeMethods.RegisterHotKey(
            _windowHandle,
            ToggleSidebarId,
            NativeMethods.ModControl | NativeMethods.ModNoRepeat,
            NativeMethods.VkSpace);

        return _registered;
    }

    public bool IsToggleSidebarMessage(int message, nint wParam) =>
        message == NativeMethods.WmHotkey &&
        wParam == ToggleSidebarId;

    public void Dispose() => DisposeRegistration();

    private void DisposeRegistration()
    {
        if (_registered && _windowHandle != 0)
        {
            _ = NativeMethods.UnregisterHotKey(_windowHandle, ToggleSidebarId);
        }

        _registered = false;
        _windowHandle = 0;
    }
}
