using Pixagen.Rendering.Raycasting;

namespace Pixagen.Rendering;

public interface IRaycastComputeRenderer
{
    bool TryRenderRaycast(in RaycastComputeRequest request);
}

public readonly record struct RaycastComputeRequest(
    int Width,
    int Height,
    float MaxDistance,
    ShadowQuality ShadowQuality,
    float ShadowSoftness,
    RayBuilder RayBuilder,
    DirectionalLight Light,
    RaycastTileBins TileBins,
    RaycastShadowBins ShadowBins,
    RenderPrimitiveBatch StaticPrimitives,
    RenderPrimitiveBatch DynamicPrimitives,
    Skybox Skybox);

public sealed class NullRaycastComputeRenderer : IRaycastComputeRenderer
{
    public static NullRaycastComputeRenderer Instance { get; } = new();

    private NullRaycastComputeRenderer()
    {
    }

    public bool TryRenderRaycast(in RaycastComputeRequest request)
    {
        return false;
    }
}
