using Avalonia.Media;

namespace Pixagen.Editor.Avalonia;

public static class PixelPalette
{
    public static readonly IBrush App = Brush("#181A1E");
    public static readonly IBrush Panel = Brush("#26292F");
    public static readonly IBrush PanelDeep = Brush("#1D2025");
    public static readonly IBrush Header = Brush("#323740");
    public static readonly IBrush HeaderActive = Brush("#3D5360");
    public static readonly IBrush Border = Brush("#59606C");
    public static readonly IBrush Accent = Brush("#5CBB9C");
    public static readonly IBrush AccentWarm = Brush("#D9A75C");
    public static readonly IBrush Text = Brush("#E2E7EC");
    public static readonly IBrush TextMuted = Brush("#99A3AD");
    public static readonly IBrush Warning = Brush("#E26C56");
    public static readonly IBrush Selection = Brush("#346378");

    private static IBrush Brush(string hex)
    {
        return new SolidColorBrush(Color.Parse(hex));
    }
}
