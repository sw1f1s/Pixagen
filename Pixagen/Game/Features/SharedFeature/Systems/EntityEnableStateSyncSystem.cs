using Pixagen.Ecs.DI;
using Pixagen.Game.Features.SharedFeature.Helper;

namespace Pixagen.Game.Features.SharedFeature.Systems;

public sealed class EntityEnableStateSyncSystem : IPreUpdateSystem
{
    private readonly WorldInject _world = default;
    private readonly CustomInject<EntityStateHelper> _entityState = default;
    private readonly ComponentInject<Parent> _parents = default;
    private readonly ComponentInject<EnableStateDirtyOneTick> _dirtyTicks = default;
    private readonly FilterInject<Include<EnableStateDirtyOneTick>> _dirtyEntities = default;

    public void PreUpdate()
    {
        foreach (Entity entity in _dirtyEntities.Value)
        {
            if (HasDirtyParent(entity))
            {
                continue;
            }

            _entityState.Value.RefreshDisabledInHierarchy(entity);
        }
    }

    private bool HasDirtyParent(Entity entity)
    {
        while (_parents.Has(entity))
        {
            ref Parent parent = ref _parents.Get(entity);
            entity = parent.Entity;
            if (entity == Entity.Empty)
            {
                return false;
            }

            if (!_world.IsAlive(entity))
            {
                return false;
            }

            if (_dirtyTicks.Has(entity))
            {
                return true;
            }
        }

        return false;
    }
}
