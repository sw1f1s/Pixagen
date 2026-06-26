using Veldrid;

namespace Pixagen.Rendering.Vulkan;

internal sealed class VulkanRenderTargets : IDisposable
{
    private const int OverlaySafeInsetPixels = 2;

    private enum OverlayDrawKind
    {
        Line,
        Rect,
        Text
    }

    private readonly record struct OrderedOverlayCommand(int Order, OverlayDrawKind Kind, object Command);

    private readonly RgbaByte[] _fallbackScenePixel = [new(0, 0, 0, 255)];
    private UiOverlayBuffer? _uiOverlay;
    private Texture? _sceneTexture;
    private TextureView? _sceneTextureView;
    private Texture? _overlayTexture;
    private TextureView? _overlayTextureView;
    private RgbaByte[] _scenePixels = [];
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

    public void UpdateSceneTexture(
        GraphicsDevice graphicsDevice,
        VulkanGpuFrameTracker frameTracker,
        FrameBuffer frameBuffer,
        Action invalidateDependentResourceSets)
    {
        EnsureSceneTexture(
            graphicsDevice,
            frameTracker,
            frameBuffer.Width,
            frameBuffer.Height,
            invalidateDependentResourceSets);
        int pixelCount = frameBuffer.Width * frameBuffer.Height;
        if (_scenePixels.Length != pixelCount)
        {
            _scenePixels = new RgbaByte[pixelCount];
        }

        ReadOnlySpan<FrameCell> cells = frameBuffer.Cells;
        for (int i = 0; i < cells.Length; i++)
        {
            FrameCell cell = cells[i];
            PixelColor color = cell.Color;
            _scenePixels[i] = new RgbaByte(color.R, color.G, color.B, cell.Alpha);
        }

        graphicsDevice.UpdateTexture(
            _sceneTexture!,
            _scenePixels,
            0,
            0,
            0,
            (uint)frameBuffer.Width,
            (uint)frameBuffer.Height,
            1,
            0,
            0);
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

        var orderedCommands = new List<OrderedOverlayCommand>(
            uiOverlay.Lines.Count + uiOverlay.Rects.Count + uiOverlay.Texts.Count);
        foreach (UiLineDrawCommand line in uiOverlay.Lines)
        {
            orderedCommands.Add(new OrderedOverlayCommand(line.Order, OverlayDrawKind.Line, line));
        }

        foreach (UiRectDrawCommand rect in uiOverlay.Rects)
        {
            orderedCommands.Add(new OrderedOverlayCommand(rect.Order, OverlayDrawKind.Rect, rect));
        }

        foreach (UiTextDrawCommand text in uiOverlay.Texts)
        {
            orderedCommands.Add(new OrderedOverlayCommand(text.Order, OverlayDrawKind.Text, text));
        }

        foreach (OrderedOverlayCommand command in orderedCommands
            .OrderBy(command => command.Order)
            .ThenBy(command => command.Kind))
        {
            switch (command.Kind)
            {
                case OverlayDrawKind.Line:
                    DrawLine((UiLineDrawCommand)command.Command);
                    break;
                case OverlayDrawKind.Rect:
                    DrawRect((UiRectDrawCommand)command.Command);
                    break;
                case OverlayDrawKind.Text:
                    var text = (UiTextDrawCommand)command.Command;
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
                    break;
            }
        }
    }

    private void DrawRect(UiRectDrawCommand rect)
    {
        int thickness = Math.Max(1, rect.Thickness);
        int right = rect.X + rect.Width - 1;
        int bottom = rect.Y + rect.Height - 1;
        DrawLine(new UiLineDrawCommand(rect.X, rect.Y, right, rect.Y, rect.Order, rect.Color, thickness));
        DrawLine(new UiLineDrawCommand(rect.X, rect.Y, rect.X, bottom, rect.Order, rect.Color, thickness));
        DrawLine(new UiLineDrawCommand(right, rect.Y, right, bottom, rect.Order, rect.Color, thickness));
        DrawLine(new UiLineDrawCommand(rect.X, bottom, right, bottom, rect.Order, rect.Color, thickness));
    }

    private void DrawLine(UiLineDrawCommand line)
    {
        int x0 = line.X0 + OverlaySafeInsetPixels;
        int y0 = line.Y0 + OverlaySafeInsetPixels;
        int x1 = line.X1 + OverlaySafeInsetPixels;
        int y1 = line.Y1 + OverlaySafeInsetPixels;
        int dx = Math.Abs(x1 - x0);
        int sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0);
        int sy = y0 < y1 ? 1 : -1;
        int error = dx + dy;

        while (true)
        {
            DrawPoint(x0, y0, line.Color, line.Thickness);
            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            int e2 = 2 * error;
            if (e2 >= dy)
            {
                error += dy;
                x0 += sx;
            }

            if (e2 <= dx)
            {
                error += dx;
                y0 += sy;
            }
        }
    }

    private void DrawPoint(int x, int y, PixelColor color, int thickness)
    {
        int radius = Math.Max(1, thickness);
        var pixel = new RgbaByte(color.R, color.G, color.B, 255);
        for (int offsetY = 0; offsetY < radius; offsetY++)
        {
            int py = y + offsetY;
            if ((uint)py >= (uint)_overlayTextureHeight)
            {
                continue;
            }

            for (int offsetX = 0; offsetX < radius; offsetX++)
            {
                int px = x + offsetX;
                if ((uint)px >= (uint)_overlayTextureWidth)
                {
                    continue;
                }

                _overlayPixels[py * _overlayTextureWidth + px] = pixel;
            }
        }
    }
}
