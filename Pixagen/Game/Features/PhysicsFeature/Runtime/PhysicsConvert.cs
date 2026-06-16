using NumericQuaternion = System.Numerics.Quaternion;
using NumericVector3 = System.Numerics.Vector3;

namespace Pixagen.Game.Features.PhysicsFeature.Runtime;

internal static class PhysicsConvert
{
    public static NumericVector3 ToFloat(Vector3 value)
    {
        return new NumericVector3((float)value.X, (float)value.Y, (float)value.Z);
    }

    public static NumericQuaternion ToFloat(Quaternion value)
    {
        Quaternion normalized = value.MagnitudeSquared <= Fix.Epsilon ? Quaternion.Identity : value.Normalized;
        return new NumericQuaternion(
            (float)normalized.X,
            (float)normalized.Y,
            (float)normalized.Z,
            (float)normalized.W);
    }

    public static Vector3 ToFixed(NumericVector3 value)
    {
        return new Vector3(
            Fix.FromDouble(value.X),
            Fix.FromDouble(value.Y),
            Fix.FromDouble(value.Z));
    }

    public static Quaternion ToFixed(NumericQuaternion value)
    {
        if (value.LengthSquared() <= float.Epsilon)
        {
            return Quaternion.Identity;
        }

        NumericQuaternion normalized = NumericQuaternion.Normalize(value);
        return new Quaternion(
            Fix.FromDouble(normalized.X),
            Fix.FromDouble(normalized.Y),
            Fix.FromDouble(normalized.Z),
            Fix.FromDouble(normalized.W));
    }

    public static float ToFloat(Fix value)
    {
        return (float)value;
    }

    public static Fix ToFixed(float value)
    {
        return Fix.FromDouble(value);
    }
}
