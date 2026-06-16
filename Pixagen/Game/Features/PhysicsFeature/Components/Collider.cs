using Pixagen.Ecs.Runtime;

namespace Pixagen.Game.Features.PhysicsFeature.Components;

public struct Collider : IComponent
{
    public ColliderShape Shape;
    public Vector3 Size;
    public Fix Radius;
    public Fix Length;

    public Collider(ColliderShape shape)
        : this(shape, Vector3.One, Fix.One / new Fix(2), Fix.One)
    {
    }

    public Collider(ColliderShape shape, Vector3 size, Fix radius, Fix length)
    {
        Shape = shape;
        Size = size;
        Radius = radius;
        Length = length;
    }

    public static Collider Box(Vector3 size)
    {
        return new Collider(ColliderShape.Box, size, Fix.One / new Fix(2), Fix.One);
    }

    public static Collider Sphere(Fix radius)
    {
        return new Collider(ColliderShape.Sphere, Vector3.One, radius, Fix.One);
    }

    public static Collider Capsule(Fix radius, Fix length)
    {
        return new Collider(ColliderShape.Capsule, Vector3.One, radius, length);
    }
}

public enum ColliderShape
{
    Box,
    Sphere,
    Capsule
}
