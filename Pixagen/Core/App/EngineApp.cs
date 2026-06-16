using System.Diagnostics;
using Pixagen.Ecs.DI;
using Pixagen.Game;
using Pixagen.Rendering;
using Pixagen.Rendering.Vulkan;
using PixagenDebug = Pixagen.Core.Debugging.Debug;

namespace Pixagen.Core.App;

public sealed class EngineApp : IDisposable
{
    private readonly EngineOptions _options;
    private readonly Time _time;
    private readonly InputState _input;
    private readonly FrameBuffer _frameBuffer;
    private readonly IRenderBackend _renderBackend;
    private readonly PerformanceStats _performanceStats;
    private readonly PixagenDebug _debug;
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
        PixagenDebug debug,
        IWorld world,
        Systems systems)
    {
        _options = options;
        _time = time;
        _input = input;
        _frameBuffer = frameBuffer;
        _renderBackend = renderBackend;
        _performanceStats = performanceStats;
        _debug = debug;
        _world = world;
        _systems = systems;
    }

    public static EngineApp CreateDefault(EngineOptions options)
    {
        PixagenDebug debug = PixagenDebug.CreateDefault();
        debug.InstallGlobalExceptionHandlers();

        IRenderBackend? renderBackend = null;
        IWorld? world = null;
        Systems? systems = null;

        try
        {
            var time = new Time();
            var input = new InputState();
            var renderBackendOptions = RenderBackendOptions.FromEngineOptions(options);
            var frameBuffer = new FrameBuffer(
                Math.Max(1, renderBackendOptions.WindowWidth / renderBackendOptions.CellPixelSize),
                Math.Max(1, renderBackendOptions.WindowHeight / renderBackendOptions.CellPixelSize));
            var performanceStats = new PerformanceStats();

            renderBackend = new VulkanWindowBackend(performanceStats);
            IRaycastComputeRenderer raycastComputeRenderer = renderBackend as IRaycastComputeRenderer ?? NullRaycastComputeRenderer.Instance;

            world = WorldBuilder.Build();
            systems = new RuntimeSystemContainer().Create(world);
            systems.SystemException += systemException => LogSystemException(debug, systemException);
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
                    debug,
                    options.RenderSettings);
            systems.Init();

            return new EngineApp(
                options,
                time,
                input,
                frameBuffer,
                renderBackend,
                performanceStats,
                debug,
                world,
                systems);
        }
        catch (Exception exception)
        {
            debug.Exception(exception, "EngineApp.CreateDefault failed.");
            DisposeCreateFailurePart(debug, nameof(renderBackend), () => renderBackend?.Dispose());
            DisposeCreateFailurePart(debug, nameof(systems), () => systems?.Dispose());
            DisposeCreateFailurePart(debug, nameof(world), () => world?.Dispose());
            debug.Dispose();
            throw;
        }
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
                _systems.Update(_time.ConsumeFixedSteps());
            }
            catch (Exception exception)
            {
                _debug.Exception(exception, "Engine frame runtime exception. Execution will continue.");
            }
            finally
            {
                performanceFrame.Dispose();
            }

            if (ShouldStop())
            {
                break;
            }

            TrySleepToTargetFrameTime(frameStart);
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

    private bool ShouldStop()
    {
        try
        {
            return _options.RunSingleFrame || _input.ExitRequested || _renderBackend.IsCloseRequested || _stopRequested;
        }
        catch (Exception exception)
        {
            _debug.Exception(exception, "Engine stop condition check failed. Execution will continue.");
            return false;
        }
    }

    private void TrySleepToTargetFrameTime(long frameStart)
    {
        try
        {
            SleepToTargetFrameTime(frameStart);
        }
        catch (Exception exception)
        {
            _debug.Exception(exception, "Engine frame sleep failed. Execution will continue.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        DisposeRuntimePart(nameof(_renderBackend), _renderBackend.Dispose);
        DisposeRuntimePart(nameof(_systems), _systems.Dispose);
        DisposeRuntimePart(nameof(_world), _world.Dispose);
        _debug.Dispose();
        GC.SuppressFinalize(this);
    }

    private void DisposeRuntimePart(string name, Action dispose)
    {
        try
        {
            dispose();
        }
        catch (Exception exception)
        {
            _debug.Exception(exception, $"EngineApp.Dispose failed for {name}.");
        }
    }

    private static void DisposeCreateFailurePart(PixagenDebug debug, string name, Action dispose)
    {
        try
        {
            dispose();
        }
        catch (Exception exception)
        {
            debug.Exception(exception, $"EngineApp.CreateDefault cleanup failed for {name}.");
        }
    }

    private static void LogSystemException(PixagenDebug debug, SystemExecutionException systemException)
    {
        debug.Exception(
            systemException.Exception,
            $"System runtime exception in {systemException.System.GetType().FullName}.{systemException.Stage}. Execution will continue.");
    }
}
