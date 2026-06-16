namespace Pixagen.Benchmark;

public static class BenchmarkCatalog
{
    public static IReadOnlyList<IBenchmarkScenario> Create()
    {
        return
        [
            new EcsStorageScenario(),
            new SharedSystemsScenario(),
            new PhysicsSystemsScenario(),
            new RenderDynamicScenario(),
            new RenderStaticRebuildScenario(),
            new UiOverlayScenario(),
            new SceneStartupScenario(),
            new FullMixedFrameScenario()
        ];
    }
}
