using Pixagen.Ecs.DI;

namespace Pixagen.Game.Features.SharedFeature.Systems;

public sealed class EntityEnableTriggerSystem : IPreUpdateSystem
{
    private readonly WorldInject _world = default;
    private readonly ComponentInject<Children> _children = default;
    private readonly ComponentInject<Parent> _parents = default;
    private readonly ComponentInject<IsEnable> _enableStates = default;
    private readonly ComponentInject<EnableNextTick> _enableNextTicks = default;
    private readonly ComponentInject<DisableNextTick> _disableNextTicks = default;
    private readonly ComponentInject<EnableOneTick> _enableTicks = default;
    private readonly ComponentInject<DisableOneTick> _disableTicks = default;
    private readonly ComponentInject<DisabledInHierarchy> _disabledInHierarchy = default;

    public void PreUpdate()
    {
        ComponentStorage<EnableNextTick> storage = _world.Value.GetComponentStorage<EnableNextTick>();
        while (storage.Count > 0)
        {
            int entityId = storage.Entities.DenseValues[storage.Count - 1];
            Entity entity = _world.Value.Entities.Get(entityId).GetEntity();
            if (!_world.IsAlive(entity))
            {
                storage.RemoveComponent(entity);
                continue;
            }

            Apply(entity);
        }
    }

    private void Apply(Entity entity)
    {
        if (!_world.IsAlive(entity))
        {
            return;
        }

        RemovePendingTicks(entity);
        Enable(entity);
        SetDisabledInHierarchy(entity, IsParentDisabled(entity));

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

    private void Enable(Entity entity)
    {
        if (_enableStates.Has(entity))
        {
            _enableStates.Remove(entity);
        }

        if (_disableTicks.Has(entity))
        {
            _disableTicks.Remove(entity);
        }

        if (!_enableTicks.Has(entity))
        {
            _enableTicks.Add(entity, new EnableOneTick());
        }
    }

    private bool IsParentDisabled(Entity entity)
    {
        if (!_parents.Has(entity))
        {
            return false;
        }

        ref Parent parent = ref _parents.Get(entity);
        Entity parentEntity = parent.Entity;
        return parentEntity != Entity.Empty &&
               _world.IsAlive(parentEntity) &&
               (_disabledInHierarchy.Has(parentEntity) ||
                (_enableStates.Has(parentEntity) && !_enableStates.Get(parentEntity).Value));
    }

    private void SetDisabledInHierarchy(Entity entity, bool disabled)
    {
        if (disabled)
        {
            if (!_disabledInHierarchy.Has(entity))
            {
                _disabledInHierarchy.Add(entity, new DisabledInHierarchy());
            }

            return;
        }

        if (_disabledInHierarchy.Has(entity))
        {
            _disabledInHierarchy.Remove(entity);
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
}
