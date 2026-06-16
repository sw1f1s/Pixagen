using Pixagen.Ecs.Runtime;

namespace Pixagen.Game.Features.RenderFeature.Components;

public struct Camera : IComponent
{
    public Fix ProjectionPlaneDistance;
    public Fix ViewportHalfWidth;
    public Fix ViewportHalfHeight;
    public Fix MaxDistance;

    public Camera(Fix projectionPlaneDistance, Fix viewportHalfWidth, Fix viewportHalfHeight, Fix maxDistance)
    {
        ProjectionPlaneDistance = projectionPlaneDistance;
        ViewportHalfWidth = viewportHalfWidth;
        ViewportHalfHeight = viewportHalfHeight;
        MaxDistance = maxDistance;
    }
}
