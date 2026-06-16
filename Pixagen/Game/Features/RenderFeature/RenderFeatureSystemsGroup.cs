using Pixagen.Game.Features.RenderFeature.Systems;
using Pixagen.Ecs.Runtime;
using Pixagen.Game.Features.RenderFeature.Raycasting;

namespace Pixagen.Game.Features.RenderFeature;

public sealed class RenderFeatureSystemsGroup : IGroupSystem
{
    public string GroupName => nameof(RenderFeatureSystemsGroup);
    public bool State => true;
    public object[] Injects { get; } =
    [
        new RenderSceneCache(),
    ];

    public ISystem[] Systems { get; } =
    [
        new StaticRenderCacheSystem(),
        new RaycastRenderSystem(),
    ];
}
