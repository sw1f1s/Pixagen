using Pixagen.Game.Features.PhysicsFeature.Systems;
using Pixagen.Game.Features.PhysicsFeature.Runtime;

namespace Pixagen.Game.Features.PhysicsFeature;

public sealed class PhysicsFeatureSystemsGroup : IGroupSystem
{
    public string GroupName => nameof(PhysicsFeatureSystemsGroup);
    public bool State => true;
    public object[] Injects { get; } =
    [
        new PhysicsWorld(),
    ];

    public ISystem[] Systems { get; } =
    [
        new PhysicsBodyCreationSystem(),
        new PhysicsBodyActivationSystem(),
        new PhysicsKinematicSyncSystem(),
        new PhysicsStepSystem(),
        new PhysicsSyncTransformSystem(),
    ];
}
