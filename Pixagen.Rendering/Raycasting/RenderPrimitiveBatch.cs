namespace Pixagen.Rendering.Raycasting;

public sealed class RenderPrimitiveBatch
{
    public static RenderPrimitiveBatch Empty { get; } = new();

    public List<TrianglePrimitive> Triangles { get; } = new();
    public List<TrianglePrimitive> ShadowTriangles { get; } = new();
    public bool HasShadowCasters { get; private set; }

    public void EnsureCapacity(int triangles, int shadowTriangles)
    {
        Triangles.EnsureCapacity(triangles);
        ShadowTriangles.EnsureCapacity(shadowTriangles);
    }

    public void Clear()
    {
        Triangles.Clear();
        ShadowTriangles.Clear();
        HasShadowCasters = false;
    }

    public void RefreshShadowState()
    {
        HasShadowCasters = ShadowTriangles.Count > 0;
    }
}
