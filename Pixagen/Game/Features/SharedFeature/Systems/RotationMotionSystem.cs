using Pixagen.Core.App;
using Pixagen.Core.Input;
using Pixagen.Core.Performance;
using Pixagen.Core.Runtime;
using Pixagen.Core.Timing;
using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.SharedFeature.Components;
using Pixagen.Game.Features.SharedFeature.Helper;
using Pixagen.Ecs.Runtime;
using Pixagen.Ecs.DI;

namespace Pixagen.Game.Features.SharedFeature.Systems;

public sealed class RotationMotionSystem : IUpdateSystem
{
    private readonly CustomInject<Time> _time = default;
    private readonly FilterInject<Include<Transform, Velocity, RotationMotion>, Exclude<IsStaticRender>> _rotatingEntities = default;
    private readonly CustomInject<EntityStateHelper> _entityState = default;
    private readonly ComponentInject<Transform> _transforms = default;
    private readonly ComponentInject<Velocity> _velocities = default;
    private readonly ComponentInject<RotationMotion> _motions = default;
    private readonly ComponentInject<RigidBody> _rigidBodies = default;

    public void Update()
    {
        Fix dt = _time.Value.DeltaTime;

        foreach (Entity entity in _rotatingEntities.Value)
        {
            if (!_entityState.Value.IsEnabled(entity) || IsPhysicsBodyNotMovedByShared(entity))
            {
                continue;
            }

            ref Transform transform = ref _transforms.Get(entity);
            ref Velocity velocity = ref _velocities.Get(entity);
            ref RotationMotion motion = ref _motions.Get(entity);

            if (motion.Axis.IsZero || motion.AnglePerSecond == Fix.Zero)
            {
                continue;
            }

            Vector3 axis = motion.Axis.Normalized;
            if (motion.LocalSpace)
            {
                axis = transform.Rotation.Normalized.Rotate(axis).Normalized;
            }

            velocity.RotationAxis = axis;
            velocity.RotationAngleDelta += motion.AnglePerSecond * dt;
        }
    }

    private bool IsPhysicsBodyNotMovedByShared(Entity entity)
    {
        return _rigidBodies.Has(entity) && _rigidBodies.Get(entity).Kind != PhysicsBodyKind.Kinematic;
    }
}
