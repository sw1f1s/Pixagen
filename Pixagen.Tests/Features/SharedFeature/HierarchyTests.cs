using Pixagen.Game.Features.SharedFeature.Helper;
using Pixagen.Game.Features.SharedFeature.Systems;
using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Tests.TestSupport;
using static Pixagen.Tests.TestSupport.EcsTestAccess;

namespace Pixagen.Tests.Features.SharedFeature;

public sealed class HierarchyTests
{
    [Fact]
    public void AddChild_SetsParentAndAddsChildOnce()
    {
        using var context = new EcsTestContext();
        Entity parent = context.State.CreateObject();
        Entity child = context.State.CreateObject();

        Assert.True(context.State.AddChild(parent, child));
        Assert.True(context.State.AddChild(parent, child));

        Assert.True(Access(child).Has<Parent>());
        Assert.Equal(parent, Access(child).Get<Parent>().Entity);
        Assert.Equal(1, Access(parent).Get<Children>().Entities.Count);
        Assert.Equal(child, Access(parent).Get<Children>().Entities[0]);
    }

    [Fact]
    public void RemoveChild_DetachesBothParentAndChild()
    {
        using var context = new EcsTestContext();
        Entity parent = context.State.CreateObject();
        Entity child = context.State.CreateObject();
        context.State.AddChild(parent, child);

        Assert.True(context.State.RemoveChild(parent, child));

        Assert.Equal(0, Access(parent).Get<Children>().Entities.Count);
        Assert.Equal(Entity.Empty, Access(child).Get<Parent>().Entity);
    }

    [Fact]
    public void RemoveFromParent_DetachesChildFromCurrentParent()
    {
        using var context = new EcsTestContext();
        Entity parent = context.State.CreateObject();
        Entity child = context.State.CreateObject();
        context.State.AddChild(parent, child);

        Assert.True(context.State.RemoveFromParent(child));

        Assert.Equal(0, Access(parent).Get<Children>().Entities.Count);
        Assert.Equal(Entity.Empty, Access(child).Get<Parent>().Entity);
    }

    [Fact]
    public void MoveToParent_RemovesChildFromOldParentAndAddsToNewParent()
    {
        using var context = new EcsTestContext();
        Entity oldParent = context.State.CreateObject();
        Entity newParent = context.State.CreateObject();
        Entity child = context.State.CreateObject();
        context.State.AddChild(oldParent, child);

        Assert.True(context.State.MoveToParent(child, newParent));

        Assert.Equal(0, Access(oldParent).Get<Children>().Entities.Count);
        Assert.True(Access(newParent).Get<Children>().Entities.Contains(child));
        Assert.Equal(newParent, Access(child).Get<Parent>().Entity);
    }

    [Fact]
    public void MoveToParent_RejectsSelfParentAndCycles()
    {
        using var context = new EcsTestContext();
        Entity root = context.State.CreateObject();
        Entity child = context.State.CreateObject();
        Entity grandChild = context.State.CreateObject();
        context.State.AddChild(root, child);
        context.State.AddChild(child, grandChild);

        Assert.False(context.State.MoveToParent(root, root));
        Assert.False(context.State.MoveToParent(root, grandChild));
        Assert.Equal(child, Access(grandChild).Get<Parent>().Entity);
        Assert.Equal(root, Access(child).Get<Parent>().Entity);
    }

    [Fact]
    public void HierarchyTransformSystem_AppliesLocalTransformFromRootToDescendants()
    {
        using var context = new EcsTestContext();
        Entity parent = context.State.CreateObject();
        Entity child = context.State.CreateObject();
        var transforms = context.Component<Transform>();
        var localTransforms = context.Component<LocalTransform>();
        transforms.Get(parent).Position = new Vector3(new Fix(2), new Fix(3), new Fix(4));
        localTransforms.Get(child).Position = new Vector3(Fix.One, new Fix(2), new Fix(3));
        context.State.AddChild(parent, child);

        var systems = context.BuildSystems(new HierarchyTransformSystem());
        systems.Update();

        AssertEx.Equal(new Vector3(new Fix(3), new Fix(5), new Fix(7)), Access(child).Get<Transform>().Position);
    }

    [Fact]
    public void MovementThenHierarchy_MovesParentAndCarriesOnlyChildrenWithIt()
    {
        using var context = new EcsTestContext();
        Entity parent = context.State.CreateObject();
        Entity child = context.State.CreateObject();
        Entity unrelated = context.State.CreateObject();
        Access(parent).Add(new Velocity { PositionDelta = new Vector3(new Fix(5), Fix.Zero, Fix.One) });
        var transforms = context.Component<Transform>();
        var localTransforms = context.Component<LocalTransform>();
        localTransforms.Get(child).Position = new Vector3(Fix.Zero, new Fix(2), Fix.Zero);
        transforms.Get(unrelated).Position = new Vector3(new Fix(10), Fix.Zero, Fix.Zero);
        context.State.AddChild(parent, child);

        var systems = context.BuildSystems(new TransformVelocityIntegrationSystem(), new HierarchyTransformSystem());
        systems.Update();

        AssertEx.Equal(new Vector3(new Fix(5), Fix.Zero, Fix.One), Access(parent).Get<Transform>().Position);
        AssertEx.Equal(new Vector3(new Fix(5), new Fix(2), Fix.One), Access(child).Get<Transform>().Position);
        AssertEx.Equal(new Vector3(new Fix(10), Fix.Zero, Fix.Zero), Access(unrelated).Get<Transform>().Position);
        AssertEx.Equal(new Vector3(Fix.Zero, new Fix(2), Fix.Zero), Access(child).Get<LocalTransform>().Position);
    }

    [Fact]
    public void TransformVelocityIntegrationSystem_RotatesDynamicRigidBodyButDoesNotMoveIt()
    {
        using var context = new EcsTestContext();
        Entity entity = context.State.CreateObject();
        Vector3 delta = new(new Fix(5), Fix.Zero, Fix.Zero);
        Access(entity).Add(new Velocity
        {
            PositionDelta = delta,
            YawDelta = Fix.One / new Fix(4)
        });
        Access(entity).Add(RigidBody.Dynamic(Fix.One));

        var systems = context.BuildSystems(new TransformVelocityIntegrationSystem());
        systems.Update();

        AssertEx.Equal(Vector3.Zero, Access(entity).Get<Transform>().Position);
        Assert.NotEqual(Quaternion.Identity, Access(entity).Get<Transform>().Rotation);
        AssertEx.Equal(delta, Access(entity).Get<Velocity>().PositionDelta);
        Assert.Equal(Fix.Zero, Access(entity).Get<Velocity>().YawDelta);
    }

    [Fact]
    public void EntityEnableStateSyncSystem_RefreshesDirectIsEnableWritesBeforeMovement()
    {
        using var context = new EcsTestContext();
        Entity parent = context.State.CreateObject();
        Entity child = context.State.CreateObject();
        context.State.AddChild(parent, child);
        Access(parent).Add(new IsEnable(false));
        Access(child).Add(new Velocity { PositionDelta = new Vector3(new Fix(5), Fix.Zero, Fix.Zero) });

        var systems = context.BuildSystems(
            new EntityEnableStateSyncSystem(),
            new TransformVelocityIntegrationSystem());
        systems.Update();

        Assert.True(Access(parent).Has<DisabledInHierarchy>());
        Assert.True(Access(child).Has<DisabledInHierarchy>());
        AssertEx.Equal(Vector3.Zero, Access(child).Get<Transform>().Position);
    }

    [Fact]
    public void IsEnabled_InheritsDisabledStateFromParent()
    {
        using var context = new EcsTestContext();
        Entity parent = context.State.CreateObject();
        Entity child = context.State.CreateObject();
        context.State.AddChild(parent, child);

        Access(parent).Add(new IsEnable(false));

        Assert.False(context.State.IsEnabled(parent));
        Assert.False(context.State.IsEnabled(child));
    }

    [Fact]
    public void MoveToParent_RefreshesDisabledInHierarchyCache()
    {
        using var context = new EcsTestContext();
        Entity disabledParent = context.State.CreateObject();
        Entity enabledParent = context.State.CreateObject();
        Entity child = context.State.CreateObject();
        Access(disabledParent).Add(new IsEnable(false));

        Assert.True(context.State.AddChild(disabledParent, child));
        Assert.True(Access(child).Has<DisabledInHierarchy>());

        Assert.True(context.State.MoveToParent(child, enabledParent));
        Assert.False(Access(child).Has<DisabledInHierarchy>());
    }
}
