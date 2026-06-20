
using System.Text.Json.Serialization;

namespace Pixagen.Game.Features.CharacterFeature.Components;

public struct FpsCharacter : IComponent
{
    public static readonly Fix DefaultStepHeight = new Fix(35) / new Fix(100);
    public static readonly Fix DefaultCameraHeightFactor = new Fix(95) / new Fix(100);

    public Fix MoveSpeed;
    public Fix CameraRotationSpeed;
    public Fix JumpSpeed;
    public Fix GroundProbeDistance;
    public Fix GroundNormalY;
    public Fix Height;
    public Fix StepHeight;
    public Fix CameraHeightFactor;
    public Vector3 MoveDirection;
    public bool JumpRequested;
    public bool IsGrounded;
    [JsonIgnore]
    public Vector3 LastSupportVelocity;
    [JsonIgnore]
    public Vector3 LastMotorPosition;
    [JsonIgnore]
    public bool HasLastMotorPosition;
    [JsonIgnore]
    public bool JumpInProgress;

    public FpsCharacter(Fix moveSpeed, Fix cameraRotationSpeed)
        : this(
            moveSpeed,
            cameraRotationSpeed,
            new Fix(5),
            new Fix(7) / new Fix(10),
            new Fix(6) / new Fix(10),
            Fix.FromDouble(1.75),
            DefaultStepHeight,
            DefaultCameraHeightFactor)
    {
    }

    public FpsCharacter(
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
            Fix.FromDouble(1.75),
            DefaultStepHeight,
            DefaultCameraHeightFactor)
    {
    }

    public FpsCharacter(
        Fix moveSpeed,
        Fix cameraRotationSpeed,
        Fix jumpSpeed,
        Fix groundProbeDistance,
        Fix groundNormalY,
        Fix height)
        : this(
            moveSpeed,
            cameraRotationSpeed,
            jumpSpeed,
            groundProbeDistance,
            groundNormalY,
            height,
            DefaultStepHeight,
            DefaultCameraHeightFactor)
    {
    }

    public FpsCharacter(
        Fix moveSpeed,
        Fix cameraRotationSpeed,
        Fix jumpSpeed,
        Fix groundProbeDistance,
        Fix groundNormalY,
        Fix height,
        Fix stepHeight,
        Fix cameraHeightFactor)
    {
        MoveSpeed = moveSpeed;
        CameraRotationSpeed = cameraRotationSpeed;
        JumpSpeed = jumpSpeed;
        GroundProbeDistance = groundProbeDistance;
        GroundNormalY = groundNormalY;
        Height = height;
        StepHeight = stepHeight;
        CameraHeightFactor = cameraHeightFactor;
        MoveDirection = Vector3.Zero;
        JumpRequested = false;
        IsGrounded = false;
        LastSupportVelocity = Vector3.Zero;
        LastMotorPosition = Vector3.Zero;
        HasLastMotorPosition = false;
        JumpInProgress = false;
    }
}
