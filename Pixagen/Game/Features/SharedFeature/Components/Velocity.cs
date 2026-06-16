
namespace Pixagen.Game.Features.SharedFeature.Components;

public struct Velocity : IComponent
{
    public Vector3 PositionDelta;
    public Vector3 RotationAxis;
    public Fix RotationAngleDelta;
    public Fix YawDelta;
    public Fix PitchDelta;
    public Fix RollDelta;
}
