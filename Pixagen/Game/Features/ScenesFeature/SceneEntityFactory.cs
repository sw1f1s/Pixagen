using Pixagen.Game.Features.ScenesFeature.Components;
using Pixagen.Game.Features.ScenesFeature.Serialization;
using Pixagen.Game.Features.SharedFeature.Helper;
using Pixagen.Ecs.DI;

namespace Pixagen.Game.Features.ScenesFeature;

public sealed class SceneEntityFactory
{
    private readonly WorldInject _world = default;
    private readonly CustomInject<EntityStateHelper> _entityState = default;
    private readonly ComponentInject<Info> _infos = default;
    private readonly ComponentInject<SceneObject> _sceneObjects = default;
    private readonly Dictionary<Type, Action<Entity, IComponent>> _componentWriters = new();

    public Entity Create(string sceneId, SceneObjectDefinition definition)
    {
        Entity entity = _entityState.Value.CreateObject();
        _sceneObjects.Add(entity, new SceneObject(sceneId));

        foreach (IComponent component in definition.Components)
        {
            AddOrReplaceComponent(entity, component);
        }

        _entityState.Value.RefreshDisabledInHierarchy(entity);
        return entity;
    }

    private void AddOrReplaceComponent(Entity entity, IComponent component)
    {
        switch (component)
        {
            case Info info:
                _infos.Replace(entity, info);
                return;

            case Transform transform:
                _entityState.Value.SetTransform(entity, transform);
                _entityState.Value.SetLocalTransform(entity, LocalTransform.FromTransform(transform));
                return;

            case LocalTransform localTransform:
                _entityState.Value.SetLocalTransform(entity, localTransform);
                return;

            case Children:
                return;

            default:
                GetComponentWriter(component.GetType()).Invoke(entity, component);
                return;
        }
    }

    private Action<Entity, IComponent> GetComponentWriter(Type componentType)
    {
        if (_componentWriters.TryGetValue(componentType, out Action<Entity, IComponent>? writer))
        {
            return writer;
        }

        writer = SceneComponentRegistry.CreateWriter(_world.Value, componentType);
        _componentWriters.Add(componentType, writer);
        return writer;
    }

}
