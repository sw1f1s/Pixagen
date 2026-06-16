using System.Numerics;
using System.Runtime.InteropServices;
using Pixagen.Ecs.DI;
using Pixagen.Game.Features.RenderFeature;
using Pixagen.Game.Features.RenderFeature.Raycasting;
using Pixagen.Game.Features.RenderFeature.Textures;
using Pixagen.Game.Features.ResourceFeature.Runtime;
using Pixagen.Game.Features.ResourceFeature.Shaders;
using Pixagen.Rendering;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Pixagen.Rendering.Vulkan;

public sealed class VulkanWindowBackend : IRenderBackend, IRaycastComputeRenderer, IUiOverlayRenderBackend
{
    private const string WindowTitle = "Pixagen";
    private const int OverlaySafeInsetPixels = 2;
    private const int MaxGpuFramesInFlight = 2;
    private static readonly TimeSpan GpuFenceTimeout = TimeSpan.FromSeconds(5);

    private readonly PerformanceStats _performanceStats;
    private readonly List<PendingGpuFrame> _pendingGpuFrames = new();
    private UiOverlayBuffer? _uiOverlay;
    private Sdl2Window? _window;
    private GraphicsDevice? _graphicsDevice;
    private CommandList? _commandList;
    private Texture? _sceneTexture;
    private TextureView? _sceneTextureView;
    private Texture? _overlayTexture;
    private TextureView? _overlayTextureView;
    private ResourceLayout? _compositeResourceLayout;
    private ResourceSet? _compositeResourceSet;
    private Pipeline? _compositePipeline;
    private RgbaByte[] _overlayPixels = [];
    private readonly CustomInject<ResourceManager> _resources = default;
    private readonly RgbaByte[] _fallbackScenePixel = [new(0, 0, 0, 255)];
    private RenderBackendOptions _options = null!;
    private int _sceneTextureWidth;
    private int _sceneTextureHeight;
    private int _overlayTextureWidth;
    private int _overlayTextureHeight;
    private bool _closeRequested;
    private bool _hasComputeSceneFrame;
    private bool _sceneTextureHasFallback;

    private ResourceLayout? _raycastComputeLayout;
    private ResourceSet? _raycastComputeSet;
    private Pipeline? _raycastComputePipeline;
    private DeviceBuffer? _raycastParamsBuffer;
    private DeviceBuffer? _triangleBuffer;
    private DeviceBuffer? _shadowTriangleBuffer;
    private DeviceBuffer? _textureInfoBuffer;
    private DeviceBuffer? _texturePixelBuffer;
    private int _triangleCapacity;
    private int _shadowTriangleCapacity;
    private int _textureInfoCapacity;
    private int _texturePixelCapacity;
    private GpuTriangle[] _gpuTriangles = [GpuTriangle.Empty];
    private GpuTriangle[] _gpuShadowTriangles = [GpuTriangle.Empty];
    private GpuTextureInfo[] _gpuTextureInfos = [GpuTextureInfo.Empty];
    private Vector4[] _gpuTexturePixels = [Vector4.One];
    private readonly Dictionary<TextureAsset, int> _raycastTextureIndices = new();
    private readonly List<TextureAsset> _raycastTextures = new();
    private uint _raycastDispatchX;
    private uint _raycastDispatchY;
    private long _currentGpuFrameStartTicks;
    private bool _hasCurrentGpuFrame;
    private int _currentRenderCalls;

    public VulkanWindowBackend(PerformanceStats performanceStats)
    {
        _performanceStats = performanceStats;
    }

    public bool IsCloseRequested => _closeRequested || _window is not { Exists: true };

    public void SetUiOverlayBuffer(UiOverlayBuffer uiOverlay)
    {
        _uiOverlay = uiOverlay ?? throw new ArgumentNullException(nameof(uiOverlay));
    }

    public void Initialize(RenderBackendOptions options)
    {
        _options = options;
        ConfigureVulkanEnvironment();

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
        ApplyWindowMode(_window, options);

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
        catch (Exception exception) when (IsVulkanLoaderFailure(exception))
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
        PollCompletedGpuFrames();
        Sdl2Window window = RequireWindow();
        input.BeginFrame();

        InputSnapshot snapshot = window.PumpEvents();
        foreach (KeyEvent keyEvent in snapshot.KeyEvents)
        {
            if (TryMapKey(keyEvent.Key, out InputKey key))
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
        GraphicsDevice graphicsDevice = RequireGraphicsDevice();
        CommandList commandList = RequireCommandList();
        int overlayWidth = frameBuffer.Width * _options.CellPixelSize;
        int overlayHeight = frameBuffer.Height * _options.CellPixelSize;

        ThrottleGpuFrames(graphicsDevice);

        if (_hasComputeSceneFrame)
        {
            UpdateOverlayTexture(overlayWidth, overlayHeight);
        }
        else
        {
            EnsureFallbackSceneTexture();
            UpdateOverlayTexture(overlayWidth, overlayHeight);
        }

        EnsureCompositeResourceSet();

        BeginGpuFrame();
        _currentRenderCalls++;
        commandList.Begin();
        if (_hasComputeSceneFrame)
        {
            commandList.SetPipeline(_raycastComputePipeline);
            commandList.SetComputeResourceSet(0, _raycastComputeSet);
            commandList.Dispatch(_raycastDispatchX, _raycastDispatchY, 1);
        }

        commandList.SetFramebuffer(graphicsDevice.SwapchainFramebuffer);
        commandList.ClearColorTarget(0, RgbaFloat.Black);
        commandList.SetPipeline(_compositePipeline);
        commandList.SetGraphicsResourceSet(0, _compositeResourceSet);
        commandList.Draw(3);
        commandList.End();

        Fence frameFence = graphicsDevice.ResourceFactory.CreateFence(signaled: false);
        graphicsDevice.SubmitCommands(commandList, frameFence);
        TrackSubmittedGpuFrame(frameFence);
        graphicsDevice.SwapBuffers();
        _performanceStats.RecordBackendFrame(new BackendPerformanceReport(_currentRenderCalls, EstimateVramBytes()));
        _hasCurrentGpuFrame = false;
        _currentRenderCalls = 0;
        _hasComputeSceneFrame = false;
        _raycastDispatchX = 0;
        _raycastDispatchY = 0;
    }

    public bool TryRenderRaycast(in RaycastComputeRequest request)
    {
        if (request.Width <= 0 || request.Height <= 0)
        {
            return false;
        }

        EnsureSceneTexture(request.Width, request.Height);
        EnsureRaycastComputePipeline();
        UploadRaycastScene(request);
        EnsureRaycastComputeResourceSet();

        BeginGpuFrame();
        _currentRenderCalls++;
        _raycastDispatchX = (uint)((request.Width + 7) / 8);
        _raycastDispatchY = (uint)((request.Height + 7) / 8);
        _hasComputeSceneFrame = true;
        _sceneTextureHasFallback = false;
        return true;
    }

    public void Dispose()
    {
        foreach (PendingGpuFrame pendingFrame in _pendingGpuFrames)
        {
            pendingFrame.Fence.Dispose();
        }

        _pendingGpuFrames.Clear();
        UnloadShaders();
        _overlayTextureView?.Dispose();
        _overlayTexture?.Dispose();
        _sceneTextureView?.Dispose();
        _sceneTexture?.Dispose();
        _commandList?.Dispose();
        _graphicsDevice?.Dispose();
        _window?.Close();
    }

    private void WarmUpShaders()
    {
        EnsureCompositePipeline();
        EnsureRaycastComputePipeline();
        EnsureFallbackSceneTexture();
        EnsureTransparentOverlayTexture();
        EnsureCompositeResourceSet();
        EnsureRaycastComputeResourceSet();
    }

    private void BeginGpuFrame()
    {
        if (_hasCurrentGpuFrame)
        {
            return;
        }

        _currentGpuFrameStartTicks = PerformanceStats.Timestamp;
        _hasCurrentGpuFrame = true;
    }

    private void TrackSubmittedGpuFrame(Fence fence)
    {
        if (!_hasCurrentGpuFrame)
        {
            _currentGpuFrameStartTicks = PerformanceStats.Timestamp;
        }

        _pendingGpuFrames.Add(new PendingGpuFrame(fence, _currentGpuFrameStartTicks));
    }

    private void ThrottleGpuFrames(GraphicsDevice graphicsDevice)
    {
        PollCompletedGpuFrames();
        while (_pendingGpuFrames.Count >= MaxGpuFramesInFlight)
        {
            WaitForGpuFrame(graphicsDevice, _pendingGpuFrames[0]);
            CompleteGpuFrame(0);
        }
    }

    private void WaitForPendingGpuFrames(GraphicsDevice graphicsDevice)
    {
        PollCompletedGpuFrames();
        while (_pendingGpuFrames.Count > 0)
        {
            WaitForGpuFrame(graphicsDevice, _pendingGpuFrames[0]);
            CompleteGpuFrame(0);
        }
    }

    private static void WaitForGpuFrame(GraphicsDevice graphicsDevice, PendingGpuFrame pendingFrame)
    {
        if (graphicsDevice.WaitForFence(pendingFrame.Fence, GpuFenceTimeout))
        {
            return;
        }

        throw new InvalidOperationException(
            $"GPU frame did not complete within {GpuFenceTimeout.TotalSeconds:0.#} seconds. " +
            "The Vulkan backend stopped submitting new frames to avoid a silent driver stall.");
    }

    private void PollCompletedGpuFrames()
    {
        for (int i = _pendingGpuFrames.Count - 1; i >= 0; i--)
        {
            PendingGpuFrame pendingFrame = _pendingGpuFrames[i];
            if (!pendingFrame.Fence.Signaled)
            {
                continue;
            }

            CompleteGpuFrame(i);
        }
    }

    private void CompleteGpuFrame(int index)
    {
        PendingGpuFrame pendingFrame = _pendingGpuFrames[index];
        _performanceStats.RecordGpuFrameSince(pendingFrame.StartTicks);
        pendingFrame.Fence.Dispose();
        _pendingGpuFrames.RemoveAt(index);
    }

    private long EstimateVramBytes()
    {
        long bytes = 0;
        bytes += (long)_sceneTextureWidth * _sceneTextureHeight * 4;
        bytes += (long)_overlayTextureWidth * _overlayTextureHeight * 4;

        if (_raycastParamsBuffer is not null)
        {
            bytes += Marshal.SizeOf<GpuRaycastParams>();
        }

        bytes += (long)_triangleCapacity * Marshal.SizeOf<GpuTriangle>();
        bytes += (long)_shadowTriangleCapacity * Marshal.SizeOf<GpuTriangle>();
        bytes += (long)_textureInfoCapacity * Marshal.SizeOf<GpuTextureInfo>();
        bytes += (long)_texturePixelCapacity * Marshal.SizeOf<Vector4>();
        return bytes;
    }

    private void UnloadShaders()
    {
        _raycastComputePipeline?.Dispose();
        _raycastComputePipeline = null;
        _raycastComputeSet?.Dispose();
        _raycastComputeSet = null;
        _raycastComputeLayout?.Dispose();
        _raycastComputeLayout = null;
        _compositePipeline?.Dispose();
        _compositePipeline = null;
        _compositeResourceSet?.Dispose();
        _compositeResourceSet = null;
        _compositeResourceLayout?.Dispose();
        _compositeResourceLayout = null;
        UnloadRaycastComputeBuffers();
        UnloadVulkanShaders();
    }

    private void UnloadRaycastComputeBuffers()
    {
        _raycastParamsBuffer?.Dispose();
        _raycastParamsBuffer = null;
        _triangleBuffer?.Dispose();
        _triangleBuffer = null;
        _shadowTriangleBuffer?.Dispose();
        _shadowTriangleBuffer = null;
        _textureInfoBuffer?.Dispose();
        _textureInfoBuffer = null;
        _texturePixelBuffer?.Dispose();
        _texturePixelBuffer = null;

        _triangleCapacity = 0;
        _shadowTriangleCapacity = 0;
        _textureInfoCapacity = 0;
        _texturePixelCapacity = 0;

        _gpuTriangles = [GpuTriangle.Empty];
        _gpuShadowTriangles = [GpuTriangle.Empty];
        _gpuTextureInfos = [GpuTextureInfo.Empty];
        _gpuTexturePixels = [Vector4.One];
    }

    private void EnsureCompositePipeline()
    {
        if (_compositePipeline is not null)
        {
            return;
        }

        GraphicsDevice graphicsDevice = RequireGraphicsDevice();
        ResourceFactory factory = graphicsDevice.ResourceFactory;
        VulkanShaderResource shaders = LoadVulkanShaders(factory);

        _compositeResourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("SceneTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("SceneSampler", ResourceKind.Sampler, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("OverlayTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("OverlaySampler", ResourceKind.Sampler, ShaderStages.Fragment)));

        GraphicsPipelineDescription pipelineDescription = new(
            BlendStateDescription.SingleDisabled,
            DepthStencilStateDescription.Disabled,
            RasterizerStateDescription.CullNone,
            PrimitiveTopology.TriangleList,
            new ShaderSetDescription([], shaders.CompositeShaders),
            [_compositeResourceLayout],
            graphicsDevice.SwapchainFramebuffer.OutputDescription,
            ResourceBindingModel.Improved);

        _compositePipeline = factory.CreateGraphicsPipeline(ref pipelineDescription);
    }

    private void EnsureSceneTexture(int width, int height)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        if (_sceneTexture is not null && width == _sceneTextureWidth && height == _sceneTextureHeight)
        {
            return;
        }

        GraphicsDevice graphicsDevice = RequireGraphicsDevice();
        ResourceFactory factory = graphicsDevice.ResourceFactory;

        WaitForPendingGpuFrames(graphicsDevice);
        _compositeResourceSet?.Dispose();
        _compositeResourceSet = null;
        _raycastComputeSet?.Dispose();
        _raycastComputeSet = null;
        _sceneTextureView?.Dispose();
        _sceneTexture?.Dispose();

        _sceneTextureWidth = width;
        _sceneTextureHeight = height;
        _sceneTextureHasFallback = false;
        _sceneTexture = factory.CreateTexture(TextureDescription.Texture2D(
            (uint)width,
            (uint)height,
            mipLevels: 1,
            arrayLayers: 1,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Sampled | TextureUsage.Storage));
        _sceneTextureView = factory.CreateTextureView(_sceneTexture);
    }

    private void EnsureFallbackSceneTexture()
    {
        EnsureSceneTexture(1, 1);
        if (_sceneTextureHasFallback)
        {
            return;
        }

        RequireGraphicsDevice().UpdateTexture(_sceneTexture!, _fallbackScenePixel, 0, 0, 0, 1, 1, 1, 0, 0);
        _sceneTextureHasFallback = true;
    }

    private void EnsureOverlayTexture(int width, int height)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        if (_overlayTexture is not null && width == _overlayTextureWidth && height == _overlayTextureHeight)
        {
            return;
        }

        GraphicsDevice graphicsDevice = RequireGraphicsDevice();
        ResourceFactory factory = graphicsDevice.ResourceFactory;

        WaitForPendingGpuFrames(graphicsDevice);
        _compositeResourceSet?.Dispose();
        _compositeResourceSet = null;
        _overlayTextureView?.Dispose();
        _overlayTexture?.Dispose();

        _overlayTextureWidth = width;
        _overlayTextureHeight = height;
        _overlayPixels = new RgbaByte[width * height];
        _overlayTexture = factory.CreateTexture(TextureDescription.Texture2D(
            (uint)width,
            (uint)height,
            mipLevels: 1,
            arrayLayers: 1,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Sampled));
        _overlayTextureView = factory.CreateTextureView(_overlayTexture);
    }

    private void EnsureTransparentOverlayTexture()
    {
        EnsureOverlayTexture(1, 1);
        _overlayPixels[0] = new RgbaByte(0, 0, 0, 0);
        RequireGraphicsDevice().UpdateTexture(_overlayTexture!, _overlayPixels, 0, 0, 0, 1, 1, 1, 0, 0);
    }

    private void EnsureCompositeResourceSet()
    {
        if (_compositeResourceSet is not null)
        {
            return;
        }

        GraphicsDevice graphicsDevice = RequireGraphicsDevice();
        _compositeResourceSet = graphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
            _compositeResourceLayout,
            _sceneTextureView,
            graphicsDevice.PointSampler,
            _overlayTextureView,
            graphicsDevice.PointSampler));
    }

    private void UpdateOverlayTexture(int width, int height)
    {
        EnsureOverlayTexture(width, height);
        RasterizeOverlay();

        RequireGraphicsDevice().UpdateTexture(
            _overlayTexture!,
            _overlayPixels,
            0,
            0,
            0,
            (uint)_overlayTextureWidth,
            (uint)_overlayTextureHeight,
            1,
            0,
            0);
    }

    private void RasterizeOverlay()
    {
        UiOverlayBuffer uiOverlay = _uiOverlay
            ?? throw new InvalidOperationException($"{nameof(VulkanWindowBackend)} requires {nameof(UiOverlayBuffer)}.");
        Array.Fill(_overlayPixels, new RgbaByte(0, 0, 0, 0));

        foreach (UiTextDrawCommand text in uiOverlay.Texts)
        {
            StrokeFont.DrawText(
                _overlayPixels,
                _overlayTextureWidth,
                _overlayTextureHeight,
                text with
                {
                    X = text.X + OverlaySafeInsetPixels,
                    Y = text.Y + OverlaySafeInsetPixels
                },
                Math.Max(1, _options.CellPixelSize));
        }
    }

    private static void ApplyWindowMode(Sdl2Window window, RenderBackendOptions options)
    {
        if (!options.Fullscreen)
        {
            return;
        }

        window.WindowState = WindowState.BorderlessFullScreen;
        Sdl2Native.SDL_SetWindowBordered(window.SdlWindowHandle, 0u);
    }

    private void EnsureRaycastComputePipeline()
    {
        if (_raycastComputePipeline is not null)
        {
            return;
        }

        GraphicsDevice graphicsDevice = RequireGraphicsDevice();
        ResourceFactory factory = graphicsDevice.ResourceFactory;
        VulkanShaderResource shaders = LoadVulkanShaders(factory);

        _raycastComputeLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("OutputTexture", ResourceKind.TextureReadWrite, ShaderStages.Compute),
            new ResourceLayoutElementDescription("RaycastParams", ResourceKind.UniformBuffer, ShaderStages.Compute),
            new ResourceLayoutElementDescription("Triangles", ResourceKind.StructuredBufferReadOnly, ShaderStages.Compute),
            new ResourceLayoutElementDescription("ShadowTriangles", ResourceKind.StructuredBufferReadOnly, ShaderStages.Compute),
            new ResourceLayoutElementDescription("TextureInfos", ResourceKind.StructuredBufferReadOnly, ShaderStages.Compute),
            new ResourceLayoutElementDescription("TexturePixels", ResourceKind.StructuredBufferReadOnly, ShaderStages.Compute)));

        ComputePipelineDescription pipelineDescription = new(
            shaders.RaycastShader,
            _raycastComputeLayout,
            8,
            8,
            1);
        _raycastComputePipeline = factory.CreateComputePipeline(ref pipelineDescription);

        _raycastParamsBuffer = factory.CreateBuffer(new BufferDescription(
            (uint)Marshal.SizeOf<GpuRaycastParams>(),
            BufferUsage.UniformBuffer | BufferUsage.Dynamic));
        EnsureStructuredBuffer(ref _triangleBuffer, ref _triangleCapacity, 1, Marshal.SizeOf<GpuTriangle>());
        EnsureStructuredBuffer(ref _shadowTriangleBuffer, ref _shadowTriangleCapacity, 1, Marshal.SizeOf<GpuTriangle>());
        EnsureStructuredBuffer(ref _textureInfoBuffer, ref _textureInfoCapacity, 1, Marshal.SizeOf<GpuTextureInfo>());
        EnsureStructuredBuffer(ref _texturePixelBuffer, ref _texturePixelCapacity, 1, Marshal.SizeOf<Vector4>());
    }

    private void UploadRaycastScene(in RaycastComputeRequest request)
    {
        GraphicsDevice graphicsDevice = RequireGraphicsDevice();

        GpuRaycastParams parameters = GpuRaycastParams.From(request);
        graphicsDevice.UpdateBuffer(_raycastParamsBuffer!, 0, ref parameters);

        _raycastTextureIndices.Clear();
        _raycastTextures.Clear();
        try
        {
            int triangleCount = FillTriangles(
                ref _gpuTriangles,
                request.StaticPrimitives.Triangles,
                request.DynamicPrimitives.Triangles,
                _raycastTextureIndices,
                _raycastTextures);
            int shadowTriangleCount = FillTriangles(
                ref _gpuShadowTriangles,
                request.StaticPrimitives.ShadowTriangles,
                request.DynamicPrimitives.ShadowTriangles,
                _raycastTextureIndices,
                _raycastTextures);
            int textureInfoCount = FillTextures(ref _gpuTextureInfos, ref _gpuTexturePixels, _raycastTextures);

            EnsureStructuredBuffer(ref _triangleBuffer, ref _triangleCapacity, Math.Max(1, triangleCount), Marshal.SizeOf<GpuTriangle>());
            EnsureStructuredBuffer(ref _shadowTriangleBuffer, ref _shadowTriangleCapacity, Math.Max(1, shadowTriangleCount), Marshal.SizeOf<GpuTriangle>());
            EnsureStructuredBuffer(ref _textureInfoBuffer, ref _textureInfoCapacity, Math.Max(1, textureInfoCount), Marshal.SizeOf<GpuTextureInfo>());
            EnsureStructuredBuffer(ref _texturePixelBuffer, ref _texturePixelCapacity, Math.Max(1, _gpuTexturePixels.Length), Marshal.SizeOf<Vector4>());

            graphicsDevice.UpdateBuffer(_triangleBuffer!, 0, _gpuTriangles.AsSpan(0, Math.Max(1, triangleCount)));
            graphicsDevice.UpdateBuffer(_shadowTriangleBuffer!, 0, _gpuShadowTriangles.AsSpan(0, Math.Max(1, shadowTriangleCount)));
            graphicsDevice.UpdateBuffer(_textureInfoBuffer!, 0, _gpuTextureInfos.AsSpan(0, Math.Max(1, textureInfoCount)));
            graphicsDevice.UpdateBuffer(_texturePixelBuffer!, 0, _gpuTexturePixels.AsSpan(0, Math.Max(1, _gpuTexturePixels.Length)));
        }
        finally
        {
            _raycastTextureIndices.Clear();
            _raycastTextures.Clear();
        }
    }

    private void EnsureRaycastComputeResourceSet()
    {
        if (_raycastComputeSet is not null)
        {
            return;
        }

        _raycastComputeSet = RequireGraphicsDevice().ResourceFactory.CreateResourceSet(new ResourceSetDescription(
            _raycastComputeLayout,
            _sceneTexture,
            _raycastParamsBuffer,
            _triangleBuffer,
            _shadowTriangleBuffer,
            _textureInfoBuffer,
            _texturePixelBuffer));
    }

    private void EnsureStructuredBuffer(
        ref DeviceBuffer? buffer,
        ref int capacity,
        int requiredCount,
        int stride)
    {
        requiredCount = Math.Max(1, requiredCount);
        if (buffer is not null && capacity >= requiredCount)
        {
            return;
        }

        GraphicsDevice graphicsDevice = RequireGraphicsDevice();
        if (buffer is not null)
        {
            WaitForPendingGpuFrames(graphicsDevice);
        }

        int newCapacity = Math.Max(requiredCount, Math.Max(1, capacity * 2));
        buffer?.Dispose();
        buffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(
            (uint)(newCapacity * stride),
            BufferUsage.StructuredBufferReadOnly,
            (uint)stride));
        capacity = newCapacity;
        _raycastComputeSet?.Dispose();
        _raycastComputeSet = null;
    }

    private static int FillTriangles(
        ref GpuTriangle[] destination,
        List<TrianglePrimitive> staticPrimitives,
        List<TrianglePrimitive> dynamicPrimitives,
        Dictionary<TextureAsset, int> textureIndices,
        List<TextureAsset> textures)
    {
        int count = staticPrimitives.Count + dynamicPrimitives.Count;
        destination = EnsureArray(destination, Math.Max(1, count));
        int index = 0;
        foreach (TrianglePrimitive triangle in staticPrimitives)
        {
            destination[index++] = GpuTriangle.From(triangle, GetTextureIndex(triangle.Material.Texture, textureIndices, textures));
        }

        foreach (TrianglePrimitive triangle in dynamicPrimitives)
        {
            destination[index++] = GpuTriangle.From(triangle, GetTextureIndex(triangle.Material.Texture, textureIndices, textures));
        }

        if (count == 0)
        {
            destination[0] = GpuTriangle.Empty;
        }

        return count;
    }

    private static int FillTextures(
        ref GpuTextureInfo[] textureInfos,
        ref Vector4[] texturePixels,
        List<TextureAsset> textures)
    {
        int textureCount = textures.Count;
        textureInfos = EnsureArray(textureInfos, Math.Max(1, textureCount));

        int pixelCount = 0;
        for (int i = 0; i < textures.Count; i++)
        {
            TextureAsset texture = textures[i];
            textureInfos[i] = new GpuTextureInfo(texture.Width, texture.Height, pixelCount, texture.MipCount);
            pixelCount += texture.MipPixelCount;
        }

        texturePixels = EnsureArray(texturePixels, Math.Max(1, pixelCount));
        int pixelIndex = 0;
        foreach (TextureAsset texture in textures)
        {
            foreach (TextureMipLevel mipLevel in texture.MipLevels)
            {
                foreach (TexturePixel pixel in mipLevel.Pixels)
                {
                    texturePixels[pixelIndex++] = new Vector4(
                        pixel.R / 255f,
                        pixel.G / 255f,
                        pixel.B / 255f,
                        pixel.A / 255f);
                }
            }
        }

        if (textureCount == 0)
        {
            textureInfos[0] = GpuTextureInfo.Empty;
        }

        if (pixelCount == 0)
        {
            texturePixels[0] = Vector4.One;
        }

        return textureCount;
    }

    private static int GetTextureIndex(
        TextureAsset? texture,
        Dictionary<TextureAsset, int> textureIndices,
        List<TextureAsset> textures)
    {
        if (texture is null)
        {
            return -1;
        }

        if (textureIndices.TryGetValue(texture, out int index))
        {
            return index;
        }

        index = textures.Count;
        textures.Add(texture);
        textureIndices[texture] = index;
        return index;
    }

    private static T[] EnsureArray<T>(T[] source, int requiredLength)
    {
        return source.Length >= requiredLength ? source : new T[requiredLength];
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

    private static Vector4 ToVector4(PixelColor color, float alpha)
    {
        const float scale = 1f / 255f;
        return new Vector4(color.R * scale, color.G * scale, color.B * scale, Math.Clamp(alpha, 0f, 1f));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GpuRaycastParams
    {
        public Vector4 View;
        public Vector4 Counts;
        public Vector4 ShadowCounts;
        public Vector4 Origin;
        public Vector4 StartDirection;
        public Vector4 XDelta;
        public Vector4 YDelta;
        public Vector4 LightDirectionIntensity;
        public Vector4 LightSettings;
        public Vector4 SkyColor;

        public static GpuRaycastParams From(in RaycastComputeRequest request)
        {
            RayBuilder rayBuilder = request.RayBuilder;
            DirectionalLight light = request.Light;

            return new GpuRaycastParams
            {
                View = new Vector4(
                    request.Width,
                    request.Height,
                    request.MaxDistance,
                    1f),
                Counts = new Vector4(
                    request.StaticPrimitives.Triangles.Count + request.DynamicPrimitives.Triangles.Count,
                    0f,
                    0f,
                    (float)request.ShadowQuality),
                ShadowCounts = new Vector4(
                    request.StaticPrimitives.ShadowTriangles.Count + request.DynamicPrimitives.ShadowTriangles.Count,
                    0f,
                    0f,
                    0f),
                Origin = new Vector4(rayBuilder.Origin, 0f),
                StartDirection = new Vector4(rayBuilder.StartDirection, 0f),
                XDelta = new Vector4(rayBuilder.XDelta, 0f),
                YDelta = new Vector4(rayBuilder.YDelta, 0f),
                LightDirectionIntensity = new Vector4(light.Direction, light.Intensity),
                LightSettings = new Vector4(
                    light.AmbientIntensity,
                    light.ShadowIntensity,
                    light.ShadowBias,
                    light.ShadowMaxDistance),
                SkyColor = new Vector4(109f / 255f, 154f / 255f, 184f / 255f, 1f)
            };
        }
    }

    private readonly record struct PendingGpuFrame(Fence Fence, long StartTicks);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct GpuTriangle
    {
        public static GpuTriangle Empty => new(
            Vector4.Zero,
            Vector4.Zero,
            Vector4.Zero,
            Vector4.Zero,
            Vector4.Zero,
            Vector4.Zero,
            Vector4.Zero,
            new Vector4(-1f, 0f, 0f, 0f),
            Vector4.Zero);

        public readonly Vector4 A;
        public readonly Vector4 B;
        public readonly Vector4 C;
        public readonly Vector4 Normal;
        public readonly Vector4 UvA_UvB;
        public readonly Vector4 UvC_Material;
        public readonly Vector4 Color;
        public readonly Vector4 Material;
        public readonly Vector4 TextureTransform;

        private GpuTriangle(
            Vector4 a,
            Vector4 b,
            Vector4 c,
            Vector4 normal,
            Vector4 uvA_UvB,
            Vector4 uvC_Material,
            Vector4 color,
            Vector4 material,
            Vector4 textureTransform)
        {
            A = a;
            B = b;
            C = c;
            Normal = normal;
            UvA_UvB = uvA_UvB;
            UvC_Material = uvC_Material;
            Color = color;
            Material = material;
            TextureTransform = textureTransform;
        }

        public static GpuTriangle From(TrianglePrimitive triangle, int textureIndex)
        {
            SurfaceMaterial material = triangle.Material;
            return new GpuTriangle(
                new Vector4(triangle.A, 0f),
                new Vector4(triangle.B, 0f),
                new Vector4(triangle.C, 0f),
                new Vector4(triangle.Normal, 0f),
                new Vector4(triangle.UvA.X, triangle.UvA.Y, triangle.UvB.X, triangle.UvB.Y),
                new Vector4(triangle.UvC.X, triangle.UvC.Y, textureIndex, (float)material.Shader),
                ToVector4(material.Color, material.Opacity),
                new Vector4(
                    material.AlphaCutoff,
                    textureIndex >= 0 ? 1f : 0f,
                    0f,
                    0f),
                new Vector4(
                    material.TextureTiling.X,
                    material.TextureTiling.Y,
                    material.TextureOffset.X,
                    material.TextureOffset.Y));
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct GpuTextureInfo
    {
        public static GpuTextureInfo Empty => new(1, 1, 0, 1);

        public readonly Vector4 SizeOffset;

        public GpuTextureInfo(int width, int height, int offset, int mipCount)
        {
            SizeOffset = new Vector4(width, height, offset, Math.Max(1, mipCount));
        }
    }

    private static void ConfigureVulkanEnvironment()
    {
#if MACOS
        Environment.SetEnvironmentVariable(
            "MVK_CONFIG_LOG_LEVEL",
            Environment.GetEnvironmentVariable("MVK_CONFIG_LOG_LEVEL") ?? "0");

        string icdPath = Path.Combine(AppContext.BaseDirectory, "MoltenVK_icd.json");
        if (!File.Exists(icdPath))
        {
            return;
        }

        Environment.SetEnvironmentVariable("VK_ICD_FILENAMES", icdPath);
        Environment.SetEnvironmentVariable("VK_DRIVER_FILES", icdPath);
#endif
    }

    private static bool IsVulkanLoaderFailure(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is DllNotFoundException)
            {
                return true;
            }

            if (current.Message.Contains("libvulkan", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("vulkan loader", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryMapKey(Key key, out InputKey inputKey)
    {
        inputKey = key switch
        {
            Key.W => InputKey.W,
            Key.A => InputKey.A,
            Key.S => InputKey.S,
            Key.D => InputKey.D,
            Key.C => InputKey.C,
            Key.Space => InputKey.Space,
            Key.Escape => InputKey.Escape,
            Key.Up => InputKey.Up,
            Key.Down => InputKey.Down,
            Key.Left => InputKey.Left,
            Key.Right => InputKey.Right,
            _ => default
        };

        return key is Key.W or Key.A or Key.S or Key.D or Key.C or Key.Space or Key.Escape or
            Key.Up or Key.Down or Key.Left or Key.Right;
    }

}
