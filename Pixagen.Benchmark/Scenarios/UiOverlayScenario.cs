using Pixagen.Ecs.Runtime;
using Pixagen.Ecs.DI;
using Pixagen.Game.Features.UIFeature;
using Pixagen.Game.Features.UIFeature.Components;
using Pixagen.Rendering;

namespace Pixagen.Benchmark;

public sealed class UiOverlayScenario : IBenchmarkScenario
{
    public string Name => "ui.overlay";
    public string Description => "FPS/profiler text mutation, UI sorting, overlay command generation, and backend present.";
    public BenchmarkMeasurementMode Mode => BenchmarkMeasurementMode.SteadyState;

    public IBenchmarkCase Create(BenchmarkContext context)
    {
        var engine = new EngineBenchmarkContext(context.Config);
        var world = new WorldInject(engine.World);
        var transforms = new ComponentInject<TransformUI>(engine.World);
        var texts = new ComponentInject<TextUI>(engine.World);
        var fpsCounters = new ComponentInject<FPSCounterUI>(engine.World);
        var profilers = new ComponentInject<ProfilerUI>(engine.World);

        int width = Math.Max(1, engine.Config.RenderWidth / 8);
        for (int i = 0; i < context.EntityCount; i++)
        {
            Entity entity = world.Create<TransformUI>();
            transforms.Get(entity) = new TransformUI(i % width, i / width, i % 16);
            texts.Add(entity, new TextUI(
                $"entity {i:000000}",
                PixelColor.FromRgb(
                    BenchmarkMath.Channel(i, 13),
                    BenchmarkMath.Channel(i, 14),
                    BenchmarkMath.Channel(i, 15)),
                fontSize: 1 + (i % 3)));

            if (i % 64 == 0)
            {
                fpsCounters.Add(entity, new FPSCounterUI());
            }

            if (i % 128 == 0)
            {
                profilers.Add(entity, new ProfilerUI(Fix.One / new Fix(2)));
            }
        }

        Systems systems = engine.BuildSystems(new UIFeatureSystemsGroup());
        return new SystemsBenchmarkCase(
            Name,
            context.EntityCount,
            engine,
            systems,
            counters: ctx =>
            {
                Dictionary<string, double> counters = ctx.CommonCounters();
                counters["uiCommands"] = ctx.UiOverlay.Texts.Count;
                counters["presentChecksum"] = ctx.RenderBackend.PresentChecksum;
                return counters;
            });
    }
}
