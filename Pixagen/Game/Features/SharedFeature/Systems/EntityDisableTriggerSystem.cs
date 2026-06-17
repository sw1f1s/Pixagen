using Pixagen.Ecs.DI;

namespace Pixagen.Game.Features.SharedFeature.Systems;

public sealed class EntityDisableTriggerSystem : IPreUpdateSystem
{
    private readonly WorldInject _world = default;
    private readonly FilterInject<Include<DisableNextTick>> _pending = default;
    private readonly ComponentInject<Children> _children = default;
    private readonly ComponentInject<IsEnable> _enableStates = default;
    private readonly ComponentInject<EnableNextTick> _enableNextTicks = default;
    private readonly ComponentInject<DisableNextTick> _disableNextTicks = default;
    private readonly ComponentInject<EnableOneTick> _enableTicks = default;
    private readonly ComponentInject<DisableOneTick> _disableTicks = default;
    private readonly ComponentInject<EnableStateDirtyOneTick> _enableStateDirtyTicks = default;

    public void PreUpdate()
    {
        foreach (Entity entity in _pending.Value)
        {
            if (!_world.IsAlive(entity) || !_disableNextTicks.Has(entity))
            {
                continue;
            }

            Apply(entity);
            MarkEnableStateDirty(entity);
        }
    }

    private void Apply(Entity entity)
    {
        if (!_world.IsAlive(entity))
        {
            return;
        }

        RemovePendingTicks(entity);
        Disable(entity);

        if (!_children.Has(entity))
        {
            return;
        }

        ref Children children = ref _children.Get(entity);
        for (int i = 0; i < children.Entities.Count; i++)
        {
            Apply(children.Entities[i]);
        }
    }

    private void Disable(Entity entity)
    {
        _enableStates.Replace(entity, new IsEnable(false));

        if (_enableTicks.Has(entity))
        {
            _enableTicks.Remove(entity);
        }

        if (!_disableTicks.Has(entity))
        {
            _disableTicks.Add(entity, new DisableOneTick());
        }
    }

    private void RemovePendingTicks(Entity entity)
    {
        if (_enableNextTicks.Has(entity))
        {
            _enableNextTicks.Remove(entity);
        }

        if (_disableNextTicks.Has(entity))
        {
            _disableNextTicks.Remove(entity);
        }
    }

    private void MarkEnableStateDirty(Entity entity)
    {
        if (!_enableStateDirtyTicks.Has(entity))
        {
            _enableStateDirtyTicks.Add(entity, new EnableStateDirtyOneTick());
        }
    }
}
