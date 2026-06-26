using System.Diagnostics;
using Pixagen.Core.App;
using Pixagen.Core.Debugging;
using Pixagen.Core.Performance;
using Pixagen.Core.Timing;
using Pixagen.Editor.Scene;
using Pixagen.Ecs.DI;
using PixagenDebug = Pixagen.Core.Debugging.Debug;

namespace Pixagen.Editor.Preview;

public sealed class EditorPreviewApp : IDisposable
{
    private readonly EngineOptions _options;
    private readonly Time _time;
    private readonly InputState _input;
    private readonly FrameBuffer _frameBuffer;
    private readonly UiOverlayBuffer _overlay;
    private readonly IRenderBackend _renderBackend;
    private readonly PerformanceStats _performanceStats;
    private readonly PixagenDebug _debug;
    private readonly IWorld _world;
    private readonly Systems _systems;
    private readonly object _inputGate = new();
    private readonly Queue<Action<InputState>> _queuedInput = new();
    private bool _initialized;
    private bool _disposed;

    private EditorPreviewApp(
        EngineOptions options,
        Time time,
        InputState input,
        FrameBuffer frameBuffer,
        UiOverlayBuffer overlay,
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
        _overlay = overlay;
        _renderBackend = renderBackend;
        _performanceStats = performanceStats;
        _debug = debug;
        _world = world;
        _systems = systems;
    }

    public static bool ShouldRun(string[] args)
    {
        return args.Any(arg => string.Equals(arg, "--scene-preview", StringComparison.OrdinalIgnoreCase));
    }

    public static EditorPreviewApp Create(string[] args)
    {
        EditorPreviewLaunchOptions launch = EditorPreviewLaunchOptions.Parse(args);
        return Create(launch, 1280, 720, captureMouse: true, showCursor: false, targetFps: 90);
    }

    public static EditorPreviewApp CreateEmbedded(string scenePath, string overlayPath, int width, int height)
    {
        var launch = new EditorPreviewLaunchOptions(
            Path.GetFullPath(scenePath),
            Path.GetFullPath(overlayPath),
            RunSingleFrame: false);

        return Create(
            launch,
            Math.Max(1, width),
            Math.Max(1, height),
            captureMouse: true,
            showCursor: false,
            targetFps: 60);
    }

    private static EditorPreviewApp Create(
        EditorPreviewLaunchOptions launch,
        int width,
        int height,
        bool captureMouse,
        bool showCursor,
        int targetFps)
    {
        var options = new EngineOptions
        {
            WindowWidth = width,
            WindowHeight = height,
            CellPixelSize = 4,
            Fullscreen = false,
            CaptureMouse = captureMouse,
            ShowCursor = showCursor,
            TargetFps = targetFps,
            AutoResize = true,
            RunSingleFrame = launch.RunSingleFrame,
            ScenePath = launch.ScenePath,
            RenderSettings = RenderSettings.Default
        };

        var debug = PixagenDebug.CreateDefault();
        var time = new Time();
        var input = new InputState();
        var backendOptions = new RenderBackendOptions(
            options.WindowWidth,
            options.WindowHeight,
            options.CellPixelSize,
            options.Fullscreen,
            options.CaptureMouse,
            options.ShowCursor,
            options.RunSingleFrame);
        var frameBuffer = new FrameBuffer(
            Math.Max(1, backendOptions.WindowWidth / backendOptions.CellPixelSize),
            Math.Max(1, backendOptions.WindowHeight / backendOptions.CellPixelSize));
        var overlay = new UiOverlayBuffer();
        var performanceStats = new PerformanceStats();
        var renderBackend = new VulkanWindowBackend(performanceStats);
        if (renderBackend is IUiOverlayRenderBackend uiOverlayBackend)
        {
            uiOverlayBackend.SetUiOverlayBuffer(overlay);
        }

        IRaycastComputeRenderer raycastComputeRenderer =
            renderBackend as IRaycastComputeRenderer ?? NullRaycastComputeRenderer.Instance;
        IWorld world = WorldBuilder.Build();
        Systems systems = new EditorSceneSystemContainer().Create(world);
        systems.SystemException += systemException => debug.Exception(
            systemException.Exception,
            $"Editor preview system failed in {systemException.System.GetType().Name}.{systemException.Stage}.");
        systems.Inject(
            time,
            input,
            options,
            frameBuffer,
            renderBackend,
            backendOptions,
            raycastComputeRenderer,
            performanceStats,
            debug,
            options.RenderSettings,
            overlay,
            new PreviewOverlayFile(launch.OverlayPath));
        systems.Init();

        return new EditorPreviewApp(
            options,
            time,
            input,
            frameBuffer,
            overlay,
            renderBackend,
            performanceStats,
            debug,
            world,
            systems);
    }

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _renderBackend.Initialize(CreateRenderBackendOptions());
        _initialized = true;
    }

    public string LastError { get; private set; } = string.Empty;

    public void InitializeFromNativeWindow(IntPtr nativeWindowHandle, string? handleDescriptor = null)
    {
        if (_initialized)
        {
            return;
        }

        RenderBackendOptions options = CreateRenderBackendOptions();
        if (_renderBackend is VulkanWindowBackend vulkanBackend)
        {
            vulkanBackend.InitializeFromNativeWindow(options, nativeWindowHandle, handleDescriptor);
        }
        else
        {
            _renderBackend.Initialize(options);
        }

        _initialized = true;
    }

    public void Run()
    {
        Initialize();

        do
        {
            long frameStart = Stopwatch.GetTimestamp();
            if (!Tick())
            {
                break;
            }

            SleepToTargetFrameTime(frameStart);
        }
        while (true);
    }

    public bool Tick()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Editor preview runtime must be initialized before ticking.");
        }

        try
        {
            _renderBackend.PumpInput(_input);
            ApplyQueuedInput();
            ResizeFrameBufferToViewport();
            _time.Tick();
            _systems.Update(_time.ConsumeFixedSteps());
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            _debug.Exception(exception, "Editor preview frame failed.");
            return false;
        }

        return !_options.RunSingleFrame && !_input.ExitRequested && !_renderBackend.IsCloseRequested;
    }

    public void ResizeViewport(int width, int height)
    {
        int normalizedWidth = Math.Max(1, width);
        int normalizedHeight = Math.Max(1, height);
        QueueInput(input =>
        {
            if (_renderBackend is VulkanWindowBackend vulkanBackend)
            {
                vulkanBackend.ResizeEmbeddedSurface(normalizedWidth, normalizedHeight);
            }
        });
    }

    public void SetKey(InputKey key, bool isDown)
    {
        QueueInput(input => input.SetKey(key, isDown));
    }

    public void SetMousePosition(float x, float y)
    {
        QueueInput(input => input.SetMousePosition(x, y));
    }

    public void SetMouseButton(InputMouseButton button, bool isDown)
    {
        QueueInput(input => input.SetMouseButton(button, isDown));
    }

    public void AddMouseDelta(float deltaX, float deltaY)
    {
        QueueInput(input => input.AddMouseDelta(deltaX, deltaY));
    }

    public void AddMouseWheelDelta(float delta)
    {
        QueueInput(input => input.AddMouseWheelDelta(delta));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _renderBackend.Dispose();
        _systems.Dispose();
        _world.Dispose();
        _overlay.DisposeInject();
        _debug.Dispose();
        GC.SuppressFinalize(this);
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

    private RenderBackendOptions CreateRenderBackendOptions()
    {
        return new RenderBackendOptions(
            _options.WindowWidth,
            _options.WindowHeight,
            _options.CellPixelSize,
            _options.Fullscreen,
            _options.CaptureMouse,
            _options.ShowCursor,
            _options.RunSingleFrame);
    }

    private void QueueInput(Action<InputState> action)
    {
        lock (_inputGate)
        {
            _queuedInput.Enqueue(action);
        }
    }

    private void ApplyQueuedInput()
    {
        while (true)
        {
            Action<InputState>? action;
            lock (_inputGate)
            {
                if (!_queuedInput.TryDequeue(out action))
                {
                    return;
                }
            }

            action(_input);
        }
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

        int sleepMs = (int)Math.Max(0, remainingTicks * 1000 / Stopwatch.Frequency);
        if (sleepMs > 1)
        {
            Thread.Sleep(sleepMs - 1);
        }
    }
}
