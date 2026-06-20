using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.RenderFeature.Raycasting;
using Pixagen.Ecs.DI;
using Pixagen.Game.Features.CharacterFeature.Components;
using Pixagen.Game.Features.SharedFeature.Helper;

namespace Pixagen.Game.Features.CharacterFeature.Systems;

public sealed class CharacterInputSystem : IUpdateSystem
{
    private static readonly Fix MouseDeltaScale = Fix.One / new Fix(70);

    private readonly CustomInject<Time> _time = default;
    private readonly CustomInject<InputState> _input = default;
    private readonly FilterInject<Include<FpsCharacter, Transform, Velocity>, Exclude<IsStaticRender>> _characters = default;
    private readonly CustomInject<EntityStateHelper> _entityState = default;
    private readonly ComponentInject<FpsCharacter> _characterComponents = default;
    private readonly ComponentInject<Transform> _transforms = default;
    private readonly ComponentInject<Velocity> _velocities = default;

    public void Update()
    {
        Fix dt = _time.Value.DeltaTime;
        InputState input = _input.Value;
        input.Poll();

        foreach (Entity entity in _characters.Value)
        {
            if (!_entityState.Value.IsEnabled(entity))
            {
                continue;
            }

            ref FpsCharacter character = ref _characterComponents.Get(entity);
            ref Transform transform = ref _transforms.Get(entity);
            ref Velocity velocity = ref _velocities.Get(entity);

            CameraBasis basis = CameraBasis.FromRotation(transform.Rotation);
            sbyte forward = Axis(input, InputKey.W, InputKey.S);
            sbyte strafe = Axis(input, InputKey.D, InputKey.A);
            Vector3 forwardDirection = Flatten(basis.Forward);
            Vector3 rightDirection = Flatten(basis.Right);
            Vector3 moveDirection =
                forwardDirection * new Fix(forward) +
                rightDirection * new Fix(strafe);

            character.MoveDirection = moveDirection.IsZero ? Vector3.Zero : moveDirection.Normalized;
            character.JumpRequested |= input.WasPressed(InputKey.Space);
            velocity.YawDelta =
                new Fix(Axis(input, InputKey.Right, InputKey.Left)) * character.CameraRotationSpeed * dt +
                new Fix(input.MouseDeltaX) * character.CameraRotationSpeed * MouseDeltaScale;
            velocity.PitchDelta = Fix.Zero;
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

    private static Vector3 Flatten(Vector3 value)
    {
        var flattened = new Vector3(value.X, Fix.Zero, value.Z);
        return flattened.IsZero ? Vector3.Zero : flattened.Normalized;
    }
}
