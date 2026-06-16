using Pixagen.Ecs.Runtime;

namespace Pixagen.Game.Features.SharedFeature.Components;

public struct RotationMotion : IComponent
{
    public Vector3 Axis;
    public Fix AnglePerSecond;
    public bool LocalSpace;

    public RotationMotion(Vector3 axis, Fix anglePerSecond, bool localSpace = false)
    {
        Axis = axis;
        AnglePerSecond = anglePerSecond;
        LocalSpace = localSpace;
    }
}
