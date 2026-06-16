using System;
using Pixagen.Core.Timing;
using Pixagen.Ecs.DI;
using Pixagen.Ecs.Runtime;
using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.SharedFeature.Components;
using Pixagen.Game.Features.SharedFeature.Helper;

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
        _rotatingEntities.Value.ForEachChunk(new ChunkJob(
            _entityState,
            _transforms,
            _velocities,
            _motions,
            _rigidBodies,
            dt));
    }

    private readonly struct ChunkJob : IFilterChunkProcessor
    {
        private readonly CustomInject<EntityStateHelper> _entityState;
        private readonly ComponentInject<Transform> _transforms;
        private readonly ComponentInject<Velocity> _velocities;
        private readonly ComponentInject<RotationMotion> _motions;
        private readonly ComponentInject<RigidBody> _rigidBodies;
        private readonly Fix _dt;

        public ChunkJob(
            CustomInject<EntityStateHelper> entityState,
            ComponentInject<Transform> transforms,
            ComponentInject<Velocity> velocities,
            ComponentInject<RotationMotion> motions,
            ComponentInject<RigidBody> rigidBodies,
            Fix dt)
        {
            _entityState = entityState;
            _transforms = transforms;
            _velocities = velocities;
            _motions = motions;
            _rigidBodies = rigidBodies;
            _dt = dt;
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
                velocity.RotationAngleDelta += motion.AnglePerSecond * _dt;
            }
        }

        private bool IsPhysicsBodyNotMovedByShared(Entity entity)
        {
            return _rigidBodies.Has(entity) && _rigidBodies.Get(entity).Kind != PhysicsBodyKind.Kinematic;
        }
    }
}
