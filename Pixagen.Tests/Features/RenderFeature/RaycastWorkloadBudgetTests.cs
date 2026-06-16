using Pixagen.Game.Features.RenderFeature;
using Pixagen.Game.Features.RenderFeature.Raycasting;

namespace Pixagen.Tests.Features.RenderFeature;

public sealed class RaycastWorkloadBudgetTests
{
    [Fact]
    public void Limit_KeepsSmallWorkload()
    {
        var resolution = new RenderResolution(320, 180);

        RaycastWorkloadBudgetResult result = RaycastWorkloadBudget.Limit(
            resolution,
            ShadowQuality.Full,
            triangleCount: 32,
            shadowTriangleCount: 16);

        Assert.Equal(resolution, result.Resolution);
        Assert.Equal(ShadowQuality.Full, result.ShadowQuality);
    }

    [Fact]
    public void Limit_DisablesShadowsBeforeScalingResolution()
    {
        var resolution = new RenderResolution(480, 270);

        RaycastWorkloadBudgetResult result = RaycastWorkloadBudget.Limit(
            resolution,
            ShadowQuality.Full,
            triangleCount: 100,
            shadowTriangleCount: 10_000);

        Assert.Equal(resolution, result.Resolution);
        Assert.Equal(ShadowQuality.Off, result.ShadowQuality);
    }

    [Fact]
    public void Limit_KeepsBinnedShadowsWhenEstimatedShadowWorkFits()
    {
        var resolution = new RenderResolution(480, 270);

        RaycastWorkloadBudgetResult result = RaycastWorkloadBudget.Limit(
            resolution,
            ShadowQuality.Full,
            estimatedPrimaryTriangleTests: 8_000_000,
            estimatedShadowTriangleTests: 12_000_000);

        Assert.Equal(resolution, result.Resolution);
        Assert.Equal(ShadowQuality.Full, result.ShadowQuality);
    }

    [Fact]
    public void Limit_ScalesResolutionWhenGeometryIsStillTooExpensive()
    {
        var resolution = new RenderResolution(480, 270);

        RaycastWorkloadBudgetResult result = RaycastWorkloadBudget.Limit(
            resolution,
            ShadowQuality.Off,
            triangleCount: 5_000,
            shadowTriangleCount: 0);

        Assert.True(result.Resolution.Width < resolution.Width);
        Assert.True(result.Resolution.Height < resolution.Height);
        Assert.Equal(ShadowQuality.Off, result.ShadowQuality);
        Assert.True(result.EstimatedTriangleTests <= RaycastWorkloadBudget.DefaultMaxEstimatedTriangleTests);
    }
}
