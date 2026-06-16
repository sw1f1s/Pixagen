using Pixagen.Game.Features.RenderFeature;
using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.RenderFeature.Raycasting;
using Pixagen.Rendering;
using Float2 = System.Numerics.Vector2;
using Float3 = System.Numerics.Vector3;

namespace Pixagen.Tests.Features.RenderFeature;

public sealed class RaycastTileBinTests
{
    [Fact]
    public void Build_AssignsSmallTriangleToSubsetOfTiles()
    {
        var batch = new RenderPrimitiveBatch();
        batch.Triangles.Add(CreateTriangle(
            new Float3(-0.2f, -0.2f, 4f),
            new Float3(0.2f, -0.2f, 4f),
            new Float3(0f, 0.2f, 4f)));
        var builder = new RaycastTileBinBuilder();

        RaycastTileBins bins = builder.Build(
            new RenderResolution(64, 64),
            Vector3.Zero,
            CreateBasis(),
            CreateCamera(),
            batch,
            RenderPrimitiveBatch.Empty,
            tileSize: 16);

        Assert.Equal(16, bins.TileCount);
        Assert.InRange(bins.IndexCount, 1, 15);
        Assert.True(bins.EstimatedPrimaryTriangleTests < 64 * 64);
    }

    [Fact]
    public void Build_AssignsNearPlaneTriangleToAllTilesConservatively()
    {
        var batch = new RenderPrimitiveBatch();
        batch.Triangles.Add(CreateTriangle(
            new Float3(-1f, -1f, -1f),
            new Float3(1f, -1f, 4f),
            new Float3(0f, 1f, 4f)));
        var builder = new RaycastTileBinBuilder();

        RaycastTileBins bins = builder.Build(
            new RenderResolution(64, 64),
            Vector3.Zero,
            CreateBasis(),
            CreateCamera(),
            batch,
            RenderPrimitiveBatch.Empty,
            tileSize: 16);

        Assert.Equal(16, bins.TileCount);
        Assert.Equal(16, bins.IndexCount);
    }

    private static CameraBasis CreateBasis()
    {
        return new CameraBasis(Vector3.Forward, Vector3.Right, Vector3.Up);
    }

    private static Camera CreateCamera()
    {
        return new Camera(Fix.One, Fix.One, Fix.One, Fix.FromDouble(32));
    }

    private static TrianglePrimitive CreateTriangle(Float3 a, Float3 b, Float3 c)
    {
        return new TrianglePrimitive(
            a,
            b,
            c,
            Float2.Zero,
            Float2.UnitX,
            Float2.UnitY,
            SurfaceMaterial.Default);
    }
}
