using Float3 = System.Numerics.Vector3;

namespace Pixagen.Rendering.Raycasting;

public sealed class RaycastShadowBins
{
    public Float3 GridMin { get; set; }
    public Float3 GridMax { get; set; } = Float3.One;
    public float CellSize { get; set; } = 1f;
    public int CellCountX { get; set; }
    public int CellCountY { get; set; }
    public int CellCountZ { get; set; }
    public int CellCount { get; set; }
    public int TriangleCount { get; set; }
    public int IndexCount { get; set; }
    public int MaxRayCells { get; set; }
    public double EstimatedShadowTriangleTests { get; set; }
    public RaycastTileRange[] Ranges { get; set; } = [new RaycastTileRange(0, 0)];
    public int[] TriangleIndices { get; set; } = [0];
}
