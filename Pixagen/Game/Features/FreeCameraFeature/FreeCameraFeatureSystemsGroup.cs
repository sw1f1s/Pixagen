using Pixagen.Game.Features.FreeCameraFeature.Systems;

namespace Pixagen.Game.Features.FreeCameraFeature;

public sealed class FreeCameraFeatureSystemsGroup : IGroupSystem
{
    public string GroupName => nameof(FreeCameraFeatureSystemsGroup);
    public bool State => true;
    public ISystem[] Systems { get; } =
    [
        new FreeCameraInputSystem(),
    ];
}
