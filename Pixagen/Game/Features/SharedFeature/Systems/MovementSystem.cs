using System;
using Pixagen.Ecs.DI;
using Pixagen.Ecs.Runtime;
using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.SharedFeature.Components;
using Pixagen.Game.Features.SharedFeature.Helper;

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
        _movingEntities.Value.ForEachChunk(new ChunkJob(_entityState, _transforms, _velocities, _rigidBodies));
    }

    private readonly struct ChunkJob : IFilterChunkProcessor
    {
        private readonly CustomInject<EntityStateHelper> _entityState;
        private readonly ComponentInject<Transform> _transforms;
        private readonly ComponentInject<Velocity> _velocities;
        private readonly ComponentInject<RigidBody> _rigidBodies;

        public ChunkJob(
            CustomInject<EntityStateHelper> entityState,
            ComponentInject<Transform> transforms,
            ComponentInject<Velocity> velocities,
            ComponentInject<RigidBody> rigidBodies)
        {
            _entityState = entityState;
            _transforms = transforms;
            _velocities = velocities;
            _rigidBodies = rigidBodies;
        }

        public void Execute(FilterChunk chunk)
        {
            foreach (Entity entity in chunk.Entities)
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
}
