using Pixagen.Rendering.Textures;

namespace Pixagen.Rendering.Raycasting;

public readonly struct Skybox
{
    public static Skybox Empty => new(PixelColor.FromRgb(0, 0, 0), null);

    public readonly PixelColor Color;
    public readonly TextureAsset? Texture;

    public Skybox(PixelColor color)
        : this(color, null)
    {
    }

    public Skybox(PixelColor color, TextureAsset? texture)
    {
        Color = color;
        Texture = texture;
    }
}
