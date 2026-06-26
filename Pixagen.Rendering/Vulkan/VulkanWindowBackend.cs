using System.Numerics;
using Pixagen.Ecs.DI;
using Pixagen.Rendering.Raycasting;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Pixagen.Rendering.Vulkan;

public sealed class VulkanWindowBackend : IRenderBackend, IRaycastComputeRenderer, IUiOverlayRenderBackend
{
    private const string WindowTitle = "Pixagen";

    private readonly IRenderPerformanceSink _performanceStats;
    private readonly VulkanGpuFrameTracker _gpuFrames;
    private readonly VulkanRenderTargets _targets = new();
    private readonly VulkanCompositePass _compositePass = new();
    private readonly VulkanRaycastComputePass _raycastPass = new();
    private readonly CustomInject<IVulkanShaderProvider> _shaderProvider = default;
    private Sdl2Window? _window;
    private GraphicsDevice? _graphicsDevice;
    private CommandList? _commandList;
    private RenderBackendOptions _options = null!;
    private int _embeddedWidth;
    private int _embeddedHeight;
    private bool _closeRequested;
    private bool _hasComputeSceneFrame;
    private int _currentRenderCalls;
    private int _currentPasses;

    public VulkanWindowBackend(IRenderPerformanceSink performanceStats)
    {
        _performanceStats = performanceStats;
        _gpuFrames = new VulkanGpuFrameTracker(performanceStats);
    }

    public bool IsCloseRequested => _gpuFrames.IsStalled || _closeRequested || (_window is not null && !_window.Exists);

    public void SetUiOverlayBuffer(UiOverlayBuffer uiOverlay)
    {
        _targets.SetUiOverlayBuffer(uiOverlay);
    }

    public void Initialize(RenderBackendOptions options)
    {
        ThrowIfInitialized();
        _options = options;
        VulkanNativeEnvironment.Configure();

        var windowCreateInfo = new WindowCreateInfo(
            x: 100,
            y: 100,
            windowWidth: options.WindowWidth,
            windowHeight: options.WindowHeight,
            windowInitialState: options.Fullscreen ? WindowState.BorderlessFullScreen : WindowState.Normal,
            windowTitle: WindowTitle);

        _window = VeldridStartup.CreateWindow(ref windowCreateInfo);
        _window.CursorVisible = options.ShowCursor;
        _window.Resizable = !options.Fullscreen;
        _window.Closing += () => _closeRequested = true;
        VulkanNativeEnvironment.ApplyWindowMode(_window, options);
        InitializeGraphicsDevice();
    }

    public void InitializeFromNativeWindow(
        RenderBackendOptions options,
        IntPtr nativeWindowHandle,
        string? handleDescriptor = null)
    {
        if (nativeWindowHandle == IntPtr.Zero)
        {
            throw new ArgumentException("Embedded Vulkan backend requires a non-zero native window handle.", nameof(nativeWindowHandle));
        }

        ThrowIfInitialized();
        _options = options;
        VulkanNativeEnvironment.Configure();

        if (OperatingSystem.IsMacOS())
        {
            InitializeEmbeddedSwapchain(
                CreateMacOSSwapchainSource(nativeWindowHandle, handleDescriptor),
                options.WindowWidth,
                options.WindowHeight);
            return;
        }

        _window = new Sdl2Window(nativeWindowHandle, threadedProcessing: false)
        {
            CursorVisible = options.ShowCursor,
            Resizable = false
        };
        _window.Closing += () => _closeRequested = true;
        InitializeGraphicsDevice();
    }

    public void ResizeEmbeddedSurface(int width, int height)
    {
        _embeddedWidth = Math.Max(1, width);
        _embeddedHeight = Math.Max(1, height);
    }

    private void InitializeGraphicsDevice()
    {
        GraphicsDeviceOptions graphicsOptions = CreateGraphicsDeviceOptions();
        try
        {
            _graphicsDevice = VeldridStartup.CreateGraphicsDevice(RequireWindow(), graphicsOptions, GraphicsBackend.Vulkan);
        }
        catch (Exception exception) when (VulkanNativeEnvironment.IsVulkanLoaderFailure(exception))
        {
            throw new InvalidOperationException(
                "Vulkan loader was not found. On macOS the app needs MoltenVK/libvulkan.dylib in the build output " +
                "or an installed Vulkan SDK. Rebuild the project so native MoltenVK assets are copied, or install " +
                "the Vulkan SDK and make libvulkan.dylib visible to the process.",
                exception);
        }

        _commandList = _graphicsDevice.ResourceFactory.CreateCommandList();
        RequireWindow().Resized += ResizeSwapchain;
        ResizeSwapchain();

        WarmUpShaders();
    }

    private void InitializeEmbeddedSwapchain(SwapchainSource source, int width, int height)
    {
        _embeddedWidth = Math.Max(1, width);
        _embeddedHeight = Math.Max(1, height);

        GraphicsDeviceOptions graphicsOptions = CreateGraphicsDeviceOptions();
        var swapchainDescription = new SwapchainDescription(
            source,
            (uint)_embeddedWidth,
            (uint)_embeddedHeight,
            depthFormat: null,
            syncToVerticalBlank: true,
            colorSrgb: false);

        try
        {
            _graphicsDevice = GraphicsDevice.CreateVulkan(graphicsOptions, swapchainDescription);
        }
        catch (Exception exception) when (VulkanNativeEnvironment.IsVulkanLoaderFailure(exception))
        {
            throw new InvalidOperationException(
                "Vulkan loader was not found. On macOS the app needs MoltenVK/libvulkan.dylib in the build output " +
                "or an installed Vulkan SDK. Rebuild the project so native MoltenVK assets are copied, or install " +
                "the Vulkan SDK and make libvulkan.dylib visible to the process.",
                exception);
        }

        _commandList = _graphicsDevice.ResourceFactory.CreateCommandList();
        ResizeSwapchain();
        WarmUpShaders();
    }

    public void PumpInput(IRenderInputSink input)
    {
        _gpuFrames.PollCompleted();
        input.BeginFrame();
        if (_window is null)
        {
            return;
        }

        Sdl2Window window = _window;
        InputSnapshot snapshot = window.PumpEvents();
        foreach (KeyEvent keyEvent in snapshot.KeyEvents)
        {
            if (VulkanInputMapper.TryMapKey(keyEvent.Key, out RenderInputKey key))
            {
                input.SetKey(key, keyEvent.Down);
            }
        }

        input.SetMousePosition(snapshot.MousePosition.X, snapshot.MousePosition.Y);
        foreach (MouseEvent mouseEvent in snapshot.MouseEvents)
        {
            if (VulkanInputMapper.TryMapMouseButton(mouseEvent.MouseButton, out RenderMouseButton button))
            {
                input.SetMouseButton(button, mouseEvent.Down);
            }
        }

        Vector2 mouseDelta = window.MouseDelta;
        if (_options.CaptureMouse)
        {
            input.AddMouseDelta(mouseDelta.X, mouseDelta.Y);
        }

        if (MathF.Abs(snapshot.WheelDelta) > float.Epsilon)
        {
            input.AddMouseWheelDelta(snapshot.WheelDelta);
        }

        if (!window.Exists)
        {
            input.RequestExit();
        }
    }

    public (int Width, int Height) GetFrameBufferSize()
    {
        int cellSize = Math.Max(1, _options.CellPixelSize);
        if (_window is null)
        {
            return (
                Math.Max(1, _embeddedWidth / cellSize),
                Math.Max(1, _embeddedHeight / cellSize));
        }

        Sdl2Window window = _window;
        return (
            Math.Max(1, window.Width / cellSize),
            Math.Max(1, window.Height / cellSize));
    }

    public void Present(FrameBuffer frameBuffer)
    {
        if (_gpuFrames.IsStalled)
        {
            return;
        }

        GraphicsDevice graphicsDevice = RequireGraphicsDevice();
        CommandList commandList = RequireCommandList();
        int overlayWidth = frameBuffer.Width * _options.CellPixelSize;
        int overlayHeight = frameBuffer.Height * _options.CellPixelSize;

        _gpuFrames.Throttle(graphicsDevice);
        ResizeSwapchain();
        if (!_hasComputeSceneFrame)
        {
            _targets.UpdateSceneTexture(graphicsDevice, _gpuFrames, frameBuffer, InvalidateSceneDependentResourceSets);
        }

        _targets.UpdateOverlayTexture(
            graphicsDevice,
            _gpuFrames,
            _options,
            overlayWidth,
            overlayHeight,
            _compositePass.InvalidateResourceSet);
        _compositePass.EnsurePipeline(graphicsDevice, LoadVulkanShaders);
        _compositePass.EnsureResourceSet(graphicsDevice, _targets.SceneTextureView, _targets.OverlayTextureView);

        if (_hasComputeSceneFrame)
        {
            SubmitRaycastComputePass(graphicsDevice, commandList);
        }

        SubmitCompositePass(graphicsDevice, commandList);
        _performanceStats.RecordBackendFrame(
            _currentRenderCalls,
            _currentPasses,
            EstimateVramBytes());
        ResetFrameState();
    }

    public bool TryRenderRaycast(in RaycastComputeRequest request)
    {
        if (request.Width <= 0 || request.Height <= 0)
        {
            return false;
        }

        GraphicsDevice graphicsDevice = RequireGraphicsDevice();
        _targets.EnsureSceneTexture(
            graphicsDevice,
            _gpuFrames,
            request.Width,
            request.Height,
            InvalidateSceneDependentResourceSets);

        if (!_raycastPass.Prepare(graphicsDevice, _targets.SceneTexture, _gpuFrames, LoadVulkanShaders, request))
        {
            return false;
        }

        _gpuFrames.BeginGpuWork();
        _currentRenderCalls++;
        _hasComputeSceneFrame = true;
        _targets.MarkSceneTextureUpdatedByCompute();
        return true;
    }

    public void Dispose()
    {
        _gpuFrames.Dispose();
        _raycastPass.Dispose();
        _compositePass.Dispose();
        UnloadVulkanShaders();
        _targets.Dispose();
        _commandList?.Dispose();
        _graphicsDevice?.Dispose();
        _window?.Close();
    }

    private void ThrowIfInitialized()
    {
        if (_window is not null || _graphicsDevice is not null)
        {
            throw new InvalidOperationException("Vulkan window backend is already initialized.");
        }
    }

    private static GraphicsDeviceOptions CreateGraphicsDeviceOptions()
    {
        return new GraphicsDeviceOptions(
            debug: false,
            swapchainDepthFormat: null,
            syncToVerticalBlank: true,
            resourceBindingModel: ResourceBindingModel.Improved,
            preferStandardClipSpaceYDirection: true,
            preferDepthRangeZeroToOne: true);
    }

    private void SubmitRaycastComputePass(GraphicsDevice graphicsDevice, CommandList commandList)
    {
        _gpuFrames.BeginGpuWork();
        _currentPasses++;
        commandList.Begin();
        _raycastPass.Dispatch(commandList);
        commandList.End();
        graphicsDevice.SubmitCommands(commandList);
    }

    private void SubmitCompositePass(GraphicsDevice graphicsDevice, CommandList commandList)
    {
        _gpuFrames.BeginGpuWork();
        _currentRenderCalls++;
        _currentPasses++;
        commandList.Begin();
        _compositePass.Draw(graphicsDevice, commandList);
        commandList.End();

        Fence frameFence = graphicsDevice.ResourceFactory.CreateFence(signaled: false);
        graphicsDevice.SubmitCommands(commandList, frameFence);
        _gpuFrames.TrackSubmittedFrame(frameFence);
        graphicsDevice.SwapBuffers();
    }

    private void WarmUpShaders()
    {
        GraphicsDevice graphicsDevice = RequireGraphicsDevice();
        _compositePass.EnsurePipeline(graphicsDevice, LoadVulkanShaders);
        _targets.EnsureFallbackSceneTexture(graphicsDevice, _gpuFrames, InvalidateSceneDependentResourceSets);
        _targets.EnsureTransparentOverlayTexture(graphicsDevice, _gpuFrames, _compositePass.InvalidateResourceSet);
        _compositePass.EnsureResourceSet(graphicsDevice, _targets.SceneTextureView, _targets.OverlayTextureView);
        _raycastPass.WarmUp(graphicsDevice, _targets.SceneTexture, _gpuFrames, LoadVulkanShaders);
    }

    private void ResetFrameState()
    {
        _gpuFrames.EndLogicalFrame();
        _currentRenderCalls = 0;
        _currentPasses = 0;
        _hasComputeSceneFrame = false;
    }

    private long EstimateVramBytes()
    {
        return _targets.EstimateVramBytes() + _raycastPass.EstimateVramBytes();
    }

    private void InvalidateSceneDependentResourceSets()
    {
        _compositePass.InvalidateResourceSet();
        _raycastPass.InvalidateResourceSet();
    }

    private void ResizeSwapchain()
    {
        if (_graphicsDevice is null)
        {
            return;
        }

        int desiredWidth = _window?.Width ?? _embeddedWidth;
        int desiredHeight = _window?.Height ?? _embeddedHeight;
        if (desiredWidth <= 0 || desiredHeight <= 0)
        {
            return;
        }

        uint width = (uint)Math.Max(1, desiredWidth);
        uint height = (uint)Math.Max(1, desiredHeight);
        if (_graphicsDevice.MainSwapchain.Framebuffer.Width == width &&
            _graphicsDevice.MainSwapchain.Framebuffer.Height == height)
        {
            return;
        }

        _graphicsDevice.MainSwapchain.Resize(width, height);
    }

    private static SwapchainSource CreateMacOSSwapchainSource(IntPtr nativeWindowHandle, string? handleDescriptor)
    {
        if (handleDescriptor?.Contains("NSWindow", StringComparison.OrdinalIgnoreCase) == true)
        {
            return SwapchainSource.CreateNSWindow(nativeWindowHandle);
        }

        return SwapchainSource.CreateNSView(nativeWindowHandle);
    }

    private Sdl2Window RequireWindow()
    {
        return _window ?? throw new InvalidOperationException("Vulkan window backend is not initialized.");
    }

    private GraphicsDevice RequireGraphicsDevice()
    {
        return _graphicsDevice ?? throw new InvalidOperationException("Vulkan graphics device is not initialized.");
    }

    private CommandList RequireCommandList()
    {
        return _commandList ?? throw new InvalidOperationException("Vulkan command list is not initialized.");
    }

    private VulkanShaderResource LoadVulkanShaders(ResourceFactory factory)
    {
        IVulkanShaderProvider shaderProvider = _shaderProvider.Value ??
            throw new InvalidOperationException($"{nameof(VulkanWindowBackend)} requires {nameof(IVulkanShaderProvider)}.");
        return shaderProvider.LoadVulkanShaders(factory);
    }

    private void UnloadVulkanShaders()
    {
        IVulkanShaderProvider? shaderProvider = _shaderProvider.Value;
        if (shaderProvider is null)
        {
            return;
        }

        try
        {
            shaderProvider.UnloadVulkanShaders();
        }
        catch (ObjectDisposedException)
        {
            // Startup failure cleanup can dispose the shader provider before the backend reaches this path.
        }
    }
}
