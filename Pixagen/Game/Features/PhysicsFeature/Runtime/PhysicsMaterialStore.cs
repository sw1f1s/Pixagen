using BepuPhysics.Collidables;
using Pixagen.Game.Features.PhysicsFeature.Components;

namespace Pixagen.Game.Features.PhysicsFeature.Runtime;

internal sealed class PhysicsMaterialStore
{
    private readonly Dictionary<PhysicsMaterialKey, PhysicsMaterial> _materials = new();

    public PhysicsMaterial DefaultMaterial { get; } = new(1f, 2f);

    public void Set(CollidableReference reference, RigidBody rigidBody)
    {
        _materials[PhysicsMaterialKey.From(reference)] = new PhysicsMaterial(
            MathF.Max(0, PhysicsConvert.ToFloat(rigidBody.Friction)),
            MathF.Max(0, PhysicsConvert.ToFloat(rigidBody.MaximumRecoveryVelocity)));
    }

    public void Remove(CollidableReference reference)
    {
        _materials.Remove(PhysicsMaterialKey.From(reference));
    }

    public PhysicsMaterial Get(CollidableReference reference)
    {
        return _materials.TryGetValue(PhysicsMaterialKey.From(reference), out PhysicsMaterial material)
            ? material
            : DefaultMaterial;
    }
}

internal readonly record struct PhysicsMaterial(float Friction, float MaximumRecoveryVelocity)
{
    public static PhysicsMaterial Combine(PhysicsMaterial a, PhysicsMaterial b)
    {
        return new PhysicsMaterial(
            MathF.Sqrt(a.Friction * b.Friction),
            MathF.Min(a.MaximumRecoveryVelocity, b.MaximumRecoveryVelocity));
    }
}

internal readonly record struct PhysicsMaterialKey(CollidableMobility Mobility, int Handle)
{
    public static PhysicsMaterialKey From(CollidableReference reference)
    {
        return new PhysicsMaterialKey(reference.Mobility, reference.RawHandleValue);
    }
}
