using Pixagen.Rendering;
using Pixagen.Ecs.DI;

namespace Pixagen.Game.Features.UIFeature.Systems;

public sealed class PresentUISystem : IUpdateSystem
{
    private readonly CustomInject<FrameBuffer> _frameBuffer = default;
    private readonly CustomInject<IRenderBackend> _renderBackend = default;

    public void Update()
    {
        _renderBackend.Value.Present(_frameBuffer.Value);
    }
}
