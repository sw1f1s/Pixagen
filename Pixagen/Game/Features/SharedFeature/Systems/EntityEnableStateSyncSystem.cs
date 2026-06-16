using Pixagen.Ecs.DI;
using Pixagen.Game.Features.SharedFeature.Helper;

namespace Pixagen.Game.Features.SharedFeature.Systems;

public sealed class EntityEnableStateSyncSystem : IPreUpdateSystem
{
    private readonly CustomInject<EntityStateHelper> _entityState = default;
    private readonly ComponentInject<IsEnable> _enableStates = default;
    private readonly ComponentInject<Parent> _parents = default;
    private readonly ComponentInject<DisabledInHierarchy> _disabledInHierarchy = default;
    private readonly FilterInject<Include<IsEnable>> _explicitEnableStates = default;
    private readonly FilterInject<Include<DisabledInHierarchy>> _disabledEntities = default;
    private int _enableStateVersion = -1;
    private int _parentVersion = -1;
    private int _disabledVersion = -1;

    public void PreUpdate()
    {
        if (_enableStateVersion == _enableStates.Version &&
            _parentVersion == _parents.Version &&
            _disabledVersion == _disabledInHierarchy.Version)
        {
            return;
        }

        SyncExplicitEnableStates();
        SyncDisabledMarkers();

        _enableStateVersion = _enableStates.Version;
        _parentVersion = _parents.Version;
        _disabledVersion = _disabledInHierarchy.Version;
    }

    private void SyncExplicitEnableStates()
    {
        foreach (Entity entity in _explicitEnableStates.Value)
        {
            _entityState.Value.RefreshDisabledInHierarchy(entity);
        }
    }

    private void SyncDisabledMarkers()
    {
        foreach (Entity entity in _disabledEntities.Value)
        {
            _entityState.Value.RefreshDisabledInHierarchy(entity);
        }
    }
}
