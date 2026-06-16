using Pixagen.Game.Features.RenderFeature.Raycasting;
using Pixagen.Game.Features.ResourceFeature.Runtime;
using Pixagen.Game.Features.ScenesFeature.Serialization;
using Pixagen.Game.Features.SharedFeature.Helper;
using Pixagen.Ecs.Runtime;
using Pixagen.Ecs.DI;

namespace Pixagen.Game.Features.ScenesFeature;

public sealed class SceneManager
{
    private readonly WorldInject _world = default;
    private readonly CustomInject<RenderSceneCache> _renderSceneCache = default;
    private readonly CustomInject<ResourceManager> _resources = default;
    private readonly CustomInject<EntityStateHelper> _entityState = default;
    private readonly CustomInject<SceneEntityFactory> _entityFactory = default;
    private readonly List<LoadedScene> _loadedScenes = new();

    public IReadOnlyList<LoadedScene> LoadedScenes => _loadedScenes;
    
    public LoadedScene AddScene(SceneDefinition scene)
    {
        return AddScene(_resources.Value.LoadSceneResources(scene));
    }

    public LoadedScene AddScene(SceneResourceScope resources)
    {
        SceneDefinition scene = resources.Scene;
        if (_loadedScenes.Any(loadedScene => loadedScene.Id == resources.SceneId))
        {
            throw new InvalidOperationException($"Scene '{resources.SceneId}' is already loaded.");
        }

        IWorld world = _world.Value;
        var entities = new List<Entity>(scene.Objects.Count);
        foreach (SceneObjectDefinition sceneObject in scene.Objects)
        {
            CreateObjectHierarchy(scene.Id, sceneObject, Entity.Empty, entities);
        }

        var loaded = new LoadedScene(world, resources, entities);
        _loadedScenes.Add(loaded);
        _renderSceneCache.Value.InvalidateStatic();
        return loaded;
    }

    public LoadedScene SwitchScene(SceneDefinition scene)
    {
        RemoveAllScenes();
        return AddScene(scene);
    }

    public LoadedScene SwitchScene(SceneResourceScope resources)
    {
        RemoveAllScenes();
        return AddScene(resources);
    }

    public async Task<LoadedScene> SwitchSceneAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        RemoveAllScenes();
        SceneResourceScope resources = await _resources.Value
            .LoadSceneWithResourcesAsync(path, cancellationToken)
            .ConfigureAwait(false);
        return AddScene(resources);
    }

    public async Task<LoadedScene> SwitchStartupSceneAsync(
        string? path,
        CancellationToken cancellationToken = default)
    {
        RemoveAllScenes();
        SceneResourceScope resources = await _resources.Value
            .LoadStartupSceneWithResourcesAsync(path, cancellationToken)
            .ConfigureAwait(false);
        return AddScene(resources);
    }

    private Entity CreateObjectHierarchy(
        string sceneId,
        SceneObjectDefinition sceneObject,
        Entity parent,
        List<Entity> entities)
    {
        Entity entity = _entityFactory.Value.Create(sceneId, sceneObject);
        entities.Add(entity);

        if (_entityState.Value.IsAlive(parent))
        {
            _entityState.Value.AddChild(parent, entity);
        }

        foreach (SceneObjectDefinition child in sceneObject.Children)
        {
            CreateObjectHierarchy(sceneId, child, entity, entities);
        }

        return entity;
    }

    public bool RemoveScene(string sceneId)
    {
        LoadedScene? scene = _loadedScenes.FirstOrDefault(loadedScene => loadedScene.Id == sceneId);
        if (scene is null)
        {
            return false;
        }

        RemoveScene(scene);
        return true;
    }

    public void RemoveScene(LoadedScene scene)
    {
        foreach (Entity entity in scene.Entities)
        {
            _entityState.Value.AddDestroyOneTick(entity);
        }

        _loadedScenes.Remove(scene);
        _resources.Value.UnloadSceneResources(scene.Resources);
        _renderSceneCache.Value.InvalidateStatic();
    }

    public void RemoveAllScenes()
    {
        for (int i = _loadedScenes.Count - 1; i >= 0; i--)
        {
            RemoveScene(_loadedScenes[i]);
        }
    }
}
