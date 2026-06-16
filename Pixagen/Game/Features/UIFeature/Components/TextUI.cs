using Pixagen.Rendering;
using Pixagen.Ecs.Runtime;

namespace Pixagen.Game.Features.UIFeature.Components;

public struct TextUI : IComponent
{
    public string Value;
    public PixelColor Color;
    public int FontSize;

    public TextUI(
        string value,
        PixelColor? color = null,
        int fontSize = 0)
    {
        Value = value;
        Color = color ?? PixelColor.FromRgb(192, 192, 192);
        FontSize = Math.Max(0, fontSize);
    }
}
