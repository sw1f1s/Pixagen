using Pixagen.Rendering;

namespace Pixagen.Game.Features.RenderFeature;

public sealed record RenderSettings(
    RenderResolution MaxInternalResolution,
    RenderScaleMode RenderScaleMode,
    ShadowQuality ShadowQuality,
    Fix ShadowSoftness,
    Fix DrawDistance,
    Fix ShadowRenderDistance)
{
    public static RenderSettings Default { get; } = new(
        new RenderResolution(480, 270),
        RenderScaleMode.FitToMax,
        ShadowQuality.Full,
        Fix.FromDouble(0.035),
        Fix.FromDouble(256),
        Fix.FromDouble(256));
}

public readonly record struct RenderResolution(int Width, int Height);

public enum RenderScaleMode
{
    Native,
    FitToMax,
    Fixed
}

public enum ShadowQuality
{
    Off,
    Low,
    Full
}
