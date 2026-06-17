using System.Globalization;

namespace Pixagen.Rendering;

public readonly record struct PixelColor(byte R, byte G, byte B)
{
    public static PixelColor FromRgb(byte r, byte g, byte b)
    {
        return new PixelColor(r, g, b);
    }

    public static PixelColor FromHex(string value)
    {
        return TryParse(value, out PixelColor color)
            ? color
            : throw new FormatException($"Invalid pixel color '{value}'.");
    }

    public static bool TryParse(string? value, out PixelColor color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string trimmed = value.Trim();
        return TryParseHex(trimmed, out color);
    }

    public PixelColor Scale(float factor)
    {
        return new PixelColor(
            ScaleChannel(R, factor),
            ScaleChannel(G, factor),
            ScaleChannel(B, factor));
    }

    public string ToHex()
    {
        return FormattableString.Invariant($"#{R:X2}{G:X2}{B:X2}");
    }

    public string ToJsonString()
    {
        return ToHex();
    }

    private static bool TryParseHex(string value, out PixelColor color)
    {
        color = default;
        string hex = value.StartsWith('#') ? value[1..] : value;
        if (hex.Length == 3)
        {
            hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);
        }

        if (hex.Length != 6 ||
            !byte.TryParse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte r) ||
            !byte.TryParse(hex[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte g) ||
            !byte.TryParse(hex[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b))
        {
            return false;
        }

        color = new PixelColor(r, g, b);
        return true;
    }

    private static byte ScaleChannel(byte value, float factor)
    {
        return (byte)Clamp((int)MathF.Round(value * MathF.Max(0f, factor)), 0, 255);
    }

    private static int Clamp(int value, int min, int max)
    {
        return Math.Min(Math.Max(value, min), max);
    }
}
