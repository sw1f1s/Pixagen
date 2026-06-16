using Float3 = System.Numerics.Vector3;

namespace Pixagen.Game.Features.RenderFeature.Raycasting;

public sealed class RaycastShadowBins
{
    internal RaycastShadowBins()
    {
    }

    public Float3 GridMin { get; internal set; }
    public Float3 GridMax { get; internal set; } = Float3.One;
    public float CellSize { get; internal set; } = 1f;
    public int CellCountX { get; internal set; }
    public int CellCountY { get; internal set; }
    public int CellCountZ { get; internal set; }
    public int CellCount { get; internal set; }
    public int TriangleCount { get; internal set; }
    public int IndexCount { get; internal set; }
    public int MaxRayCells { get; internal set; }
    public double EstimatedShadowTriangleTests { get; internal set; }
    public RaycastTileRange[] Ranges { get; internal set; } = [new RaycastTileRange(0, 0)];
    public int[] TriangleIndices { get; internal set; } = [0];
}

public sealed class RaycastShadowBinBuilder
{
    private const int MinCellsPerAxis = 4;
    private const int MaxCellsPerAxis = 32;
    private const float GridPaddingFactor = 0.25f;

    private readonly RaycastShadowBins _bins = new();
    private ShadowTriangleCellBounds[] _triangleBounds = [];
    private int[] _cellCounts = [];
    private int[] _cellWriteOffsets = [];
    private int _triangleBoundsCount;

    public RaycastShadowBins Build(
        RenderResolution resolution,
        DirectionalLight light,
        RenderPrimitiveBatch staticPrimitives,
        RenderPrimitiveBatch dynamicPrimitives)
    {
        int triangleCount = staticPrimitives.ShadowTriangles.Count + dynamicPrimitives.ShadowTriangles.Count;
        if (!light.CastsShadows || triangleCount == 0)
        {
            Clear();
            return _bins;
        }

        ShadowBounds bounds = CalculateBounds(staticPrimitives.ShadowTriangles, dynamicPrimitives.ShadowTriangles);
        InitializeGrid(bounds, triangleCount, light);
        EnsureCellCapacity(_bins.CellCount);
        Array.Clear(_cellCounts, 0, _bins.CellCount);
        _triangleBoundsCount = 0;
        EnsureTriangleBoundsCapacity(triangleCount);

        _bins.TriangleCount = triangleCount;
        int triangleIndex = 0;
        AddTriangleBounds(staticPrimitives.ShadowTriangles, ref triangleIndex);
        AddTriangleBounds(dynamicPrimitives.ShadowTriangles, ref triangleIndex);

        EnsureRangeCapacity(_bins.CellCount);
        int indexCount = FillRanges(_bins.CellCount);
        EnsureIndexCapacity(indexCount);
        FillIndices();

        _bins.IndexCount = indexCount;
        _bins.EstimatedShadowTriangleTests = EstimateShadowTriangleTests(resolution, light);
        return _bins;
    }

    private void Clear()
    {
        _bins.GridMin = Float3.Zero;
        _bins.GridMax = Float3.One;
        _bins.CellSize = 1f;
        _bins.CellCountX = 0;
        _bins.CellCountY = 0;
        _bins.CellCountZ = 0;
        _bins.CellCount = 0;
        _bins.TriangleCount = 0;
        _bins.IndexCount = 0;
        _bins.MaxRayCells = 0;
        _bins.EstimatedShadowTriangleTests = 0;
        if (_bins.Ranges.Length == 0)
        {
            _bins.Ranges = [new RaycastTileRange(0, 0)];
        }

        if (_bins.TriangleIndices.Length == 0)
        {
            _bins.TriangleIndices = [0];
        }

        _bins.Ranges[0] = new RaycastTileRange(0, 0);
        _bins.TriangleIndices[0] = 0;
    }

    private static ShadowBounds CalculateBounds(
        List<TrianglePrimitive> staticPrimitives,
        List<TrianglePrimitive> dynamicPrimitives)
    {
        var bounds = ShadowBounds.Empty;
        foreach (TrianglePrimitive triangle in staticPrimitives)
        {
            bounds.Encapsulate(triangle);
        }

        foreach (TrianglePrimitive triangle in dynamicPrimitives)
        {
            bounds.Encapsulate(triangle);
        }

        return bounds;
    }

    private void InitializeGrid(in ShadowBounds bounds, int triangleCount, DirectionalLight light)
    {
        Float3 min = bounds.Min;
        Float3 max = bounds.Max;
        Float3 extent = Float3.Max(max - min, new Float3(RenderMath.Epsilon));
        float maxExtent = MathF.Max(extent.X, MathF.Max(extent.Y, extent.Z));
        int targetCells = ResolveTargetCellsPerAxis(triangleCount);
        float cellSize = MathF.Max(maxExtent / targetCells, RenderMath.Epsilon);
        Float3 padding = new(cellSize * GridPaddingFactor + RenderMath.Epsilon);

        min -= padding;
        max += padding;
        extent = Float3.Max(max - min, new Float3(RenderMath.Epsilon));

        _bins.GridMin = min;
        _bins.GridMax = max;
        _bins.CellSize = cellSize;
        _bins.CellCountX = ResolveCellCount(extent.X, cellSize);
        _bins.CellCountY = ResolveCellCount(extent.Y, cellSize);
        _bins.CellCountZ = ResolveCellCount(extent.Z, cellSize);
        _bins.CellCount = _bins.CellCountX * _bins.CellCountY * _bins.CellCountZ;
        _bins.MaxRayCells = ResolveMaxRayCells(light);
    }

    private static int ResolveTargetCellsPerAxis(int triangleCount)
    {
        double cubeRoot = Math.Pow(Math.Max(1, triangleCount), 1.0 / 3.0);
        return Math.Clamp((int)Math.Ceiling(cubeRoot * 2.0), MinCellsPerAxis, MaxCellsPerAxis);
    }

    private static int ResolveCellCount(float extent, float cellSize)
    {
        return Math.Clamp((int)MathF.Ceiling(extent / MathF.Max(cellSize, RenderMath.Epsilon)), 1, MaxCellsPerAxis);
    }

    private int ResolveMaxRayCells(DirectionalLight light)
    {
        int gridTraversalLimit = _bins.CellCountX + _bins.CellCountY + _bins.CellCountZ + 3;
        int distanceLimit = Math.Max(1, (int)MathF.Ceiling(light.ShadowMaxDistance / _bins.CellSize) + 3);
        return Math.Max(1, Math.Min(gridTraversalLimit, distanceLimit));
    }

    private void AddTriangleBounds(List<TrianglePrimitive> triangles, ref int triangleIndex)
    {
        foreach (TrianglePrimitive triangle in triangles)
        {
            ShadowTriangleCellBounds bounds = CalculateTriangleCellBounds(triangle, triangleIndex);
            _triangleBounds[_triangleBoundsCount++] = bounds;
            AddCellCounts(bounds);
            triangleIndex++;
        }
    }

    private ShadowTriangleCellBounds CalculateTriangleCellBounds(in TrianglePrimitive triangle, int triangleIndex)
    {
        Float3 min = Float3.Min(triangle.A, Float3.Min(triangle.B, triangle.C));
        Float3 max = Float3.Max(triangle.A, Float3.Max(triangle.B, triangle.C));
        return new ShadowTriangleCellBounds(
            triangleIndex,
            ToCellX(min.X),
            ToCellX(max.X),
            ToCellY(min.Y),
            ToCellY(max.Y),
            ToCellZ(min.Z),
            ToCellZ(max.Z));
    }

    private void AddCellCounts(in ShadowTriangleCellBounds bounds)
    {
        for (int z = bounds.MinZ; z <= bounds.MaxZ; z++)
        {
            int zOffset = z * _bins.CellCountX * _bins.CellCountY;
            for (int y = bounds.MinY; y <= bounds.MaxY; y++)
            {
                int rowOffset = zOffset + y * _bins.CellCountX;
                for (int x = bounds.MinX; x <= bounds.MaxX; x++)
                {
                    _cellCounts[rowOffset + x]++;
                }
            }
        }
    }

    private int FillRanges(int cellCount)
    {
        int offset = 0;
        for (int cellIndex = 0; cellIndex < cellCount; cellIndex++)
        {
            int count = _cellCounts[cellIndex];
            _bins.Ranges[cellIndex] = new RaycastTileRange(offset, count);
            _cellWriteOffsets[cellIndex] = offset;
            offset += count;
        }

        return offset;
    }

    private void FillIndices()
    {
        for (int i = 0; i < _triangleBoundsCount; i++)
        {
            ShadowTriangleCellBounds bounds = _triangleBounds[i];
            for (int z = bounds.MinZ; z <= bounds.MaxZ; z++)
            {
                int zOffset = z * _bins.CellCountX * _bins.CellCountY;
                for (int y = bounds.MinY; y <= bounds.MaxY; y++)
                {
                    int rowOffset = zOffset + y * _bins.CellCountX;
                    for (int x = bounds.MinX; x <= bounds.MaxX; x++)
                    {
                        int cellIndex = rowOffset + x;
                        _bins.TriangleIndices[_cellWriteOffsets[cellIndex]++] = bounds.TriangleIndex;
                    }
                }
            }
        }
    }

    private double EstimateShadowTriangleTests(RenderResolution resolution, DirectionalLight light)
    {
        if (_bins.CellCount == 0 || _bins.IndexCount == 0)
        {
            return 0;
        }

        double pixelCount = (double)Math.Max(1, resolution.Width) * Math.Max(1, resolution.Height);
        double averageCellOccupancy = (double)_bins.IndexCount / _bins.CellCount;
        double rayCells = Math.Min(
            _bins.MaxRayCells,
            Math.Max(1, Math.Ceiling(light.ShadowMaxDistance / _bins.CellSize) + 1));
        return pixelCount * averageCellOccupancy * rayCells;
    }

    private void EnsureCellCapacity(int cellCount)
    {
        if (_cellCounts.Length < cellCount)
        {
            _cellCounts = new int[cellCount];
            _cellWriteOffsets = new int[cellCount];
        }
    }

    private void EnsureRangeCapacity(int cellCount)
    {
        if (_bins.Ranges.Length < cellCount)
        {
            _bins.Ranges = new RaycastTileRange[cellCount];
        }
    }

    private void EnsureTriangleBoundsCapacity(int triangleCount)
    {
        if (_triangleBounds.Length < triangleCount)
        {
            _triangleBounds = new ShadowTriangleCellBounds[Math.Max(1, triangleCount)];
        }
    }

    private void EnsureIndexCapacity(int indexCount)
    {
        if (_bins.TriangleIndices.Length < Math.Max(1, indexCount))
        {
            _bins.TriangleIndices = new int[Math.Max(1, indexCount)];
        }

        if (indexCount == 0)
        {
            _bins.TriangleIndices[0] = 0;
        }
    }

    private int ToCellX(float value)
    {
        return ToCell(value, _bins.GridMin.X, _bins.CellCountX);
    }

    private int ToCellY(float value)
    {
        return ToCell(value, _bins.GridMin.Y, _bins.CellCountY);
    }

    private int ToCellZ(float value)
    {
        return ToCell(value, _bins.GridMin.Z, _bins.CellCountZ);
    }

    private int ToCell(float value, float min, int count)
    {
        int cell = (int)MathF.Floor((value - min) / _bins.CellSize);
        return Math.Clamp(cell, 0, count - 1);
    }

    private readonly record struct ShadowTriangleCellBounds(
        int TriangleIndex,
        int MinX,
        int MaxX,
        int MinY,
        int MaxY,
        int MinZ,
        int MaxZ);

    private struct ShadowBounds
    {
        public static ShadowBounds Empty => new(Float3.Zero, Float3.Zero, false);

        public Float3 Min;
        public Float3 Max;
        private bool _hasBounds;

        private ShadowBounds(Float3 min, Float3 max, bool hasBounds)
        {
            Min = min;
            Max = max;
            _hasBounds = hasBounds;
        }

        public void Encapsulate(in TrianglePrimitive triangle)
        {
            Encapsulate(triangle.A);
            Encapsulate(triangle.B);
            Encapsulate(triangle.C);
        }

        private void Encapsulate(Float3 point)
        {
            if (!_hasBounds)
            {
                Min = point;
                Max = point;
                _hasBounds = true;
                return;
            }

            Min = Float3.Min(Min, point);
            Max = Float3.Max(Max, point);
        }
    }
}
