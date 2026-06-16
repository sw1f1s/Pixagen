
namespace Pixagen.Game.Features.SharedFeature.Components;

public struct LerpMovement : IComponent
{
    public Vector3 From;
    public Vector3 To;
    public Fix Duration;
    public Fix Elapsed;
    public MovementLoopMode Mode;

    public LerpMovement(Vector3 from, Vector3 to, Fix duration, MovementLoopMode mode = MovementLoopMode.PingPong)
    {
        From = from;
        To = to;
        Duration = duration;
        Elapsed = Fix.Zero;
        Mode = mode;
    }
}

public enum MovementLoopMode
{
    Lerp,
    PingPong
}
