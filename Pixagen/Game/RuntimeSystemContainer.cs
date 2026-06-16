using Pixagen.Game.Features.DebugFeature;
using Pixagen.Game.Features.FPSCharacterFeature;
using Pixagen.Game.Features.FreeCameraFeature;
using Pixagen.Game.Features.PhysicsFeature;
using Pixagen.Game.Features.RenderFeature;
using Pixagen.Game.Features.ResourceFeature;
using Pixagen.Game.Features.ScenesFeature;
using Pixagen.Game.Features.SharedFeature;
using Pixagen.Game.Features.UIFeature;

namespace Pixagen.Game;

public sealed class RuntimeSystemContainer
{
    public Systems Create(IWorld world)
    {
        var systems = new Systems(world);
        systems
            .Add(new DebugFeatureSystemsGroup())
            .Add(new ResourceFeatureSystemsGroup())
            .Add(new ScenesFeatureSystemsGroup())
            .Add(new FreeCameraFeatureSystemsGroup())
            .Add(new FPSCharacterFeatureSystemsGroup())
            .Add(new PhysicsFeatureSystemsGroup())
            .Add(new SharedFeatureSystemsGroup())
            .Add(new RenderFeatureSystemsGroup())
            .Add(new UIFeatureSystemsGroup());

        return systems;
    }
}
