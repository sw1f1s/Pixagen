using Pixagen.Ecs.Runtime;
using Pixagen.Ecs.DI;
using Pixagen.Game.Features.SharedFeature;
using Pixagen.Game.Features.SharedFeature.Components;

namespace Pixagen.Benchmark;

public sealed class SharedSystemsScenario : IBenchmarkScenario
{
    public string Name => "shared.systems";
    public string Description => "Movement, rotation, lerp, hierarchy, enable/disable triggers, destroy, and one-tick cleanup.";
    public BenchmarkMeasurementMode Mode => BenchmarkMeasurementMode.SteadyState;

    public IBenchmarkCase Create(BenchmarkContext context)
    {
        var engine = new EngineBenchmarkContext(context.Config);
        var world = new WorldInject(engine.World);
        var transforms = new ComponentInject<Transform>(engine.World);
        var velocities = new ComponentInject<Velocity>(engine.World);
        var lerps = new ComponentInject<LerpMovement>(engine.World);
        var rotations = new ComponentInject<RotationMotion>(engine.World);
        var entities = new Entity[context.EntityCount];
        var toggleEntities = new List<Entity>(Math.Max(1, context.EntityCount / 8));

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = world.Create<Transform>();
            entities[i] = entity;
            transforms.Get(entity) = new Transform(BenchmarkMath.GridPosition(i, entities.Length, Fix.One, Fix.Zero, Fix.Zero));
            velocities.Add(entity, new Velocity());

            if ((i & 1) == 0)
            {
                lerps.Add(entity, new LerpMovement(
                    BenchmarkMath.GridPosition(i, entities.Length, Fix.One, Fix.Zero, Fix.Zero),
                    BenchmarkMath.GridPosition(i, entities.Length, Fix.One, Fix.One, new Fix(8)),
                    new Fix(2)));
            }

            if (i % 3 == 0)
            {
                rotations.Add(entity, new RotationMotion(Vector3.Up, Fix.One / new Fix(2)));
            }

            if (i % 8 == 0)
            {
                toggleEntities.Add(entity);
            }
        }

        for (int i = 1; i < entities.Length; i += 5)
        {
            engine.State.AddChild(entities[i - 1], entities[i]);
        }

        Systems systems = engine.BuildSystems(
            new VelocityWorkloadSystem(),
            new EntityToggleWorkloadSystem(toggleEntities.ToArray()),
            new SharedFeatureSystemsGroup());

        return new SystemsBenchmarkCase(
            Name,
            context.EntityCount,
            engine,
            systems,
            counters: ctx =>
            {
                Dictionary<string, double> counters = ctx.CommonCounters();
                counters["hierarchyPairs"] = context.EntityCount / 5;
                counters["toggleCandidates"] = toggleEntities.Count;
                return counters;
            });
    }
}
