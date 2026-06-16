using System;
using FixedMathSharp;
using Pixagen.Ecs.DI;
using Pixagen.Ecs.Runtime;
using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.SharedFeature.Components;
using Pixagen.Game.Features.SharedFeature.Helper;

namespace Pixagen.Game.Features.SharedFeature.Systems;

public sealed class RotationSystem : IUpdateSystem
{
    private readonly FilterInject<Include<Transform, Velocity>, Exclude<IsStaticRender>> _rotatingEntities = default;
    private readonly CustomInject<EntityStateHelper> _entityState = default;
    private readonly ComponentInject<Transform> _transforms = default;
    private readonly ComponentInject<Velocity> _velocities = default;

    public void Update()
    {
        _rotatingEntities.Value.ForEachChunk(new ChunkJob(_entityState, _transforms, _velocities));
    }

    private static void ApplyAxisAngle(ref Quaternion rotation, Vector3 axis, Fix angle)
    {
        if (axis.IsZero || angle == Fix.Zero)
        {
            return;
        }

        Vector3 normalizedAxis = axis.Normalized;
        if (normalizedAxis.IsZero)
        {
            return;
        }

        Fix normalizedAngle = NormalizeAngle(angle);
        if (normalizedAngle == Fix.Zero)
        {
            return;
        }

        rotation = Quaternion.FromAxisAngle(normalizedAxis, normalizedAngle) * rotation;
    }

    private static Fix NormalizeAngle(Fix angle)
    {
        Fix normalized = FixedMath.FastMod(angle + Fix.Pi, Fix.TwoPi);
        if (normalized < Fix.Zero)
        {
            normalized += Fix.TwoPi;
        }

        return normalized - Fix.Pi;
    }

    private readonly struct ChunkJob : IFilterChunkProcessor
    {
        private readonly CustomInject<EntityStateHelper> _entityState;
        private readonly ComponentInject<Transform> _transforms;
        private readonly ComponentInject<Velocity> _velocities;

        public ChunkJob(
            CustomInject<EntityStateHelper> entityState,
            ComponentInject<Transform> transforms,
            ComponentInject<Velocity> velocities)
        {
            _entityState = entityState;
            _transforms = transforms;
            _velocities = velocities;
        }

        public void Execute(FilterChunk chunk)
        {
            foreach (Entity entity in chunk.Entities)
            {
                if (!_entityState.Value.IsEnabled(entity))
                {
                    continue;
                }

                ref Transform transform = ref _transforms.Get(entity);
                ref Velocity velocity = ref _velocities.Get(entity);

                if ((velocity.RotationAxis.IsZero || velocity.RotationAngleDelta == Fix.Zero) &&
                    velocity.YawDelta == Fix.Zero &&
                    velocity.PitchDelta == Fix.Zero &&
                    velocity.RollDelta == Fix.Zero)
                {
                    continue;
                }

                Quaternion rotation = transform.Rotation.MagnitudeSquared <= Fix.Epsilon
                    ? Quaternion.Identity
                    : transform.Rotation.Normalized;
                ApplyAxisAngle(ref rotation, velocity.RotationAxis, velocity.RotationAngleDelta);

                ApplyAxisAngle(ref rotation, Vector3.Up, velocity.YawDelta);

                if (velocity.PitchDelta != Fix.Zero)
                {
                    Vector3 right = rotation.Rotate(Vector3.Right).Normalized;
                    ApplyAxisAngle(ref rotation, right, velocity.PitchDelta);
                }

                if (velocity.RollDelta != Fix.Zero)
                {
                    Vector3 forward = rotation.Rotate(Vector3.Forward).Normalized;
                    ApplyAxisAngle(ref rotation, forward, velocity.RollDelta);
                }

                transform.Rotation = rotation.Normalized;
                velocity.RotationAxis = Vector3.Zero;
                velocity.RotationAngleDelta = Fix.Zero;
                velocity.YawDelta = Fix.Zero;
                velocity.PitchDelta = Fix.Zero;
                velocity.RollDelta = Fix.Zero;
            }
        }
    }
}
