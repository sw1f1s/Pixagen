using Pixagen.Core.App;
using Pixagen.Core.Input;
using Pixagen.Core.Performance;
using Pixagen.Core.Runtime;
using Pixagen.Core.Timing;

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
