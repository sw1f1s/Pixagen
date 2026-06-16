using System.Numerics;
using BepuPhysics;
using BepuUtilities;
using NumericVector3 = System.Numerics.Vector3;

namespace Pixagen.Game.Features.PhysicsFeature.Runtime;

public struct PhysicsPoseIntegratorCallbacks : IPoseIntegratorCallbacks
{
    private NumericVector3 _gravity;
    private Vector3Wide _gravityWide;

    public PhysicsPoseIntegratorCallbacks(NumericVector3 gravity)
    {
        _gravity = gravity;
        _gravityWide = default;
    }

    public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;

    public bool AllowSubstepsForUnconstrainedBodies => false;

    public bool IntegrateVelocityForKinematics => false;

    public void Initialize(Simulation simulation)
    {
    }

    public void PrepareForIntegration(float dt)
    {
        Vector3Wide.Broadcast(_gravity, out _gravityWide);
    }

    public void IntegrateVelocity(
        Vector<int> bodyIndices,
        Vector3Wide position,
        QuaternionWide orientation,
        BodyInertiaWide localInertia,
        Vector<int> integrationMask,
        int workerIndex,
        Vector<float> dt,
        ref BodyVelocityWide velocity)
    {
        velocity.Linear += _gravityWide * dt;
    }
}
