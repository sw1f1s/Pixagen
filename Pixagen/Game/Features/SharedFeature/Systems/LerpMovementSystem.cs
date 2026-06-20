using Pixagen.Ecs.DI;
using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.RenderFeature.Components;

namespace Pixagen.Game.Features.SharedFeature.Systems;

public sealed class LerpMovementSystem : IUpdateSystem
{
    private readonly CustomInject<Time> _time = default;
    private readonly FilterInject<Include<Transform, Velocity, LerpMovement>, Exclude<IsStaticRender, RigidBody, DisabledInHierarchy>> _movingEntities = default;
    private readonly ComponentInject<Transform> _transforms = default;
    private readonly ComponentInject<Velocity> _velocities = default;
    private readonly ComponentInject<LerpMovement> _movements = default;

    public void Update()
    {
        Fix dt = _time.Value.DeltaTime;
        _movingEntities.Value.ForEachChunk(new ChunkJob(
            _transforms,
            _velocities,
            _movements,
            dt));
    }

    internal static Vector3 GetTargetPosition(ref LerpMovement movement, Fix dt)
    {
        if (movement.Duration <= Fix.Zero)
        {
            movement.Elapsed = Fix.Zero;
            return movement.To;
        }

        movement.Elapsed += dt;
        Fix t = movement.Mode == MovementLoopMode.PingPong
            ? PingPong01(movement.Elapsed / movement.Duration)
            : Clamp01(movement.Elapsed / movement.Duration);

        return movement.From + (movement.To - movement.From) * t;
    }

    private static Fix Clamp01(Fix value)
    {
        if (value <= Fix.Zero)
        {
            return Fix.Zero;
        }

        return value >= Fix.One ? Fix.One : value;
    }

    private static Fix PingPong01(Fix value)
    {
        if (value <= Fix.Zero)
        {
            return Fix.Zero;
        }

        int whole = Fix.ToInt(value);
        Fix fraction = value - new Fix(whole);
        return (whole & 1) == 0 ? fraction : Fix.One - fraction;
    }

    private readonly struct ChunkJob : IFilterChunkProcessor
    {
        private readonly ComponentInject<Transform> _transforms;
        private readonly ComponentInject<Velocity> _velocities;
        private readonly ComponentInject<LerpMovement> _movements;
        private readonly Fix _dt;

        public ChunkJob(
            ComponentInject<Transform> transforms,
            ComponentInject<Velocity> velocities,
            ComponentInject<LerpMovement> movements,
            Fix dt)
        {
            _transforms = transforms;
            _velocities = velocities;
            _movements = movements;
            _dt = dt;
        }

        public void Execute(FilterChunk chunk)
        {
            foreach (Entity entity in chunk.Entities)
            {
                ref Transform transform = ref _transforms.Get(entity);
                ref Velocity velocity = ref _velocities.Get(entity);
                ref LerpMovement movement = ref _movements.Get(entity);

                Vector3 target = GetTargetPosition(ref movement, _dt);
                velocity.PositionDelta += target - transform.Position;
            }
        }
    }
}
