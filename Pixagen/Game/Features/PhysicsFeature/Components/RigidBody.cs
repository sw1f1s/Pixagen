using Pixagen.Ecs.Runtime;

namespace Pixagen.Game.Features.PhysicsFeature.Components;

public struct RigidBody : IComponent
{
    public PhysicsBodyKind Kind;
    public Fix Mass;
    public Fix Friction;
    public Fix MaximumRecoveryVelocity;
    public bool LockRotation;

    public RigidBody(PhysicsBodyKind kind)
        : this(kind, Fix.One, Fix.One, Fix.Two, false)
    {
    }

    public RigidBody(
        PhysicsBodyKind kind,
        Fix mass,
        Fix friction,
        Fix maximumRecoveryVelocity,
        bool lockRotation = false)
    {
        Kind = kind;
        Mass = mass;
        Friction = friction;
        MaximumRecoveryVelocity = maximumRecoveryVelocity;
        LockRotation = lockRotation;
    }

    public static RigidBody Static() => new(PhysicsBodyKind.Static);

    public static RigidBody Dynamic(Fix mass, bool lockRotation = false)
    {
        return new RigidBody(PhysicsBodyKind.Dynamic, mass, Fix.One, Fix.Two, lockRotation);
    }

    public static RigidBody Kinematic(bool lockRotation = false)
    {
        return new RigidBody(PhysicsBodyKind.Kinematic, Fix.One, Fix.One, Fix.Two, lockRotation);
    }
}

public enum PhysicsBodyKind
{
    Static,
    Dynamic,
    Kinematic
}
