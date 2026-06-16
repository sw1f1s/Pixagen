
namespace Pixagen.Game.Features.ScenesFeature.Components;

public struct SceneObject : IComponent
{
    public string SceneId;

    public SceneObject(string sceneId)
    {
        SceneId = sceneId;
    }
}
