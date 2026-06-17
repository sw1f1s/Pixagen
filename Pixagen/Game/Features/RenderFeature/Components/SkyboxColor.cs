using Pixagen.Rendering;

namespace Pixagen.Game.Features.RenderFeature.Components;

public struct SkyboxColor : IComponent
{
    public PixelColor Color;

    public SkyboxColor(PixelColor color)
    {
        Color = color;
    }
}
