namespace Pixagen.Rendering;

public readonly record struct FrameCell
{
    public FrameCell(char glyph, PixelColor foreground, PixelColor background)
        : this(glyph, foreground, background, 255, 255, 0)
    {
    }

    public FrameCell(
        char glyph,
        PixelColor foreground,
        PixelColor background,
        byte foregroundAlpha,
        byte backgroundAlpha,
        int fontSize = 0)
    {
        Glyph = glyph;
        Foreground = foreground;
        Background = background;
        ForegroundAlpha = foregroundAlpha;
        BackgroundAlpha = backgroundAlpha;
        FontSize = Math.Max(0, fontSize);
    }

    public char Glyph { get; }
    public PixelColor Foreground { get; }
    public PixelColor Background { get; }
    public byte ForegroundAlpha { get; }
    public byte BackgroundAlpha { get; }
    public int FontSize { get; }

    public static FrameCell Empty => new(' ', PixelColor.FromRgb(192, 192, 192), PixelColor.FromRgb(0, 0, 0));
    public static FrameCell Transparent => new('\0', PixelColor.FromRgb(0, 0, 0), PixelColor.FromRgb(0, 0, 0), 0, 0);

    public bool IsTransparent => Glyph == '\0' && ForegroundAlpha == 0 && BackgroundAlpha == 0;
    public bool HasBackground => BackgroundAlpha > 0;
}
