using Pixagen.Ecs.Runtime;
using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.ResourceFeature.Runtime;
using Pixagen.Game.Features.ScenesFeature;
using Pixagen.Game.Features.ScenesFeature.Serialization;
using Pixagen.Game.Features.SharedFeature.Components;
using Pixagen.Rendering;

namespace Pixagen.Benchmark;

public sealed class SceneStartupScenario : IBenchmarkScenario
{
    public string Name => "scene.startup";
    public string Description => "Synthetic scene resource discovery/load and runtime entity hierarchy creation.";
    public BenchmarkMeasurementMode Mode => BenchmarkMeasurementMode.SetupOnly;

    public IBenchmarkCase Create(BenchmarkContext context)
    {
        var engine = new EngineBenchmarkContext(context.Config);
        var scene = CreateScene(context.EntityCount);
        SceneEntityFactory factory = engine.InjectObject(new SceneEntityFactory());
        SceneManager manager = engine.InjectObject(new SceneManager(), factory);
        SceneResourceScope scope = engine.Resources.LoadSceneResources(scene);
        LoadedScene loaded = manager.AddScene(scope);

        return new SetupOnlyBenchmarkCase(
            Name,
            context.EntityCount,
            engine,
            ctx =>
            {
                ResourceStats stats = ctx.Resources.GetStats();
                return new Dictionary<string, double>
                {
                    ["sceneEntities"] = loaded.Entities.Count,
                    ["meshes"] = stats.MeshCount,
                    ["textures"] = stats.TextureCount,
                    ["textureBytes"] = stats.TextureBytes
                };
            });
    }

    private static SceneDefinition CreateScene(int entityCount)
    {
        var scene = new SceneDefinition
        {
            Id = $"benchmark-scene-{entityCount}",
            Name = $"Benchmark Scene {entityCount}"
        };

        for (int i = 0; i < entityCount; i++)
        {
            var definition = new SceneObjectDefinition();
            definition.Components.Add(new Info($"benchmark-{i}", $"Benchmark {i}"));
            definition.Components.Add(new Transform(BenchmarkMath.GridPosition(i, entityCount, Fix.One, Fix.Zero, new Fix(8))));
            definition.Components.Add(new Mesh(i % 7 == 0 ? "cube" : "plane"));
            definition.Components.Add(CreateMaterial(i));

            if (i % 6 == 0)
            {
                definition.Components.Add(new ShadowCaster());
            }

            if (i % 10 == 0)
            {
                definition.Components.Add(i % 20 == 0 ? RigidBody.Static() : RigidBody.Dynamic(Fix.One));
                definition.Components.Add(Collider.Box(Vector3.One));
            }

            scene.Objects.Add(definition);
        }

        return scene;
    }

    private static Material CreateMaterial(int index)
    {
        PixelColor color = PixelColor.FromRgb(
            BenchmarkMath.Channel(index, 16),
            BenchmarkMath.Channel(index, 17),
            BenchmarkMath.Channel(index, 18));

        return index % 3 == 0
            ? new Material(color, new MaterialTexture("checker"))
            : new Material(color);
    }
}
