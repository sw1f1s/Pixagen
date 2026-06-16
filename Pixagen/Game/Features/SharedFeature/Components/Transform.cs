
namespace Pixagen.Game.Features.SharedFeature.Components;

public struct Transform : IComponent
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;

    public Transform(Vector3 position)
        : this(position, Quaternion.Identity, Vector3.One)
    {
    }

    public Transform(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Position = position;
        Rotation = rotation;
        Scale = scale;
    }
}
