using BepuPhysics;
using BepuPhysics.Collidables;

namespace Pixagen.Game.Features.PhysicsFeature.Components;

public struct PhysicsBodyReference : IComponent
{
    public PhysicsBodyKind Kind;
    public BodyHandle BodyHandle;
    public StaticHandle StaticHandle;
    public TypedIndex ShapeIndex;
    public bool Active;

    public PhysicsBodyReference(BodyHandle bodyHandle, PhysicsBodyKind kind, TypedIndex shapeIndex)
    {
        Kind = kind;
        BodyHandle = bodyHandle;
        StaticHandle = default;
        ShapeIndex = shapeIndex;
        Active = true;
    }

    public PhysicsBodyReference(StaticHandle staticHandle, TypedIndex shapeIndex)
    {
        Kind = PhysicsBodyKind.Static;
        BodyHandle = default;
        StaticHandle = staticHandle;
        ShapeIndex = shapeIndex;
        Active = true;
    }
}
