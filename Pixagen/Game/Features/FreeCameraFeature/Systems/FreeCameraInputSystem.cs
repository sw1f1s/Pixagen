using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.RenderFeature.Raycasting;
using Pixagen.Game.Features.SharedFeature.Helper;
using Pixagen.Ecs.DI;
using FreeCameraComponent = Pixagen.Game.Features.FreeCameraFeature.Components.FreeCamera;

namespace Pixagen.Game.Features.FreeCameraFeature.Systems;

public sealed class FreeCameraInputSystem : IUpdateSystem
{
    private static readonly Fix MouseDeltaScale = Fix.One / new Fix(70);

    private readonly CustomInject<Time> _time = default;
    private readonly CustomInject<InputState> _input = default;
    private readonly FilterInject<Include<FreeCameraComponent, Transform, Velocity>, Exclude<IsStaticRender>> _cameras = default;
    private readonly CustomInject<EntityStateHelper> _entityState = default;
    private readonly ComponentInject<FreeCameraComponent> _freeCameras = default;
    private readonly ComponentInject<Transform> _transforms = default;
    private readonly ComponentInject<Velocity> _velocities = default;

    public void Update()
    {
        Fix dt = _time.Value.DeltaTime;
        InputState input = _input.Value;
        input.Poll();

        foreach (Entity entity in _cameras.Value)
        {
            if (!_entityState.Value.IsEnabled(entity))
            {
                continue;
            }

            ref FreeCameraComponent camera = ref _freeCameras.Get(entity);
            ref Transform transform = ref _transforms.Get(entity);
            ref Velocity velocity = ref _velocities.Get(entity);

            CameraBasis basis = CameraBasis.FromRotation(transform.Rotation);
            sbyte forward = Axis(input, InputKey.W, InputKey.S);
            sbyte strafe = Axis(input, InputKey.D, InputKey.A);
            sbyte vertical = Axis(input, InputKey.Space, InputKey.C);

            velocity.PositionDelta =
                basis.Forward * (forward * camera.MoveSpeed * dt) +
                basis.Right * (strafe * camera.MoveSpeed * dt) +
                Vector3.Up * (vertical * camera.MoveSpeed * dt);
            velocity.YawDelta =
                new Fix(Axis(input, InputKey.Right, InputKey.Left)) * camera.RotationSpeed * dt +
                new Fix(input.MouseDeltaX) * camera.RotationSpeed * MouseDeltaScale;
            velocity.PitchDelta =
                new Fix(Axis(input, InputKey.Down, InputKey.Up)) * camera.RotationSpeed * dt +
                new Fix(input.MouseDeltaY) * camera.RotationSpeed * MouseDeltaScale;
            velocity.RollDelta = Fix.Zero;
        }
    }

    private static sbyte Axis(InputState input, InputKey positive, InputKey negative)
    {
        sbyte value = 0;

        if (input.IsDown(positive))
        {
            value += 1;
        }

        if (input.IsDown(negative))
        {
            value -= 1;
        }

        return value;
    }
}
