using FixedMathSharp;
using Pixagen.Ecs.DI;
using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.RenderFeature.Components;

namespace Pixagen.Game.Features.SharedFeature.Systems;

public sealed class TransformVelocityIntegrationSystem : IUpdateSystem
{
    private readonly FilterInject<Include<Transform, Velocity>, Exclude<IsStaticRender, RigidBody, DisabledInHierarchy>> _movingEntities = default;
    private readonly FilterInject<Include<Transform, Velocity, RigidBody>, Exclude<IsStaticRender, DisabledInHierarchy>> _movingRigidBodies = default;
    private readonly ComponentInject<Transform> _transforms = default;
    private readonly ComponentInject<Velocity> _velocities = default;
    private readonly ComponentInject<RigidBody> _rigidBodies = default;

    public void Update()
    {
        _movingEntities.Value.ForEachChunk(new ChunkJob(_transforms, _velocities, _rigidBodies, false));
        _movingRigidBodies.Value.ForEachChunk(new ChunkJob(_transforms, _velocities, _rigidBodies, true));
    }

    private readonly struct ChunkJob : IFilterChunkProcessor
    {
        private readonly ComponentInject<Transform> _transforms;
        private readonly ComponentInject<Velocity> _velocities;
        private readonly ComponentInject<RigidBody> _rigidBodies;
        private readonly bool _checkRigidBodyKind;

        public ChunkJob(
            ComponentInject<Transform> transforms,
            ComponentInject<Velocity> velocities,
            ComponentInject<RigidBody> rigidBodies,
            bool checkRigidBodyKind)
        {
            _transforms = transforms;
            _velocities = velocities;
            _rigidBodies = rigidBodies;
            _checkRigidBodyKind = checkRigidBodyKind;
        }

        public void Execute(FilterChunk chunk)
        {
            foreach (Entity entity in chunk.Entities)
            {
                bool skipPosition = _checkRigidBodyKind && _rigidBodies.Get(entity).Kind != PhysicsBodyKind.Kinematic;
                Integrate(ref _transforms.Get(entity), ref _velocities.Get(entity), skipPosition);
            }
        }

        private static void Integrate(ref Transform transform, ref Velocity velocity, bool skipPosition)
        {
            if (!skipPosition && !velocity.PositionDelta.IsZero)
            {
                transform.Position += velocity.PositionDelta;
                velocity.PositionDelta = Vector3.Zero;
            }

            if ((velocity.RotationAxis.IsZero || velocity.RotationAngleDelta == Fix.Zero) &&
                velocity.YawDelta == Fix.Zero &&
                velocity.PitchDelta == Fix.Zero &&
                velocity.RollDelta == Fix.Zero)
            {
                return;
            }

            if ((velocity.RotationAxis.IsZero || velocity.RotationAngleDelta == Fix.Zero) &&
                velocity.PitchDelta == Fix.Zero &&
                velocity.RollDelta == Fix.Zero)
            {
                ApplyYawOnly(ref transform, ref velocity);
                return;
            }

            Quaternion rotation = NormalizeIfNeeded(transform.Rotation);
            ApplyAxisAngle(ref rotation, velocity.RotationAxis, velocity.RotationAngleDelta, false);
            ApplyAxisAngle(ref rotation, Vector3.Up, velocity.YawDelta, true);

            if (velocity.PitchDelta != Fix.Zero)
            {
                ApplyAxisAngle(ref rotation, rotation.Rotate(Vector3.Right), velocity.PitchDelta, true);
            }

            if (velocity.RollDelta != Fix.Zero)
            {
                ApplyAxisAngle(ref rotation, rotation.Rotate(Vector3.Forward), velocity.RollDelta, true);
            }

            transform.Rotation = NormalizeIfNeeded(rotation);
            velocity.RotationAxis = Vector3.Zero;
            velocity.RotationAngleDelta = Fix.Zero;
            velocity.YawDelta = Fix.Zero;
            velocity.PitchDelta = Fix.Zero;
            velocity.RollDelta = Fix.Zero;
        }

        private static void ApplyYawOnly(ref Transform transform, ref Velocity velocity)
        {
            if (velocity.YawDelta == Fix.Zero)
            {
                ClearRotation(ref velocity);
                return;
            }

            Quaternion rotation = NormalizeZeroRotation(transform.Rotation);
            ApplyAxisAngle(ref rotation, Vector3.Up, velocity.YawDelta, true);
            transform.Rotation = NormalizeIfNeeded(rotation);
            ClearRotation(ref velocity);
        }

        private static void ApplyAxisAngle(ref Quaternion rotation, Vector3 axis, Fix angle, bool axisIsNormalized)
        {
            if (angle == Fix.Zero)
            {
                return;
            }

            if (axis.IsZero)
            {
                return;
            }

            if (!axisIsNormalized)
            {
                axis = axis.Normalized;
                if (axis.IsZero)
                {
                    return;
                }
            }

            angle = NormalizeAngleIfNeeded(angle);
            if (angle == Fix.Zero)
            {
                return;
            }

            rotation = Quaternion.FromAxisAngle(axis, angle) * rotation;
        }

        private static void ClearRotation(ref Velocity velocity)
        {
            velocity.RotationAxis = Vector3.Zero;
            velocity.RotationAngleDelta = Fix.Zero;
            velocity.YawDelta = Fix.Zero;
            velocity.PitchDelta = Fix.Zero;
            velocity.RollDelta = Fix.Zero;
        }

        private static Quaternion NormalizeZeroRotation(Quaternion rotation)
        {
            return rotation.MagnitudeSquared <= Fix.Epsilon ? Quaternion.Identity : rotation;
        }

        private static Fix NormalizeAngleIfNeeded(Fix angle)
        {
            if (angle >= -Fix.Pi && angle <= Fix.Pi)
            {
                return angle;
            }

            Fix normalized = FixedMath.FastMod(angle + Fix.Pi, Fix.TwoPi);
            if (normalized < Fix.Zero)
            {
                normalized += Fix.TwoPi;
            }

            return normalized - Fix.Pi;
        }

        private static Quaternion NormalizeIfNeeded(Quaternion rotation)
        {
            Fix magnitudeSquared = rotation.MagnitudeSquared;
            if (magnitudeSquared <= Fix.Epsilon)
            {
                return Quaternion.Identity;
            }

            Fix error = magnitudeSquared - Fix.One;
            if (error < Fix.Zero)
            {
                error = -error;
            }

            return error <= Fix.Epsilon ? rotation : rotation.Normalized;
        }
    }
}
