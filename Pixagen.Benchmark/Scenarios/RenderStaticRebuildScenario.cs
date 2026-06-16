using Pixagen.Ecs.Runtime;
using Pixagen.Ecs.DI;
using Pixagen.Game.Features.RenderFeature;
using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.SharedFeature.Components;
using Pixagen.Rendering;

namespace Pixagen.Benchmark;

public sealed class RenderStaticRebuildScenario : IBenchmarkScenario
{
    public string Name => "render.static-rebuild";
    public string Description => "Static render cache rebuild under dirty static meshes, then culling and headless compute render.";
    public BenchmarkMeasurementMode Mode => BenchmarkMeasurementMode.SteadyState;

    public IBenchmarkCase Create(BenchmarkContext context)
    {
        var engine = new EngineBenchmarkContext(context.Config);
        engine.Resources.LoadMesh("plane");
        engine.Resources.LoadTexture("checker");
        ScenarioEntities.CreateCamera(engine);
        ScenarioEntities.CreateLight(engine);

        var world = new WorldInject(engine.World);
        var transforms = new ComponentInject<Transform>(engine.World);
        var meshes = new ComponentInject<Mesh>(engine.World);
        var materials = new ComponentInject<Material>(engine.World);
        var staticMarkers = new ComponentInject<IsStaticRender>(engine.World);
        var shadows = new ComponentInject<ShadowCaster>(engine.World);

        for (int i = 0; i < context.EntityCount; i++)
        {
            Entity entity = world.Create<Transform>();
            transforms.Get(entity) = new Transform(
                BenchmarkMath.GridPosition(i, context.EntityCount, Fix.One, Fix.Zero, new Fix(8)),
                Quaternion.Identity,
                Vector3.One);
            meshes.Add(entity, new Mesh("plane"));
            materials.Add(entity, CreateMaterial(i));
            staticMarkers.Add(entity, new IsStaticRender());

            if (i % 3 == 0)
            {
                shadows.Add(entity, new ShadowCaster());
            }
        }

        Systems systems = engine.BuildSystems(new RenderFeatureSystemsGroup());
        return new SystemsBenchmarkCase(
            Name,
            context.EntityCount,
            engine,
            systems,
            beforeUpdate: ctx => ctx.RenderSceneCache.InvalidateStatic());
    }

    private static Material CreateMaterial(int index)
    {
        PixelColor color = PixelColor.FromRgb(
            BenchmarkMath.Channel(index, 10),
            BenchmarkMath.Channel(index, 11),
            BenchmarkMath.Channel(index, 12));

        return index % 5 == 0
            ? new Material(color, new MaterialTexture("checker"))
            : new Material(color);
    }
}
