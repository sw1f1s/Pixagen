using Pixagen.Game.Features.RenderFeature.Components;
using Float3 = System.Numerics.Vector3;

namespace Pixagen.Game.Features.RenderFeature.Raycasting;

public sealed class RenderFrustumCuller
{
    public void Cull(
        RenderPrimitiveBatch source,
        RenderPrimitiveBatch destination,
        in RenderViewFrustum frustum,
        Float3 shadowOrigin,
        float shadowRenderDistance)
    {
        destination.Clear();
        destination.EnsureCapacity(source.Triangles.Count, source.ShadowTriangles.Count);

        foreach (TrianglePrimitive triangle in source.Triangles)
        {
            if (frustum.ContainsBounds(triangle.BoundsCenter, triangle.BoundsRadius))
            {
                destination.Triangles.Add(triangle);
            }
        }

        foreach (TrianglePrimitive triangle in source.ShadowTriangles)
        {
            if (IsWithinShadowDistance(triangle, shadowOrigin, shadowRenderDistance))
            {
                destination.ShadowTriangles.Add(triangle);
            }
        }

        destination.RefreshShadowState();
    }

    private static bool IsWithinShadowDistance(
        in TrianglePrimitive triangle,
        Float3 origin,
        float maxDistance)
    {
        float expandedDistance = maxDistance + triangle.BoundsRadius;
        return Float3.DistanceSquared(triangle.BoundsCenter, origin) <= expandedDistance * expandedDistance;
    }
}

public readonly struct RenderViewFrustum
{
    private const float PixelCellAspect = 1f;

    private readonly Float3 _origin;
    private readonly Float3 _forward;
    private readonly Float3 _right;
    private readonly Float3 _up;
    private readonly float _projectionPlaneDistance;
    private readonly float _viewportHalfWidth;
    private readonly float _viewportHalfHeight;
    private readonly float _maxDistance;

    private RenderViewFrustum(
        Float3 origin,
        Float3 forward,
        Float3 right,
        Float3 up,
        float projectionPlaneDistance,
        float viewportHalfWidth,
        float viewportHalfHeight,
        float maxDistance)
    {
        _origin = origin;
        _forward = forward;
        _right = right;
        _up = up;
        _projectionPlaneDistance = MathF.Max(projectionPlaneDistance, RenderMath.Epsilon);
        _viewportHalfWidth = MathF.Max(viewportHalfWidth, RenderMath.Epsilon);
        _viewportHalfHeight = MathF.Max(viewportHalfHeight, RenderMath.Epsilon);
        _maxDistance = MathF.Max(maxDistance, RenderMath.Epsilon);
    }

    public static RenderViewFrustum Create(
        int width,
        int height,
        Vector3 origin,
        CameraBasis basis,
        Camera camera,
        float maxDistance)
    {
        float viewportHalfHeight = RenderMath.ToFloat(camera.ViewportHalfHeight);
        float viewportHalfWidth = viewportHalfHeight * Math.Max(1, width) / Math.Max(1, height) * PixelCellAspect;

        return new RenderViewFrustum(
            RenderMath.ToFloat(origin),
            RenderMath.ToFloat(basis.Forward),
            RenderMath.ToFloat(basis.Right),
            RenderMath.ToFloat(basis.Up),
            RenderMath.ToFloat(camera.ProjectionPlaneDistance),
            viewportHalfWidth,
            viewportHalfHeight,
            maxDistance);
    }

    public bool ContainsBounds(Float3 center, float radius)
    {
        Float3 local = center - _origin;
        float z = Float3.Dot(local, _forward);

        if (z + radius <= RenderMath.Epsilon || z - radius >= _maxDistance)
        {
            return false;
        }

        if (z <= radius)
        {
            return true;
        }

        float x = Float3.Dot(local, _right);
        float y = Float3.Dot(local, _up);
        float distanceScale = z / _projectionPlaneDistance;
        float halfWidthAtDepth = _viewportHalfWidth * distanceScale;
        float halfHeightAtDepth = _viewportHalfHeight * distanceScale;
        float padding = radius + RenderMath.Epsilon;

        return x >= -halfWidthAtDepth - padding &&
            x <= halfWidthAtDepth + padding &&
            y >= -halfHeightAtDepth - padding &&
            y <= halfHeightAtDepth + padding;
    }
}
