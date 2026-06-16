using Pixagen.Game.Features.PhysicsFeature;
using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.SharedFeature.Components;
using Pixagen.Game.Features.SharedFeature.Helper;
using Pixagen.Ecs.Runtime;
using Pixagen.Tests.TestSupport;
using static Pixagen.Tests.TestSupport.EcsTestAccess;

namespace Pixagen.Tests.Features.PhysicsFeature;

public sealed class PhysicsTests
{
    [Fact]
    public void PhysicsFeature_CreatesBodyReferenceForDynamicBody()
    {
        using var context = new EcsTestContext();
        Entity entity = context.State.CreateObject();
        Access(entity).Add(RigidBody.Dynamic(Fix.One));
        Access(entity).Add(Collider.Sphere(Fix.One));

        context.BuildSystems(new PhysicsFeatureSystemsGroup());

        Assert.True(Access(entity).Has<PhysicsBodyReference>());
        Assert.Equal(PhysicsBodyKind.Dynamic, Access(entity).Get<PhysicsBodyReference>().Kind);
    }

    [Fact]
    public void PhysicsFeature_DoesNotCreateDynamicBodyWhenEntityIsDisabled()
    {
        using var context = new EcsTestContext();
        Entity entity = context.State.CreateObject();
        Access(entity).Add(new IsEnable(false));
        Access(entity).Add(RigidBody.Dynamic(Fix.One));
        Access(entity).Add(Collider.Sphere(Fix.One));

        context.BuildSystems(new PhysicsFeatureSystemsGroup());

        Assert.False(Access(entity).Has<PhysicsBodyReference>());
    }

    [Fact]
    public void PhysicsStep_ChangesDynamicBodyPositionFromGravity()
    {
        using var context = new EcsTestContext();
        Entity entity = context.State.CreateObject();
        var transforms = context.Component<Transform>();
        transforms.Get(entity).Position = new Vector3(Fix.Zero, new Fix(5), Fix.Zero);
        Access(entity).Add(RigidBody.Dynamic(Fix.One));
        Access(entity).Add(Collider.Sphere(Fix.One / new Fix(2)));
        context.SetDeltaTime(Fix.One / new Fix(30));

        var systems = context.BuildSystems(new PhysicsFeatureSystemsGroup());
        Fix beforeY = Access(entity).Get<Transform>().Position.Y;

        systems.Update();
        systems.Update();
        systems.Update();

        Fix afterY = Access(entity).Get<Transform>().Position.Y;
        Assert.True(afterY < beforeY);
    }

    [Fact]
    public void DisabledDynamicBody_DoesNotSyncTransformFromPhysics()
    {
        using var context = new EcsTestContext();
        Entity entity = context.State.CreateObject();
        var transforms = context.Component<Transform>();
        transforms.Get(entity).Position = new Vector3(Fix.Zero, new Fix(5), Fix.Zero);
        Access(entity).Add(RigidBody.Dynamic(Fix.One));
        Access(entity).Add(Collider.Sphere(Fix.One / new Fix(2)));
        context.SetDeltaTime(Fix.One / new Fix(30));
        var systems = context.BuildSystems(new PhysicsFeatureSystemsGroup());
        Access(entity).Add(new IsEnable(false));

        systems.Update();

        AssertEx.Equal(new Vector3(Fix.Zero, new Fix(5), Fix.Zero), Access(entity).Get<Transform>().Position);
    }
}
