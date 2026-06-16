using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;

namespace Pixagen.Game.Features.PhysicsFeature.Runtime;

internal struct PhysicsNarrowPhaseCallbacks : INarrowPhaseCallbacks
{
    private readonly PhysicsMaterialStore _materials;
    private readonly SpringSettings _springSettings;

    public PhysicsNarrowPhaseCallbacks(PhysicsMaterialStore materials)
    {
        _materials = materials;
        _springSettings = new SpringSettings(30, 1);
    }

    public void Initialize(Simulation simulation)
    {
    }

    public bool AllowContactGeneration(
        int workerIndex,
        CollidableReference a,
        CollidableReference b,
        ref float speculativeMargin)
    {
        return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
    }

    public bool ConfigureContactManifold<TManifold>(
        int workerIndex,
        CollidablePair pair,
        ref TManifold manifold,
        out PairMaterialProperties pairMaterial)
        where TManifold : unmanaged, IContactManifold<TManifold>
    {
        PhysicsMaterial material = PhysicsMaterial.Combine(
            _materials.Get(pair.A),
            _materials.Get(pair.B));
        pairMaterial = new PairMaterialProperties(
            material.Friction,
            material.MaximumRecoveryVelocity,
            _springSettings);
        return true;
    }

    public bool AllowContactGeneration(
        int workerIndex,
        CollidablePair pair,
        int childIndexA,
        int childIndexB)
    {
        return true;
    }

    public bool ConfigureContactManifold(
        int workerIndex,
        CollidablePair pair,
        int childIndexA,
        int childIndexB,
        ref ConvexContactManifold manifold)
    {
        return true;
    }

    public void Dispose()
    {
    }
}
