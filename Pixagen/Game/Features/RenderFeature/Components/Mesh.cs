using Pixagen.Ecs.Runtime;

namespace Pixagen.Game.Features.RenderFeature.Components;

public struct Mesh : IComponent
{
    public string Asset;

    public Mesh(string asset)
    {
        Asset = asset;
    }
}
