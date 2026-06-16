using System.Numerics;
using Pixagen.Ecs.DI;
using Pixagen.Game.Features.RenderFeature;
using Pixagen.Game.Features.RenderFeature.Raycasting;
using Pixagen.Game.Features.ResourceFeature.Runtime;
using Pixagen.Game.Features.ResourceFeature.Shaders;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Pixagen.Rendering.Vulkan;

public sealed class VulkanWindowBackend : IRenderBackend, IRaycastComputeRenderer, IUiOverlayRenderBackend
{
    private const string WindowTitle = "Pixagen";

    private readonly PerformanceStats _performanceStats;
    private readonly VulkanGpuFrameTracker _gpuFrames;
    private readonly VulkanRenderTargets _targets = new();
    private readonly VulkanCompositePass _compositePass = new();
    private readonly VulkanRaycastComputePass _raycastPass = new();
    private readonly CustomInject<ResourceManager> _resources = default;
    private Sdl2Window? _window;
    private GraphicsDevice? _graphicsDevice;
    private CommandList? _commandList;
    private RenderBackendOptions _options = null!;
    private bool _closeRequested;
    private bool _hasComputeSceneFrame;
    private int _currentRenderCalls;
    private int _currentPasses;

    public VulkanWindowBackend(PerformanceStats performanceStats)
    {
        _performanceStats = performanceStats;
        _gpuFrames = new VulkanGpuFrameTracker(performanceStats);
    }

    public bool IsCloseRequested => _gpuFrames.IsStalled || _closeRequested || _window is not { Exists: true };

    public void SetUiOverlayBuffer(UiOverlayBuffer uiOverlay)
    {
        _targets.SetUiOverlayBuffer(uiOverlay);
    }

    public void Initialize(RenderBackendOptions options)
    {
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

        GraphicsDeviceOptions graphicsOptions = new(
            debug: false,
            swapchainDepthFormat: null,
            syncToVerticalBlank: true,
            resourceBindingModel: ResourceBindingModel.Improved,
            preferStandardClipSpaceYDirection: true,
            preferDepthRangeZeroToOne: true);

        try
        {
            _graphicsDevice = VeldridStartup.CreateGraphicsDevice(_window, graphicsOptions, GraphicsBackend.Vulkan);
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
        _window.Resized += ResizeSwapchain;
        ResizeSwapchain();

        WarmUpShaders();
    }

    public void PumpInput(InputState input)
    {
        _gpuFrames.PollCompleted();
        Sdl2Window window = RequireWindow();
        input.BeginFrame();

        InputSnapshot snapshot = window.PumpEvents();
        foreach (KeyEvent keyEvent in snapshot.KeyEvents)
        {
            if (VulkanInputMapper.TryMapKey(keyEvent.Key, out InputKey key))
            {
                input.SetKey(key, keyEvent.Down);
            }
        }

        Vector2 mouseDelta = window.MouseDelta;
        if (_options.CaptureMouse)
        {
            input.AddMouseDelta(mouseDelta.X, mouseDelta.Y);
        }

        if (!window.Exists)
        {
            input.RequestExit();
        }
    }

    public (int Width, int Height) GetFrameBufferSize()
    {
        Sdl2Window window = RequireWindow();
        int cellSize = Math.Max(1, _options.CellPixelSize);
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
        if (!_hasComputeSceneFrame)
        {
            _targets.EnsureFallbackSceneTexture(graphicsDevice, _gpuFrames, InvalidateSceneDependentResourceSets);
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
        _performanceStats.RecordBackendFrame(new BackendPerformanceReport(
            _currentRenderCalls,
            _currentPasses,
            EstimateVramBytes()));
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
        if (_graphicsDevice is null || _window is null || _window.Width <= 0 || _window.Height <= 0)
        {
            return;
        }

        _graphicsDevice.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
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
        ResourceManager resources = _resources.Value ??
            throw new InvalidOperationException($"{nameof(VulkanWindowBackend)} requires {nameof(ResourceManager)}.");
        return resources.LoadVulkanShaders(factory);
    }

    private void UnloadVulkanShaders()
    {
        ResourceManager? resources = _resources.Value;
        if (resources is null)
        {
            return;
        }

        try
        {
            resources.UnloadVulkanShaders();
        }
        catch (ObjectDisposedException)
        {
            // Startup failure cleanup can dispose ResourceManager before the backend reaches this path.
        }
    }
}
