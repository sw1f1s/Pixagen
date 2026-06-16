using Pixagen.Game.Features.SharedFeature.Components;
using Pixagen.Ecs.Runtime;
using Pixagen.Ecs.DI;

namespace Pixagen.Game.Features.SharedFeature.Systems;

public sealed class EntityEnableTriggerSystem : IPreUpdateSystem
{
    private readonly WorldInject _world = default;
    private readonly ComponentInject<Children> _children = default;
    private readonly ComponentInject<IsEnable> _enableStates = default;
    private readonly ComponentInject<EnableNextTick> _enableNextTicks = default;
    private readonly ComponentInject<DisableNextTick> _disableNextTicks = default;
    private readonly ComponentInject<EnableOneTick> _enableTicks = default;
    private readonly ComponentInject<DisableOneTick> _disableTicks = default;

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
