using Pixagen.Ecs.Runtime;
using Pixagen.Ecs.DI;
using Pixagen.Game.Features.PhysicsFeature.Runtime;
using Pixagen.Game.Features.RenderFeature;
using Pixagen.Game.Features.RenderFeature.Raycasting;
using Pixagen.Game.Features.ResourceFeature.Runtime;
using Pixagen.Game.Features.SharedFeature.Helper;
using Pixagen.Rendering;

namespace Pixagen.Benchmark;

public sealed class EngineBenchmarkContext : IDisposable
{
    private bool _disposed;

    public EngineBenchmarkContext(BenchmarkConfig config)
    {
        Config = config;
        Options = CreateOptions(config);
        BackendOptions = RenderBackendOptions.FromEngineOptions(Options);
        Time = new Time();
        Input = new InputState();
        World = WorldBuilder.Build();
        Resources = new ResourceManager();
        PhysicsWorld = new PhysicsWorld();
        RenderSceneCache = new RenderSceneCache();
        State = new EntityStateHelper();
        FrameBuffer = new FrameBuffer(config.RenderWidth, config.RenderHeight);
        RenderBackend = new HeadlessRenderBackend(config.RenderWidth, config.RenderHeight);
        ComputeRenderer = new MeasuredRaycastComputeRenderer();
        PerformanceStats = new PerformanceStats();
        UiOverlay = new UiOverlayBuffer();

        using var injector = new Systems(World);
        injector.InjectObject(State);
    }

    public BenchmarkConfig Config { get; }
    public EngineOptions Options { get; }
    public RenderBackendOptions BackendOptions { get; }
    public Time Time { get; }
    public InputState Input { get; }
    public IWorld World { get; }
    public ResourceManager Resources { get; }
    public PhysicsWorld PhysicsWorld { get; }
    public RenderSceneCache RenderSceneCache { get; }
    public EntityStateHelper State { get; }
    public FrameBuffer FrameBuffer { get; }
    public HeadlessRenderBackend RenderBackend { get; }
    public MeasuredRaycastComputeRenderer ComputeRenderer { get; }
    public PerformanceStats PerformanceStats { get; }
    public UiOverlayBuffer UiOverlay { get; }

    public object[] Services =>
    [
        Time,
        Input,
        Options,
        Resources,
        PhysicsWorld,
        RenderSceneCache,
        State,
        FrameBuffer,
        RenderBackend,
        BackendOptions,
        ComputeRenderer,
        PerformanceStats,
        UiOverlay,
        Options.RenderSettings
    ];

    public Systems BuildSystems(params ISystem[] systems)
    {
        return BuildSystems([], systems);
    }

    public Systems BuildSystems(object[] extraServices, params ISystem[] systems)
    {
        var container = new Systems(World);
        foreach (ISystem system in systems)
        {
            container.Add(system);
        }

        container.Inject(Services.Concat(extraServices).ToArray());
        container.Init();
        return container;
    }

    public T InjectObject<T>(T target, params object[] extraServices)
        where T : class
    {
        using var container = new Systems(World);
        return container.InjectObject(target, Services.Concat(extraServices).ToArray());
    }

    public void AdvanceFrame()
    {
        Input.BeginFrame();
        Input.AddMouseDelta(2, -1);
        Time.Advance(Fix.One / new Fix(60));
    }

    public Dictionary<string, double> CommonCounters()
    {
        PerformanceSnapshot snapshot = PerformanceStats.Snapshot;
        return new Dictionary<string, double>
        {
            ["fps"] = snapshot.FramesPerSecond,
            ["instantFps"] = snapshot.InstantFramesPerSecond,
            ["frameCount"] = snapshot.FrameCount,
            ["triangles"] = snapshot.Triangles,
            ["shadowTriangles"] = snapshot.ShadowTriangles,
            ["textures"] = snapshot.TextureCount,
            ["textureBytes"] = snapshot.TextureBytes,
            ["managedBytes"] = snapshot.ManagedMemoryBytes,
            ["workingSetBytes"] = snapshot.WorkingSetBytes,
            ["privateBytes"] = snapshot.PrivateMemoryBytes,
            ["gcHeapBytes"] = snapshot.GcHeapBytes,
            ["presentCalls"] = RenderBackend.PresentCalls,
            ["overlayTexts"] = UiOverlay.Texts.Count,
            ["computePixels"] = ComputeRenderer.LastPixels,
            ["computeTriangles"] = ComputeRenderer.LastTriangles,
            ["computeChecksum"] = ComputeRenderer.LastChecksum
        };
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        UiOverlay.DisposeInject();
        RenderSceneCache.DisposeInject();
        PhysicsWorld.Dispose();
        Resources.Dispose();
        World.Dispose();
        RenderBackend.Dispose();
    }

    private static EngineOptions CreateOptions(BenchmarkConfig config)
    {
        return new EngineOptions
        {
            WindowWidth = config.RenderWidth,
            WindowHeight = config.RenderHeight,
            CellPixelSize = 1,
            TargetFps = 0,
            RunSingleFrame = false,
            CaptureMouse = false,
            ShowCursor = false,
            AutoResize = false,
            Fullscreen = false,
            RenderSettings = new RenderSettings(
                new RenderResolution(config.RenderWidth, config.RenderHeight),
                RenderScaleMode.Fixed,
                ShadowQuality.Full,
                Fix.FromDouble(256),
                Fix.FromDouble(160))
        };
    }
}
