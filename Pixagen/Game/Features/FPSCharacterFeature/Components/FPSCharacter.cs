
namespace Pixagen.Game.Features.FPSCharacterFeature.Components;

public struct FPSCharacter : IComponent
{
    public Fix MoveSpeed;
    public Fix CameraRotationSpeed;
    public Fix JumpSpeed;
    public Fix GroundProbeDistance;
    public Fix GroundNormalY;
    public Fix Height;
    public Vector3 MoveDirection;
    public bool JumpRequested;
    public bool IsGrounded;

    public FPSCharacter(Fix moveSpeed, Fix cameraRotationSpeed)
        : this(
            moveSpeed,
            cameraRotationSpeed,
            new Fix(5),
            new Fix(7) / new Fix(10),
            new Fix(6) / new Fix(10),
            Fix.FromDouble(1.75))
    {
    }

    public FPSCharacter(
        Fix moveSpeed,
        Fix cameraRotationSpeed,
        Fix jumpSpeed,
        Fix groundProbeDistance,
        Fix groundNormalY)
        : this(
            moveSpeed,
            cameraRotationSpeed,
            jumpSpeed,
            groundProbeDistance,
            groundNormalY,
            Fix.FromDouble(1.75))
    {
    }

    public FPSCharacter(
        Fix moveSpeed,
        Fix cameraRotationSpeed,
        Fix jumpSpeed,
        Fix groundProbeDistance,
        Fix groundNormalY,
        Fix height)
    {
        MoveSpeed = moveSpeed;
        CameraRotationSpeed = cameraRotationSpeed;
        JumpSpeed = jumpSpeed;
        GroundProbeDistance = groundProbeDistance;
        GroundNormalY = groundNormalY;
        Height = height;
        MoveDirection = Vector3.Zero;
        JumpRequested = false;
        IsGrounded = false;
    }
}
