using Pixagen.Core.App;

namespace Pixagen.Rendering;

public sealed record RenderBackendOptions(
    int WindowWidth,
    int WindowHeight,
    int CellPixelSize,
    bool Fullscreen,
    bool CaptureMouse,
    bool ShowCursor,
    bool RunSingleFrame)
{
    public static RenderBackendOptions FromEngineOptions(EngineOptions options)
    {
        return new RenderBackendOptions(
            options.WindowWidth,
            options.WindowHeight,
            options.CellPixelSize,
            options.Fullscreen,
            options.CaptureMouse,
            options.ShowCursor,
            options.RunSingleFrame);
    }
}
