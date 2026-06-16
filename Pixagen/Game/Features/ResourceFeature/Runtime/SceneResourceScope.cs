using Pixagen.Game.Features.ScenesFeature.Serialization;

namespace Pixagen.Game.Features.ResourceFeature.Runtime;

public sealed class SceneResourceScope
{
    internal SceneResourceScope(
        SceneDefinition scene,
        string? scenePath,
        bool isDefaultScene,
        IReadOnlyCollection<string> meshAssets,
        IReadOnlyCollection<string> textureAssets)
    {
        Scene = scene;
        ScenePath = scenePath;
        IsDefaultScene = isDefaultScene;
        MeshAssets = meshAssets.ToArray();
        TextureAssets = textureAssets.ToArray();
    }

    public SceneDefinition Scene { get; }
    public string SceneId => Scene.Id;
    public string SceneName => Scene.Name;
    public IReadOnlyList<string> MeshAssets { get; }
    public IReadOnlyList<string> TextureAssets { get; }

    internal string? ScenePath { get; }
    internal bool IsDefaultScene { get; }
}
