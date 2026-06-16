using Pixagen.Game.Features.SharedFeature.Systems;
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
        new HierarchyDirtyQueueInitSystem(),
        new EntityDisableTriggerSystem(),
        new EntityEnableTriggerSystem(),
        new EntityEnableStateSyncSystem(),
        new RotationMotionSystem(),
        new LerpMovementSystem(),
        new HierarchyDirtySystem(),
        new TransformVelocityIntegrationSystem(),
        new HierarchyTransformSystem(),
        new DestroySystem(),
    ];
}
