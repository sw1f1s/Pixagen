using Pixagen.Ecs.DI;

namespace Pixagen.Game.Features.SharedFeature.Systems;

public sealed class EntityDisableTriggerSystem : IPreUpdateSystem
{
    private readonly WorldInject _world = default;
    private readonly ComponentInject<Children> _children = default;
    private readonly ComponentInject<IsEnable> _enableStates = default;
    private readonly ComponentInject<EnableNextTick> _enableNextTicks = default;
    private readonly ComponentInject<DisableNextTick> _disableNextTicks = default;
    private readonly ComponentInject<EnableOneTick> _enableTicks = default;
    private readonly ComponentInject<DisableOneTick> _disableTicks = default;
    private readonly ComponentInject<DisabledInHierarchy> _disabledInHierarchy = default;

    public void PreUpdate()
    {
        ComponentStorage<DisableNextTick> storage = _world.Value.GetComponentStorage<DisableNextTick>();
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
        Disable(entity);
        SetDisabledInHierarchy(entity);

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
        if (!_enableStates.Has(entity))
        {
            _enableStates.Add(entity, new IsEnable(false));
        }
        else
        {
            ref IsEnable state = ref _enableStates.Get(entity);
            state.Value = false;
        }

        if (_enableTicks.Has(entity))
        {
            _enableTicks.Remove(entity);
        }

        if (!_disableTicks.Has(entity))
        {
            _disableTicks.Add(entity, new DisableOneTick());
        }
    }

    private void SetDisabledInHierarchy(Entity entity)
    {
        if (!_disabledInHierarchy.Has(entity))
        {
            _disabledInHierarchy.Add(entity, new DisabledInHierarchy());
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
