using Pixagen.Ecs.DI;
using Pixagen.Core.Timing;
using Pixagen.Game.Features.FreeCameraFeature.Components;
using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.RenderFeature.Raycasting;
using Pixagen.Game.Features.SharedFeature.Components;
using Pixagen.Game.Features.SharedFeature.Helper;

namespace Pixagen.Editor.Scene;

public sealed class EditorSceneCameraSystem : IUpdateSystem
{
    private const string EditorCameraId = "editor-preview-camera";
    private static readonly Fix MouseDeltaScale = Fix.One / new Fix(70);
    private static readonly Fix WheelDeltaScale = Fix.One / new Fix(2);
    private static readonly Fix FastMoveMultiplier = new Fix(4);

    private readonly CustomInject<Time> _time = default;
    private readonly CustomInject<InputState> _input = default;
    private readonly FilterInject<Include<Info, FreeCamera, Transform, Velocity>, Exclude<IsStaticRender>> _cameras = default;
    private readonly CustomInject<EntityStateHelper> _entityState = default;
    private readonly ComponentInject<Info> _infos = default;
    private readonly ComponentInject<FreeCamera> _freeCameras = default;
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

            ref Info info = ref _infos.Get(entity);
            if (!string.Equals(info.Id, EditorCameraId, StringComparison.Ordinal))
            {
                continue;
            }

            ref FreeCamera camera = ref _freeCameras.Get(entity);
            ref Transform transform = ref _transforms.Get(entity);
            ref Velocity velocity = ref _velocities.Get(entity);

            CameraBasis basis = CameraBasis.FromRotation(transform.Rotation);
            bool sceneNavigationActive = input.IsDown(InputMouseButton.Right);
            Fix moveSpeed = camera.MoveSpeed * (IsFastMove(input) ? FastMoveMultiplier : Fix.One);
            sbyte forward = sceneNavigationActive ? Axis(input, InputKey.W, InputKey.S) : (sbyte)0;
            sbyte strafe = sceneNavigationActive ? Axis(input, InputKey.D, InputKey.A) : (sbyte)0;
            sbyte vertical = sceneNavigationActive
                ? CombineAxis(Axis(input, InputKey.E, InputKey.Q), Axis(input, InputKey.Space, InputKey.C))
                : (sbyte)0;
            Fix wheelDelta = new Fix(input.MouseWheelDelta) * camera.MoveSpeed * WheelDeltaScale;

            velocity.PositionDelta =
                basis.Forward * ((forward * moveSpeed * dt) + wheelDelta) +
                basis.Right * (strafe * moveSpeed * dt) +
                Vector3.Up * (vertical * moveSpeed * dt);
            velocity.YawDelta =
                new Fix(Axis(input, InputKey.Right, InputKey.Left)) * camera.RotationSpeed * dt +
                (sceneNavigationActive ? new Fix(input.MouseDeltaX) * camera.RotationSpeed * MouseDeltaScale : Fix.Zero);
            velocity.PitchDelta =
                new Fix(Axis(input, InputKey.Down, InputKey.Up)) * camera.RotationSpeed * dt +
                (sceneNavigationActive ? new Fix(input.MouseDeltaY) * camera.RotationSpeed * MouseDeltaScale : Fix.Zero);
            velocity.RollDelta = Fix.Zero;
        }
    }

    private static bool IsFastMove(InputState input)
    {
        return input.IsDown(InputKey.LeftShift) || input.IsDown(InputKey.RightShift);
    }

    private static sbyte CombineAxis(sbyte primary, sbyte fallback)
    {
        return primary != 0 ? primary : fallback;
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
