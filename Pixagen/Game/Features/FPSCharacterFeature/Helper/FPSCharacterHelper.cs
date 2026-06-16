using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.SharedFeature.Components;
using Pixagen.Ecs.Runtime;
using Pixagen.Ecs.DI;
using Pixagen.Game.Features.FPSCharacterFeature.Components;
using Pixagen.Game.Features.SharedFeature.Helper;

namespace Pixagen.Game.Features.FPSCharacterFeature.Helper;

public sealed class FPSCharacterHelper
{
    private readonly CustomInject<EntityStateHelper> _entityState = default;
    private readonly ComponentInject<Transform> _transforms = default;
    private readonly ComponentInject<LocalTransform> _localTransforms = default;
    private readonly ComponentInject<Velocity> _velocities = default;
    private readonly ComponentInject<FPSCharacter> _fpsCharacters = default;
    private readonly ComponentInject<RigidBody> _rigidBodies = default;
    private readonly ComponentInject<Collider> _colliders = default;
    private readonly ComponentInject<Camera> _cameras = default;
    private readonly ComponentInject<FPSCharacterCamera> _fpsCameras = default;

    public Entity Create()
    {
        return Create(new FPSCharacterCreateOptions());
    }

    public Entity Create(in Vector3 position)
    {
        return Create(new FPSCharacterCreateOptions { Position = position });
    }

    public Entity Create(FPSCharacterCreateOptions options)
    {
        Entity character = _entityState.Value.CreateObject();
        var characterTransform = new Transform(options.Position, options.Rotation, Vector3.One);
        SetTransform(character, characterTransform);

        _velocities.Add(character, new Velocity());
        _fpsCharacters.Add(character, new FPSCharacter(
            options.MoveSpeed,
            options.CameraRotationSpeed,
            options.JumpSpeed,
            options.GroundProbeDistance,
            options.GroundNormalY,
            options.Height));
        _rigidBodies.Add(character, RigidBody.Dynamic(options.Mass, lockRotation: true));
        _colliders.Add(character, Collider.Capsule(options.CapsuleRadius, ResolveCapsuleLength(options)));

        Entity camera = CreateCamera(characterTransform, options);
        _entityState.Value.AddChild(character, camera);

        return character;
    }

    private Entity CreateCamera(in Transform parentTransform, FPSCharacterCreateOptions options)
    {
        Entity camera = _entityState.Value.CreateObject();
        var fpsCamera = new FPSCharacterCamera(
            options.CameraPitch,
            options.CameraMinPitch,
            options.CameraMaxPitch);
        var localTransform = new LocalTransform(
            new Vector3(Fix.Zero, ResolveCameraHeight(options), Fix.Zero),
            Quaternion.FromAxisAngle(Vector3.Right, fpsCamera.Pitch),
            Vector3.One);
        SetTransform(camera, ToWorldTransform(parentTransform, localTransform));

        ref LocalTransform existingLocalTransform = ref _localTransforms.Get(camera);
        existingLocalTransform = localTransform;

        _cameras.Add(camera, new Camera(
            options.CameraProjectionPlaneDistance,
            options.CameraViewportHalfWidth,
            options.CameraViewportHalfHeight,
            options.CameraMaxDistance));
        _fpsCameras.Add(camera, fpsCamera);

        return camera;
    }

    private void SetTransform(in Entity entity, in Transform transform)
    {
        ref Transform existingTransform = ref _transforms.Get(entity);
        existingTransform = transform;

        ref LocalTransform localTransform = ref _localTransforms.Get(entity);
        localTransform = LocalTransform.FromTransform(transform);
    }

    private static Transform ToWorldTransform(in Transform parentTransform, in LocalTransform localTransform)
    {
        Quaternion parentRotation = parentTransform.Rotation.MagnitudeSquared <= Fix.Epsilon
            ? Quaternion.Identity
            : parentTransform.Rotation.Normalized;
        Quaternion localRotation = localTransform.Rotation.MagnitudeSquared <= Fix.Epsilon
            ? Quaternion.Identity
            : localTransform.Rotation.Normalized;

        return new Transform(
            parentTransform.Position + parentRotation.Rotate(localTransform.Position),
            (parentRotation * localRotation).Normalized,
            new Vector3(
                parentTransform.Scale.X * localTransform.Scale.X,
                parentTransform.Scale.Y * localTransform.Scale.Y,
                parentTransform.Scale.Z * localTransform.Scale.Z));
    }

    private static Fix ResolveCapsuleLength(FPSCharacterCreateOptions options)
    {
        if (options.CapsuleLength > Fix.Zero)
        {
            return options.CapsuleLength;
        }

        Fix capsuleLength = options.Height - options.CapsuleRadius * new Fix(2);
        return capsuleLength > Fix.Epsilon ? capsuleLength : Fix.Epsilon;
    }

    private static Fix ResolveCameraHeight(FPSCharacterCreateOptions options)
    {
        return options.CameraHeight > Fix.Zero ? options.CameraHeight : options.Height / new Fix(2);
    }
}
