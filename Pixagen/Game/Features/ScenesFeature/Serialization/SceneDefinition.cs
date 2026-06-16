using Pixagen.Ecs.Runtime;

namespace Pixagen.Game.Features.ScenesFeature.Serialization;

public sealed class SceneDefinition
{
    public int Version { get; set; } = 1;
    public string Id { get; set; } = "scene";
    public string Name { get; set; } = "Scene";
    public List<SceneObjectDefinition> Objects { get; set; } = new();
}

public sealed class SceneObjectDefinition
{
    public List<IComponent> Components { get; set; } = new();
    public List<SceneObjectDefinition> Children { get; set; } = new();
}
