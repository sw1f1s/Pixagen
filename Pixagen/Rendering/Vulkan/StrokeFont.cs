using Veldrid;

namespace Pixagen.Rendering.Vulkan;

internal static class StrokeFont
{
    private readonly record struct Segment(float X1, float Y1, float X2, float Y2);

    private static readonly IReadOnlyDictionary<char, Segment[]> Glyphs = new Dictionary<char, Segment[]>
    {
        ['0'] = [S(0.2f, 0.05f, 0.8f, 0.05f), S(0.8f, 0.05f, 0.8f, 0.95f), S(0.8f, 0.95f, 0.2f, 0.95f), S(0.2f, 0.95f, 0.2f, 0.05f)],
        ['1'] = [S(0.5f, 0.05f, 0.5f, 0.95f), S(0.35f, 0.2f, 0.5f, 0.05f), S(0.32f, 0.95f, 0.68f, 0.95f)],
        ['2'] = [S(0.2f, 0.15f, 0.5f, 0.05f), S(0.5f, 0.05f, 0.8f, 0.15f), S(0.8f, 0.15f, 0.8f, 0.42f), S(0.8f, 0.42f, 0.2f, 0.95f), S(0.2f, 0.95f, 0.82f, 0.95f)],
        ['3'] = [S(0.18f, 0.08f, 0.82f, 0.08f), S(0.82f, 0.08f, 0.62f, 0.5f), S(0.62f, 0.5f, 0.82f, 0.92f), S(0.18f, 0.92f, 0.82f, 0.92f), S(0.35f, 0.5f, 0.65f, 0.5f)],
        ['4'] = [S(0.72f, 0.05f, 0.72f, 0.95f), S(0.2f, 0.55f, 0.85f, 0.55f), S(0.2f, 0.55f, 0.68f, 0.05f)],
        ['5'] = [S(0.8f, 0.08f, 0.22f, 0.08f), S(0.22f, 0.08f, 0.22f, 0.5f), S(0.22f, 0.5f, 0.75f, 0.5f), S(0.75f, 0.5f, 0.82f, 0.75f), S(0.82f, 0.75f, 0.62f, 0.95f), S(0.62f, 0.95f, 0.18f, 0.95f)],
        ['6'] = [S(0.78f, 0.12f, 0.35f, 0.05f), S(0.35f, 0.05f, 0.2f, 0.5f), S(0.2f, 0.5f, 0.28f, 0.9f), S(0.28f, 0.9f, 0.72f, 0.9f), S(0.72f, 0.9f, 0.8f, 0.58f), S(0.8f, 0.58f, 0.22f, 0.58f)],
        ['7'] = [S(0.18f, 0.08f, 0.85f, 0.08f), S(0.85f, 0.08f, 0.42f, 0.95f)],
        ['8'] = [S(0.25f, 0.05f, 0.75f, 0.05f), S(0.75f, 0.05f, 0.78f, 0.45f), S(0.78f, 0.45f, 0.22f, 0.45f), S(0.22f, 0.45f, 0.25f, 0.05f), S(0.22f, 0.55f, 0.78f, 0.55f), S(0.78f, 0.55f, 0.75f, 0.95f), S(0.75f, 0.95f, 0.25f, 0.95f), S(0.25f, 0.95f, 0.22f, 0.55f)],
        ['9'] = [S(0.78f, 0.5f, 0.72f, 0.1f), S(0.72f, 0.1f, 0.28f, 0.1f), S(0.28f, 0.1f, 0.2f, 0.42f), S(0.2f, 0.42f, 0.78f, 0.42f), S(0.78f, 0.42f, 0.65f, 0.92f), S(0.65f, 0.92f, 0.22f, 0.88f)],
        ['A'] = [S(0.15f, 0.95f, 0.5f, 0.05f), S(0.5f, 0.05f, 0.85f, 0.95f), S(0.28f, 0.58f, 0.72f, 0.58f)],
        ['B'] = [S(0.2f, 0.05f, 0.2f, 0.95f), S(0.2f, 0.05f, 0.68f, 0.05f), S(0.68f, 0.05f, 0.82f, 0.22f), S(0.82f, 0.22f, 0.68f, 0.45f), S(0.2f, 0.45f, 0.68f, 0.45f), S(0.68f, 0.45f, 0.84f, 0.68f), S(0.84f, 0.68f, 0.68f, 0.95f), S(0.2f, 0.95f, 0.68f, 0.95f)],
        ['C'] = [S(0.82f, 0.15f, 0.62f, 0.05f), S(0.62f, 0.05f, 0.25f, 0.12f), S(0.25f, 0.12f, 0.18f, 0.5f), S(0.18f, 0.5f, 0.25f, 0.88f), S(0.25f, 0.88f, 0.62f, 0.95f), S(0.62f, 0.95f, 0.82f, 0.85f)],
        ['D'] = [S(0.2f, 0.05f, 0.2f, 0.95f), S(0.2f, 0.05f, 0.62f, 0.05f), S(0.62f, 0.05f, 0.84f, 0.28f), S(0.84f, 0.28f, 0.84f, 0.72f), S(0.84f, 0.72f, 0.62f, 0.95f), S(0.62f, 0.95f, 0.2f, 0.95f)],
        ['E'] = [S(0.82f, 0.05f, 0.2f, 0.05f), S(0.2f, 0.05f, 0.2f, 0.95f), S(0.2f, 0.5f, 0.72f, 0.5f), S(0.2f, 0.95f, 0.82f, 0.95f)],
        ['F'] = [S(0.2f, 0.05f, 0.2f, 0.95f), S(0.2f, 0.05f, 0.82f, 0.05f), S(0.2f, 0.5f, 0.72f, 0.5f)],
        ['G'] = [S(0.82f, 0.18f, 0.62f, 0.05f), S(0.62f, 0.05f, 0.25f, 0.12f), S(0.25f, 0.12f, 0.18f, 0.5f), S(0.18f, 0.5f, 0.25f, 0.88f), S(0.25f, 0.88f, 0.68f, 0.95f), S(0.68f, 0.95f, 0.84f, 0.72f), S(0.84f, 0.72f, 0.84f, 0.58f), S(0.84f, 0.58f, 0.55f, 0.58f)],
        ['H'] = [S(0.18f, 0.05f, 0.18f, 0.95f), S(0.82f, 0.05f, 0.82f, 0.95f), S(0.18f, 0.5f, 0.82f, 0.5f)],
        ['I'] = [S(0.28f, 0.05f, 0.72f, 0.05f), S(0.5f, 0.05f, 0.5f, 0.95f), S(0.28f, 0.95f, 0.72f, 0.95f)],
        ['J'] = [S(0.75f, 0.05f, 0.75f, 0.78f), S(0.75f, 0.78f, 0.6f, 0.95f), S(0.6f, 0.95f, 0.32f, 0.95f), S(0.32f, 0.95f, 0.18f, 0.78f)],
        ['K'] = [S(0.2f, 0.05f, 0.2f, 0.95f), S(0.82f, 0.05f, 0.2f, 0.55f), S(0.38f, 0.45f, 0.85f, 0.95f)],
        ['L'] = [S(0.22f, 0.05f, 0.22f, 0.95f), S(0.22f, 0.95f, 0.82f, 0.95f)],
        ['M'] = [S(0.15f, 0.95f, 0.15f, 0.05f), S(0.15f, 0.05f, 0.5f, 0.55f), S(0.5f, 0.55f, 0.85f, 0.05f), S(0.85f, 0.05f, 0.85f, 0.95f)],
        ['N'] = [S(0.18f, 0.95f, 0.18f, 0.05f), S(0.18f, 0.05f, 0.82f, 0.95f), S(0.82f, 0.95f, 0.82f, 0.05f)],
        ['O'] = [S(0.28f, 0.05f, 0.72f, 0.05f), S(0.72f, 0.05f, 0.85f, 0.22f), S(0.85f, 0.22f, 0.85f, 0.78f), S(0.85f, 0.78f, 0.72f, 0.95f), S(0.72f, 0.95f, 0.28f, 0.95f), S(0.28f, 0.95f, 0.15f, 0.78f), S(0.15f, 0.78f, 0.15f, 0.22f), S(0.15f, 0.22f, 0.28f, 0.05f)],
        ['P'] = [S(0.2f, 0.95f, 0.2f, 0.05f), S(0.2f, 0.05f, 0.7f, 0.05f), S(0.7f, 0.05f, 0.85f, 0.25f), S(0.85f, 0.25f, 0.7f, 0.48f), S(0.7f, 0.48f, 0.2f, 0.48f)],
        ['Q'] = [S(0.28f, 0.05f, 0.72f, 0.05f), S(0.72f, 0.05f, 0.85f, 0.22f), S(0.85f, 0.22f, 0.85f, 0.78f), S(0.85f, 0.78f, 0.72f, 0.95f), S(0.72f, 0.95f, 0.28f, 0.95f), S(0.28f, 0.95f, 0.15f, 0.78f), S(0.15f, 0.78f, 0.15f, 0.22f), S(0.15f, 0.22f, 0.28f, 0.05f), S(0.58f, 0.68f, 0.88f, 1.0f)],
        ['R'] = [S(0.2f, 0.95f, 0.2f, 0.05f), S(0.2f, 0.05f, 0.7f, 0.05f), S(0.7f, 0.05f, 0.85f, 0.25f), S(0.85f, 0.25f, 0.7f, 0.48f), S(0.7f, 0.48f, 0.2f, 0.48f), S(0.48f, 0.5f, 0.85f, 0.95f)],
        ['S'] = [S(0.82f, 0.12f, 0.62f, 0.05f), S(0.62f, 0.05f, 0.25f, 0.08f), S(0.25f, 0.08f, 0.18f, 0.4f), S(0.18f, 0.4f, 0.75f, 0.58f), S(0.75f, 0.58f, 0.82f, 0.88f), S(0.82f, 0.88f, 0.6f, 0.95f), S(0.6f, 0.95f, 0.18f, 0.9f)],
        ['T'] = [S(0.15f, 0.05f, 0.85f, 0.05f), S(0.5f, 0.05f, 0.5f, 0.95f)],
        ['U'] = [S(0.18f, 0.05f, 0.18f, 0.78f), S(0.18f, 0.78f, 0.35f, 0.95f), S(0.35f, 0.95f, 0.65f, 0.95f), S(0.65f, 0.95f, 0.82f, 0.78f), S(0.82f, 0.78f, 0.82f, 0.05f)],
        ['V'] = [S(0.15f, 0.05f, 0.5f, 0.95f), S(0.5f, 0.95f, 0.85f, 0.05f)],
        ['W'] = [S(0.1f, 0.05f, 0.28f, 0.95f), S(0.28f, 0.95f, 0.5f, 0.42f), S(0.5f, 0.42f, 0.72f, 0.95f), S(0.72f, 0.95f, 0.9f, 0.05f)],
        ['X'] = [S(0.18f, 0.05f, 0.82f, 0.95f), S(0.82f, 0.05f, 0.18f, 0.95f)],
        ['Y'] = [S(0.15f, 0.05f, 0.5f, 0.48f), S(0.85f, 0.05f, 0.5f, 0.48f), S(0.5f, 0.48f, 0.5f, 0.95f)],
        ['Z'] = [S(0.18f, 0.05f, 0.82f, 0.05f), S(0.82f, 0.05f, 0.18f, 0.95f), S(0.18f, 0.95f, 0.82f, 0.95f)],
        ['|'] = [S(0.5f, 0.05f, 0.5f, 0.95f)],
        ['/'] = [S(0.82f, 0.05f, 0.18f, 0.95f)],
        ['\\'] = [S(0.18f, 0.05f, 0.82f, 0.95f)],
        ['+'] = [S(0.5f, 0.22f, 0.5f, 0.78f), S(0.22f, 0.5f, 0.78f, 0.5f)],
        ['-'] = [S(0.22f, 0.5f, 0.78f, 0.5f)],
        [':'] = [S(0.5f, 0.32f, 0.5f, 0.34f), S(0.5f, 0.68f, 0.5f, 0.7f)],
        ['.'] = [S(0.5f, 0.9f, 0.5f, 0.92f)],
        [','] = [S(0.52f, 0.82f, 0.42f, 1.0f)],
        ['!'] = [S(0.5f, 0.05f, 0.5f, 0.72f), S(0.5f, 0.9f, 0.5f, 0.92f)],
        ['?'] = [S(0.25f, 0.18f, 0.4f, 0.05f), S(0.4f, 0.05f, 0.7f, 0.08f), S(0.7f, 0.08f, 0.78f, 0.35f), S(0.78f, 0.35f, 0.5f, 0.58f), S(0.5f, 0.58f, 0.5f, 0.72f), S(0.5f, 0.9f, 0.5f, 0.92f)],
    };

    public static void DrawText(RgbaByte[] pixels, int width, int height, UiTextDrawCommand command, int fallbackFontSize)
    {
        int fontSize = Math.Max(1, command.FontSize > 0 ? command.FontSize : fallbackFontSize);
        int originX = command.X;
        int x = originX;
        int y = command.Y;
        int lineHeight = Math.Max(1, (int)MathF.Ceiling(fontSize * 1.25f));
        RgbaByte color = ToRgba(command.Color);

        foreach (char rawGlyph in command.Value)
        {
            if (rawGlyph == '\r')
            {
                continue;
            }

            if (rawGlyph == '\n')
            {
                x = originX;
                y += lineHeight;
                continue;
            }

            char glyph = char.ToUpperInvariant(rawGlyph);
            int advance = GetAdvance(glyph, fontSize);
            if (glyph != ' ')
            {
                DrawGlyph(pixels, width, height, glyph, x, y, fontSize, color);
            }

            x += advance;
        }
    }

    private static void DrawGlyph(RgbaByte[] pixels, int width, int height, char glyph, int x, int y, int size, RgbaByte color)
    {
        if (!Glyphs.TryGetValue(glyph, out Segment[]? segments))
        {
            segments = [S(0.2f, 0.05f, 0.8f, 0.05f), S(0.8f, 0.05f, 0.8f, 0.95f), S(0.8f, 0.95f, 0.2f, 0.95f), S(0.2f, 0.95f, 0.2f, 0.05f)];
        }

        int thickness = Math.Max(1, size / 9);
        foreach (Segment segment in segments)
        {
            DrawLine(
                pixels,
                width,
                height,
                x + segment.X1 * size,
                y + segment.Y1 * size,
                x + segment.X2 * size,
                y + segment.Y2 * size,
                thickness,
                color);
        }
    }

    private static void DrawLine(
        RgbaByte[] pixels,
        int width,
        int height,
        float x1,
        float y1,
        float x2,
        float y2,
        int thickness,
        RgbaByte color)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        int steps = Math.Max(1, (int)MathF.Ceiling(MathF.Max(MathF.Abs(dx), MathF.Abs(dy)) * 1.5f));
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            int x = (int)MathF.Round(x1 + dx * t);
            int y = (int)MathF.Round(y1 + dy * t);
            Plot(pixels, width, height, x, y, thickness, color);
        }
    }

    private static void Plot(RgbaByte[] pixels, int width, int height, int x, int y, int thickness, RgbaByte color)
    {
        int radius = thickness / 2;
        for (int offsetY = -radius; offsetY <= radius; offsetY++)
        {
            int pixelY = y + offsetY;
            if ((uint)pixelY >= (uint)height)
            {
                continue;
            }

            for (int offsetX = -radius; offsetX <= radius; offsetX++)
            {
                int pixelX = x + offsetX;
                if ((uint)pixelX < (uint)width)
                {
                    pixels[pixelY * width + pixelX] = color;
                }
            }
        }
    }

    private static int GetAdvance(char glyph, int fontSize)
    {
        return glyph == ' ' ? Math.Max(1, fontSize / 2) : Math.Max(1, (int)MathF.Ceiling(fontSize * 0.72f));
    }

    private static RgbaByte ToRgba(PixelColor color)
    {
        return new RgbaByte(color.R, color.G, color.B, 255);
    }

    private static Segment S(float x1, float y1, float x2, float y2)
    {
        return new Segment(x1, y1, x2, y2);
    }
}
