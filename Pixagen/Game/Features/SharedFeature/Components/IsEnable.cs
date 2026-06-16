using Pixagen.Ecs.Runtime;

namespace Pixagen.Game.Features.SharedFeature.Components;

public struct IsEnable : IComponent
{
    public bool Value;

    public IsEnable(bool value)
    {
        Value = value;
    }
}
