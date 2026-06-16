using Pixagen.Ecs.DI;

namespace Pixagen.Game.Features.RenderFeature.Raycasting;

public sealed class RenderSceneCache : IDisposeInject
{
    public RenderPrimitiveBatch Static { get; } = new();
    public RenderPrimitiveBatch Dynamic { get; } = new();
    public bool StaticDirty { get; private set; } = true;

    public void InvalidateStatic()
    {
        StaticDirty = true;
    }

    public void MarkStaticClean()
    {
        StaticDirty = false;
    }

    public void Clear()
    {
        Static.Clear();
        Dynamic.Clear();
        StaticDirty = true;
    }

    public void DisposeInject()
    {
        Clear();
    }
}
