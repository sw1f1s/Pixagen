using Pixagen.Game.Features.SharedFeature.Systems;
using Pixagen.Ecs.Runtime;
using Pixagen.Game.Features.SharedFeature.Helper;

namespace Pixagen.Game.Features.SharedFeature;

public sealed class SharedFeatureSystemsGroup : IGroupSystem
{
    public string GroupName => nameof(SharedFeatureSystemsGroup);
    public bool State => true;

    public object[] Injects { get; } =
    [
        new EntityStateHelper()
    ];

    public ISystem[] Systems { get; } =
    [
        new EntityDisableTriggerSystem(),
        new EntityEnableTriggerSystem(),
        new RotationMotionSystem(),
        new LerpMovementSystem(),
        new RotationSystem(),
        new MovementSystem(),
        new HierarchyTransformSystem(),
        new DestroySystem(),
    ];
}
