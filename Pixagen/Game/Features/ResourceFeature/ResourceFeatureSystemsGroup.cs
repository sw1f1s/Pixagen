using Pixagen.Ecs.Runtime;
using Pixagen.Game.Features.ResourceFeature.Runtime;

namespace Pixagen.Game.Features.ResourceFeature;

public sealed class ResourceFeatureSystemsGroup : IGroupSystem
{
    public string GroupName => nameof(ResourceFeatureSystemsGroup);
    public bool State => true;

    public object[] Injects { get; } =
    [
        new ResourceManager(),
    ];

    public ISystem[] Systems { get; } = [];
}
