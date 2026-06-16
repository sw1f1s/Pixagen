using Pixagen.Core.App;
using Pixagen.Core.Input;
using Pixagen.Core.Performance;
using Pixagen.Core.Runtime;
using Pixagen.Core.Timing;
using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.PhysicsFeature.Runtime;
using Pixagen.Game.Features.SharedFeature.Components;
using Pixagen.Game.Features.SharedFeature.Helper;
using Pixagen.Ecs.Runtime;
using Pixagen.Ecs.DI;

namespace Pixagen.Game.Features.PhysicsFeature.Systems;

public sealed class PhysicsKinematicSyncSystem : IFixedUpdateSystem
{
    private readonly CustomInject<Time> _time = default;
    private readonly CustomInject<PhysicsWorld> _physicsWorld = default;
    private readonly FilterInject<Include<Transform, RigidBody, PhysicsBodyReference>> _bodies = default;
    private readonly CustomInject<EntityStateHelper> _entityState = default;
    private readonly ComponentInject<Transform> _transforms = default;
    private readonly ComponentInject<RigidBody> _rigidBodies = default;
    private readonly ComponentInject<PhysicsBodyReference> _references = default;

    public void FixedUpdate()
    {
        float dt = MathF.Max(0.0001f, PhysicsConvert.ToFloat(_time.Value.FixedDeltaTime));
        PhysicsWorld physicsWorld = _physicsWorld.Value;

        foreach (Entity entity in _bodies.Value)
        {
            if (!_entityState.Value.IsEnabled(entity))
            {
                continue;
            }

            ref RigidBody rigidBody = ref _rigidBodies.Get(entity);
            if (rigidBody.Kind != PhysicsBodyKind.Kinematic)
            {
                continue;
            }

            ref Transform transform = ref _transforms.Get(entity);
            ref PhysicsBodyReference reference = ref _references.Get(entity);
            physicsWorld.SetKinematicPose(reference, transform, dt);
        }
    }
}
