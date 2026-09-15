namespace Snap.Core.Hotkeys;

public sealed record HotkeyGesture(HotkeyModifiers Modifiers, uint VirtualKey)
{
    public bool IsValid => Modifiers is not HotkeyModifiers.None && VirtualKey is > 0 and <= 0xFE;

    public bool ConflictsWithWeChatCaptureDefault =>
        Modifiers == HotkeyModifiers.Alt && VirtualKey == 0x41;

    public string ToDisplayString()
    {
        var parts = new List<string>(5);
        if (Modifiers.HasFlag(HotkeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            parts.Add("Alt");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Windows))
        {
            parts.Add("Win");
        }

        parts.Add(GetKeyLabel(VirtualKey));
        return string.Join(" + ", parts);
    }

    private static string GetKeyLabel(uint virtualKey) => virtualKey switch
    {
        >= 0x30 and <= 0x39 => ((char)virtualKey).ToString(),
        >= 0x41 and <= 0x5A => ((char)virtualKey).ToString(),
        >= 0x70 and <= 0x87 => $"F{virtualKey - 0x6F}",
        0x20 => "Space",
        0x2D => "Insert",
        0x2E => "Delete",
        0xBA => ";",
        0xBB => "=",
        0xBC => ",",
        0xBD => "-",
        0xBE => ".",
        0xBF => "/",
        0xC0 => "`",
        0xDB => "[",
        0xDC => "\\",
        0xDD => "]",
        0xDE => "'",
        _ => $"Key 0x{virtualKey:X2}",
    };
}
