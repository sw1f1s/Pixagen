using Pixagen.Ecs.DI;
using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.SharedFeature.Systems;

namespace Pixagen.Game.Features.PhysicsFeature.Systems;

public sealed class PhysicsKinematicMotionSystem : IFixedUpdateSystem
{
    private readonly CustomInject<Time> _time = default;
    private readonly FilterInject<Include<Transform, LerpMovement, RigidBody>, Exclude<IsStaticRender, DisabledInHierarchy>> _movingBodies = default;
    private readonly ComponentInject<Transform> _transforms = default;
    private readonly ComponentInject<LerpMovement> _movements = default;
    private readonly ComponentInject<RigidBody> _rigidBodies = default;

    public void FixedUpdate()
    {
        Fix dt = _time.Value.FixedDeltaTime;
        foreach (Entity entity in _movingBodies.Value)
        {
            ref RigidBody rigidBody = ref _rigidBodies.Get(entity);
            if (rigidBody.Kind != PhysicsBodyKind.Kinematic)
            {
                continue;
            }

            ref Transform transform = ref _transforms.Get(entity);
            ref LerpMovement movement = ref _movements.Get(entity);
            transform.Position = LerpMovementSystem.GetTargetPosition(ref movement, dt);
        }
    }
}
