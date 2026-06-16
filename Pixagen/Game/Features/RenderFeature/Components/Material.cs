using Pixagen.Rendering;

namespace Pixagen.Game.Features.RenderFeature.Components;

public struct Material : IComponent
{
    public PixelColor Color;
    public MaterialTexture? Texture;
    public MaterialTransparency? Transparency;
    public MaterialShaderKind Shader;

    public Material(PixelColor color)
        : this(color, null, null, MaterialShaderKind.Lit)
    {
    }

    public Material(PixelColor color, MaterialTexture texture)
        : this(color, texture, null, MaterialShaderKind.Lit)
    {
    }

    public Material(PixelColor color, MaterialTransparency transparency)
        : this(color, null, transparency, MaterialShaderKind.Lit)
    {
    }

    public Material(
        PixelColor color,
        MaterialTexture? texture,
        MaterialTransparency? transparency,
        MaterialShaderKind shader)
    {
        Color = color;
        Texture = texture;
        Transparency = transparency;
        Shader = shader;
    }
}

public readonly struct MaterialTexture
{
    public readonly string Asset;
    public readonly Fix TilingX;
    public readonly Fix TilingY;
    public readonly Fix OffsetX;
    public readonly Fix OffsetY;

    public MaterialTexture(string asset)
        : this(asset, Fix.One, Fix.One, Fix.Zero, Fix.Zero)
    {
    }

    public MaterialTexture(string asset, Fix tilingX, Fix tilingY)
        : this(asset, tilingX, tilingY, Fix.Zero, Fix.Zero)
    {
    }

    public MaterialTexture(string asset, Fix tilingX, Fix tilingY, Fix offsetX, Fix offsetY)
    {
        Asset = asset;
        TilingX = tilingX;
        TilingY = tilingY;
        OffsetX = offsetX;
        OffsetY = offsetY;
    }
}

public readonly struct MaterialTransparency
{
    public readonly Fix Opacity;
    public readonly Fix AlphaCutoff;

    public MaterialTransparency(Fix opacity)
        : this(opacity, Fix.Zero)
    {
    }

    public MaterialTransparency(Fix opacity, Fix alphaCutoff)
    {
        Opacity = opacity;
        AlphaCutoff = alphaCutoff;
    }
}

public enum MaterialShaderKind
{
    Lit,
    Unlit
}
