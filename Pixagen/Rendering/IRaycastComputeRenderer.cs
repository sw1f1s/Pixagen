using Pixagen.Game.Features.RenderFeature;
using Pixagen.Game.Features.RenderFeature.Raycasting;

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
    RayBuilder RayBuilder,
    DirectionalLight Light,
    RenderPrimitiveBatch StaticPrimitives,
    RenderPrimitiveBatch DynamicPrimitives);

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
