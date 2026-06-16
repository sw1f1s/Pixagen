namespace Pixagen.Game.Features.RenderFeature.Raycasting;

public readonly record struct CameraBasis(Vector3 Forward, Vector3 Right, Vector3 Up)
{
    public static CameraBasis FromRotation(Quaternion rotation)
    {
        Quaternion normalized = rotation.Normalized;
        Vector3 forward = normalized.Rotate(Vector3.Forward).Normalized;
        Vector3 right = normalized.Rotate(Vector3.Right).Normalized;
        Vector3 up = normalized.Rotate(Vector3.Up).Normalized;

        return new CameraBasis(forward, right, up);
    }
}
