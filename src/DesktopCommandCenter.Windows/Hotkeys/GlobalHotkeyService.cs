using DesktopCommandCenter.Windows.Interop;

namespace DesktopCommandCenter.Windows.Hotkeys;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int ToggleSidebarId = 0x4443;
    private const int FocusSearchId = 0x4444;
    private const int SnapLeftId = 0x4445;
    private const int SnapRightId = 0x4446;
    private const int ToggleTopmostId = 0x4447;
    private const int OpacityUpId = 0x4448;
    private const int OpacityDownId = 0x4449;

    private readonly HashSet<int> _registeredIds = [];
    private nint _windowHandle;

    public void RegisterDefaults(
        nint windowHandle,
        string? togglePreset,
        string? searchPreset,
        bool windowControlHotkeysEnabled)
    {
        if (windowHandle == 0)
        {
            return;
        }

        Unregister();
        _windowHandle = windowHandle;

        var toggle = ResolveToggle(togglePreset);
        var search = ResolveSearch(searchPreset);

        Register(
            ToggleSidebarId,
            toggle.Modifiers,
            toggle.VirtualKey);

        Register(
            FocusSearchId,
            search.Modifiers,
            search.VirtualKey);

        if (!windowControlHotkeysEnabled)
        {
            return;
        }

        var windowModifiers =
            NativeMethods.ModControl |
            NativeMethods.ModAlt;

        Register(SnapLeftId, windowModifiers, NativeMethods.VkLeft);
        Register(SnapRightId, windowModifiers, NativeMethods.VkRight);
        Register(ToggleTopmostId, windowModifiers, NativeMethods.VkT);
        Register(OpacityUpId, windowModifiers, NativeMethods.VkUp);
        Register(OpacityDownId, windowModifiers, NativeMethods.VkDown);
    }

    public void Unregister()
    {
        if (_windowHandle != 0)
        {
            foreach (var id in _registeredIds)
            {
                _ = NativeMethods.UnregisterHotKey(_windowHandle, id);
            }
        }

        _registeredIds.Clear();
        _windowHandle = 0;
    }

    public bool IsToggleSidebarMessage(int message, nint wParam) =>
        IsMessage(message, wParam, ToggleSidebarId);

    public bool IsFocusSearchMessage(int message, nint wParam) =>
        IsMessage(message, wParam, FocusSearchId);

    public bool IsSnapLeftMessage(int message, nint wParam) =>
        IsMessage(message, wParam, SnapLeftId);

    public bool IsSnapRightMessage(int message, nint wParam) =>
        IsMessage(message, wParam, SnapRightId);

    public bool IsToggleTopmostMessage(int message, nint wParam) =>
        IsMessage(message, wParam, ToggleTopmostId);

    public bool IsOpacityUpMessage(int message, nint wParam) =>
        IsMessage(message, wParam, OpacityUpId);

    public bool IsOpacityDownMessage(int message, nint wParam) =>
        IsMessage(message, wParam, OpacityDownId);

    public void Dispose() => Unregister();

    private void Register(int id, uint modifiers, uint virtualKey)
    {
        if (NativeMethods.RegisterHotKey(
                _windowHandle,
                id,
                modifiers | NativeMethods.ModNoRepeat,
                virtualKey))
        {
            _registeredIds.Add(id);
        }
    }

    private static bool IsMessage(int message, nint wParam, int id) =>
        message == NativeMethods.WmHotkey && wParam == id;

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

    private readonly record struct HotkeyDefinition(
        uint Modifiers,
        uint VirtualKey);
}
