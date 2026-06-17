using Veldrid;

namespace Pixagen.Rendering.Vulkan;

internal sealed class VulkanRenderTargets : IDisposable
{
    private const int OverlaySafeInsetPixels = 2;

    private readonly RgbaByte[] _fallbackScenePixel = [new(0, 0, 0, 255)];
    private UiOverlayBuffer? _uiOverlay;
    private Texture? _sceneTexture;
    private TextureView? _sceneTextureView;
    private Texture? _overlayTexture;
    private TextureView? _overlayTextureView;
    private RgbaByte[] _overlayPixels = [];
    private int _sceneTextureWidth;
    private int _sceneTextureHeight;
    private int _overlayTextureWidth;
    private int _overlayTextureHeight;
    private bool _sceneTextureHasFallback;

    public Texture SceneTexture => _sceneTexture ??
        throw new InvalidOperationException("Scene texture is not initialized.");

    public TextureView SceneTextureView => _sceneTextureView ??
        throw new InvalidOperationException("Scene texture view is not initialized.");

    public TextureView OverlayTextureView => _overlayTextureView ??
        throw new InvalidOperationException("Overlay texture view is not initialized.");

    public void SetUiOverlayBuffer(UiOverlayBuffer uiOverlay)
    {
        _uiOverlay = uiOverlay ?? throw new ArgumentNullException(nameof(uiOverlay));
    }

    public void EnsureSceneTexture(
        GraphicsDevice graphicsDevice,
        VulkanGpuFrameTracker frameTracker,
        int width,
        int height,
        Action invalidateDependentResourceSets)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        if (_sceneTexture is not null && width == _sceneTextureWidth && height == _sceneTextureHeight)
        {
            return;
        }

        frameTracker.WaitForPending(graphicsDevice);
        invalidateDependentResourceSets();
        _sceneTextureView?.Dispose();
        _sceneTexture?.Dispose();

        _sceneTextureWidth = width;
        _sceneTextureHeight = height;
        _sceneTextureHasFallback = false;
        _sceneTexture = graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
            (uint)width,
            (uint)height,
            mipLevels: 1,
            arrayLayers: 1,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Sampled | TextureUsage.Storage));
        _sceneTextureView = graphicsDevice.ResourceFactory.CreateTextureView(_sceneTexture);
    }

    public void EnsureFallbackSceneTexture(
        GraphicsDevice graphicsDevice,
        VulkanGpuFrameTracker frameTracker,
        Action invalidateDependentResourceSets)
    {
        EnsureSceneTexture(graphicsDevice, frameTracker, 1, 1, invalidateDependentResourceSets);
        if (_sceneTextureHasFallback)
        {
            return;
        }

        graphicsDevice.UpdateTexture(_sceneTexture!, _fallbackScenePixel, 0, 0, 0, 1, 1, 1, 0, 0);
        _sceneTextureHasFallback = true;
    }

    public void MarkSceneTextureUpdatedByCompute()
    {
        _sceneTextureHasFallback = false;
    }

    public void EnsureTransparentOverlayTexture(
        GraphicsDevice graphicsDevice,
        VulkanGpuFrameTracker frameTracker,
        Action invalidateCompositeResourceSet)
    {
        EnsureOverlayTexture(graphicsDevice, frameTracker, 1, 1, invalidateCompositeResourceSet);
        _overlayPixels[0] = new RgbaByte(0, 0, 0, 0);
        graphicsDevice.UpdateTexture(_overlayTexture!, _overlayPixels, 0, 0, 0, 1, 1, 1, 0, 0);
    }

    public void UpdateOverlayTexture(
        GraphicsDevice graphicsDevice,
        VulkanGpuFrameTracker frameTracker,
        RenderBackendOptions options,
        int width,
        int height,
        Action invalidateCompositeResourceSet)
    {
        EnsureOverlayTexture(graphicsDevice, frameTracker, width, height, invalidateCompositeResourceSet);
        RasterizeOverlay(options.CellPixelSize);

        graphicsDevice.UpdateTexture(
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

    public long EstimateVramBytes()
    {
        long bytes = 0;
        bytes += (long)_sceneTextureWidth * _sceneTextureHeight * 4;
        bytes += (long)_overlayTextureWidth * _overlayTextureHeight * 4;
        return bytes;
    }

    public void Dispose()
    {
        _overlayTextureView?.Dispose();
        _overlayTexture?.Dispose();
        _sceneTextureView?.Dispose();
        _sceneTexture?.Dispose();
    }

    private void EnsureOverlayTexture(
        GraphicsDevice graphicsDevice,
        VulkanGpuFrameTracker frameTracker,
        int width,
        int height,
        Action invalidateCompositeResourceSet)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        if (_overlayTexture is not null && width == _overlayTextureWidth && height == _overlayTextureHeight)
        {
            return;
        }

        frameTracker.WaitForPending(graphicsDevice);
        invalidateCompositeResourceSet();
        _overlayTextureView?.Dispose();
        _overlayTexture?.Dispose();

        _overlayTextureWidth = width;
        _overlayTextureHeight = height;
        _overlayPixels = new RgbaByte[width * height];
        _overlayTexture = graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
            (uint)width,
            (uint)height,
            mipLevels: 1,
            arrayLayers: 1,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Sampled));
        _overlayTextureView = graphicsDevice.ResourceFactory.CreateTextureView(_overlayTexture);
    }

    private void RasterizeOverlay(int cellPixelSize)
    {
        UiOverlayBuffer uiOverlay = _uiOverlay ??
            throw new InvalidOperationException($"{nameof(VulkanRenderTargets)} requires {nameof(UiOverlayBuffer)}.");
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
                Math.Max(1, cellPixelSize));
        }
    }
}
