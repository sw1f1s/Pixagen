using Float3 = System.Numerics.Vector3;
using Float2 = System.Numerics.Vector2;

namespace Pixagen.Game.Features.RenderFeature.Meshes;

public sealed class MeshAsset
{
    public MeshAsset(string id, Float3[] vertices, MeshTriangle[] triangles)
    {
        Id = id;
        Vertices = vertices;
        Triangles = triangles;
        Bounds = MeshBounds.From(vertices);
    }

    public string Id { get; }
    public Float3[] Vertices { get; }
    public MeshTriangle[] Triangles { get; }
    public MeshBounds Bounds { get; }
}

public readonly record struct MeshTriangle(int A, int B, int C, Float2 UvA, Float2 UvB, Float2 UvC);

public readonly record struct MeshBounds(Float3 Center, float Radius)
{
    public static MeshBounds From(ReadOnlySpan<Float3> vertices)
    {
        if (vertices.IsEmpty)
        {
            return new MeshBounds(Float3.Zero, 0f);
        }

        Float3 min = vertices[0];
        Float3 max = vertices[0];
        foreach (Float3 vertex in vertices)
        {
            min = Float3.Min(min, vertex);
            max = Float3.Max(max, vertex);
        }

        Float3 center = (min + max) * 0.5f;
        float radius = 0f;
        foreach (Float3 vertex in vertices)
        {
            radius = MathF.Max(radius, (vertex - center).Length());
        }

        return new MeshBounds(center, radius);
    }
}
