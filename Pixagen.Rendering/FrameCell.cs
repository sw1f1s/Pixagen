namespace Pixagen.Rendering;

public readonly record struct FrameCell
{
    public FrameCell(PixelColor color)
        : this(color, 255)
    {
    }

    public FrameCell(PixelColor color, byte alpha)
    {
        Color = color;
        Alpha = alpha;
    }

    public PixelColor Color { get; }
    public byte Alpha { get; }

    public static FrameCell Empty { get; } = new(PixelColor.FromRgb(0, 0, 0));
    public static FrameCell Transparent { get; } = new(PixelColor.FromRgb(0, 0, 0), 0);

    public bool IsTransparent => Alpha == 0;
}
