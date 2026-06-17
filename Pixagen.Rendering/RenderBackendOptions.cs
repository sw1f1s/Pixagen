
namespace Pixagen.Rendering;

public sealed record RenderBackendOptions(
    int WindowWidth,
    int WindowHeight,
    int CellPixelSize,
    bool Fullscreen,
    bool CaptureMouse,
    bool ShowCursor,
    bool RunSingleFrame);
