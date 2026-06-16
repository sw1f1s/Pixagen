using Pixagen.Ecs.Runtime;

namespace Pixagen.Game.Features.SharedFeature.Components;

public struct LocalTransform : IComponent
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;

    public LocalTransform(Vector3 position)
        : this(position, Quaternion.Identity, Vector3.One)
    {
    }

    public LocalTransform(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Position = position;
        Rotation = rotation;
        Scale = scale;
    }

    public static LocalTransform FromTransform(Transform transform)
    {
        return new LocalTransform(transform.Position, transform.Rotation, transform.Scale);
    }
}
