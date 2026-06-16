using Pixagen.Game.Features.SharedFeature;
using Pixagen.Game.Features.SharedFeature.Systems;
using Pixagen.Tests.TestSupport;
using static Pixagen.Tests.TestSupport.EcsTestAccess;

namespace Pixagen.Tests.Features.SharedFeature;

public sealed class EntityTriggerTests
{
    [Fact]
    public void CreateObject_AddsSpawnOneTickImmediately()
    {
        using var context = new EcsTestContext();

        Entity entity = context.State.CreateObject();

        Assert.True(Access(entity).Has<SpawnOneTick>());
    }

    [Fact]
    public void SystemsUpdate_RemovesOneTickComponentsAtEndOfPass()
    {
        using var context = new EcsTestContext();
        Entity entity = context.State.CreateObject();
        Access(entity).Add(new EnableOneTick());
        Access(entity).Add(new DisableOneTick());
        bool sawSpawn = false;
        bool sawEnable = false;
        bool sawDisable = false;

        var systems = context.BuildSystems(new ProbeSystem(() =>
        {
            sawSpawn = Access(entity).Has<SpawnOneTick>();
            sawEnable = Access(entity).Has<EnableOneTick>();
            sawDisable = Access(entity).Has<DisableOneTick>();
        }));
        systems.Update();

        Assert.True(sawSpawn);
        Assert.True(sawEnable);
        Assert.True(sawDisable);
        Assert.False(Access(entity).Has<SpawnOneTick>());
        Assert.False(Access(entity).Has<EnableOneTick>());
        Assert.False(Access(entity).Has<DisableOneTick>());
    }

    [Fact]
    public void SetEnabled_QueuesDisableNextTickWithoutChangingStateImmediately()
    {
        using var context = new EcsTestContext();
        Entity entity = context.State.CreateObject();

        Assert.True(context.State.Disable(entity));

        Assert.True(context.State.IsEnabled(entity));
        Assert.True(Access(entity).Has<DisableNextTick>());
        Assert.False(Access(entity).Has<DisableOneTick>());
        Assert.False(context.State.Disable(entity));
    }

    [Fact]
    public void SetEnabled_CanCancelPendingDisableBeforePreUpdate()
    {
        using var context = new EcsTestContext();
        Entity entity = context.State.CreateObject();

        Assert.True(context.State.Disable(entity));
        Assert.True(context.State.Enable(entity));

        Assert.True(context.State.IsEnabled(entity));
        Assert.False(Access(entity).Has<DisableNextTick>());
        Assert.False(Access(entity).Has<EnableNextTick>());
    }

    [Fact]
    public void EntityDisableTriggerSystem_ConvertsDisableNextTickToDisableOneTickBeforeUpdate()
    {
        using var context = new EcsTestContext();
        Entity entity = context.State.CreateObject();
        bool sawDisable = false;
        bool wasDisabledDuringUpdate = false;
        var systems = context.BuildSystems(
            new EntityDisableTriggerSystem(),
            new ProbeSystem(() =>
            {
                sawDisable = Access(entity).Has<DisableOneTick>();
                wasDisabledDuringUpdate = !context.State.IsEnabled(entity);
            }));

        context.State.Disable(entity);
        systems.Update();

        Assert.True(sawDisable);
        Assert.True(wasDisabledDuringUpdate);
        Assert.False(Access(entity).Has<DisableNextTick>());
        Assert.False(Access(entity).Has<DisableOneTick>());
        Assert.False(context.State.IsEnabled(entity));
    }

    [Fact]
    public void EntityEnableTriggerSystem_ConvertsEnableNextTickToEnableOneTickBeforeUpdate()
    {
        using var context = new EcsTestContext();
        Entity entity = context.State.CreateObject();
        bool sawEnable = false;
        bool wasEnabledDuringUpdate = false;
        var systems = context.BuildSystems(
            new EntityDisableTriggerSystem(),
            new EntityEnableTriggerSystem(),
            new ProbeSystem(() =>
            {
                sawEnable = Access(entity).Has<EnableOneTick>();
                wasEnabledDuringUpdate = context.State.IsEnabled(entity);
            }));

        context.State.Disable(entity);
        systems.Update();
        Assert.True(context.State.Enable(entity));
        systems.Update();

        Assert.True(sawEnable);
        Assert.True(wasEnabledDuringUpdate);
        Assert.False(Access(entity).Has<EnableNextTick>());
        Assert.False(Access(entity).Has<EnableOneTick>());
        Assert.True(context.State.IsEnabled(entity));
    }

    [Fact]
    public void EntityTriggerSystems_ApplyDisableAndEnableToChildren()
    {
        using var context = new EcsTestContext();
        Entity parent = context.State.CreateObject();
        Entity child = context.State.CreateObject();
        context.State.AddChild(parent, child);
        bool sawParentDisable = false;
        bool sawChildDisable = false;
        bool sawParentEnable = false;
        bool sawChildEnable = false;
        bool enablePass = false;
        var systems = context.BuildSystems(
            new EntityDisableTriggerSystem(),
            new EntityEnableTriggerSystem(),
            new ProbeSystem(() =>
            {
                if (enablePass)
                {
                    sawParentEnable = Access(parent).Has<EnableOneTick>();
                    sawChildEnable = Access(child).Has<EnableOneTick>();
                    return;
                }

                sawParentDisable = Access(parent).Has<DisableOneTick>();
                sawChildDisable = Access(child).Has<DisableOneTick>();
            }));

        context.State.Disable(parent);
        systems.Update();

        Assert.True(sawParentDisable);
        Assert.True(sawChildDisable);
        Assert.True(Access(parent).Has<DisabledInHierarchy>());
        Assert.True(Access(child).Has<DisabledInHierarchy>());
        Assert.False(context.State.IsEnabled(parent));
        Assert.False(context.State.IsEnabled(child));

        enablePass = true;
        Assert.True(context.State.Enable(parent));
        systems.Update();

        Assert.True(sawParentEnable);
        Assert.True(sawChildEnable);
        Assert.False(Access(parent).Has<DisabledInHierarchy>());
        Assert.False(Access(child).Has<DisabledInHierarchy>());
        Assert.True(context.State.IsEnabled(parent));
        Assert.True(context.State.IsEnabled(child));
    }

    [Fact]
    public void SharedFeatureSystemsGroup_CleansOneTickMarkersAtEndOfPass()
    {
        using var context = new EcsTestContext();
        Entity entity = context.State.CreateObject();

        var systems = context.BuildSystems(new SharedFeatureSystemsGroup());
        context.State.Disable(entity);
        systems.Update();

        Assert.False(Access(entity).Has<SpawnOneTick>());
        Assert.False(Access(entity).Has<DisableNextTick>());
        Assert.False(Access(entity).Has<EnableOneTick>());
        Assert.False(Access(entity).Has<DisableOneTick>());
        Assert.False(context.State.IsEnabled(entity));
    }

    [Fact]
    public void SetEnabled_ReturnsFalseForDeadEntityAlreadyEnabledOrNoChange()
    {
        using var context = new EcsTestContext();
        Entity entity = context.State.CreateObject();

        Assert.False(context.State.Enable(entity));
        Assert.True(context.State.Disable(entity));
        Assert.False(context.State.Disable(entity));
        Access(entity).Destroy();
        Assert.False(context.State.Enable(entity));
    }

    private sealed class ProbeSystem : IUpdateSystem
    {
        private readonly Action _update;

        public ProbeSystem(Action update)
        {
            _update = update;
        }

        public void Update()
        {
            _update();
        }
    }
}
