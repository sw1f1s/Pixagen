using Float3 = System.Numerics.Vector3;

namespace Pixagen.Rendering.Raycasting;

public readonly struct RayBuilder
{
    public readonly Float3 Origin;
    public readonly Float3 StartDirection;
    public readonly Float3 XDelta;
    public readonly Float3 YDelta;

    public RayBuilder(Float3 origin, Float3 startDirection, Float3 xDelta, Float3 yDelta)
    {
        Origin = origin;
        StartDirection = startDirection;
        XDelta = xDelta;
        YDelta = yDelta;
    }
}
