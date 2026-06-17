using Pixagen.Game.Features.RenderFeature;
using System.Globalization;

namespace Pixagen.Core.App;

public sealed class EngineOptions
{
    public int WindowWidth { get; init; }
    public int WindowHeight { get; init; }
    public int CellPixelSize { get; init; }
    public int TargetFps { get; init; }
    public bool RunSingleFrame { get; init; }
    public bool CaptureMouse { get; init; } = true;
    public bool ShowCursor { get; init; }
    public bool AutoResize { get; init; } = true;
    public bool Fullscreen { get; init; } = true;
    public string? ScenePath { get; init; }
    public string? SaveDefaultScenePath { get; init; }
    public RenderSettings RenderSettings { get; init; } = RenderSettings.Default;

    public static EngineOptions FromArgs(string[] args)
    {
        (int windowWidth, int windowHeight) = ParseResolution(args, "--window-size", defaultWidth: 1920, defaultHeight: 1080);

        return new EngineOptions
        {
            WindowWidth = windowWidth,
            WindowHeight = windowHeight,
            RunSingleFrame = args.Any(arg => string.Equals(arg, "--once", StringComparison.OrdinalIgnoreCase)),
            CaptureMouse = !args.Any(arg => string.Equals(arg, "--no-mouse", StringComparison.OrdinalIgnoreCase)),
            ShowCursor = args.Any(arg => string.Equals(arg, "--show-cursor", StringComparison.OrdinalIgnoreCase)),
            TargetFps = Math.Max(0, ParseNullableInt(args, "--fps") ?? ParseNullableInt(args, "--target-fps") ?? 90),
            AutoResize = !args.Any(arg => string.Equals(arg, "--fixed-resolution", StringComparison.OrdinalIgnoreCase)),
            Fullscreen = args.Any(arg => string.Equals(arg, "--fullscreen", StringComparison.OrdinalIgnoreCase)),
            CellPixelSize = Math.Max(
                1,
                ParseNullableInt(args, "--cell-pixel-size") ??
                    ParseNullableInt(args, "--pixel-size") ??
                    ParseNullableInt(args, "--font-size") ??
                    4),
            ScenePath = ParseNullableString(args, "--scene"),
            SaveDefaultScenePath = ParseNullableString(args, "--save-default-scene"),
            RenderSettings = ParseRenderSettings(args)
        };
    }

    private static RenderSettings ParseRenderSettings(string[] args)
    {
        RenderSettings defaults = RenderSettings.Default;
        RenderResolution maxInternalResolution =
            ParseRenderResolution(args, "--max-internal-resolution") ??
            ParseRenderResolution(args, "--internal-resolution") ??
            ParseRenderResolution(args, "--render-resolution") ??
            defaults.MaxInternalResolution;

        return new RenderSettings(
            maxInternalResolution,
            ParseNullableEnum<RenderScaleMode>(args, "--render-scale-mode") ??
                ParseNullableEnum<RenderScaleMode>(args, "--scale-mode") ??
                defaults.RenderScaleMode,
            ParseNullableEnum<ShadowQuality>(args, "--shadow-quality") ?? defaults.ShadowQuality,
            ParseNullableFix(args, "--shadow-softness") ??
                ParseNullableFix(args, "--soft-shadows") ??
                defaults.ShadowSoftness,
            ParseNullableFix(args, "--draw-distance") ??
                ParseNullableFix(args, "--render-distance") ??
                ParseNullableFix(args, "--view-distance") ??
                defaults.DrawDistance,
            ParseNullableFix(args, "--shadow-render-distance") ??
                ParseNullableFix(args, "--shadow-distance") ??
                ParseNullableFix(args, "--max-shadow-render-distance") ??
                defaults.ShadowRenderDistance);
    }

    private static (int Width, int Height) ParseResolution(string[] args, string name, int defaultWidth, int defaultHeight)
    {
        return TryParseResolutionValue(ParseNullableString(args, name), out int width, out int height)
            ? (Math.Max(1, width), Math.Max(1, height))
            : (defaultWidth, defaultHeight);
    }

    private static RenderResolution? ParseRenderResolution(string[] args, string name)
    {
        return TryParseResolutionValue(ParseNullableString(args, name), out int width, out int height)
            ? new RenderResolution(Math.Max(1, width), Math.Max(1, height))
            : null;
    }

    private static bool TryParseResolutionValue(string? value, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string[] parts = value.Split('x', 'X');
        return parts.Length == 2 &&
            int.TryParse(parts[0], out width) &&
            int.TryParse(parts[1], out height);
    }

    private static int? ParseNullableInt(string[] args, string name)
    {
        string? value = ParseNullableString(args, name);
        return int.TryParse(value, out int result) ? result : null;
    }

    private static Fix? ParseNullableFix(string[] args, string name)
    {
        string? value = ParseNullableString(args, name);
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result)
            ? Fix.FromDouble(result)
            : null;
    }

    private static TEnum? ParseNullableEnum<TEnum>(string[] args, string name)
        where TEnum : struct, Enum
    {
        string? value = ParseNullableString(args, name);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalizedValue = NormalizeEnumValue(value);
        foreach (TEnum enumValue in Enum.GetValues<TEnum>())
        {
            if (NormalizeEnumValue(enumValue.ToString()) == normalizedValue)
            {
                return enumValue;
            }
        }

        return null;
    }

    private static string NormalizeEnumValue(string value)
    {
        return value
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
    }

    private static string? ParseNullableString(string[] args, string name)
    {
        string? value = null;
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (arg.StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
            {
                value = arg[(name.Length + 1)..];
                continue;
            }

            if (string.Equals(arg, name, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                value = args[i + 1];
            }
        }

        return value;
    }
}
