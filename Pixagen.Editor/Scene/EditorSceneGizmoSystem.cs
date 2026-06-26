using Pixagen.Ecs.DI;
using Pixagen.Core.App;
using Pixagen.Editor.Preview;

namespace Pixagen.Editor.Scene;

public sealed class EditorSceneGizmoSystem : IUpdateSystem
{
    private readonly CustomInject<FrameBuffer> _frameBuffer = default;
    private readonly CustomInject<EngineOptions> _options = default;
    private readonly CustomInject<UiOverlayBuffer> _overlay = default;
    private readonly CustomInject<PreviewOverlayFile> _overlayFile = default;

    public void Update()
    {
        UiOverlayBuffer overlay = _overlay.Value;
        overlay.Clear();

        int cellSize = Math.Max(1, _options.Value.CellPixelSize);
        int width = Math.Max(1, _frameBuffer.Value.Width * cellSize);
        int height = Math.Max(1, _frameBuffer.Value.Height * cellSize);
        PreviewOverlayState state = _overlayFile.Value.Read();

        DrawSceneReticle(overlay, width, height);
        DrawAxisGizmo(overlay, height);
        DrawSelectionGizmo(overlay, state);
    }

    private static void DrawSceneReticle(UiOverlayBuffer overlay, int width, int height)
    {
        int centerX = width / 2;
        int centerY = height / 2;
        var color = PixelColor.FromRgb(104, 210, 180);
        overlay.AddLine(new UiLineDrawCommand(centerX - 10, centerY, centerX - 3, centerY, 10, color));
        overlay.AddLine(new UiLineDrawCommand(centerX + 3, centerY, centerX + 10, centerY, 10, color));
        overlay.AddLine(new UiLineDrawCommand(centerX, centerY - 10, centerX, centerY - 3, 10, color));
        overlay.AddLine(new UiLineDrawCommand(centerX, centerY + 3, centerX, centerY + 10, 10, color));
    }

    private static void DrawAxisGizmo(UiOverlayBuffer overlay, int height)
    {
        int originX = 28;
        int originY = Math.Max(48, height - 44);

        overlay.AddLine(new UiLineDrawCommand(originX, originY, originX + 28, originY, 20, PixelColor.FromRgb(238, 92, 92), 2));
        overlay.AddLine(new UiLineDrawCommand(originX, originY, originX, originY - 28, 20, PixelColor.FromRgb(92, 224, 124), 2));
        overlay.AddLine(new UiLineDrawCommand(originX, originY, originX + 18, originY - 18, 20, PixelColor.FromRgb(84, 156, 238), 2));

        overlay.AddText(new UiTextDrawCommand(originX + 34, originY - 5, 21, "X", PixelColor.FromRgb(238, 92, 92), 12));
        overlay.AddText(new UiTextDrawCommand(originX - 4, originY - 42, 21, "Y", PixelColor.FromRgb(92, 224, 124), 12));
        overlay.AddText(new UiTextDrawCommand(originX + 23, originY - 31, 21, "Z", PixelColor.FromRgb(84, 156, 238), 12));
    }

    private static void DrawSelectionGizmo(UiOverlayBuffer overlay, PreviewOverlayState state)
    {
        if (string.IsNullOrWhiteSpace(state.SelectedName) ||
            string.Equals(state.SelectionKind, "None", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var accent = PixelColor.FromRgb(239, 191, 111);
        overlay.AddRect(new UiRectDrawCommand(8, 8, 168, 34, 30, accent));
        overlay.AddText(new UiTextDrawCommand(14, 14, 31, state.SelectedName, accent, 12));

        if (!string.IsNullOrWhiteSpace(state.Transform))
        {
            overlay.AddText(new UiTextDrawCommand(14, 32, 31, state.Transform, PixelColor.FromRgb(188, 198, 208), 10));
        }
    }
}
