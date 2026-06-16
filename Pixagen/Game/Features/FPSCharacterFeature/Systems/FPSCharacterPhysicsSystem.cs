using BepuPhysics;
using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.PhysicsFeature.Runtime;
using Pixagen.Ecs.DI;
using Pixagen.Game.Features.FPSCharacterFeature.Components;
using Pixagen.Game.Features.SharedFeature.Helper;
using NumericVector3 = System.Numerics.Vector3;

namespace Pixagen.Game.Features.FPSCharacterFeature.Systems;

public sealed class FPSCharacterPhysicsSystem : IFixedUpdateSystem
{
    private readonly CustomInject<PhysicsWorld> _physicsWorld = default;
    private readonly FilterInject<Include<FPSCharacter, Transform, RigidBody, PhysicsBodyReference>> _characters = default;
    private readonly CustomInject<EntityStateHelper> _entityState = default;
    private readonly ComponentInject<FPSCharacter> _fpsCharacters = default;
    private readonly ComponentInject<Transform> _transforms = default;
    private readonly ComponentInject<RigidBody> _rigidBodies = default;
    private readonly ComponentInject<PhysicsBodyReference> _references = default;

    public void FixedUpdate()
    {
        PhysicsWorld physicsWorld = _physicsWorld.Value;

        foreach (Entity entity in _characters.Value)
        {
            if (!_entityState.Value.IsEnabled(entity))
            {
                continue;
            }

            ref FPSCharacter character = ref _fpsCharacters.Get(entity);
            ref Transform transform = ref _transforms.Get(entity);
            ref RigidBody rigidBody = ref _rigidBodies.Get(entity);
            ref PhysicsBodyReference reference = ref _references.Get(entity);

            if (rigidBody.Kind != PhysicsBodyKind.Dynamic ||
                !physicsWorld.TryGetBody(reference, out BodyReference body))
            {
                character.JumpRequested = false;
                continue;
            }

            NumericVector3 position = PhysicsConvert.ToFloat(transform.Position);
            bool grounded = physicsWorld.IsGrounded(
                reference,
                position,
                MathF.Max(0.01f, PhysicsConvert.ToFloat(character.GroundProbeDistance)),
                Math.Clamp(PhysicsConvert.ToFloat(character.GroundNormalY), 0f, 1f));

            character.IsGrounded = grounded;

            NumericVector3 desiredMove = PhysicsConvert.ToFloat(character.MoveDirection);
            if (desiredMove.LengthSquared() > 1f)
            {
                desiredMove = NumericVector3.Normalize(desiredMove);
            }

            float moveSpeed = MathF.Max(0, PhysicsConvert.ToFloat(character.MoveSpeed));
            NumericVector3 linearVelocity = body.Velocity.Linear;
            linearVelocity.X = desiredMove.X * moveSpeed;
            linearVelocity.Z = desiredMove.Z * moveSpeed;

            if (character.JumpRequested && grounded)
            {
                linearVelocity.Y = MathF.Max(linearVelocity.Y, PhysicsConvert.ToFloat(character.JumpSpeed));
                body.Awake = true;
            }

            body.Velocity.Linear = linearVelocity;
            body.Velocity.Angular = NumericVector3.Zero;
            body.Pose.Orientation = System.Numerics.Quaternion.Identity;
            body.Awake = true;

            character.JumpRequested = false;
        }
    }
}
