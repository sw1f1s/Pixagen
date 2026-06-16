
namespace Pixagen.Rendering;

public interface IRenderBackend : IDisposable
{
    bool IsCloseRequested { get; }

    void Initialize(RenderBackendOptions options);
    void PumpInput(InputState input);
    (int Width, int Height) GetFrameBufferSize();
    void Present(FrameBuffer frameBuffer);
}

public interface IUiOverlayRenderBackend
{
    void SetUiOverlayBuffer(UiOverlayBuffer uiOverlay);
}
