using Pixagen.Ecs.DI;

namespace Pixagen.Rendering;

public sealed class UiOverlayBuffer : IDisposeInject
{
    private readonly List<UiTextDrawCommand> _texts = new();

    public IReadOnlyList<UiTextDrawCommand> Texts => _texts;

    public void Clear()
    {
        _texts.Clear();
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
}

public readonly record struct UiTextDrawCommand(
    int X,
    int Y,
    int Order,
    string Value,
    PixelColor Color,
    int FontSize);
