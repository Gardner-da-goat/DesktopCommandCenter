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
        string? toggleHotkey,
        string? searchHotkey,
        bool windowControlHotkeysEnabled,
        string? snapLeftHotkey,
        string? snapRightHotkey,
        string? toggleTopmostHotkey,
        string? opacityUpHotkey,
        string? opacityDownHotkey)
    {
        if (windowHandle == 0)
        {
            return;
        }

        Unregister();
        _windowHandle = windowHandle;

        RegisterIfValid(ToggleSidebarId, toggleHotkey);
        RegisterIfValid(FocusSearchId, searchHotkey);

        if (!windowControlHotkeysEnabled)
        {
            return;
        }

        RegisterIfValid(SnapLeftId, snapLeftHotkey);
        RegisterIfValid(SnapRightId, snapRightHotkey);
        RegisterIfValid(ToggleTopmostId, toggleTopmostHotkey);
        RegisterIfValid(OpacityUpId, opacityUpHotkey);
        RegisterIfValid(OpacityDownId, opacityDownHotkey);
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

    public static bool IsSupportedHotkey(string? text) =>
        TryParseHotkey(text, out _);

    private void RegisterIfValid(int id, string? text)
    {
        if (!TryParseHotkey(text, out var hotkey))
        {
            return;
        }

        if (NativeMethods.RegisterHotKey(
                _windowHandle,
                id,
                hotkey.Modifiers | NativeMethods.ModNoRepeat,
                hotkey.VirtualKey))
        {
            _registeredIds.Add(id);
        }
    }

    private static bool IsMessage(int message, nint wParam, int id) =>
        message == NativeMethods.WmHotkey && wParam == id;

    private static bool TryParseHotkey(
        string? text,
        out HotkeyDefinition hotkey)
    {
        hotkey = default;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var parts = text
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

        if (parts.Length < 2)
        {
            return false;
        }

        uint modifiers = 0;
        uint virtualKey = 0;
        var hasKey = false;

        foreach (var rawPart in parts)
        {
            var part = rawPart.Trim();

            if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                part.Equals("Control", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= NativeMethods.ModControl;
                continue;
            }

            if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= NativeMethods.ModAlt;
                continue;
            }

            if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= NativeMethods.ModShift;
                continue;
            }

            if (part.Equals("Win", StringComparison.OrdinalIgnoreCase) ||
                part.Equals("Windows", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= NativeMethods.ModWin;
                continue;
            }

            if (hasKey || !TryParseVirtualKey(part, out virtualKey))
            {
                return false;
            }

            hasKey = true;
        }

        if (!hasKey || modifiers == 0)
        {
            return false;
        }

        hotkey = new HotkeyDefinition(modifiers, virtualKey);
        return true;
    }

    private static bool TryParseVirtualKey(string keyText, out uint virtualKey)
    {
        virtualKey = 0;
        var key = keyText.Trim();

        if (key.Length == 1)
        {
            var character = char.ToUpperInvariant(key[0]);
            if (character is >= 'A' and <= 'Z' ||
                character is >= '0' and <= '9')
            {
                virtualKey = character;
                return true;
            }
        }

        if (key.Length is 2 or 3 &&
            key[0] is 'F' or 'f' &&
            int.TryParse(key[1..], out var functionKey) &&
            functionKey is >= 1 and <= 12)
        {
            virtualKey = (uint)(0x70 + functionKey - 1);
            return true;
        }

        virtualKey = key.ToLowerInvariant() switch
        {
            "space" => 0x20,
            "left" => 0x25,
            "up" => 0x26,
            "right" => 0x27,
            "down" => 0x28,
            "enter" or "return" => 0x0D,
            "tab" => 0x09,
            "escape" or "esc" => 0x1B,
            "home" => 0x24,
            "end" => 0x23,
            "pageup" or "pgup" => 0x21,
            "pagedown" or "pgdn" => 0x22,
            "insert" or "ins" => 0x2D,
            "delete" or "del" => 0x2E,
            _ => 0
        };

        return virtualKey != 0;
    }

    private readonly record struct HotkeyDefinition(
        uint Modifiers,
        uint VirtualKey);
}
