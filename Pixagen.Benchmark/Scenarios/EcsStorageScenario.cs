using Pixagen.Ecs.Runtime;
using Pixagen.Ecs.DI;
using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.SharedFeature.Components;
using Pixagen.Rendering;

namespace Pixagen.Benchmark;

public sealed class EcsStorageScenario : IBenchmarkScenario
{
    public string Name => "ecs.storage";
    public string Description => "Entity create/setup plus hot filter iteration, component get/replace, and sparse storage access.";
    public BenchmarkMeasurementMode Mode => BenchmarkMeasurementMode.SteadyState;

    public IBenchmarkCase Create(BenchmarkContext context)
    {
        var engine = new EngineBenchmarkContext(context.Config);
        var world = new WorldInject(engine.World);
        var transforms = new ComponentInject<Transform>(engine.World);
        var velocities = new ComponentInject<Velocity>(engine.World);
        var materials = new ComponentInject<Material>(engine.World);
        var enableStates = new ComponentInject<IsEnable>(engine.World);
        var staticMarkers = new ComponentInject<IsStaticRender>(engine.World);
        var entities = new Entity[context.EntityCount];

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = world.Create<Transform>();
            entities[i] = entity;
            transforms.Get(entity) = new Transform(BenchmarkMath.GridPosition(i, entities.Length, Fix.One, Fix.Zero, Fix.Zero));
            velocities.Add(entity, new Velocity());
            enableStates.Add(entity, new IsEnable(true));

            if ((i & 1) == 0)
            {
                materials.Add(entity, new Material(PixelColor.FromRgb(
                    BenchmarkMath.Channel(i, 1),
                    BenchmarkMath.Channel(i, 2),
                    BenchmarkMath.Channel(i, 3))));
            }

            if (i % 11 == 0)
            {
                staticMarkers.Add(entity, new IsStaticRender());
            }
        }

        using var movingMask = new FilterMask<Transform, Velocity>.Exclude<IsStaticRender>();
        using var materialMask = new FilterMask<Transform, Material>();
        Filter movingFilter = engine.World.GetFilter(movingMask);
        Filter materialFilter = engine.World.GetFilter(materialMask);

        int frame = 0;
        long checksum = 0;
        void Step()
        {
            engine.AdvanceFrame();
            int visited = 0;
            foreach (Entity entity in movingFilter)
            {
                ref Transform transform = ref transforms.Get(entity);
                ref Velocity velocity = ref velocities.Get(entity);
                velocity.PositionDelta = new Vector3(Fix.One / new Fix(64), Fix.Zero, Fix.One / new Fix(128));
                transform.Position += velocity.PositionDelta;
                checksum += entity.Id;
                visited++;
            }

            int materialVisited = 0;
            foreach (Entity entity in materialFilter)
            {
                ref Material material = ref materials.Get(entity);
                material.Color = PixelColor.FromRgb(
                    BenchmarkMath.Channel(entity.Id + frame, 4),
                    material.Color.G,
                    material.Color.B);
                materialVisited++;
            }

            int toggles = Math.Clamp(entities.Length / 512, 1, 256);
            for (int i = 0; i < toggles; i++)
            {
                Entity entity = entities[(frame * toggles + i) % entities.Length];
                enableStates.Replace(entity, new IsEnable(((frame + i) & 1) == 0));
            }

            frame++;
            checksum += visited + materialVisited;
        }

        IReadOnlyDictionary<string, double> Counters()
        {
            return new Dictionary<string, double>
            {
                ["movingFilter"] = movingFilter.GetCount(),
                ["materialFilter"] = materialFilter.GetCount(),
                ["checksum"] = checksum
            };
        }

        return new DelegateBenchmarkCase(Name, context.EntityCount, Step, Counters, engine.Dispose);
    }
}
