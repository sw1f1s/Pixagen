using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.RenderFeature.Meshes;
using Pixagen.Game.Features.RenderFeature.Textures;
using Pixagen.Game.Features.ResourceFeature.Runtime;
using Pixagen.Game.Features.SharedFeature.Components;
using Pixagen.Rendering;
using Pixagen.Ecs.Runtime;
using Float3 = System.Numerics.Vector3;
using Float2 = System.Numerics.Vector2;
using FloatQuaternion = System.Numerics.Quaternion;

namespace Pixagen.Game.Features.RenderFeature.Raycasting;

public static class RenderPrimitiveFactory
{
    public static void AddMeshTriangles(
        in Transform transform,
        in Mesh mesh,
        SurfaceMaterial material,
        ResourceManager resources,
        List<TrianglePrimitive> destination)
    {
        AddMeshTriangles(transform, resources.GetMesh(mesh.Asset), material, destination);
    }

    public static void AddShadowMeshTriangles(
        in Transform transform,
        in Mesh mesh,
        SurfaceMaterial material,
        ResourceManager resources,
        List<TrianglePrimitive> destination)
    {
        AddMeshTriangles(transform, resources.GetMesh(mesh.Asset), material, destination);
    }

    private static void AddMeshTriangles(
        in Transform transform,
        MeshAsset mesh,
        SurfaceMaterial material,
        List<TrianglePrimitive> destination)
    {
        FloatQuaternion rotation = RenderMath.ToFloat(transform.Rotation.Normalized);
        Float3 position = RenderMath.ToFloat(transform.Position);
        Float3 scale = RenderMath.ToFloat(transform.Scale);
        Float3[] vertices = mesh.Vertices;

        foreach (MeshTriangle triangle in mesh.Triangles)
        {
            destination.Add(new TrianglePrimitive(
                TransformPoint(vertices[triangle.A], position, rotation, scale),
                TransformPoint(vertices[triangle.B], position, rotation, scale),
                TransformPoint(vertices[triangle.C], position, rotation, scale),
                triangle.UvA,
                triangle.UvB,
                triangle.UvC,
                material));
        }
    }

    private static Float3 TransformPoint(Float3 point, Float3 position, FloatQuaternion rotation, Float3 scale)
    {
        return Float3.Transform(point * scale, rotation) + position;
    }

    public static SurfaceMaterial ResolveMaterial(in Material material, ResourceManager resources)
    {
        PixelColor color = material.Color;
        TextureAsset? texture = null;
        float opacity = 1f;
        float alphaCutoff = 0.01f;
        MaterialShaderKind shader = material.Shader;
        Float2 textureTiling = Float2.One;
        Float2 textureOffset = Float2.Zero;

        if (material.Texture is { } textureComponent && !string.IsNullOrWhiteSpace(textureComponent.Asset))
        {
            texture = resources.GetTexture(textureComponent.Asset);
            textureTiling = ResolveTextureTiling(textureComponent);
            textureOffset = ResolveTextureOffset(textureComponent);
        }

        if (material.Transparency is { } transparency)
        {
            opacity = Math.Clamp(RenderMath.ToFloat(transparency.Opacity), 0f, 1f);
            alphaCutoff = Math.Clamp(RenderMath.ToFloat(transparency.AlphaCutoff), 0f, 1f);
        }

        return new SurfaceMaterial(color, texture, opacity, alphaCutoff, shader, textureTiling, textureOffset);
    }

    private static Float2 ResolveTextureTiling(MaterialTexture texture)
    {
        return new Float2(
            MathF.Max(RenderMath.ToFloat(texture.TilingX), RenderMath.Epsilon),
            MathF.Max(RenderMath.ToFloat(texture.TilingY), RenderMath.Epsilon));
    }

    private static Float2 ResolveTextureOffset(MaterialTexture texture)
    {
        return new Float2(
            RenderMath.ToFloat(texture.OffsetX),
            RenderMath.ToFloat(texture.OffsetY));
    }
}
