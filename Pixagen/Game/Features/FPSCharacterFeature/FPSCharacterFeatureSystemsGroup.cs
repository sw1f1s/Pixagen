using Pixagen.Game.Features.FPSCharacterFeature.Systems;
using Pixagen.Game.Features.FPSCharacterFeature.Helper;

namespace Pixagen.Game.Features.FPSCharacterFeature;

public sealed class FPSCharacterFeatureSystemsGroup : IGroupSystem
{
    public string GroupName => nameof(FPSCharacterFeatureSystemsGroup);
    public bool State => true;

    public object[] Injects { get; } =
    [
        new FPSCharacterHelper()
    ];

    public ISystem[] Systems =>
    [
        new FPSCharacterInputSystem(),
        new FPSCharacterCameraInputSystem(),
        new FPSCharacterPhysicsSystem(),
    ];
}
