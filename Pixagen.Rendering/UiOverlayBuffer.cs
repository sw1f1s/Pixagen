using Pixagen.Ecs.DI;

namespace Pixagen.Rendering;

public sealed class UiOverlayBuffer : IDisposeInject
{
    private readonly List<UiTextDrawCommand> _texts = new();
    private readonly List<UiLineDrawCommand> _lines = new();
    private readonly List<UiRectDrawCommand> _rects = new();

    public IReadOnlyList<UiTextDrawCommand> Texts => _texts;
    public IReadOnlyList<UiLineDrawCommand> Lines => _lines;
    public IReadOnlyList<UiRectDrawCommand> Rects => _rects;

    public void Clear()
    {
        _texts.Clear();
        _lines.Clear();
        _rects.Clear();
    }

    public void DisposeInject()
    {
        Clear();
    }

    public void AddText(UiTextDrawCommand command)
    {
        if (!string.IsNullOrEmpty(command.Value))
        {
            _texts.Add(command);
        }
    }

    public void AddLine(UiLineDrawCommand command)
    {
        _lines.Add(command);
    }

    public void AddRect(UiRectDrawCommand command)
    {
        if (command.Width > 0 && command.Height > 0)
        {
            _rects.Add(command);
        }
    }
}

public readonly record struct UiTextDrawCommand(
    int X,
    int Y,
    int Order,
    string Value,
    PixelColor Color,
    int FontSize);

public readonly record struct UiLineDrawCommand(
    int X0,
    int Y0,
    int X1,
    int Y1,
    int Order,
    PixelColor Color,
    int Thickness = 1);

public readonly record struct UiRectDrawCommand(
    int X,
    int Y,
    int Width,
    int Height,
    int Order,
    PixelColor Color,
    int Thickness = 1);
