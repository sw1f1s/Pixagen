using Pixagen.Game.Features.CharacterFeature;
using Pixagen.Game.Features.CharacterFeature.Helper;
using Pixagen.Game.Features.CharacterFeature.Components;
using Pixagen.Game.Features.CharacterFeature.Systems;
using Pixagen.Game.Features.PhysicsFeature;
using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.PhysicsFeature.Runtime;
using Pixagen.Game.Features.PhysicsFeature.Systems;
using Pixagen.Game.Features.SharedFeature.Helper;
using Pixagen.Game.Features.SharedFeature.Systems;
using Pixagen.Tests.TestSupport;
using static Pixagen.Tests.TestSupport.EcsTestAccess;

namespace Pixagen.Tests.Features.CharacterFeature;

public sealed class CharacterInputTests
{
    [Fact]
    public void CharacterInputSystem_WritesMoveDirectionFromWASD()
    {
        using var context = new EcsTestContext();
        Entity character = CreateCharacter(context);
        context.Input.SetKey(InputKey.W, true);
        context.Input.SetKey(InputKey.D, true);

        var systems = context.BuildSystems(new CharacterInputSystem());
        systems.Update();

        Vector3 expected = (Vector3.Forward + Vector3.Right).Normalized;
        AssertEx.Equal(expected, Access(character).Get<FpsCharacter>().MoveDirection);
    }

    [Fact]
    public void CharacterInputSystem_RequestsJumpOnlyOnPress()
    {
        using var context = new EcsTestContext();
        Entity character = CreateCharacter(context);
        context.Input.SetKey(InputKey.Space, true);

        var systems = context.BuildSystems(new CharacterInputSystem());
        systems.Update();

        Assert.True(Access(character).Get<FpsCharacter>().JumpRequested);
    }

    [Fact]
    public void CharacterInputSystem_WritesYawButLeavesPitchForCameraSystem()
    {
        using var context = new EcsTestContext();
        Entity character = CreateCharacter(context);
        context.Input.AddMouseDelta(35, 20);

        var systems = context.BuildSystems(new CharacterInputSystem());
        systems.Update();

        var velocities = context.Component<Velocity>();
        ref Velocity velocity = ref velocities.Get(character);
        Assert.True(velocity.YawDelta > Fix.Zero);
        Assert.Equal(Fix.Zero, velocity.PitchDelta);
    }

    [Fact]
    public void FpsCameraCharacterInputSystem_ClampsMousePitchAndUpdatesLocalRotation()
    {
        using var context = new EcsTestContext();
        Entity character = CreateCharacter(context);
        Entity camera = FindFpsCameraCharacter(character);
        context.Input.AddMouseDelta(0, 10000);

        var systems = context.BuildSystems(new FpsCameraCharacterInputSystem());
        systems.Update();

        var cameraComponents = context.Component<FpsCameraCharacter>();
        ref FpsCameraCharacter cameraState = ref cameraComponents.Get(camera);
        Assert.Equal(cameraState.MaxPitch, cameraState.Pitch);
        Assert.NotEqual(Quaternion.Identity, Access(camera).Get<LocalTransform>().Rotation);
    }

    [Fact]
    public void FpsCameraCharacterInputSystem_UsesParentRotationSpeed()
    {
        using var context = new EcsTestContext();
        Entity character = CreateCharacter(context, new CharacterCreateOptions
        {
            CameraRotationSpeed = new Fix(4)
        });
        Entity camera = FindFpsCameraCharacter(character);
        context.Input.AddMouseDelta(0, 7);

        var systems = context.BuildSystems(new FpsCameraCharacterInputSystem());
        systems.Update();

        Fix expectedPitch = new Fix(7) * new Fix(4) * (Fix.One / new Fix(70));
        Assert.Equal(expectedPitch, Access(camera).Get<FpsCameraCharacter>().Pitch);
    }

    [Fact]
    public void FpsCameraCharacterInputSystem_SkipsDisabledParent()
    {
        using var context = new EcsTestContext();
        Entity character = CreateCharacter(context);
        Entity camera = FindFpsCameraCharacter(character);
        context.Input.AddMouseDelta(0, 100);

        var systems = context.BuildSystems(
            new EntityDisableTriggerSystem(),
            new EntityEnableStateSyncSystem(),
            new FpsCameraCharacterInputSystem());
        context.State.Disable(character);
        systems.Update();

        Assert.Equal(Fix.Zero, Access(camera).Get<FpsCameraCharacter>().Pitch);
        Assert.Equal(Quaternion.Identity, Access(camera).Get<LocalTransform>().Rotation);
    }

    [Fact]
    public void CharacterPhysicsSystem_WritesHorizontalVelocityFromMoveDirection()
    {
        using var context = new EcsTestContext();
        Entity character = CreateCharacter(context);
        var characterComponents = context.Component<FpsCharacter>();
        ref FpsCharacter characterState = ref characterComponents.Get(character);
        characterState.MoveDirection = Vector3.Right;
        PhysicsBodyReference reference = context.PhysicsWorld.AddBody(
            Access(character).Get<Transform>(),
            Access(character).Get<RigidBody>(),
            Access(character).Get<Collider>());
        Access(character).Add(reference);
        context.SetDeltaTime(Fix.One / new Fix(60));

        var systems = context.BuildSystems(new CharacterPhysicsSystem());
        systems.Update();

        Assert.True(context.PhysicsWorld.TryGetBody(Access(character).Get<PhysicsBodyReference>(), out var body));
        Assert.True(body.Velocity.Linear.X > 0);
        Assert.Equal(0, body.Velocity.Linear.Z);
    }

    [Fact]
    public void CharacterPhysicsSystem_ClearsJumpRequestWhenBodyIsMissing()
    {
        using var context = new EcsTestContext();
        Entity character = CreateCharacter(context);
        var characterComponents = context.Component<FpsCharacter>();
        ref FpsCharacter characterState = ref characterComponents.Get(character);
        characterState.JumpRequested = true;
        Access(character).Add(new PhysicsBodyReference());
        context.SetDeltaTime(Fix.One / new Fix(60));

        var systems = context.BuildSystems(new CharacterPhysicsSystem());
        systems.Update();

        Assert.False(Access(character).Get<FpsCharacter>().JumpRequested);
    }

    [Fact]
    public void CharacterHelper_UsesStepAndRelativeCameraHeightDefaults()
    {
        using var context = new EcsTestContext();
        Entity character = CreateCharacter(context);
        Entity camera = FindFpsCameraCharacter(character);

        ref FpsCharacter characterState = ref context.Component<FpsCharacter>().Get(character);
        Fix expectedCameraY = Fix.FromDouble(1.75) *
            (FpsCharacter.DefaultCameraHeightFactor - Fix.One / new Fix(2));

        Assert.Equal(FpsCharacter.DefaultStepHeight, characterState.StepHeight);
        Assert.Equal(FpsCharacter.DefaultCameraHeightFactor, characterState.CameraHeightFactor);
        Assert.Equal(Fix.Zero, Access(character).Get<RigidBody>().Friction);
        Assert.Equal(expectedCameraY, Access(camera).Get<LocalTransform>().Position.Y);
    }

    [Fact]
    public void FpsCameraCharacterHeightSystem_RepositionsCameraFromColliderHeight()
    {
        using var context = new EcsTestContext();
        Entity character = CreateCharacter(context);
        Entity camera = FindFpsCameraCharacter(character);
        context.Component<FpsCharacter>().Get(character).CameraHeightFactor = Fix.One;
        context.Component<LocalTransform>().Get(camera).Position = Vector3.Zero;

        var systems = context.BuildSystems(new FpsCameraCharacterHeightSystem());
        systems.Update();

        Assert.Equal(new Fix(7) / new Fix(8), Access(camera).Get<LocalTransform>().Position.Y);
    }

    [Fact]
    public void CharacterPhysicsSystem_InheritsGroundVelocityFromKinematicPlatform()
    {
        using var context = new EcsTestContext();
        Entity character = CreateCharacter(context);
        AddCharacterBody(context, character);
        PhysicsBodyReference platformReference = AddKinematicBox(
            context,
            new Vector3(Fix.Zero, Fix.FromDouble(-0.925), Fix.Zero),
            new Vector3(new Fix(4), new Fix(1) / new Fix(10), new Fix(4)));
        context.SetDeltaTime(Fix.One / new Fix(60));

        Assert.True(context.PhysicsWorld.TryGetBody(platformReference, out var platformBody));
        platformBody.Velocity.Linear = new global::System.Numerics.Vector3(1f, -0.5f, 0f);

        var systems = context.BuildSystems(new CharacterPhysicsSystem());
        systems.Update();

        Assert.True(context.PhysicsWorld.TryGetBody(Access(character).Get<PhysicsBodyReference>(), out var body));
        Assert.Equal(1f, body.Velocity.Linear.X, 3);
        Assert.Equal(-0.5f, body.Velocity.Linear.Y, 3);
    }

    [Fact]
    public void CharacterPhysicsSystem_AppliesSmoothPositionOffsetForReachableStep()
    {
        using var context = new EcsTestContext();
        Entity character = CreateCharacter(context);
        ref FpsCharacter characterState = ref context.Component<FpsCharacter>().Get(character);
        characterState.MoveDirection = new Vector3(Fix.Zero, Fix.Zero, Fix.One);
        AddCharacterBody(context, character);
        AddStaticBox(
            context,
            new Vector3(Fix.Zero, Fix.FromDouble(-0.925), Fix.Zero),
            new Vector3(new Fix(4), new Fix(1) / new Fix(10), new Fix(4)));
        AddStaticBox(
            context,
            new Vector3(Fix.Zero, Fix.FromDouble(-0.675), Fix.FromDouble(0.45)),
            new Vector3(new Fix(4), new Fix(1) / new Fix(10), new Fix(5) / new Fix(10)));
        context.SetDeltaTime(Fix.One / new Fix(60));
        Assert.True(context.PhysicsWorld.TryGetBody(Access(character).Get<PhysicsBodyReference>(), out var body));
        float beforeY = body.Pose.Position.Y;
        float beforeTransformY = (float)Access(character).Get<Transform>().Position.Y;

        var systems = context.BuildSystems(new CharacterPhysicsSystem());
        systems.Update();

        float stepOffset = body.Pose.Position.Y - beforeY;
        float transformStepOffset = (float)Access(character).Get<Transform>().Position.Y - beforeTransformY;
        Assert.True(stepOffset > 0);
        Assert.True(stepOffset <= 0.03f);
        Assert.Equal(stepOffset, transformStepOffset, 3);
        Assert.Equal(0f, body.Velocity.Linear.Y, 3);
    }

    [Fact]
    public void CharacterPhysicsSystem_DoesNotPreClimbDistantStep()
    {
        using var context = new EcsTestContext();
        Entity character = CreateCharacter(context);
        ref FpsCharacter characterState = ref context.Component<FpsCharacter>().Get(character);
        characterState.MoveDirection = new Vector3(Fix.Zero, Fix.Zero, Fix.One);
        AddCharacterBody(context, character);
        AddStaticBox(
            context,
            new Vector3(Fix.Zero, Fix.FromDouble(-0.925), Fix.Zero),
            new Vector3(new Fix(4), new Fix(1) / new Fix(10), new Fix(4)));
        AddStaticBox(
            context,
            new Vector3(Fix.Zero, Fix.FromDouble(-0.675), Fix.FromDouble(1.15)),
            new Vector3(new Fix(4), new Fix(1) / new Fix(10), Fix.One));
        context.SetDeltaTime(Fix.One / new Fix(60));
        Assert.True(context.PhysicsWorld.TryGetBody(Access(character).Get<PhysicsBodyReference>(), out var body));
        float beforeY = body.Pose.Position.Y;

        var systems = context.BuildSystems(new CharacterPhysicsSystem());
        systems.Update();

        Assert.Equal(beforeY, body.Pose.Position.Y, 3);
        Assert.Equal(0f, body.Velocity.Linear.Y, 3);
    }

    [Fact]
    public void CharacterPhysicsSystem_StepClimbDoesNotFightGroundDriftCorrection()
    {
        using var context = new EcsTestContext();
        Entity character = CreateCharacter(context);
        ref FpsCharacter characterState = ref context.Component<FpsCharacter>().Get(character);
        characterState.MoveDirection = new Vector3(Fix.Zero, Fix.Zero, Fix.One);
        AddCharacterBody(context, character);
        AddStaticBox(
            context,
            new Vector3(Fix.Zero, Fix.FromDouble(-0.925), Fix.Zero),
            new Vector3(new Fix(4), new Fix(1) / new Fix(10), new Fix(4)));
        AddStaticBox(
            context,
            new Vector3(Fix.Zero, Fix.FromDouble(-0.675), Fix.FromDouble(0.45)),
            new Vector3(new Fix(4), new Fix(1) / new Fix(10), new Fix(5) / new Fix(10)));
        context.SetDeltaTime(Fix.One / new Fix(60));
        Assert.True(context.PhysicsWorld.TryGetBody(Access(character).Get<PhysicsBodyReference>(), out var body));
        float previousY = body.Pose.Position.Y;

        var systems = context.BuildSystems(new CharacterPhysicsSystem());
        for (int i = 0; i < 10; i++)
        {
            systems.Update();

            Assert.True(context.PhysicsWorld.TryGetBody(Access(character).Get<PhysicsBodyReference>(), out body));
            Assert.True(body.Pose.Position.Y >= previousY);
            Assert.Equal(0f, body.Velocity.Linear.Y, 3);
            previousY = body.Pose.Position.Y;
        }

        Assert.True(previousY > 0.1f);
    }

    [Fact]
    public void CharacterPhysicsSystem_ClampsSolverRiseWhileClimbingStep()
    {
        using var context = new EcsTestContext();
        Entity character = CreateCharacter(context);
        ref FpsCharacter characterState = ref context.Component<FpsCharacter>().Get(character);
        characterState.MoveDirection = new Vector3(Fix.Zero, Fix.Zero, Fix.One);
        characterState.LastMotorPosition = Vector3.Zero;
        characterState.HasLastMotorPosition = true;
        AddCharacterBody(context, character);
        AddStaticBox(
            context,
            new Vector3(Fix.Zero, Fix.FromDouble(-0.925), Fix.Zero),
            new Vector3(new Fix(4), new Fix(1) / new Fix(10), new Fix(4)));
        AddStaticBox(
            context,
            new Vector3(Fix.Zero, Fix.FromDouble(-0.675), Fix.FromDouble(0.45)),
            new Vector3(new Fix(4), new Fix(1) / new Fix(10), new Fix(5) / new Fix(10)));
        context.SetDeltaTime(Fix.One / new Fix(60));
        Assert.True(context.PhysicsWorld.TryGetBody(Access(character).Get<PhysicsBodyReference>(), out var body));
        body.Pose.Position = new global::System.Numerics.Vector3(0f, 0.12f, 0f);
        body.UpdateBounds();

        var systems = context.BuildSystems(new CharacterPhysicsSystem());
        systems.Update();

        Assert.True(context.PhysicsWorld.TryGetBody(Access(character).Get<PhysicsBodyReference>(), out body));
        Assert.True(body.Pose.Position.Y <= 0.02f);
        Assert.Equal(body.Pose.Position.Y, (float)Access(character).Get<Transform>().Position.Y, 3);
        Assert.Equal(0f, body.Velocity.Linear.Y, 3);
    }

    [Fact]
    public void CharacterPhysicsSystem_KeepsStepRiseBoundedAfterPhysicsStep()
    {
        using var context = new EcsTestContext();
        Entity character = CreateCharacter(context);
        ref FpsCharacter characterState = ref context.Component<FpsCharacter>().Get(character);
        characterState.MoveDirection = new Vector3(Fix.Zero, Fix.Zero, Fix.One);
        AddCharacterBody(context, character);
        AddStaticBox(
            context,
            new Vector3(Fix.Zero, Fix.FromDouble(-0.925), Fix.Zero),
            new Vector3(new Fix(4), new Fix(1) / new Fix(10), new Fix(4)));
        AddStaticBox(
            context,
            new Vector3(Fix.Zero, Fix.FromDouble(-0.675), Fix.FromDouble(0.45)),
            new Vector3(new Fix(4), new Fix(1) / new Fix(10), new Fix(5) / new Fix(10)));
        context.SetDeltaTime(Fix.One / new Fix(60));
        Assert.True(context.PhysicsWorld.TryGetBody(Access(character).Get<PhysicsBodyReference>(), out var body));
        float previousY = body.Pose.Position.Y;

        var systems = context.BuildSystems(
            new PhysicsFeatureSystemsGroup(),
            new CharacterFeatureSystemsGroup());
        for (int i = 0; i < 20; i++)
        {
            systems.Update();

            Assert.True(context.PhysicsWorld.TryGetBody(Access(character).Get<PhysicsBodyReference>(), out body));
            float deltaY = body.Pose.Position.Y - previousY;
            Assert.True(deltaY <= 0.02f, $"Frame {i}: y delta was {deltaY}");
            Assert.True(body.Velocity.Linear.Y <= 0.01f, $"Frame {i}: y velocity was {body.Velocity.Linear.Y}");
            previousY = body.Pose.Position.Y;
        }
    }

    [Fact]
    public void CharacterPhysicsSystem_LimitsGroundedVerticalDriftCorrection()
    {
        using var context = new EcsTestContext();
        Entity character = CreateCharacter(context);
        AddCharacterBody(context, character);
        AddStaticBox(
            context,
            new Vector3(Fix.Zero, Fix.FromDouble(-0.925), Fix.Zero),
            new Vector3(new Fix(4), new Fix(1) / new Fix(10), new Fix(4)));
        context.SetDeltaTime(Fix.One / new Fix(60));
        Assert.True(context.PhysicsWorld.TryGetBody(Access(character).Get<PhysicsBodyReference>(), out var body));
        body.Pose.Position = new global::System.Numerics.Vector3(0f, 0.08f, 0f);
        body.UpdateBounds();

        var systems = context.BuildSystems(new CharacterPhysicsSystem());
        systems.Update();

        Assert.True(context.PhysicsWorld.TryGetBody(Access(character).Get<PhysicsBodyReference>(), out body));
        Assert.True(body.Pose.Position.Y < 0.08f);
        Assert.True(body.Velocity.Linear.Y >= -2.01f);
        Assert.Equal(body.Pose.Position.Y, (float)Access(character).Get<Transform>().Position.Y, 3);
    }

    [Fact]
    public void CharacterPhysicsSystem_ReadsKinematicLerpVelocityAfterSync()
    {
        using var context = new EcsTestContext();
        Entity character = CreateCharacter(context);
        AddCharacterBody(context, character);
        AddKinematicLerpBoxEntity(context);
        context.SetDeltaTime(Fix.One / new Fix(60));

        var systems = context.BuildSystems(
            new PhysicsFeatureSystemsGroup(),
            new CharacterFeatureSystemsGroup());
        systems.Update();

        Assert.True(context.PhysicsWorld.TryGetBody(Access(character).Get<PhysicsBodyReference>(), out var body));
        Assert.Equal(1f, body.Velocity.Linear.X, 2);
        Assert.True(body.Pose.Position.X > 0.015f);
        Assert.Equal(body.Pose.Position.X, (float)Access(character).Get<Transform>().Position.X, 3);

        float firstFrameX = body.Pose.Position.X;
        systems.Update();

        Assert.True(context.PhysicsWorld.TryGetBody(Access(character).Get<PhysicsBodyReference>(), out body));
        float secondFrameDeltaX = body.Pose.Position.X - firstFrameX;
        Assert.True(secondFrameDeltaX > 0.015f);
        Assert.True(secondFrameDeltaX < 0.025f);
    }

    [Fact]
    public void CharacterPhysicsSystem_DoesNotSnapDownWhileJumpingInsideGroundProbe()
    {
        using var context = new EcsTestContext();
        Entity character = CreateCharacter(context);
        context.Component<FpsCharacter>().Get(character).JumpRequested = true;
        AddCharacterBody(context, character);
        AddStaticBox(
            context,
            new Vector3(Fix.Zero, Fix.FromDouble(-0.925), Fix.Zero),
            new Vector3(new Fix(4), new Fix(1) / new Fix(10), new Fix(4)));
        context.SetDeltaTime(Fix.One / new Fix(60));

        var systems = context.BuildSystems(
            new CharacterPhysicsSystem(),
            new PhysicsStepSystem(),
            new PhysicsSyncTransformSystem());
        systems.Update();
        systems.Update();

        Assert.True(context.PhysicsWorld.TryGetBody(Access(character).Get<PhysicsBodyReference>(), out var body));
        Assert.True(body.Velocity.Linear.Y > 0);
        Assert.False(Access(character).Get<FpsCharacter>().IsGrounded);
    }

    private static Entity FindFpsCameraCharacter(Entity character)
    {
        var children = Access(character).Get<Children>().Entities;
        for (int i = 0; i < children.Count; i++)
        {
            Entity child = children[i];
            if (Access(child).Has<FpsCameraCharacter>())
            {
                return child;
            }
        }

        throw new InvalidOperationException("Character camera child was not found.");
    }

    private static Entity CreateCharacter(EcsTestContext context)
    {
        var characters = context.Inject(new CharacterHelper());
        return characters.Create();
    }

    private static Entity CreateCharacter(EcsTestContext context, CharacterCreateOptions options)
    {
        var characters = context.Inject(new CharacterHelper());
        return characters.Create(options);
    }

    private static PhysicsBodyReference AddCharacterBody(EcsTestContext context, Entity character)
    {
        PhysicsBodyReference reference = context.PhysicsWorld.AddBody(
            Access(character).Get<Transform>(),
            Access(character).Get<RigidBody>(),
            Access(character).Get<Collider>());
        Access(character).Add(reference);
        return reference;
    }

    private static void AddStaticBox(EcsTestContext context, Vector3 position, Vector3 size)
    {
        context.PhysicsWorld.AddBody(
            new Transform(position),
            RigidBody.Static(),
            Collider.Box(size));
    }

    private static PhysicsBodyReference AddKinematicBox(EcsTestContext context, Vector3 position, Vector3 size)
    {
        return context.PhysicsWorld.AddBody(
            new Transform(position),
            RigidBody.Kinematic(lockRotation: true),
            Collider.Box(size));
    }

    private static Entity AddKinematicLerpBoxEntity(EcsTestContext context)
    {
        Entity platform = context.State.CreateObject();
        var transforms = context.Component<Transform>();
        var rigidBodies = context.Component<RigidBody>();
        var colliders = context.Component<Collider>();
        var movements = context.Component<LerpMovement>();

        var transform = new Transform(new Vector3(Fix.Zero, Fix.FromDouble(-0.925), Fix.Zero));
        transforms.Get(platform) = transform;
        rigidBodies.Add(platform, RigidBody.Kinematic(lockRotation: true));
        colliders.Add(platform, Collider.Box(new Vector3(new Fix(4), new Fix(1) / new Fix(10), new Fix(4))));
        movements.Add(platform, new LerpMovement(
            transform.Position,
            transform.Position + Vector3.Right,
            Fix.One,
            MovementLoopMode.Lerp));
        Access(platform).Add(context.PhysicsWorld.AddBody(
            transform,
            Access(platform).Get<RigidBody>(),
            Access(platform).Get<Collider>()));
        return platform;
    }
}
