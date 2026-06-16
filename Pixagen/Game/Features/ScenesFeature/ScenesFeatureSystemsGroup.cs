using Pixagen.Game.Features.ScenesFeature.Systems;

namespace Pixagen.Game.Features.ScenesFeature;

public sealed class ScenesFeatureSystemsGroup : IGroupSystem
{
    public string GroupName => nameof(ScenesFeatureSystemsGroup);
    public bool State => true;

    public object[] Injects { get; } =
    [
        new SceneEntityFactory(),
        new SceneManager(),
    ];

    public ISystem[] Systems { get; } =
    [
        new StartupSceneSystem(),
    ];
}
