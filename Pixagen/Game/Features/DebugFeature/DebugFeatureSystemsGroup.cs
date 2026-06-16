using Pixagen.Ecs.Runtime;
using Pixagen.Game.Features.DebugFeature.Systems;

namespace Pixagen.Game.Features.DebugFeature;

public sealed class DebugFeatureSystemsGroup : IGroupSystem
{
    public string GroupName => nameof(DebugFeatureSystemsGroup);
    public bool State => true;

    public ISystem[] Systems { get; } =
    [
        new StartupLogSystem(),
    ];
}
