namespace Pixagen.Rendering.Raycasting;

public readonly record struct RaycastTileRange(int Offset, int Count);

public sealed class RaycastTileBins
{
    public int TileSize { get; set; }
    public int TileColumns { get; set; }
    public int TileRows { get; set; }
    public int TileCount { get; set; }
    public int TriangleCount { get; set; }
    public int IndexCount { get; set; }
    public double EstimatedPrimaryTriangleTests { get; set; }
    public RaycastTileRange[] Ranges { get; set; } = [new RaycastTileRange(0, 0)];
    public int[] TriangleIndices { get; set; } = [0];
}
