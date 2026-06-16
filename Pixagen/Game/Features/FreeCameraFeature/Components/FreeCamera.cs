
namespace Pixagen.Game.Features.FreeCameraFeature.Components;

public struct FreeCamera : IComponent
{
    public Fix MoveSpeed;
    public Fix RotationSpeed;

    public FreeCamera(Fix moveSpeed, Fix rotationSpeed)
    {
        MoveSpeed = moveSpeed;
        RotationSpeed = rotationSpeed;
    }
}
