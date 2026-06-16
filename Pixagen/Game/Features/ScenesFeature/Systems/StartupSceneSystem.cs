using Pixagen.Ecs.DI;
using Pixagen.Game.Features.ResourceFeature.Runtime;

namespace Pixagen.Game.Features.ScenesFeature.Systems;

public sealed class StartupSceneSystem : IInitSystem
{
    private readonly CustomInject<SceneManager> _sceneManager = default;
    private readonly CustomInject<ResourceManager> _resources = default;
    private readonly CustomInject<EngineOptions> _options = default;

    public void Init()
    {
        SceneResourceScope resources = _resources.Value
            .LoadStartupSceneWithResourcesAsync(_options.Value.ScenePath)
            .GetAwaiter()
            .GetResult();
        _sceneManager.Value.AddScene(resources);
    }
}
