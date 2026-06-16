using Pixagen.Game.Features.RenderFeature.Components;
using Float3 = System.Numerics.Vector3;

namespace Pixagen.Game.Features.RenderFeature.Raycasting;

public readonly struct RayBuilder
{
    public readonly Float3 Origin;
    public readonly Float3 StartDirection;
    public readonly Float3 XDelta;
    public readonly Float3 YDelta;

    private RayBuilder(Float3 origin, Float3 startDirection, Float3 xDelta, Float3 yDelta)
    {
        Origin = origin;
        StartDirection = startDirection;
        XDelta = xDelta;
        YDelta = yDelta;
    }

    public static RayBuilder Create(
        int width,
        int height,
        Vector3 origin,
        CameraBasis basis,
        Camera camera)
    {
        float renderWidth = Math.Max(1, width);
        float renderHeight = Math.Max(1, height);
        float viewportHalfHeight = RenderMath.ToFloat(camera.ViewportHalfHeight);
        float viewportHalfWidth = GetViewportHalfWidth(width, height, camera);
        float ndcStartX = (1f - renderWidth) / renderWidth;
        float ndcStartY = (renderHeight - 1f) / renderHeight;
        float ndcStepX = 2f / renderWidth;
        float ndcStepY = 2f / renderHeight;
        Float3 forward = RenderMath.ToFloat(basis.Forward);
        Float3 right = RenderMath.ToFloat(basis.Right);
        Float3 up = RenderMath.ToFloat(basis.Up);

        Float3 startDirection =
            forward * RenderMath.ToFloat(camera.ProjectionPlaneDistance) +
            right * (ndcStartX * viewportHalfWidth) +
            up * (ndcStartY * viewportHalfHeight);

        Float3 xDelta = right * (ndcStepX * viewportHalfWidth);
        Float3 yDelta = up * (-ndcStepY * viewportHalfHeight);

        return new RayBuilder(RenderMath.ToFloat(origin), startDirection, xDelta, yDelta);
    }

    private static float GetViewportHalfWidth(int width, int height, Camera camera)
    {
        const float pixelCellAspect = 1f;
        return RenderMath.ToFloat(camera.ViewportHalfHeight) * width / Math.Max(1, height) * pixelCellAspect;
    }
}
