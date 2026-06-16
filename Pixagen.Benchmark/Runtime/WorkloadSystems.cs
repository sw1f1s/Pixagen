using System;
using Pixagen.Ecs.Runtime;
using Pixagen.Ecs.DI;
using Pixagen.Game.Features.SharedFeature.Components;
using Pixagen.Game.Features.SharedFeature.Helper;

namespace Pixagen.Benchmark;

public sealed class VelocityWorkloadSystem : IUpdateSystem
{
    private readonly CustomInject<Time> _time = default;
    private readonly FilterInject<Include<Transform, Velocity>> _moving = default;
    private readonly ComponentInject<Velocity> _velocities = default;

    public int LastVisited { get; private set; }

    public void Update()
    {
        Fix frameFactor = new Fix((int)(_time.Value.FrameIndex % 120) + 1) / new Fix(4096);
        Fix baseDelta = Fix.One / new Fix(128);
        _moving.Value.ForEachChunk(new ChunkJob(_velocities, frameFactor, baseDelta));
        LastVisited = (int)_moving.Value.GetCount();
    }

    private readonly struct ChunkJob : IFilterChunkProcessor
    {
        private readonly ComponentInject<Velocity> _velocities;
        private readonly Fix _frameFactor;
        private readonly Fix _baseDelta;

        public ChunkJob(ComponentInject<Velocity> velocities, Fix frameFactor, Fix baseDelta)
        {
            _velocities = velocities;
            _frameFactor = frameFactor;
            _baseDelta = baseDelta;
        }

        public void Execute(FilterChunk chunk)
        {
            foreach (Entity entity in chunk.Entities)
            {
                ref Velocity velocity = ref _velocities.Get(entity);
                int pattern = entity.Id & 7;
                velocity.PositionDelta = new Vector3(
                    new Fix((pattern & 1) == 0 ? 1 : -1) * _baseDelta,
                    new Fix((pattern & 2) == 0 ? 1 : 0) * _frameFactor,
                    new Fix((pattern & 4) == 0 ? 1 : -1) * _baseDelta);
                velocity.RotationAxis = Vector3.Up;
                velocity.RotationAngleDelta = Fix.Zero;
                velocity.YawDelta = _frameFactor;
                velocity.PitchDelta = _frameFactor / new Fix(2);
                velocity.RollDelta = Fix.Zero;
            }
        }
    }
}

public sealed class EntityToggleWorkloadSystem : IUpdateSystem
{
    private readonly Entity[] _entities;
    private readonly CustomInject<EntityStateHelper> _state = default;
    private int _frame;

    public EntityToggleWorkloadSystem(Entity[] entities)
    {
        _entities = entities;
    }

    public int LastToggles { get; private set; }

    public void Update()
    {
        if (_entities.Length == 0)
        {
            LastToggles = 0;
            return;
        }

        int toggles = Math.Clamp(_entities.Length / 256, 1, 512);
        for (int i = 0; i < toggles; i++)
        {
            Entity entity = _entities[(_frame * toggles + i) % _entities.Length];
            _state.Value.SetEnabled(entity, ((_frame + i) & 1) == 0);
        }

        _frame++;
        LastToggles = toggles;
    }
}
