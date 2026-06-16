using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.RenderFeature.Textures;
using Pixagen.Rendering;
using Float3 = System.Numerics.Vector3;
using Float2 = System.Numerics.Vector2;

namespace Pixagen.Game.Features.RenderFeature.Raycasting;

public readonly struct TrianglePrimitive
{
    public readonly Float3 A;
    public readonly Float3 B;
    public readonly Float3 C;
    public readonly Float3 Normal;
    public readonly Float2 UvA;
    public readonly Float2 UvB;
    public readonly Float2 UvC;
    public readonly SurfaceMaterial Material;
    public readonly Float3 BoundsCenter;
    public readonly float BoundsRadius;

    public TrianglePrimitive(
        Float3 a,
        Float3 b,
        Float3 c,
        Float2 uvA,
        Float2 uvB,
        Float2 uvC,
        SurfaceMaterial material)
    {
        A = a;
        B = b;
        C = c;
        UvA = uvA;
        UvB = uvB;
        UvC = uvC;
        Material = material;

        Float3 ab = b - a;
        Float3 ac = c - a;
        Normal = RenderMath.NormalizeOr(Float3.Cross(ab, ac), Float3.UnitY);

        BoundsCenter = (a + b + c) / 3f;
        BoundsRadius = MathF.Max(
            (a - BoundsCenter).Length(),
            MathF.Max((b - BoundsCenter).Length(), (c - BoundsCenter).Length()));
    }

    public Float2 GetUv(float u, float v)
    {
        return UvA + (UvB - UvA) * u + (UvC - UvA) * v;
    }
}

public readonly struct SurfaceMaterial
{
    public static SurfaceMaterial Default => new(
        PixelColor.FromRgb(192, 192, 192),
        null,
        1f,
        0.01f,
        MaterialShaderKind.Lit,
        Float2.One,
        Float2.Zero);

    public readonly PixelColor Color;
    public readonly TextureAsset? Texture;
    public readonly float Opacity;
    public readonly float AlphaCutoff;
    public readonly MaterialShaderKind Shader;
    public readonly Float2 TextureTiling;
    public readonly Float2 TextureOffset;

    public SurfaceMaterial(
        PixelColor color,
        TextureAsset? texture,
        float opacity,
        float alphaCutoff,
        MaterialShaderKind shader,
        Float2 textureTiling,
        Float2 textureOffset)
    {
        Color = color;
        Texture = texture;
        Opacity = Math.Clamp(opacity, 0f, 1f);
        AlphaCutoff = Math.Clamp(alphaCutoff, 0f, 1f);
        Shader = shader;
        TextureTiling = textureTiling;
        TextureOffset = textureOffset;
    }
}

public readonly struct DirectionalLight
{
    public static DirectionalLight Default => new(
        Float3.UnitY,
        new LightDirection(
            Fix.One,
            Fix.One / new Fix(5),
            new Fix(3) / new Fix(5),
            Fix.One / new Fix(20),
            new Fix(100)));

    public readonly Float3 Direction;
    public readonly float Intensity;
    public readonly float AmbientIntensity;
    public readonly float ShadowIntensity;
    public readonly float ShadowBias;
    public readonly float ShadowMaxDistance;
    public readonly bool CastsShadows;

    public DirectionalLight(Float3 direction, LightDirection settings)
    {
        Direction = RenderMath.NormalizeOr(direction, Float3.UnitY);
        Intensity = MathF.Max(RenderMath.ToFloat(settings.Intensity), 0f);
        AmbientIntensity = Math.Clamp(RenderMath.ToFloat(settings.AmbientIntensity), 0f, 1f);
        ShadowIntensity = Math.Clamp(RenderMath.ToFloat(settings.ShadowIntensity), 0f, 1f);
        ShadowBias = MathF.Max(RenderMath.ToFloat(settings.ShadowBias), RenderMath.Epsilon);
        ShadowMaxDistance = MathF.Max(RenderMath.ToFloat(settings.ShadowMaxDistance), 1f);
        CastsShadows = ShadowIntensity > 0f;
    }
}
