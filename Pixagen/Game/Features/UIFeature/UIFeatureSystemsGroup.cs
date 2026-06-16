using Pixagen.Game.Features.UIFeature.Systems;
using Pixagen.Rendering;

namespace Pixagen.Game.Features.UIFeature;

public sealed class UIFeatureSystemsGroup : IGroupSystem
{
    private readonly UiOverlayBuffer _uiOverlayBuffer = new();

    public string GroupName => nameof(UIFeatureSystemsGroup);
    public bool State => true;
    public object[] Injects => [_uiOverlayBuffer];
    public ISystem[] Systems { get; } =
    [
        new BindUiOverlaySystem(),
        new FpsUISystem(),
        new ProfilerUISystem(),
        new RenderUISystem(),
        new PresentUISystem(),
    ];
}
