using Pixagen.Game.Features.CharacterFeature.Systems;
using Pixagen.Game.Features.CharacterFeature.Helper;

namespace Pixagen.Game.Features.CharacterFeature;

public sealed class CharacterFeatureSystemsGroup : IGroupSystem
{
    public string GroupName => nameof(CharacterFeatureSystemsGroup);
    public bool State => true;

    public object[] Injects { get; } =
    [
        new CharacterHelper()
    ];

    public ISystem[] Systems =>
    [
        new CharacterInputSystem(),
        new FpsCameraCharacterInputSystem(),
        new FpsCameraCharacterHeightSystem(),
        new CharacterPhysicsSystem(),
    ];
}
