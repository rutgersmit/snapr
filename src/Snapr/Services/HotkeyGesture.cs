using System;
using System.Collections.Generic;
using System.Windows.Input;
using Snapr.Interop;

namespace Snapr.Services;

/// <summary>A global keyboard shortcut, e.g. "PrintScreen" or "Ctrl+Shift+S".</summary>
public sealed record HotkeyGesture(ModifierKeys Modifiers, Key Key)
{
    public static readonly HotkeyGesture Default = new(ModifierKeys.None, Key.Snapshot);

    public uint NativeModifiers
    {
        get
        {
            uint mods = 0;
            if (Modifiers.HasFlag(ModifierKeys.Alt)) mods |= NativeMethods.MOD_ALT;
            if (Modifiers.HasFlag(ModifierKeys.Control)) mods |= NativeMethods.MOD_CONTROL;
            if (Modifiers.HasFlag(ModifierKeys.Shift)) mods |= NativeMethods.MOD_SHIFT;
            if (Modifiers.HasFlag(ModifierKeys.Windows)) mods |= NativeMethods.MOD_WIN;
            return mods;
        }
    }

    public uint VirtualKey => (uint)KeyInterop.VirtualKeyFromKey(Key);

    public static bool IsModifierKey(Key key) => key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift
        or Key.RightShift or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin or Key.System;

    public override string ToString()
    {
        var parts = new List<string>();
        if (Modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        parts.Add(KeyName(Key));
        return string.Join("+", parts);
    }

    public static HotkeyGesture Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Default;

        var modifiers = ModifierKeys.None;
        Key? key = null;
        foreach (var raw in text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (raw.ToLowerInvariant())
            {
                case "ctrl" or "control": modifiers |= ModifierKeys.Control; break;
                case "shift": modifiers |= ModifierKeys.Shift; break;
                case "alt": modifiers |= ModifierKeys.Alt; break;
                case "win" or "windows": modifiers |= ModifierKeys.Windows; break;
                case "printscreen" or "prtsc": key = Key.Snapshot; break;
                default:
                    if (raw.Length == 1 && char.IsDigit(raw[0]))
                        key = Key.D0 + (raw[0] - '0');
                    else if (Enum.TryParse<Key>(raw, true, out var parsed))
                        key = parsed;
                    break;
            }
        }

        return key is null || IsModifierKey(key.Value) ? Default : new HotkeyGesture(modifiers, key.Value);
    }

    private static string KeyName(Key key) => key switch
    {
        Key.Snapshot => "PrintScreen",
        >= Key.D0 and <= Key.D9 => ((char)('0' + (key - Key.D0))).ToString(),
        _ => key.ToString(),
    };
}
