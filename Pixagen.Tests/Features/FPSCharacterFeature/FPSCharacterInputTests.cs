using Pixagen.Game.Features.FPSCharacterFeature;
using Pixagen.Game.Features.FPSCharacterFeature.Helper;
using Pixagen.Game.Features.FPSCharacterFeature.Components;
using Pixagen.Game.Features.FPSCharacterFeature.Systems;
using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.PhysicsFeature.Runtime;
using Pixagen.Game.Features.SharedFeature.Helper;
using Pixagen.Tests.TestSupport;
using static Pixagen.Tests.TestSupport.EcsTestAccess;

namespace Pixagen.Tests.Features.FPSCharacterFeature;

public sealed class FPSCharacterInputTests
{
    [Fact]
    public void FPSCharacterInputSystem_WritesMoveDirectionFromWASD()
    {
        using var context = new EcsTestContext();
        Entity character = CreateCharacter(context);
        context.Input.SetKey(InputKey.W, true);
        context.Input.SetKey(InputKey.D, true);

        var systems = context.BuildSystems(new FPSCharacterInputSystem());
        systems.Update();

        Vector3 expected = (Vector3.Forward + Vector3.Right).Normalized;
        AssertEx.Equal(expected, Access(character).Get<FPSCharacter>().MoveDirection);
    }

    [Fact]
    public void FPSCharacterInputSystem_RequestsJumpOnlyOnPress()
    {
        using var context = new EcsTestContext();
        Entity character = CreateCharacter(context);
        context.Input.SetKey(InputKey.Space, true);

        var systems = context.BuildSystems(new FPSCharacterInputSystem());
        systems.Update();

        Assert.True(Access(character).Get<FPSCharacter>().JumpRequested);
    }

    [Fact]
    public void FPSCharacterInputSystem_WritesYawButLeavesPitchForCameraSystem()
    {
        using var context = new EcsTestContext();
        Entity character = CreateCharacter(context);
        context.Input.AddMouseDelta(35, 20);

        var systems = context.BuildSystems(new FPSCharacterInputSystem());
        systems.Update();

        var velocities = context.Component<Velocity>();
        ref Velocity velocity = ref velocities.Get(character);
        Assert.True(velocity.YawDelta > Fix.Zero);
        Assert.Equal(Fix.Zero, velocity.PitchDelta);
    }

    [Fact]
    public void FPSCharacterCameraInputSystem_ClampsMousePitchAndUpdatesLocalRotation()
    {
        using var context = new EcsTestContext();
        Entity character = CreateCharacter(context);
        Entity camera = FindCharacterCamera(character);
        context.Input.AddMouseDelta(0, 10000);

        var systems = context.BuildSystems(new FPSCharacterCameraInputSystem());
        systems.Update();

        var fpsCameras = context.Component<FPSCharacterCamera>();
        ref FPSCharacterCamera fpsCamera = ref fpsCameras.Get(camera);
        Assert.Equal(fpsCamera.MaxPitch, fpsCamera.Pitch);
        Assert.NotEqual(Quaternion.Identity, Access(camera).Get<LocalTransform>().Rotation);
    }

    [Fact]
    public void FPSCharacterCameraInputSystem_UsesParentRotationSpeed()
    {
        using var context = new EcsTestContext();
        Entity character = CreateCharacter(context, new FPSCharacterCreateOptions
        {
            CameraRotationSpeed = new Fix(4)
        });
        Entity camera = FindCharacterCamera(character);
        context.Input.AddMouseDelta(0, 7);

        var systems = context.BuildSystems(new FPSCharacterCameraInputSystem());
        systems.Update();

        Fix expectedPitch = new Fix(7) * new Fix(4) * (Fix.One / new Fix(70));
        Assert.Equal(expectedPitch, Access(camera).Get<FPSCharacterCamera>().Pitch);
    }

    [Fact]
    public void FPSCharacterCameraInputSystem_SkipsDisabledParent()
    {
        using var context = new EcsTestContext();
        Entity character = CreateCharacter(context);
        Entity camera = FindCharacterCamera(character);
        Access(character).Add(new IsEnable(false));
        context.Input.AddMouseDelta(0, 100);

        var systems = context.BuildSystems(new FPSCharacterCameraInputSystem());
        systems.Update();

        Assert.Equal(Fix.Zero, Access(camera).Get<FPSCharacterCamera>().Pitch);
        Assert.Equal(Quaternion.Identity, Access(camera).Get<LocalTransform>().Rotation);
    }

    [Fact]
    public void FPSCharacterPhysicsSystem_WritesHorizontalVelocityFromMoveDirection()
    {
        using var context = new EcsTestContext();
        Entity character = CreateCharacter(context);
        var fpsCharacters = context.Component<FPSCharacter>();
        ref FPSCharacter fpsCharacter = ref fpsCharacters.Get(character);
        fpsCharacter.MoveDirection = Vector3.Right;
        PhysicsBodyReference reference = context.PhysicsWorld.AddBody(
            Access(character).Get<Transform>(),
            Access(character).Get<RigidBody>(),
            Access(character).Get<Collider>());
        Access(character).Add(reference);
        context.SetDeltaTime(Fix.One / new Fix(60));

        var systems = context.BuildSystems(new FPSCharacterPhysicsSystem());
        systems.Update();

        Assert.True(context.PhysicsWorld.TryGetBody(Access(character).Get<PhysicsBodyReference>(), out var body));
        Assert.True(body.Velocity.Linear.X > 0);
        Assert.Equal(0, body.Velocity.Linear.Z);
    }

    [Fact]
    public void FPSCharacterPhysicsSystem_ClearsJumpRequestWhenBodyIsMissing()
    {
        using var context = new EcsTestContext();
        Entity character = CreateCharacter(context);
        var fpsCharacters = context.Component<FPSCharacter>();
        ref FPSCharacter fpsCharacter = ref fpsCharacters.Get(character);
        fpsCharacter.JumpRequested = true;
        Access(character).Add(new PhysicsBodyReference());
        context.SetDeltaTime(Fix.One / new Fix(60));

        var systems = context.BuildSystems(new FPSCharacterPhysicsSystem());
        systems.Update();

        Assert.False(Access(character).Get<FPSCharacter>().JumpRequested);
    }

    private static Entity FindCharacterCamera(Entity character)
    {
        var children = Access(character).Get<Children>().Entities;
        for (int i = 0; i < children.Count; i++)
        {
            Entity child = children[i];
            if (Access(child).Has<FPSCharacterCamera>())
            {
                return child;
            }
        }

        throw new InvalidOperationException("FPS character camera child was not found.");
    }

    private static Entity CreateCharacter(EcsTestContext context)
    {
        var characters = context.Inject(new FPSCharacterHelper());
        return characters.Create();
    }

    private static Entity CreateCharacter(EcsTestContext context, FPSCharacterCreateOptions options)
    {
        var characters = context.Inject(new FPSCharacterHelper());
        return characters.Create(options);
    }
}
