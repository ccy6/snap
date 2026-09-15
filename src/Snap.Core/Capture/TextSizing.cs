namespace Snap.Core.Capture;

public enum TextSize
{
    Small,
    Medium,
    Large,
}

public static class TextSizing
{
    public static double ToFontSize(TextSize size) => size switch
    {
        TextSize.Small => 16,
        TextSize.Medium => 24,
        TextSize.Large => 32,
        _ => throw new ArgumentOutOfRangeException(nameof(size)),
    };
}
