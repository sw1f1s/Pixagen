using Pixagen.Ecs.Runtime;
using Pixagen.Ecs.DI;
using Pixagen.Rendering;

namespace Pixagen.Game.Features.UIFeature.Systems;

public sealed class BindUiOverlaySystem : IInitSystem
{
    private readonly CustomInject<IRenderBackend> _renderBackend = default;
    private readonly CustomInject<UiOverlayBuffer> _uiOverlay = default;

    public void Init()
    {
        if (_renderBackend.Value is IUiOverlayRenderBackend uiOverlayBackend)
        {
            uiOverlayBackend.SetUiOverlayBuffer(_uiOverlay.Value);
        }
    }
}
