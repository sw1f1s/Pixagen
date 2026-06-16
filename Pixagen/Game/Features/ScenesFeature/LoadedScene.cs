using Pixagen.Game.Features.ResourceFeature.Runtime;

namespace Pixagen.Game.Features.ScenesFeature;

public sealed class LoadedScene
{
    private readonly List<Entity> _entities;

    internal LoadedScene(IWorld world, SceneResourceScope resources, IReadOnlyList<Entity> entities)
    {
        World = world;
        Resources = resources;
        Id = resources.SceneId;
        Name = resources.SceneName;
        _entities = new List<Entity>(entities);
    }

    internal IWorld World { get; }
    internal SceneResourceScope Resources { get; }
    public string Id { get; }
    public string Name { get; }
    public IReadOnlyList<Entity> Entities => _entities;
}
