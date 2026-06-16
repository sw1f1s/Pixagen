using System.Diagnostics;
using Pixagen.Ecs.DI;
using Pixagen.Game;
using Pixagen.Rendering;
using Pixagen.Rendering.Vulkan;

namespace Pixagen.Core.App;

public sealed class EngineApp : IDisposable
{
    private readonly EngineOptions _options;
    private readonly Time _time;
    private readonly InputState _input;
    private readonly FrameBuffer _frameBuffer;
    private readonly IRenderBackend _renderBackend;
    private readonly PerformanceStats _performanceStats;
    private readonly IWorld _world;
    private readonly Systems _systems;

    private volatile bool _stopRequested;
    private bool _disposed;

    private EngineApp(
        EngineOptions options,
        Time time,
        InputState input,
        FrameBuffer frameBuffer,
        IRenderBackend renderBackend,
        PerformanceStats performanceStats,
        IWorld world,
        Systems systems)
    {
        _options = options;
        _time = time;
        _input = input;
        _frameBuffer = frameBuffer;
        _renderBackend = renderBackend;
        _performanceStats = performanceStats;
        _world = world;
        _systems = systems;
    }

    public static EngineApp CreateDefault(EngineOptions options)
    {
        var time = new Time();
        var input = new InputState();
        var renderBackendOptions = RenderBackendOptions.FromEngineOptions(options);
        var frameBuffer = new FrameBuffer(
            Math.Max(1, renderBackendOptions.WindowWidth / renderBackendOptions.CellPixelSize),
            Math.Max(1, renderBackendOptions.WindowHeight / renderBackendOptions.CellPixelSize));
        var performanceStats = new PerformanceStats();
        
        IRenderBackend renderBackend = new VulkanWindowBackend(performanceStats);
        IRaycastComputeRenderer raycastComputeRenderer = renderBackend as IRaycastComputeRenderer ?? NullRaycastComputeRenderer.Instance;

        IWorld world = WorldBuilder.Build();
        Systems systems = new RuntimeSystemContainer().Create(world);
        systems
            .Inject(
                time,
                input,
                options,
                frameBuffer,
                renderBackend,
                renderBackendOptions,
                raycastComputeRenderer,
                performanceStats,
                options.RenderSettings);
        systems.Init();

        return new EngineApp(
            options,
            time,
            input,
            frameBuffer,
            renderBackend,
            performanceStats,
            world,
            systems);
    }

    public void Run()
    {
        _renderBackend.Initialize(RenderBackendOptions.FromEngineOptions(_options));

        do
        {
            long frameStart = Stopwatch.GetTimestamp();
            PerformanceFrameScope performanceFrame = _performanceStats.BeginFrame();

            try
            {
                _renderBackend.PumpInput(_input);
                ResizeFrameBufferToViewport();
                _time.Tick();
                _systems.Update();
            }
            finally
            {
                performanceFrame.Dispose();
            }

            if (_options.RunSingleFrame || _input.ExitRequested || _renderBackend.IsCloseRequested || _stopRequested)
            {
                break;
            }

            SleepToTargetFrameTime(frameStart);
        }
        while (true);
    }

    public void Stop()
    {
        _stopRequested = true;
    }

    private void ResizeFrameBufferToViewport()
    {
        if (!_options.AutoResize)
        {
            return;
        }

        (int width, int height) = _renderBackend.GetFrameBufferSize();
        _frameBuffer.Resize(width, height);
    }

    private void SleepToTargetFrameTime(long frameStart)
    {
        if (_options.TargetFps <= 0)
        {
            return;
        }

        long targetTicks = Stopwatch.Frequency / _options.TargetFps;
        long targetTimestamp = frameStart + targetTicks;
        long remainingTicks = targetTimestamp - Stopwatch.GetTimestamp();

        if (remainingTicks <= 0)
        {
            return;
        }

        long sleepThreshold = Stopwatch.Frequency / 500;
        if (remainingTicks > sleepThreshold)
        {
            double sleepMs = (remainingTicks - sleepThreshold) * 1000.0 / Stopwatch.Frequency;
            Thread.Sleep(Math.Max(0, (int)sleepMs));
        }

        var spinWait = new SpinWait();
        while (Stopwatch.GetTimestamp() < targetTimestamp)
        {
            spinWait.SpinOnce();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _systems.Dispose();
        _world.Dispose();
        _renderBackend.Dispose();
        GC.SuppressFinalize(this);
    }
}
