using Pixagen.Game.Features.PhysicsFeature.Runtime;
using Pixagen.Game.Features.RenderFeature.Raycasting;
using Pixagen.Game.Features.SharedFeature.Components;
using Pixagen.Game.Features.SharedFeature.Helper;
using Pixagen.Game.Features.SharedFeature.Systems;
using Pixagen.Ecs.Runtime;
using Pixagen.Tests.TestSupport;
using static Pixagen.Tests.TestSupport.EcsTestAccess;

namespace Pixagen.Tests.Features.SharedFeature;

public sealed class ObjectLifecycleTests
{
    [Fact]
    public void CreateEntity_AddsInfo()
    {
        using var context = new EcsTestContext();

        Entity entity = context.World.CreateEntity<Transform>();

        AssertEx.Alive(entity);
        Assert.True(Access(entity).Has<Info>());
        Assert.False(string.IsNullOrWhiteSpace(Access(entity).Get<Info>().Id));
    }

    [Fact]
    public void CreateObject_AddsBaseTransformLocalTransformAndChildren()
    {
        using var context = new EcsTestContext();

        Entity entity = context.State.CreateObject();

        AssertEx.Alive(entity);
        Assert.True(Access(entity).Has<Info>());
        Assert.True(Access(entity).Has<Transform>());
        Assert.True(Access(entity).Has<LocalTransform>());
        Assert.True(Access(entity).Has<Children>());
        Assert.True(Access(entity).Has<SpawnOneTick>());
        AssertEx.Equal(Vector3.Zero, Access(entity).Get<Transform>().Position);
        AssertEx.Equal(Vector3.Zero, Access(entity).Get<LocalTransform>().Position);
        Assert.Equal(0, Access(entity).Get<Children>().Entities.Count);
        Assert.NotNull(Access(entity).Get<Children>().Entities);
    }

    [Fact]
    public void CopyObject_CopiesObjectHierarchyWithIndependentChildren()
    {
        using var context = new EcsTestContext();
        Entity root = context.State.CreateObject();
        Entity child = context.State.CreateObject();
        Entity grandChild = context.State.CreateObject();
        Entity laterChild = context.State.CreateObject();
        context.State.AddChild(root, child);
        context.State.AddChild(child, grandChild);
        var infos = context.Component<Info>();
        infos.Get(root).Name = "Root";
        infos.Get(child).Name = "Child";
        infos.Get(grandChild).Name = "Grand Child";
        var transforms = context.Component<Transform>();
        transforms.Get(child).Position = new Vector3(new Fix(2), new Fix(3), new Fix(4));

        Entity rootCopy = context.State.CopyObject(root);
        Entity childCopy = SingleChild(rootCopy);
        Entity grandChildCopy = SingleChild(childCopy);
        context.State.AddChild(root, laterChild);

        AssertEx.Alive(rootCopy);
        AssertEx.Alive(childCopy);
        AssertEx.Alive(grandChildCopy);
        Assert.NotEqual(root, rootCopy);
        Assert.NotEqual(child, childCopy);
        Assert.NotEqual(grandChild, grandChildCopy);
        Assert.False(Access(rootCopy).Has<Parent>());
        Assert.Equal(rootCopy, Access(childCopy).Get<Parent>().Entity);
        Assert.Equal(childCopy, Access(grandChildCopy).Get<Parent>().Entity);
        Assert.False(Access(rootCopy).Get<Children>().Entities.Contains(child));
        Assert.False(Access(rootCopy).Get<Children>().Entities.Contains(laterChild));
        Assert.Equal(1, Access(rootCopy).Get<Children>().Entities.Count);
        Assert.Equal(2, Access(root).Get<Children>().Entities.Count);
        Assert.Equal("Root", Access(rootCopy).Get<Info>().Name);
        Assert.Equal("Child", Access(childCopy).Get<Info>().Name);
        Assert.Equal("Grand Child", Access(grandChildCopy).Get<Info>().Name);
        Assert.NotEqual(Access(root).Get<Info>().Id, Access(rootCopy).Get<Info>().Id);
        Assert.NotEqual(Access(child).Get<Info>().Id, Access(childCopy).Get<Info>().Id);
        AssertEx.Equal(Access(child).Get<Transform>().Position, Access(childCopy).Get<Transform>().Position);
        Assert.True(Access(rootCopy).Has<SpawnOneTick>());
        Assert.True(Access(childCopy).Has<SpawnOneTick>());
        Assert.True(Access(grandChildCopy).Has<SpawnOneTick>());
    }

    [Fact]
    public void CopyObject_AttachesRootCopyToSameParent()
    {
        using var context = new EcsTestContext();
        Entity parent = context.State.CreateObject();
        Entity child = context.State.CreateObject();
        Entity grandChild = context.State.CreateObject();
        context.State.AddChild(parent, child);
        context.State.AddChild(child, grandChild);

        Entity childCopy = context.State.CopyObject(child);

        Assert.True(Access(parent).Get<Children>().Entities.Contains(child));
        Assert.True(Access(parent).Get<Children>().Entities.Contains(childCopy));
        Assert.Equal(parent, Access(childCopy).Get<Parent>().Entity);
        Entity grandChildCopy = SingleChild(childCopy);
        Assert.Equal(childCopy, Access(grandChildCopy).Get<Parent>().Entity);
        Assert.False(Access(childCopy).Get<Children>().Entities.Contains(grandChild));
    }

    [Fact]
    public void AddDestroyOneTick_MarksRootAndAllDescendants()
    {
        using var context = new EcsTestContext();
        Entity root = context.State.CreateObject();
        Entity child = context.State.CreateObject();
        Entity grandChild = context.State.CreateObject();
        Entity unrelated = context.State.CreateObject();

        context.State.AddChild(root, child);
        context.State.AddChild(child, grandChild);

        Assert.True(context.State.AddDestroyOneTick(root));

        Assert.True(Access(root).Has<DestroyOneTick>());
        Assert.True(Access(child).Has<DestroyOneTick>());
        Assert.True(Access(grandChild).Has<DestroyOneTick>());
        Assert.False(Access(unrelated).Has<DestroyOneTick>());
    }

    [Fact]
    public void AddDestroyOneTick_ReturnsFalseForEmptyOrDeadEntity()
    {
        using var context = new EcsTestContext();
        Entity entity = context.State.CreateObject();
        Access(entity).Destroy();

        Assert.False(context.State.AddDestroyOneTick(Entity.Empty));
        Assert.False(context.State.AddDestroyOneTick(entity));
    }

    [Fact]
    public void DestroySystem_DestroysMarkedEntitiesAndKeepsUnmarkedAlive()
    {
        using var context = new EcsTestContext();
        Entity root = context.State.CreateObject();
        Entity child = context.State.CreateObject();
        Entity unrelated = context.State.CreateObject();
        context.State.AddChild(root, child);
        context.State.AddDestroyOneTick(root);

        var systems = context.BuildSystems(new DestroySystem());
        systems.Update();

        AssertEx.Dead(root);
        AssertEx.Dead(child);
        AssertEx.Alive(unrelated);
    }

    [Fact]
    public void DestroySystem_RemovesPhysicsBodyBeforeDestroyingEntity()
    {
        using var context = new EcsTestContext();
        Entity entity = context.State.CreateObject();
        Access(entity).Add(new Pixagen.Game.Features.PhysicsFeature.Components.RigidBody(
            Pixagen.Game.Features.PhysicsFeature.Components.PhysicsBodyKind.Dynamic));
        Access(entity).Add(Pixagen.Game.Features.PhysicsFeature.Components.Collider.Sphere(Fix.One));
        Access(entity).Add(context.PhysicsWorld.AddBody(
            Access(entity).Get<Transform>(),
            Access(entity).Get<Pixagen.Game.Features.PhysicsFeature.Components.RigidBody>(),
            Access(entity).Get<Pixagen.Game.Features.PhysicsFeature.Components.Collider>()));

        context.State.AddDestroyOneTick(entity);

        var systems = context.BuildSystems(new DestroySystem());
        systems.Update();

        AssertEx.Dead(entity);
    }

    private static Entity SingleChild(Entity entity)
    {
        var children = Access(entity).Get<Children>().Entities;
        Assert.Equal(1, children.Count);
        return children[0];
    }
}
