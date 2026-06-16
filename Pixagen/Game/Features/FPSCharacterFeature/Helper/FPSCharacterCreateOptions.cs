using Pixagen.Game.Features.FPSCharacterFeature.Components;

namespace Pixagen.Game.Features.FPSCharacterFeature.Helper;

public sealed class FPSCharacterCreateOptions
{
    public Vector3 Position { get; init; } = Vector3.Zero;
    public Quaternion Rotation { get; init; } = Quaternion.Identity;
    public Fix MoveSpeed { get; init; } = new Fix(5);
    public Fix CameraRotationSpeed { get; init; } = Fix.Two;
    public Fix JumpSpeed { get; init; } = new Fix(5);
    public Fix GroundProbeDistance { get; init; } = Fix.FromDouble(1.05);
    public Fix GroundNormalY { get; init; } = new Fix(6) / new Fix(10);
    public Fix Height { get; init; } = Fix.FromDouble(1.75);
    public Fix Mass { get; init; } = Fix.One;
    public Fix CapsuleRadius { get; init; } = new Fix(3) / new Fix(10);
    public Fix CapsuleLength { get; init; }
    public Fix CameraHeight { get; init; }
    public Fix CameraPitch { get; init; }
    public Fix CameraMinPitch { get; init; } = FPSCharacterCamera.DefaultMinPitch;
    public Fix CameraMaxPitch { get; init; } = FPSCharacterCamera.DefaultMaxPitch;
    public Fix CameraProjectionPlaneDistance { get; init; } = Fix.One;
    public Fix CameraViewportHalfWidth { get; init; } = Fix.One;
    public Fix CameraViewportHalfHeight { get; init; } = new Fix(9) / new Fix(16);
    public Fix CameraMaxDistance { get; init; } = new Fix(32);
}