using Pixagen.Game.Features.RenderFeature;

namespace Pixagen.Game.Features.RenderFeature.Raycasting;

public readonly record struct RaycastWorkloadBudgetResult(
    RenderResolution Resolution,
    ShadowQuality ShadowQuality,
    double EstimatedTriangleTests);

public static class RaycastWorkloadBudget
{
    public const long DefaultMaxEstimatedTriangleTests = 96_000_000;

    private const double TraceLayerCostFactor = 1.25;
    private const double LowShadowCostFactor = 0.5;

    public static RaycastWorkloadBudgetResult Limit(
        RenderResolution requestedResolution,
        ShadowQuality requestedShadowQuality,
        int triangleCount,
        int shadowTriangleCount)
    {
        return Limit(
            requestedResolution,
            requestedShadowQuality,
            triangleCount,
            shadowTriangleCount,
            DefaultMaxEstimatedTriangleTests);
    }

    public static RaycastWorkloadBudgetResult Limit(
        RenderResolution requestedResolution,
        ShadowQuality requestedShadowQuality,
        int triangleCount,
        int shadowTriangleCount,
        long maxEstimatedTriangleTests)
    {
        RenderResolution resolution = Sanitize(requestedResolution);
        double pixelCount = (double)resolution.Width * resolution.Height;
        double estimatedPrimaryTriangleTests = pixelCount * Math.Max(0, triangleCount);
        return Limit(
            resolution,
            requestedShadowQuality,
            estimatedPrimaryTriangleTests,
            EstimateNaiveShadowTriangleTests(resolution, shadowTriangleCount),
            maxEstimatedTriangleTests);
    }

    public static RaycastWorkloadBudgetResult Limit(
        RenderResolution requestedResolution,
        ShadowQuality requestedShadowQuality,
        double estimatedPrimaryTriangleTests,
        int shadowTriangleCount)
    {
        return Limit(
            requestedResolution,
            requestedShadowQuality,
            estimatedPrimaryTriangleTests,
            EstimateNaiveShadowTriangleTests(requestedResolution, shadowTriangleCount),
            DefaultMaxEstimatedTriangleTests);
    }

    public static RaycastWorkloadBudgetResult Limit(
        RenderResolution requestedResolution,
        ShadowQuality requestedShadowQuality,
        double estimatedPrimaryTriangleTests,
        double estimatedShadowTriangleTests)
    {
        return Limit(
            requestedResolution,
            requestedShadowQuality,
            estimatedPrimaryTriangleTests,
            estimatedShadowTriangleTests,
            DefaultMaxEstimatedTriangleTests);
    }

    public static RaycastWorkloadBudgetResult Limit(
        RenderResolution requestedResolution,
        ShadowQuality requestedShadowQuality,
        double estimatedPrimaryTriangleTests,
        int shadowTriangleCount,
        long maxEstimatedTriangleTests)
    {
        return Limit(
            requestedResolution,
            requestedShadowQuality,
            estimatedPrimaryTriangleTests,
            EstimateNaiveShadowTriangleTests(requestedResolution, shadowTriangleCount),
            maxEstimatedTriangleTests);
    }

    public static RaycastWorkloadBudgetResult Limit(
        RenderResolution requestedResolution,
        ShadowQuality requestedShadowQuality,
        double estimatedPrimaryTriangleTests,
        double estimatedShadowTriangleTests,
        long maxEstimatedTriangleTests)
    {
        RenderResolution resolution = Sanitize(requestedResolution);
        ShadowQuality shadowQuality = requestedShadowQuality;
        double safePrimaryTriangleTests = Math.Max(0, estimatedPrimaryTriangleTests);
        double safeShadowTriangleTests = Math.Max(0, estimatedShadowTriangleTests);
        double maxTests = Math.Max(1, maxEstimatedTriangleTests);

        while (shadowQuality != ShadowQuality.Off &&
            EstimateTriangleTests(resolution, shadowQuality, safePrimaryTriangleTests, safeShadowTriangleTests) > maxTests)
        {
            shadowQuality = shadowQuality == ShadowQuality.Full
                ? ShadowQuality.Low
                : ShadowQuality.Off;
        }

        double estimatedTests = EstimateTriangleTests(
            resolution,
            shadowQuality,
            safePrimaryTriangleTests,
            safeShadowTriangleTests);

        if (estimatedTests <= maxTests)
        {
            return new RaycastWorkloadBudgetResult(resolution, shadowQuality, estimatedTests);
        }

        double scale = Math.Sqrt(maxTests / estimatedTests);
        resolution = Scale(resolution, scale);
        safePrimaryTriangleTests *= scale * scale;
        safeShadowTriangleTests *= scale * scale;
        estimatedTests = EstimateTriangleTests(
            resolution,
            shadowQuality,
            safePrimaryTriangleTests,
            safeShadowTriangleTests);

        return new RaycastWorkloadBudgetResult(resolution, shadowQuality, estimatedTests);
    }

    public static double EstimateTriangleTests(
        RenderResolution resolution,
        ShadowQuality shadowQuality,
        int triangleCount,
        int shadowTriangleCount)
    {
        RenderResolution safeResolution = Sanitize(resolution);
        double pixelCount = (double)safeResolution.Width * safeResolution.Height;
        return EstimateTriangleTests(
            safeResolution,
            shadowQuality,
            pixelCount * Math.Max(0, triangleCount),
            EstimateNaiveShadowTriangleTests(safeResolution, shadowTriangleCount));
    }

    public static double EstimateTriangleTests(
        RenderResolution resolution,
        ShadowQuality shadowQuality,
        double estimatedPrimaryTriangleTests,
        int shadowTriangleCount)
    {
        RenderResolution safeResolution = Sanitize(resolution);
        return EstimateTriangleTests(
            safeResolution,
            shadowQuality,
            estimatedPrimaryTriangleTests,
            EstimateNaiveShadowTriangleTests(safeResolution, shadowTriangleCount));
    }

    public static double EstimateTriangleTests(
        RenderResolution resolution,
        ShadowQuality shadowQuality,
        double estimatedPrimaryTriangleTests,
        double estimatedShadowTriangleTests)
    {
        _ = resolution;
        double traceCost = Math.Max(0, estimatedPrimaryTriangleTests) * TraceLayerCostFactor;
        double shadowCost = shadowQuality switch
        {
            ShadowQuality.Full => Math.Max(0, estimatedShadowTriangleTests),
            ShadowQuality.Low => Math.Max(0, estimatedShadowTriangleTests) * LowShadowCostFactor,
            _ => 0
        };

        return traceCost + shadowCost;
    }

    private static RenderResolution Sanitize(RenderResolution resolution)
    {
        return new RenderResolution(Math.Max(1, resolution.Width), Math.Max(1, resolution.Height));
    }

    private static double EstimateNaiveShadowTriangleTests(RenderResolution resolution, int shadowTriangleCount)
    {
        RenderResolution safeResolution = Sanitize(resolution);
        double pixelCount = (double)safeResolution.Width * safeResolution.Height;
        return pixelCount * Math.Max(0, shadowTriangleCount);
    }

    private static RenderResolution Scale(RenderResolution resolution, double scale)
    {
        if (double.IsNaN(scale) || double.IsInfinity(scale) || scale <= 0)
        {
            return new RenderResolution(1, 1);
        }

        scale = Math.Min(1, scale);
        return new RenderResolution(
            Math.Max(1, (int)Math.Floor(resolution.Width * scale)),
            Math.Max(1, (int)Math.Floor(resolution.Height * scale)));
    }
}
