using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Ecs.DI;
using Pixagen.Game.Features.CharacterFeature.Components;
using Pixagen.Game.Features.SharedFeature.Helper;

namespace Pixagen.Game.Features.CharacterFeature.Helper;

public sealed class CharacterHelper
{
    private readonly CustomInject<EntityStateHelper> _entityState = default;
    private readonly ComponentInject<Velocity> _velocities = default;
    private readonly ComponentInject<FpsCharacter> _characterComponents = default;
    private readonly ComponentInject<RigidBody> _rigidBodies = default;
    private readonly ComponentInject<Collider> _colliders = default;
    private readonly ComponentInject<Camera> _cameras = default;
    private readonly ComponentInject<FpsCameraCharacter> _cameraComponents = default;

    public Entity Create()
    {
        return Create(new CharacterCreateOptions());
    }

    public Entity Create(in Vector3 position)
    {
        return Create(new CharacterCreateOptions { Position = position });
    }

    public Entity Create(CharacterCreateOptions options)
    {
        Entity character = _entityState.Value.CreateObject();
        var characterTransform = new Transform(options.Position, options.Rotation, Vector3.One);
        _entityState.Value.SetTransform(character, characterTransform);
        _entityState.Value.SetLocalTransform(character, LocalTransform.FromTransform(characterTransform));

        Fix capsuleLength = ResolveCapsuleLength(options);
        Fix colliderHeight = ResolveCapsuleHeight(options.CapsuleRadius, capsuleLength, options.Height);
        Fix cameraHeightFactor = ResolveCameraHeightFactor(options, colliderHeight);
        _velocities.Add(character, new Velocity());
        _characterComponents.Add(character, new FpsCharacter(
            options.MoveSpeed,
            options.CameraRotationSpeed,
            options.JumpSpeed,
            options.GroundProbeDistance,
            options.GroundNormalY,
            options.Height,
            options.StepHeight,
            cameraHeightFactor));
        _rigidBodies.Add(character, new RigidBody(
            PhysicsBodyKind.Dynamic,
            options.Mass,
            Fix.Zero,
            new Fix(25) / new Fix(100),
            lockRotation: true));
        _colliders.Add(character, Collider.Capsule(options.CapsuleRadius, capsuleLength));

        Entity camera = CreateCamera(characterTransform, options, colliderHeight, cameraHeightFactor);
        _entityState.Value.AddChild(character, camera);

        return character;
    }

    private Entity CreateCamera(
        in Transform parentTransform,
        CharacterCreateOptions options,
        Fix colliderHeight,
        Fix cameraHeightFactor)
    {
        Entity camera = _entityState.Value.CreateObject();
        var cameraState = new FpsCameraCharacter(
            options.CameraPitch,
            options.CameraMinPitch,
            options.CameraMaxPitch);
        var localTransform = new LocalTransform(
            new Vector3(Fix.Zero, ResolveCameraLocalHeight(colliderHeight, cameraHeightFactor), Fix.Zero),
            Quaternion.FromAxisAngle(Vector3.Right, cameraState.Pitch),
            Vector3.One);
        _entityState.Value.SetTransform(camera, ToWorldTransform(parentTransform, localTransform));
        _entityState.Value.SetLocalTransform(camera, localTransform);

        _cameras.Add(camera, new Camera(
            options.CameraProjectionPlaneDistance,
            options.CameraViewportHalfWidth,
            options.CameraViewportHalfHeight,
            options.CameraMaxDistance));
        _cameraComponents.Add(camera, cameraState);

        return camera;
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

    private static Fix ResolveCapsuleLength(CharacterCreateOptions options)
    {
        if (options.CapsuleLength > Fix.Zero)
        {
            return options.CapsuleLength;
        }

        Fix capsuleLength = options.Height - options.CapsuleRadius * new Fix(2);
        return capsuleLength > Fix.Epsilon ? capsuleLength : Fix.Epsilon;
    }

    private static Fix ResolveCapsuleHeight(Fix radius, Fix length, Fix fallbackHeight)
    {
        Fix height = length + radius * new Fix(2);
        if (height > Fix.Epsilon)
        {
            return height;
        }

        return fallbackHeight > Fix.Epsilon ? fallbackHeight : Fix.One;
    }

    private static Fix ResolveCameraHeightFactor(CharacterCreateOptions options, Fix colliderHeight)
    {
        if (options.CameraHeight > Fix.Zero && colliderHeight > Fix.Epsilon)
        {
            return Clamp01((options.CameraHeight + colliderHeight / new Fix(2)) / colliderHeight);
        }

        return Clamp01(options.CameraHeightFactor);
    }

    private static Fix ResolveCameraLocalHeight(Fix colliderHeight, Fix factor)
    {
        return colliderHeight * Clamp01(factor) - colliderHeight / new Fix(2);
    }

    private static Fix Clamp01(Fix value)
    {
        if (value <= Fix.Zero)
        {
            return Fix.Zero;
        }

        return value >= Fix.One ? Fix.One : value;
    }
}
