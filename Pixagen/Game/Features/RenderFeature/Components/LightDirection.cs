using Pixagen.Ecs.Runtime;

namespace Pixagen.Game.Features.RenderFeature.Components;

public struct LightDirection : IComponent
{
    public Fix Intensity;
    public Fix AmbientIntensity;
    public Fix ShadowIntensity;
    public Fix ShadowBias;
    public Fix ShadowMaxDistance;

    public LightDirection(
        Fix intensity,
        Fix ambientIntensity,
        Fix shadowIntensity,
        Fix shadowBias,
        Fix shadowMaxDistance)
    {
        Intensity = intensity;
        AmbientIntensity = ambientIntensity;
        ShadowIntensity = shadowIntensity;
        ShadowBias = shadowBias;
        ShadowMaxDistance = shadowMaxDistance;
    }
}
