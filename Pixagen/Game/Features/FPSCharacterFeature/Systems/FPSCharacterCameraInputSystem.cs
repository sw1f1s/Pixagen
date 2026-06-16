using Pixagen.Game.Features.SharedFeature.Helper;
using Pixagen.Ecs.DI;
using Pixagen.Game.Features.FPSCharacterFeature.Components;

namespace Pixagen.Game.Features.FPSCharacterFeature.Systems;

public sealed class FPSCharacterCameraInputSystem : IUpdateSystem
{
    private static readonly Fix MouseDeltaScale = Fix.One / new Fix(70);

    private readonly CustomInject<Time> _time = default;
    private readonly CustomInject<InputState> _input = default;
    private readonly FilterInject<Include<FPSCharacterCamera, LocalTransform, Parent>> _cameras = default;
    private readonly CustomInject<EntityStateHelper> _entityState = default;
    private readonly ComponentInject<FPSCharacterCamera> _fpsCameras = default;
    private readonly ComponentInject<FPSCharacter> _fpsCharacters = default;
    private readonly ComponentInject<LocalTransform> _localTransforms = default;
    private readonly ComponentInject<Parent> _parents = default;

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

            ref Parent parent = ref _parents.Get(entity);
            if (parent.Entity == Entity.Empty ||
                !_entityState.Value.IsAlive(parent.Entity) ||
                !_fpsCharacters.Has(parent.Entity))
            {
                continue;
            }

            ref FPSCharacter character = ref _fpsCharacters.Get(parent.Entity);
            Fix pitchDelta =
                new Fix(Axis(input, InputKey.Down, InputKey.Up)) * character.CameraRotationSpeed * dt +
                new Fix(input.MouseDeltaY) * character.CameraRotationSpeed * MouseDeltaScale;

            ApplyCameraPitch(entity, pitchDelta);
        }
    }

    private void ApplyCameraPitch(Entity entity, Fix pitchDelta)
    {
        ref FPSCharacterCamera camera = ref _fpsCameras.Get(entity);
        camera.EnsurePitchLimits();

        Fix nextPitch = FPSCharacterCamera.Clamp(
            camera.Pitch + pitchDelta,
            camera.MinPitch,
            camera.MaxPitch);
        camera.Pitch = nextPitch;

        ref LocalTransform localTransform = ref _localTransforms.Get(entity);
        localTransform.Rotation = Quaternion.FromAxisAngle(Vector3.Right, nextPitch);
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
