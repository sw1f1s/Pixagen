using Pixagen.Rendering;

namespace Pixagen.Benchmark;

public sealed class HeadlessRenderBackend : IRenderBackend, IUiOverlayRenderBackend
{
    private UiOverlayBuffer? _uiOverlay;
    private int _width;
    private int _height;

    public HeadlessRenderBackend(int width, int height)
    {
        _width = Math.Max(1, width);
        _height = Math.Max(1, height);
    }

    public bool IsCloseRequested => false;
    public int PresentCalls { get; private set; }
    public long PresentedCells { get; private set; }
    public long PresentChecksum { get; private set; }
    public int LastOverlayTexts => _uiOverlay?.Texts.Count ?? 0;

    public void Initialize(RenderBackendOptions options)
    {
        _width = Math.Max(1, options.WindowWidth);
        _height = Math.Max(1, options.WindowHeight);
    }

    public void PumpInput(IRenderInputSink input)
    {
        input.BeginFrame();
    }

    public (int Width, int Height) GetFrameBufferSize()
    {
        return (_width, _height);
    }

    public void Present(FrameBuffer frameBuffer)
    {
        PresentCalls++;
        ReadOnlySpan<FrameCell> cells = frameBuffer.Cells;
        PresentedCells += cells.Length;

        int stride = Math.Max(1, cells.Length / 4096);
        long checksum = PresentChecksum;
        for (int i = 0; i < cells.Length; i += stride)
        {
            FrameCell cell = cells[i];
            checksum += cell.Color.R;
            checksum += cell.Color.G;
            checksum += cell.Color.B;
            checksum += cell.Alpha;
        }

        if (_uiOverlay is not null)
        {
            foreach (UiTextDrawCommand text in _uiOverlay.Texts)
            {
                checksum += text.X;
                checksum += text.Y;
                checksum += text.Order;
                checksum += text.Value.Length;
                checksum += text.FontSize;
            }
        }

        PresentChecksum = checksum;
    }

    public void SetUiOverlayBuffer(UiOverlayBuffer uiOverlay)
    {
        _uiOverlay = uiOverlay;
    }

    public void Dispose()
    {
    }
}
