using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Rendering;
using Pixagen.Rendering.Raycasting;
using Float2 = System.Numerics.Vector2;
using Float3 = System.Numerics.Vector3;

namespace Pixagen.Game.Features.RenderFeature.Raycasting;

public sealed class RaycastTileBinBuilder
{
    public const int DefaultTileSize = 16;

    private const float NearPlaneEpsilon = 0.001f;
    private readonly RaycastTileBins _bins = new();
    private TriangleTileBounds[] _triangleBounds = [];
    private int[] _tileCounts = [];
    private int[] _tileWriteOffsets = [];
    private int _triangleBoundsCount;

    public RaycastTileBins Build(
        RenderResolution resolution,
        in Vector3 cameraPosition,
        CameraBasis basis,
        in Camera camera,
        RenderPrimitiveBatch staticPrimitives,
        RenderPrimitiveBatch dynamicPrimitives)
    {
        return Build(
            resolution,
            cameraPosition,
            basis,
            camera,
            staticPrimitives,
            dynamicPrimitives,
            DefaultTileSize);
    }

    public RaycastTileBins Build(
        RenderResolution resolution,
        in Vector3 cameraPosition,
        CameraBasis basis,
        in Camera camera,
        RenderPrimitiveBatch staticPrimitives,
        RenderPrimitiveBatch dynamicPrimitives,
        int tileSize)
    {
        int width = Math.Max(1, resolution.Width);
        int height = Math.Max(1, resolution.Height);
        int safeTileSize = Math.Max(1, tileSize);
        int tileColumns = Math.Max(1, (width + safeTileSize - 1) / safeTileSize);
        int tileRows = Math.Max(1, (height + safeTileSize - 1) / safeTileSize);
        int tileCount = tileColumns * tileRows;
        int triangleCount = staticPrimitives.Triangles.Count + dynamicPrimitives.Triangles.Count;

        EnsureTileCapacity(tileCount);
        Array.Clear(_tileCounts, 0, tileCount);
        _triangleBoundsCount = 0;
        EnsureTriangleBoundsCapacity(triangleCount);
        _bins.TileSize = safeTileSize;
        _bins.TileColumns = tileColumns;
        _bins.TileRows = tileRows;
        _bins.TileCount = tileCount;
        _bins.TriangleCount = triangleCount;

        var projector = new TriangleProjector(
            width,
            height,
            safeTileSize,
            tileColumns,
            tileRows,
            cameraPosition,
            basis,
            camera);

        int triangleIndex = 0;
        AddTriangleBounds(staticPrimitives.Triangles, ref triangleIndex, projector);
        AddTriangleBounds(dynamicPrimitives.Triangles, ref triangleIndex, projector);

        EnsureRangeCapacity(tileCount);
        int indexCount = FillRanges(tileCount);
        EnsureIndexCapacity(indexCount);
        FillIndices();

        _bins.IndexCount = indexCount;
        _bins.EstimatedPrimaryTriangleTests = EstimatePrimaryTriangleTests(width, height, safeTileSize, tileColumns, tileRows);
        return _bins;
    }

    private void AddTriangleBounds(
        List<TrianglePrimitive> triangles,
        ref int triangleIndex,
        in TriangleProjector projector)
    {
        foreach (TrianglePrimitive triangle in triangles)
        {
            if (projector.TryProject(triangle, out TriangleTileBounds bounds))
            {
                bounds = bounds with { TriangleIndex = triangleIndex };
                _triangleBounds[_triangleBoundsCount++] = bounds;
                AddTileCounts(bounds);
            }

            triangleIndex++;
        }
    }

    private void AddTileCounts(in TriangleTileBounds bounds)
    {
        for (int tileY = bounds.MinTileY; tileY <= bounds.MaxTileY; tileY++)
        {
            int rowOffset = tileY * _bins.TileColumns;
            for (int tileX = bounds.MinTileX; tileX <= bounds.MaxTileX; tileX++)
            {
                _tileCounts[rowOffset + tileX]++;
            }
        }
    }

    private int FillRanges(int tileCount)
    {
        int offset = 0;
        for (int tileIndex = 0; tileIndex < tileCount; tileIndex++)
        {
            int count = _tileCounts[tileIndex];
            _bins.Ranges[tileIndex] = new RaycastTileRange(offset, count);
            _tileWriteOffsets[tileIndex] = offset;
            offset += count;
        }

        return offset;
    }

    private void FillIndices()
    {
        for (int i = 0; i < _triangleBoundsCount; i++)
        {
            TriangleTileBounds bounds = _triangleBounds[i];
            for (int tileY = bounds.MinTileY; tileY <= bounds.MaxTileY; tileY++)
            {
                int rowOffset = tileY * _bins.TileColumns;
                for (int tileX = bounds.MinTileX; tileX <= bounds.MaxTileX; tileX++)
                {
                    int tileIndex = rowOffset + tileX;
                    _bins.TriangleIndices[_tileWriteOffsets[tileIndex]++] = bounds.TriangleIndex;
                }
            }
        }
    }

    private double EstimatePrimaryTriangleTests(
        int width,
        int height,
        int tileSize,
        int tileColumns,
        int tileRows)
    {
        double tests = 0;
        for (int tileY = 0; tileY < tileRows; tileY++)
        {
            int tileHeight = Math.Min(tileSize, height - tileY * tileSize);
            for (int tileX = 0; tileX < tileColumns; tileX++)
            {
                int tileWidth = Math.Min(tileSize, width - tileX * tileSize);
                int tileIndex = tileY * tileColumns + tileX;
                tests += (double)Math.Max(0, tileWidth) * Math.Max(0, tileHeight) * _tileCounts[tileIndex];
            }
        }

        return tests;
    }

    private void EnsureTileCapacity(int tileCount)
    {
        if (_tileCounts.Length < tileCount)
        {
            _tileCounts = new int[tileCount];
            _tileWriteOffsets = new int[tileCount];
        }
    }

    private void EnsureRangeCapacity(int tileCount)
    {
        if (_bins.Ranges.Length < tileCount)
        {
            _bins.Ranges = new RaycastTileRange[tileCount];
        }
    }

    private void EnsureTriangleBoundsCapacity(int triangleCount)
    {
        if (_triangleBounds.Length < triangleCount)
        {
            _triangleBounds = new TriangleTileBounds[Math.Max(1, triangleCount)];
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

    private readonly record struct TriangleTileBounds(
        int TriangleIndex,
        int MinTileX,
        int MaxTileX,
        int MinTileY,
        int MaxTileY);

    private readonly struct TriangleProjector
    {
        private readonly int _width;
        private readonly int _height;
        private readonly int _tileSize;
        private readonly int _tileColumns;
        private readonly int _tileRows;
        private readonly Float3 _origin;
        private readonly Float3 _forward;
        private readonly Float3 _right;
        private readonly Float3 _up;
        private readonly float _projectionPlaneDistance;
        private readonly float _viewportHalfWidth;
        private readonly float _viewportHalfHeight;

        public TriangleProjector(
            int width,
            int height,
            int tileSize,
            int tileColumns,
            int tileRows,
            in Vector3 cameraPosition,
            CameraBasis basis,
            in Camera camera)
        {
            _width = width;
            _height = height;
            _tileSize = tileSize;
            _tileColumns = tileColumns;
            _tileRows = tileRows;
            _origin = RenderMath.ToFloat(cameraPosition);
            _forward = RenderMath.ToFloat(basis.Forward);
            _right = RenderMath.ToFloat(basis.Right);
            _up = RenderMath.ToFloat(basis.Up);
            _projectionPlaneDistance = MathF.Max(RenderMath.ToFloat(camera.ProjectionPlaneDistance), RenderMath.Epsilon);
            _viewportHalfHeight = MathF.Max(RenderMath.ToFloat(camera.ViewportHalfHeight), RenderMath.Epsilon);
            _viewportHalfWidth = _viewportHalfHeight * width / Math.Max(1, height);
        }

        public bool TryProject(in TrianglePrimitive triangle, out TriangleTileBounds bounds)
        {
            if (!TryProjectPoint(triangle.A, out Float2 a) ||
                !TryProjectPoint(triangle.B, out Float2 b) ||
                !TryProjectPoint(triangle.C, out Float2 c))
            {
                bounds = FullScreenBounds();
                return true;
            }

            float minX = MathF.Min(a.X, MathF.Min(b.X, c.X)) - 1f;
            float maxX = MathF.Max(a.X, MathF.Max(b.X, c.X)) + 1f;
            float minY = MathF.Min(a.Y, MathF.Min(b.Y, c.Y)) - 1f;
            float maxY = MathF.Max(a.Y, MathF.Max(b.Y, c.Y)) + 1f;

            if (maxX < 0 || maxY < 0 || minX >= _width || minY >= _height)
            {
                bounds = default;
                return false;
            }

            bounds = new TriangleTileBounds(
                0,
                ClampTile((int)MathF.Floor(minX / _tileSize), _tileColumns),
                ClampTile((int)MathF.Floor(maxX / _tileSize), _tileColumns),
                ClampTile((int)MathF.Floor(minY / _tileSize), _tileRows),
                ClampTile((int)MathF.Floor(maxY / _tileSize), _tileRows));
            return true;
        }

        private bool TryProjectPoint(Float3 point, out Float2 pixel)
        {
            Float3 local = point - _origin;
            float z = Float3.Dot(local, _forward);
            if (z <= NearPlaneEpsilon)
            {
                pixel = default;
                return false;
            }

            float x = Float3.Dot(local, _right);
            float y = Float3.Dot(local, _up);
            float projectedX = x * _projectionPlaneDistance / (z * _viewportHalfWidth);
            float projectedY = y * _projectionPlaneDistance / (z * _viewportHalfHeight);
            pixel = new Float2(
                (projectedX + 1f) * 0.5f * _width,
                (1f - projectedY) * 0.5f * _height);
            return true;
        }

        private TriangleTileBounds FullScreenBounds()
        {
            return new TriangleTileBounds(0, 0, _tileColumns - 1, 0, _tileRows - 1);
        }

        private static int ClampTile(int value, int count)
        {
            return Math.Clamp(value, 0, count - 1);
        }
    }
}
