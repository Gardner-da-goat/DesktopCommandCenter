using DesktopCommandCenter.Windows.Interop;

namespace DesktopCommandCenter.Windows.Hotkeys;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int ToggleSidebarId = 0x4443;
    private const int FocusSearchId = 0x4444;

    private nint _windowHandle;
    private bool _toggleRegistered;
    private bool _focusRegistered;

    public void RegisterDefaults(
        nint windowHandle,
        string? togglePreset,
        string? searchPreset)
    {
        if (windowHandle == 0)
        {
            return;
        }

        Unregister();
        _windowHandle = windowHandle;

        var toggle = ResolveToggle(togglePreset);
        var search = ResolveSearch(searchPreset);

        _toggleRegistered = NativeMethods.RegisterHotKey(
            _windowHandle,
            ToggleSidebarId,
            toggle.Modifiers | NativeMethods.ModNoRepeat,
            toggle.VirtualKey);

        _focusRegistered = NativeMethods.RegisterHotKey(
            _windowHandle,
            FocusSearchId,
            search.Modifiers | NativeMethods.ModNoRepeat,
            search.VirtualKey);
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

    private static HotkeyDefinition ResolveToggle(string? preset) =>
        preset switch
        {
            "Ctrl+Alt+Space" => new HotkeyDefinition(
                NativeMethods.ModControl | NativeMethods.ModAlt,
                NativeMethods.VkSpace),
            "Ctrl+Alt+D" => new HotkeyDefinition(
                NativeMethods.ModControl | NativeMethods.ModAlt,
                NativeMethods.VkD),
            _ => new HotkeyDefinition(
                NativeMethods.ModControl,
                NativeMethods.VkSpace)
        };

    private static HotkeyDefinition ResolveSearch(string? preset) =>
        preset switch
        {
            "Ctrl+Shift+F" => new HotkeyDefinition(
                NativeMethods.ModControl | NativeMethods.ModShift,
                NativeMethods.VkF),
            "Ctrl+Alt+F" => new HotkeyDefinition(
                NativeMethods.ModControl | NativeMethods.ModAlt,
                NativeMethods.VkF),
            _ => new HotkeyDefinition(
                NativeMethods.ModControl | NativeMethods.ModShift,
                NativeMethods.VkSpace)
        };

    private readonly record struct HotkeyDefinition(uint Modifiers, uint VirtualKey);
}
