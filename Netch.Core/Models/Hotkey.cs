using System.Runtime.Versioning;
using Windows.System;

namespace Netch.Models;

/// <summary>Values match the MOD_* flags of RegisterHotKey.</summary>
[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 0x1,
    Control = 0x2,
    Shift = 0x4,
    Windows = 0x8
}

/// <summary>A global hotkey, stored in <see cref="Scene.Hotkey" /> as text such as "Ctrl+Alt+1".</summary>
[SupportedOSPlatform("windows10.0.10240")]
public readonly record struct Hotkey(HotkeyModifiers Modifiers, VirtualKey Key)
{
    public override string ToString()
    {
        var parts = new List<string>();
        if (Modifiers.HasFlag(HotkeyModifiers.Control))
            parts.Add("Ctrl");
        if (Modifiers.HasFlag(HotkeyModifiers.Alt))
            parts.Add("Alt");
        if (Modifiers.HasFlag(HotkeyModifiers.Shift))
            parts.Add("Shift");
        if (Modifiers.HasFlag(HotkeyModifiers.Windows))
            parts.Add("Win");

        parts.Add(Key is >= VirtualKey.Number0 and <= VirtualKey.Number9 or >= VirtualKey.A and <= VirtualKey.Z
            ? ((char)Key).ToString()
            : Key.ToString());
        return string.Join("+", parts);
    }

    /// <summary>Requires at least one modifier, so plain typing never triggers a scene.</summary>
    public static bool TryParse(string? text, out Hotkey hotkey)
    {
        hotkey = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var modifiers = HotkeyModifiers.None;
        VirtualKey? key = null;
        foreach (var part in text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl":
                    modifiers |= HotkeyModifiers.Control;
                    break;
                case "alt":
                    modifiers |= HotkeyModifiers.Alt;
                    break;
                case "shift":
                    modifiers |= HotkeyModifiers.Shift;
                    break;
                case "win":
                    modifiers |= HotkeyModifiers.Windows;
                    break;
                default:
                    if (key != null || !TryParseKey(part, out var parsed))
                        return false;

                    key = parsed;
                    break;
            }
        }

        if (key == null || modifiers == HotkeyModifiers.None)
            return false;

        hotkey = new Hotkey(modifiers, key.Value);
        return true;
    }

    public static bool IsModifier(VirtualKey key) => key is VirtualKey.Control or VirtualKey.LeftControl or VirtualKey.RightControl
        or VirtualKey.Menu or VirtualKey.LeftMenu or VirtualKey.RightMenu
        or VirtualKey.Shift or VirtualKey.LeftShift or VirtualKey.RightShift
        or VirtualKey.LeftWindows or VirtualKey.RightWindows;

    private static bool TryParseKey(string text, out VirtualKey key)
    {
        key = default;
        if (text.Length == 1 && char.IsAsciiLetterOrDigit(text[0]))
        {
            key = (VirtualKey)char.ToUpperInvariant(text[0]);
            return true;
        }

        // Enum.TryParse also accepts numbers, which ToString never writes.
        return char.IsAsciiLetter(text[0]) && Enum.TryParse(text, true, out key) && !IsModifier(key);
    }
}
