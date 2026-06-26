using Pixagen.Game.Features.RenderFeature;
using Pixagen.Game.Features.ResourceFeature;
using Pixagen.Game.Features.ScenesFeature;
using Pixagen.Game.Features.SharedFeature;
using Pixagen.Game.Features.UIFeature.Systems;

namespace Pixagen.Editor.Scene;

public sealed class EditorSceneSystemContainer
{
    public Systems Create(IWorld world)
    {
        var systems = new Systems(world);
        systems
            .Add(new ResourceFeatureSystemsGroup())
            .Add(new ScenesFeatureSystemsGroup())
            .Add(new EditorSceneCameraSystem())
            .Add(new SharedFeatureSystemsGroup())
            .Add(new RenderFeatureSystemsGroup())
            .Add(new EditorSceneGizmoSystem())
            .Add(new PresentUISystem());

        return systems;
    }
}
