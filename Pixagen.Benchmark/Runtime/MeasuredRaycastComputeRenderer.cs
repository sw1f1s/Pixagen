using Pixagen.Game.Features.RenderFeature.Raycasting;
using Pixagen.Rendering;
using Float3 = System.Numerics.Vector3;

namespace Pixagen.Benchmark;

public sealed class MeasuredRaycastComputeRenderer : IRaycastComputeRenderer
{
    public int LastPixels { get; private set; }
    public int LastTriangles { get; private set; }
    public int LastShadowTriangles { get; private set; }
    public int LastPixelSamples { get; private set; }
    public double LastChecksum { get; private set; }

    public bool TryRenderRaycast(in RaycastComputeRequest request)
    {
        LastPixels = Math.Max(0, request.Width * request.Height);
        LastTriangles = request.StaticPrimitives.Triangles.Count + request.DynamicPrimitives.Triangles.Count;
        LastShadowTriangles = request.StaticPrimitives.ShadowTriangles.Count + request.DynamicPrimitives.ShadowTriangles.Count;

        double checksum = request.MaxDistance + request.Light.Intensity + request.Light.AmbientIntensity;
        checksum += AccumulatePrimitives(request.StaticPrimitives.Triangles);
        checksum += AccumulatePrimitives(request.DynamicPrimitives.Triangles);
        checksum += AccumulatePrimitives(request.StaticPrimitives.ShadowTriangles) * 0.5;
        checksum += AccumulatePrimitives(request.DynamicPrimitives.ShadowTriangles) * 0.5;

        int pixelStride = Math.Max(1, LastPixels / 16_384);
        int samples = 0;
        for (int y = 0; y < request.Height; y++)
        {
            for (int x = 0; x < request.Width; x++)
            {
                int pixelIndex = y * request.Width + x;
                if (pixelIndex % pixelStride != 0)
                {
                    continue;
                }

                Float3 direction = request.RayBuilder.StartDirection +
                    request.RayBuilder.XDelta * x +
                    request.RayBuilder.YDelta * y;
                checksum += direction.X * 0.00011;
                checksum += direction.Y * 0.00013;
                checksum += direction.Z * 0.00017;
                samples++;
            }
        }

        LastPixelSamples = samples;
        LastChecksum = checksum;
        return true;
    }

    private static double AccumulatePrimitives(List<TrianglePrimitive> triangles)
    {
        double checksum = 0;
        foreach (TrianglePrimitive triangle in triangles)
        {
            checksum += triangle.BoundsCenter.X * 0.001;
            checksum += triangle.BoundsCenter.Y * 0.002;
            checksum += triangle.BoundsCenter.Z * 0.003;
            checksum += triangle.BoundsRadius * 0.004;
            checksum += triangle.Normal.X * 0.005;
            checksum += triangle.Normal.Y * 0.006;
            checksum += triangle.Normal.Z * 0.007;
            checksum += triangle.Material.Color.R;
            checksum += triangle.Material.Color.G;
            checksum += triangle.Material.Color.B;
            checksum += triangle.Material.Opacity;
        }

        return checksum;
    }
}
