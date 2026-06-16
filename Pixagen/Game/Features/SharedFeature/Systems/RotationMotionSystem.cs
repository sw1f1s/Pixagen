using Pixagen.Ecs.DI;
using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.RenderFeature.Components;

namespace Pixagen.Game.Features.SharedFeature.Systems;

public sealed class RotationMotionSystem : IUpdateSystem
{
    private readonly CustomInject<Time> _time = default;
    private readonly FilterInject<Include<Transform, Velocity, RotationMotion>, Exclude<IsStaticRender, RigidBody, DisabledInHierarchy>> _rotatingEntities = default;
    private readonly FilterInject<Include<Transform, Velocity, RotationMotion, RigidBody>, Exclude<IsStaticRender, DisabledInHierarchy>> _rotatingRigidBodies = default;
    private readonly ComponentInject<Transform> _transforms = default;
    private readonly ComponentInject<Velocity> _velocities = default;
    private readonly ComponentInject<RotationMotion> _motions = default;
    private readonly ComponentInject<RigidBody> _rigidBodies = default;

    public void Update()
    {
        Fix dt = _time.Value.DeltaTime;
        _rotatingEntities.Value.ForEachChunk(new ChunkJob(
            _transforms,
            _velocities,
            _motions,
            _rigidBodies,
            dt,
            false));
        _rotatingRigidBodies.Value.ForEachChunk(new ChunkJob(
            _transforms,
            _velocities,
            _motions,
            _rigidBodies,
            dt,
            true));
    }

    private readonly struct ChunkJob : IFilterChunkProcessor
    {
        private readonly ComponentInject<Transform> _transforms;
        private readonly ComponentInject<Velocity> _velocities;
        private readonly ComponentInject<RotationMotion> _motions;
        private readonly ComponentInject<RigidBody> _rigidBodies;
        private readonly Fix _dt;
        private readonly bool _checkRigidBodyKind;

        public ChunkJob(
            ComponentInject<Transform> transforms,
            ComponentInject<Velocity> velocities,
            ComponentInject<RotationMotion> motions,
            ComponentInject<RigidBody> rigidBodies,
            Fix dt,
            bool checkRigidBodyKind)
        {
            _transforms = transforms;
            _velocities = velocities;
            _motions = motions;
            _rigidBodies = rigidBodies;
            _dt = dt;
            _checkRigidBodyKind = checkRigidBodyKind;
        }

        public void Execute(FilterChunk chunk)
        {
            foreach (Entity entity in chunk.Entities)
            {
                if (_checkRigidBodyKind && _rigidBodies.Get(entity).Kind != PhysicsBodyKind.Kinematic)
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
                    Quaternion rotation = transform.Rotation.MagnitudeSquared <= Fix.Epsilon
                        ? Quaternion.Identity
                        : transform.Rotation.Normalized;
                    axis = rotation.Rotate(axis);
                }

                velocity.RotationAxis = axis;
                velocity.RotationAngleDelta += motion.AnglePerSecond * _dt;
            }
        }
    }
}
