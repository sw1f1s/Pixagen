using Float3 = System.Numerics.Vector3;
using FloatQuaternion = System.Numerics.Quaternion;

namespace Pixagen.Game.Features.RenderFeature.Raycasting;

public static class RenderMath
{
    public const float Epsilon = 0.0001f;

    public static float ToFloat(Fix value)
    {
        return (float)value;
    }

    public static Float3 ToFloat(Vector3 value)
    {
        return new Float3(ToFloat(value.X), ToFloat(value.Y), ToFloat(value.Z));
    }

    public static FloatQuaternion ToFloat(Quaternion value)
    {
        return FloatQuaternion.Normalize(new FloatQuaternion(
            ToFloat(value.X),
            ToFloat(value.Y),
            ToFloat(value.Z),
            ToFloat(value.W)));
    }

    public static Float3 NormalizeOr(Float3 value, Float3 fallback)
    {
        float lengthSquared = value.LengthSquared();
        return lengthSquared <= Epsilon * Epsilon
            ? fallback
            : value / MathF.Sqrt(lengthSquared);
    }
}
