using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.SharedFeature.Components;
using Pixagen.Game.Features.SharedFeature.Helper;
using Pixagen.Ecs.Runtime;
using Pixagen.Ecs.DI;

namespace Pixagen.Game.Features.SharedFeature.Systems;

public sealed class MovementSystem : IUpdateSystem
{
    private readonly FilterInject<Include<Transform, Velocity>, Exclude<IsStaticRender>> _movingEntities = default;
    private readonly CustomInject<EntityStateHelper> _entityState = default;
    private readonly ComponentInject<Transform> _transforms = default;
    private readonly ComponentInject<Velocity> _velocities = default;
    private readonly ComponentInject<RigidBody> _rigidBodies = default;

    public void Update()
    {
        foreach (Entity entity in _movingEntities.Value)
        {
            if (!_entityState.Value.IsEnabled(entity) || IsPhysicsBodyNotMovedByShared(entity))
            {
                continue;
            }

            ref Transform transform = ref _transforms.Get(entity);
            ref Velocity velocity = ref _velocities.Get(entity);

            transform.Position += velocity.PositionDelta;
            velocity.PositionDelta = new Vector3(Fix.Zero, Fix.Zero, Fix.Zero);
        }
    }

    private bool IsPhysicsBodyNotMovedByShared(Entity entity)
    {
        return _rigidBodies.Has(entity) && _rigidBodies.Get(entity).Kind != PhysicsBodyKind.Kinematic;
    }
}
