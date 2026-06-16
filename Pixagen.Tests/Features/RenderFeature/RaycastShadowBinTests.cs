using Pixagen.Game.Features.RenderFeature;
using Pixagen.Game.Features.RenderFeature.Raycasting;
using Pixagen.Rendering;
using Float2 = System.Numerics.Vector2;
using Float3 = System.Numerics.Vector3;

namespace Pixagen.Tests.Features.RenderFeature;

public sealed class RaycastShadowBinTests
{
    [Fact]
    public void Build_AssignsShadowTriangleToGridCells()
    {
        var batch = new RenderPrimitiveBatch();
        batch.ShadowTriangles.Add(CreateTriangle(
            new Float3(-0.2f, 0f, 4f),
            new Float3(0.2f, 0f, 4f),
            new Float3(0f, 0.4f, 4f)));
        var builder = new RaycastShadowBinBuilder();

        RaycastShadowBins bins = builder.Build(
            new RenderResolution(64, 64),
            DirectionalLight.Default,
            batch,
            RenderPrimitiveBatch.Empty);

        Assert.True(bins.CellCount > 0);
        Assert.True(bins.IndexCount > 0);
        Assert.True(bins.EstimatedShadowTriangleTests > 0);
    }

    [Fact]
    public void Build_EstimatesLessWorkThanNaiveFullShadowLoopForDistributedTriangles()
    {
        var batch = new RenderPrimitiveBatch();
        for (int z = 0; z < 4; z++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    Float3 center = new(x * 2f, y * 2f, 4f + z * 2f);
                    batch.ShadowTriangles.Add(CreateTriangle(
                        center + new Float3(-0.1f, 0f, 0f),
                        center + new Float3(0.1f, 0f, 0f),
                        center + new Float3(0f, 0.1f, 0f)));
                }
            }
        }

        var builder = new RaycastShadowBinBuilder();

        RaycastShadowBins bins = builder.Build(
            new RenderResolution(64, 64),
            DirectionalLight.Default,
            batch,
            RenderPrimitiveBatch.Empty);

        double naiveShadowTests = 64 * 64 * batch.ShadowTriangles.Count;
        Assert.True(bins.EstimatedShadowTriangleTests < naiveShadowTests);
    }

    [Fact]
    public void Build_ClearsWhenThereAreNoShadowTriangles()
    {
        var builder = new RaycastShadowBinBuilder();

        RaycastShadowBins bins = builder.Build(
            new RenderResolution(64, 64),
            DirectionalLight.Default,
            RenderPrimitiveBatch.Empty,
            RenderPrimitiveBatch.Empty);

        Assert.Equal(0, bins.CellCount);
        Assert.Equal(0, bins.IndexCount);
        Assert.Equal(0, bins.EstimatedShadowTriangleTests);
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
