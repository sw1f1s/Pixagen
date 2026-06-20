using BepuPhysics;
using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.PhysicsFeature.Runtime;
using Pixagen.Ecs.DI;
using Pixagen.Game.Features.CharacterFeature.Components;
using Pixagen.Game.Features.SharedFeature.Helper;
using NumericVector3 = System.Numerics.Vector3;

namespace Pixagen.Game.Features.CharacterFeature.Systems;

public sealed class CharacterPhysicsSystem : IFixedUpdateSystem
{
    private const float GroundSnapSkin = 0.03f;
    private const float GroundedUpwardVelocityTolerance = 0.05f;
    private const float MinimumStepDelta = 0.015f;
    private const float StepProbeSkin = 0.05f;
    private const float StepClearance = 0.015f;
    private const float MaxStepClimbSpeed = 1.0f;
    private const float MaxGroundStickCorrectionSpeed = 2.0f;
    private const float GroundedRiseClampEpsilon = 0.002f;
    private const float MaxCharacterRecoveryVelocity = 0.25f;

    private readonly CustomInject<Time> _time = default;
    private readonly CustomInject<PhysicsWorld> _physicsWorld = default;
    private readonly FilterInject<Include<FpsCharacter, Transform, RigidBody, Collider, PhysicsBodyReference>> _characters = default;
    private readonly CustomInject<EntityStateHelper> _entityState = default;
    private readonly ComponentInject<FpsCharacter> _characterComponents = default;
    private readonly ComponentInject<Transform> _transforms = default;
    private readonly ComponentInject<RigidBody> _rigidBodies = default;
    private readonly ComponentInject<Collider> _colliders = default;
    private readonly ComponentInject<PhysicsBodyReference> _references = default;

    public void FixedUpdate()
    {
        PhysicsWorld physicsWorld = _physicsWorld.Value;
        float dt = MathF.Max(0.0001f, PhysicsConvert.ToFloat(_time.Value.FixedDeltaTime));

        foreach (Entity entity in _characters.Value)
        {
            if (!_entityState.Value.IsEnabled(entity))
            {
                continue;
            }

            ref FpsCharacter character = ref _characterComponents.Get(entity);
            ref Transform transform = ref _transforms.Get(entity);
            ref RigidBody rigidBody = ref _rigidBodies.Get(entity);
            ref Collider collider = ref _colliders.Get(entity);
            ref PhysicsBodyReference reference = ref _references.Get(entity);

            if (rigidBody.Kind != PhysicsBodyKind.Dynamic ||
                !physicsWorld.TryGetBody(reference, out BodyReference body))
            {
                character.JumpRequested = false;
                character.LastSupportVelocity = Vector3.Zero;
                character.LastMotorPosition = Vector3.Zero;
                character.HasLastMotorPosition = false;
                character.JumpInProgress = false;
                continue;
            }

            if (rigidBody.Friction != Fix.Zero ||
                PhysicsConvert.ToFloat(rigidBody.MaximumRecoveryVelocity) > MaxCharacterRecoveryVelocity)
            {
                rigidBody.Friction = Fix.Zero;
                rigidBody.MaximumRecoveryVelocity = PhysicsConvert.ToFixed(MaxCharacterRecoveryVelocity);
                physicsWorld.SetBodyMaterial(reference, rigidBody);
            }

            NumericVector3 position = body.Pose.Position;
            float groundProbeDistance = MathF.Max(0.01f, PhysicsConvert.ToFloat(character.GroundProbeDistance));
            float minimumGroundNormalY = Math.Clamp(PhysicsConvert.ToFloat(character.GroundNormalY), 0f, 1f);
            bool grounded = physicsWorld.TryGetGroundHit(
                reference,
                position,
                groundProbeDistance,
                minimumGroundNormalY,
                out PhysicsGroundHit groundHit);

            NumericVector3 desiredMove = PhysicsConvert.ToFloat(character.MoveDirection);
            desiredMove.Y = 0;
            if (desiredMove.LengthSquared() > 1f)
            {
                desiredMove = NumericVector3.Normalize(desiredMove);
            }

            float moveSpeed = MathF.Max(0, PhysicsConvert.ToFloat(character.MoveSpeed));
            NumericVector3 supportVelocity = grounded ? groundHit.Velocity : NumericVector3.Zero;
            NumericVector3 lastSupportVelocity = PhysicsConvert.ToFloat(character.LastSupportVelocity);
            bool hasLastMotorPosition = character.HasLastMotorPosition;
            NumericVector3 lastMotorPosition = hasLastMotorPosition
                ? PhysicsConvert.ToFloat(character.LastMotorPosition)
                : body.Pose.Position;
            NumericVector3 linearVelocity = body.Velocity.Linear;
            bool movingUpFromSupport = character.JumpInProgress &&
                grounded &&
                linearVelocity.Y > GroundedUpwardVelocityTolerance &&
                linearVelocity.Y > supportVelocity.Y + GroundedUpwardVelocityTolerance;
            bool motorGrounded = grounded && !movingUpFromSupport;
            character.IsGrounded = motorGrounded;

            linearVelocity.X = desiredMove.X * moveSpeed + supportVelocity.X;
            linearVelocity.Z = desiredMove.Z * moveSpeed + supportVelocity.Z;
            bool jumped = character.JumpRequested && motorGrounded;
            bool poseCorrected = false;

            if (jumped)
            {
                float inheritedJumpVelocity = MathF.Max(0, supportVelocity.Y);
                linearVelocity.Y = MathF.Max(
                    linearVelocity.Y,
                    PhysicsConvert.ToFloat(character.JumpSpeed) + inheritedJumpVelocity);
                character.JumpInProgress = true;
                body.Awake = true;
            }
            else if (motorGrounded)
            {
                character.JumpInProgress = false;
                float groundedVelocityY = ResolveGroundedVerticalVelocity(
                    in character,
                    in collider,
                    in groundHit,
                    supportVelocity.Y,
                    dt);
                if (TryApplyStepClimb(
                    physicsWorld,
                    reference,
                    in character,
                    in collider,
                    body,
                    desiredMove,
                    moveSpeed,
                    groundProbeDistance,
                    minimumGroundNormalY,
                    dt,
                    in groundHit,
                    out bool stepPoseCorrected))
                {
                    groundedVelocityY = supportVelocity.Y;
                    poseCorrected |= stepPoseCorrected;
                }
                else
                {
                    poseCorrected |= TryClampGroundedVerticalDrift(
                        in character,
                        in collider,
                        body,
                        in groundHit,
                        dt);
                }

                poseCorrected |= TryApplySupportVelocityDelta(body, supportVelocity - lastSupportVelocity, dt);
                poseCorrected |= TryClampGroundedRise(
                    body,
                    hasLastMotorPosition,
                    lastMotorPosition.Y,
                    supportVelocity.Y,
                    dt);
                linearVelocity.Y = groundedVelocityY;
                body.Awake = true;
            }

            body.Velocity.Linear = linearVelocity;
            body.Velocity.Angular = NumericVector3.Zero;
            body.Pose.Orientation = System.Numerics.Quaternion.Identity;
            body.Awake = true;
            character.LastSupportVelocity = motorGrounded && !jumped
                ? PhysicsConvert.ToFixed(supportVelocity)
                : Vector3.Zero;
            if (motorGrounded && !jumped)
            {
                character.LastMotorPosition = PhysicsConvert.ToFixed(body.Pose.Position);
                character.HasLastMotorPosition = true;
            }
            else
            {
                character.LastMotorPosition = Vector3.Zero;
                character.HasLastMotorPosition = false;
            }

            if (poseCorrected)
            {
                SyncBodyPoseToTransform(entity, in rigidBody, body, ref transform);
            }

            character.JumpRequested = false;
        }
    }

    private static float ResolveGroundedVerticalVelocity(
        in FpsCharacter character,
        in Collider collider,
        in PhysicsGroundHit groundHit,
        float supportVelocityY,
        float dt)
    {
        float halfHeight = GetColliderHeight(collider, character) * 0.5f;
        float groundGap = groundHit.Distance - halfHeight;
        if (groundGap <= GroundSnapSkin)
        {
            return supportVelocityY;
        }

        float snapVelocity = -MathF.Min(groundGap / dt, MaxGroundStickCorrectionSpeed);
        return MathF.Min(supportVelocityY, snapVelocity);
    }

    private static bool TryClampGroundedVerticalDrift(
        in FpsCharacter character,
        in Collider collider,
        BodyReference body,
        in PhysicsGroundHit groundHit,
        float dt)
    {
        float halfHeight = GetColliderHeight(collider, character) * 0.5f;
        float maxGroundedCenterY = groundHit.Position.Y + halfHeight + GroundSnapSkin;
        float correction = body.Pose.Position.Y - maxGroundedCenterY;
        if (correction <= 0)
        {
            return false;
        }

        float correctionDelta = MathF.Min(correction, MaxGroundStickCorrectionSpeed * dt);
        if (correctionDelta <= 0)
        {
            return false;
        }

        NumericVector3 position = body.Pose.Position;
        body.Pose.Position = new NumericVector3(position.X, position.Y - correctionDelta, position.Z);
        body.UpdateBounds();
        return true;
    }

    private static bool TryApplySupportVelocityDelta(
        BodyReference body,
        NumericVector3 supportVelocityDelta,
        float dt)
    {
        NumericVector3 correction = supportVelocityDelta * dt;
        if (correction.LengthSquared() <= 0.000001f)
        {
            return false;
        }

        body.Pose.Position += correction;
        body.UpdateBounds();
        return true;
    }

    private static bool TryClampGroundedRise(
        BodyReference body,
        bool hasLastMotorPosition,
        float lastMotorPositionY,
        float supportVelocityY,
        float dt)
    {
        if (!hasLastMotorPosition)
        {
            return false;
        }

        float supportRise = MathF.Max(0, supportVelocityY) * dt;
        float maxRise = MathF.Max(MaxStepClimbSpeed * dt, supportRise) + GroundedRiseClampEpsilon;
        float maxY = lastMotorPositionY + maxRise;
        if (body.Pose.Position.Y <= maxY)
        {
            return false;
        }

        NumericVector3 position = body.Pose.Position;
        body.Pose.Position = new NumericVector3(position.X, maxY, position.Z);
        body.UpdateBounds();
        return true;
    }

    private static bool TryApplyStepClimb(
        PhysicsWorld physicsWorld,
        PhysicsBodyReference reference,
        in FpsCharacter character,
        in Collider collider,
        BodyReference body,
        NumericVector3 desiredMove,
        float moveSpeed,
        float groundProbeDistance,
        float minimumGroundNormalY,
        float dt,
        in PhysicsGroundHit groundHit,
        out bool poseCorrected)
    {
        poseCorrected = false;
        float stepHeight = MathF.Max(0, PhysicsConvert.ToFloat(character.StepHeight));
        if (stepHeight <= 0 || desiredMove.LengthSquared() <= 0.0001f)
        {
            return false;
        }

        NumericVector3 position = body.Pose.Position;
        NumericVector3 direction = NumericVector3.Normalize(desiredMove);
        float halfHeight = GetColliderHeight(collider, character) * 0.5f;
        if (!TryFindStepHit(
            physicsWorld,
            reference,
            position,
            direction,
            GetColliderRadius(collider),
            moveSpeed,
            groundProbeDistance,
            minimumGroundNormalY,
            dt,
            stepHeight,
            in groundHit,
            out PhysicsGroundHit stepHit))
        {
            return false;
        }

        float targetCenterY = stepHit.Position.Y + halfHeight + StepClearance;
        float remainingHeight = targetCenterY - position.Y;
        if (remainingHeight <= GroundSnapSkin)
        {
            return true;
        }

        float climbDelta = MathF.Min(remainingHeight, MaxStepClimbSpeed * dt);
        if (climbDelta <= 0)
        {
            return false;
        }

        body.Pose.Position = new NumericVector3(position.X, position.Y + climbDelta, position.Z);
        body.UpdateBounds();
        poseCorrected = true;
        return true;
    }

    private void SyncBodyPoseToTransform(
        Entity entity,
        in RigidBody rigidBody,
        BodyReference body,
        ref Transform transform)
    {
        transform.Position = PhysicsConvert.ToFixed(body.Pose.Position);
        if (!rigidBody.LockRotation)
        {
            transform.Rotation = PhysicsConvert.ToFixed(body.Pose.Orientation);
        }

        _entityState.Value.SetTransform(entity, transform);
    }

    private static bool TryFindStepHit(
        PhysicsWorld physicsWorld,
        PhysicsBodyReference reference,
        NumericVector3 position,
        NumericVector3 direction,
        float colliderRadius,
        float moveSpeed,
        float groundProbeDistance,
        float minimumGroundNormalY,
        float dt,
        float stepHeight,
        in PhysicsGroundHit groundHit,
        out PhysicsGroundHit stepHit)
    {
        float probeDistance = colliderRadius + moveSpeed * dt + StepProbeSkin;
        float rayDistance = MathF.Max(
            groundProbeDistance + stepHeight + StepProbeSkin,
            groundHit.Distance + stepHeight + StepProbeSkin);

        NumericVector3 probeOrigin = new(
            position.X + direction.X * probeDistance,
            position.Y + stepHeight + StepProbeSkin,
            position.Z + direction.Z * probeDistance);

        if (!physicsWorld.TryGetGroundHit(
            reference,
            probeOrigin,
            rayDistance,
            minimumGroundNormalY,
            out PhysicsGroundHit candidate))
        {
            stepHit = default;
            return false;
        }

        float stepDelta = candidate.Position.Y - groundHit.Position.Y;
        if (stepDelta <= MinimumStepDelta || stepDelta > stepHeight + StepProbeSkin)
        {
            stepHit = default;
            return false;
        }

        stepHit = candidate;
        return true;
    }

    private static float GetColliderHeight(in Collider collider, in FpsCharacter character)
    {
        float height = collider.Shape switch
        {
            ColliderShape.Box => MathF.Abs(PhysicsConvert.ToFloat(collider.Size.Y)),
            ColliderShape.Sphere => MathF.Abs(PhysicsConvert.ToFloat(collider.Radius)) * 2f,
            ColliderShape.Capsule => MathF.Abs(PhysicsConvert.ToFloat(collider.Length)) +
                MathF.Abs(PhysicsConvert.ToFloat(collider.Radius)) * 2f,
            _ => PhysicsConvert.ToFloat(character.Height)
        };

        if (height > 0.001f)
        {
            return height;
        }

        return MathF.Max(0.001f, PhysicsConvert.ToFloat(character.Height));
    }

    private static float GetColliderRadius(in Collider collider)
    {
        return collider.Shape switch
        {
            ColliderShape.Box => MathF.Max(
                MathF.Abs(PhysicsConvert.ToFloat(collider.Size.X)),
                MathF.Abs(PhysicsConvert.ToFloat(collider.Size.Z))) * 0.5f,
            ColliderShape.Sphere or ColliderShape.Capsule => MathF.Abs(PhysicsConvert.ToFloat(collider.Radius)),
            _ => 0.25f
        };
    }
}
