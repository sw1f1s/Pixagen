using Pixagen.Ecs.DI;
using Pixagen.Ecs.Collections;
using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.RenderFeature.Components;

namespace Pixagen.Game.Features.SharedFeature.Systems;

public sealed class HierarchyDirtySystem : IInitSystem, IUpdateSystem
{
    private readonly FilterInject<Include<Transform, Velocity, HasChildren>, Exclude<IsStaticRender, RigidBody, DisabledInHierarchy>> _movingParents = default;
    private readonly FilterInject<Include<Transform, Velocity, HasChildren, RigidBody>, Exclude<IsStaticRender, DisabledInHierarchy>> _movingRigidBodyParents = default;
    private readonly FilterInject<Include<HierarchyDirtyQueue>> _dirtyQueues = default;
    private readonly WorldInject _world = default;
    private readonly ComponentInject<Velocity> _velocities = default;
    private readonly ComponentInject<RigidBody> _rigidBodies = default;
    private readonly ComponentInject<HierarchyDirtyQueue> _dirtyQueueComponents = default;
    private Entity _dirtyQueueEntity;

    public void Init()
    {
        _ = GetDirtyQueue();
    }

    public void Update()
    {
        ref HierarchyDirtyQueue queue = ref GetDirtyQueue();
        PooledList<Entity> transformDirtyEntities = queue.TransformDirtyEntities;
        MarkMovingParents(_movingParents.Value, checkRigidBodyKind: false, transformDirtyEntities);
        MarkMovingParents(_movingRigidBodyParents.Value, checkRigidBodyKind: true, transformDirtyEntities);
    }

    private void MarkMovingParents(Filter filter, bool checkRigidBodyKind, PooledList<Entity> transformDirtyEntities)
    {
        foreach (Entity entity in filter)
        {
            if (checkRigidBodyKind && _rigidBodies.Get(entity).Kind != PhysicsBodyKind.Kinematic)
            {
                continue;
            }

            ref Velocity velocity = ref _velocities.Get(entity);
            if (velocity.PositionDelta.IsZero &&
                (velocity.RotationAxis.IsZero || velocity.RotationAngleDelta == Fix.Zero) &&
                velocity.YawDelta == Fix.Zero &&
                velocity.PitchDelta == Fix.Zero &&
                velocity.RollDelta == Fix.Zero)
            {
                continue;
            }

            transformDirtyEntities.Add(entity);
        }
    }

    private ref HierarchyDirtyQueue GetDirtyQueue()
    {
        if (_dirtyQueueEntity != Entity.Empty &&
            _world.IsAlive(_dirtyQueueEntity) &&
            _dirtyQueueComponents.Has(_dirtyQueueEntity))
        {
            return ref _dirtyQueueComponents.Get(_dirtyQueueEntity);
        }

        foreach (Entity entity in _dirtyQueues.Value)
        {
            _dirtyQueueEntity = entity;
            return ref _dirtyQueueComponents.Get(entity);
        }

        throw new InvalidOperationException($"{nameof(HierarchyDirtyQueue)} was not created. Add HierarchyDirtyQueueInitSystem before hierarchy systems.");
    }
}
