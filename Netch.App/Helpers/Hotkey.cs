using Windows.System;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Netch.App.Helpers;

/// <summary>A global hotkey, stored in <see cref="Models.Scene.Hotkey" /> as text such as "Ctrl+Alt+1".</summary>
internal readonly record struct Hotkey(HOT_KEY_MODIFIERS Modifiers, VirtualKey Key)
{
    public override string ToString()
    {
        var parts = new List<string>();
        if (Modifiers.HasFlag(HOT_KEY_MODIFIERS.MOD_CONTROL))
            parts.Add("Ctrl");
        if (Modifiers.HasFlag(HOT_KEY_MODIFIERS.MOD_ALT))
            parts.Add("Alt");
        if (Modifiers.HasFlag(HOT_KEY_MODIFIERS.MOD_SHIFT))
            parts.Add("Shift");
        if (Modifiers.HasFlag(HOT_KEY_MODIFIERS.MOD_WIN))
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

        HOT_KEY_MODIFIERS modifiers = 0;
        VirtualKey? key = null;
        foreach (var part in text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl":
                    modifiers |= HOT_KEY_MODIFIERS.MOD_CONTROL;
                    break;
                case "alt":
                    modifiers |= HOT_KEY_MODIFIERS.MOD_ALT;
                    break;
                case "shift":
                    modifiers |= HOT_KEY_MODIFIERS.MOD_SHIFT;
                    break;
                case "win":
                    modifiers |= HOT_KEY_MODIFIERS.MOD_WIN;
                    break;
                default:
                    if (key != null || !TryParseKey(part, out var parsed))
                        return false;

                    key = parsed;
                    break;
            }
        }

        if (key == null || modifiers == 0)
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
        if (text.Length == 1 && char.IsAsciiLetterOrDigit(text[0]))
        {
            key = (VirtualKey)char.ToUpperInvariant(text[0]);
            return true;
        }

        return Enum.TryParse(text, true, out key) && !IsModifier(key);
    }
}
