using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.UIFeature.Components;
using Pixagen.Rendering;
using FPSCharacterCameraComponent = Pixagen.Game.Features.FPSCharacterFeature.Components.FPSCharacterCamera;
using FPSCharacterComponent = Pixagen.Game.Features.FPSCharacterFeature.Components.FPSCharacter;

namespace Pixagen.Game.Features.ScenesFeature.Serialization;

public static class DefaultSceneFactory
{
    private const string ControlsText = "WASD Space Arrows Mouse | Esc/Ctrl+C exits";

    public static SceneDefinition Create()
    {
        return new SceneDefinition
        {
            Id = "default",
            Name = "Default Scene",
            Objects =
            [
                CreateFPSCharacter(),
                CreateLight(),
                CreateGround(),
                CreateStaticSphere("red-sphere", "Red Sphere", V(-2, 0, 4), V(1, 1, 1), PixelColor.FromRgb(232, 92, 76)),
                CreateYellowMesh(),
                CreateStaticSphere("cyan-sphere", "Cyan Sphere", V(2, 0.5, 7), V(1.5, 1.5, 1.5), PixelColor.FromRgb(121, 214, 218)),
                CreateMovingBlueMesh(),
                CreateMovingGreenMesh(),
                CreateMovingMagentaMesh(),
                CreateFpsCounter(),
                CreateProfiler(),
                CreateControlsText(),
            ]
        };
    }

    private static SceneObjectDefinition CreateFPSCharacter()
    {
        return new SceneObjectDefinition
        {
            Components =
            [
                new Info("fps-character", "FPS Character"),
                new Transform(V(0, 0, -6)),
                new Velocity(),
                new FPSCharacterComponent(F(5), Fix.Two, F(5), F(1.05), F(0.6), F(1.75)),
                RigidBody.Dynamic(F(1), lockRotation: true),
                Collider.Capsule(F(0.3), F(1.15))
            ],
            Children =
            [
                CreateFPSCharacterCamera()
            ]
        };
    }

    private static SceneObjectDefinition CreateFPSCharacterCamera()
    {
        return new SceneObjectDefinition
        {
            Components =
            [
                new Info("fps-character-camera", "FPS Character Camera"),
                new Transform(V(0, 0.875, -6)),
                new LocalTransform(V(0, 0.875, 0)),
                new Camera(Fix.One, Fix.One, F(9) / F(16), F(32)),
                new FPSCharacterCameraComponent(Fix.Zero)
            ]
        };
    }

    private static SceneObjectDefinition CreateLight()
    {
        return new SceneObjectDefinition
        {
            Components =
            [
                new Info("main-light", "Main Light"),
                new Transform(
                    V(-4, 6, -4),
                    Quaternion.FromDirection(V(-1, 2, -1).Normalized),
                    Vector3.One),
                new LightDirection(
                    Fix.One,
                    Fix.One / F(5),
                    F(4) / F(5),
                    Fix.One / F(20),
                    F(100))
            ]
        };
    }

    private static SceneObjectDefinition CreateGround()
    {
        return new SceneObjectDefinition
        {
            Components =
            [
                new Info("ground", "Ground"),
                new Transform(V(0, -1, 0), Quaternion.Identity, V(96, 1, 96)),
                new Mesh("plane.obj"),
                new Material(PixelColor.FromRgb(103, 169, 112), new MaterialTexture("checker.ppm", F(16), F(16))),
                RigidBody.Static(),
                Collider.Box(V(96, 0.05, 96)),
                new IsStaticRender()
            ]
        };
    }

    private static SceneObjectDefinition CreateStaticSphere(
        string id,
        string name,
        Vector3 position,
        Vector3 scale,
        PixelColor color)
    {
        return new SceneObjectDefinition
        {
            Components =
            [
                new Info(id, name),
                new Transform(position, Quaternion.Identity, scale),
                new Mesh("sphere.obj"),
                new Material(color),
                RigidBody.Static(),
                Collider.Sphere(Max(scale.X, Max(scale.Y, scale.Z))),
                new IsStaticRender(),
                new ShadowCaster()
            ]
        };
    }

    private static SceneObjectDefinition CreateYellowMesh()
    {
        return new SceneObjectDefinition
        {
            Components =
            [
                new Info("yellow-cube", "Yellow Cube"),
                new Transform(
                    V(0, 0, 5),
                    Quaternion.FromAxisAngle(Vector3.Up, Fix.PiOver6),
                    V(1.5, 1.5, 1.5)),
                new Mesh("cube.obj"),
                new Material(PixelColor.FromRgb(224, 197, 86)),
                RigidBody.Static(),
                Collider.Box(V(1.5, 1.5, 1.5)),
                new IsStaticRender(),
                new ShadowCaster()
            ]
        };
    }

    private static SceneObjectDefinition CreateFpsCounter()
    {
        return new SceneObjectDefinition
        {
            Components =
            [
                new Info("ui-fps", "FPS Counter"),
                new TransformUI(0, 0, order: 0),
                new TextUI(string.Empty, PixelColor.FromRgb(240, 219, 86), 16),
                new FPSCounterUI()
            ]
        };
    }

    private static SceneObjectDefinition CreateProfiler()
    {
        return new SceneObjectDefinition
        {
            Components =
            [
                new Info("ui-profiler", "Profiler"),
                new TransformUI(0, 16, order: 2),
                new TextUI(string.Empty, PixelColor.FromRgb(132, 236, 188), 12),
                new ProfilerUI(Fix.One / F(4))
            ]
        };
    }

    private static SceneObjectDefinition CreateMovingBlueMesh()
    {
        return new SceneObjectDefinition
        {
            Components =
            [
                new Info("moving-blue-cube", "Moving Blue Cube"),
                new Transform(V(-4, 0, 6), Quaternion.Identity, Vector3.One),
                new Velocity(),
                new Mesh("cube.obj"),
                new Material(PixelColor.FromRgb(91, 141, 255)),
                RigidBody.Kinematic(),
                Collider.Box(Vector3.One),
                new ShadowCaster(),
                new RotationMotion(Vector3.Up, F(1.4)),
                new LerpMovement(V(-4, 0, 5), V(-4, 1.5, 8), F(2.2), MovementLoopMode.PingPong)
            ]
        };
    }

    private static SceneObjectDefinition CreateMovingGreenMesh()
    {
        return new SceneObjectDefinition
        {
            Components =
            [
                new Info("moving-green-cube", "Moving Green Cube"),
                new Transform(V(-6, 0, 8), Quaternion.Identity, Vector3.One),
                new Velocity(),
                new Mesh("cube.obj"),
                new Material(PixelColor.FromRgb(93, 210, 128)),
                RigidBody.Kinematic(),
                Collider.Box(Vector3.One),
                new ShadowCaster(),
                new RotationMotion(V(0, 1, 1), F(1.1)),
                new LerpMovement(V(-6, 0, 7), V(-2.5, 0.75, 8.5), F(2.8), MovementLoopMode.PingPong)
            ]
        };
    }

    private static SceneObjectDefinition CreateMovingMagentaMesh()
    {
        return new SceneObjectDefinition
        {
            Components =
            [
                new Info("moving-magenta-sphere", "Moving Magenta Sphere"),
                new Transform(V(4, 0, 5), Quaternion.Identity, V(0.75, 0.75, 0.75)),
                new Velocity(),
                new Mesh("sphere.obj"),
                new Material(PixelColor.FromRgb(214, 102, 214), new MaterialTransparency(F(0.65))),
                RigidBody.Kinematic(),
                Collider.Sphere(F(0.75)),
                new ShadowCaster(),
                new RotationMotion(V(1, 1, 0), F(2.1)),
                new LerpMovement(V(4, -0.25, 4), V(4, 1.25, 7), F(1.8), MovementLoopMode.PingPong)
            ]
        };
    }

    private static SceneObjectDefinition CreateControlsText()
    {
        return new SceneObjectDefinition
        {
            Components =
            [
                new Info("ui-controls", "Controls Text"),
                new TransformUI(0, 8, order: 1),
                new TextUI(ControlsText, PixelColor.FromRgb(240, 219, 86), 16)
            ]
        };
    }

    private static Vector3 V(double x, double y, double z)
    {
        return new Vector3(F(x), F(y), F(z));
    }

    private static Fix F(double value)
    {
        return Fix.FromDouble(value);
    }

    private static Fix Max(Fix left, Fix right)
    {
        return left >= right ? left : right;
    }
}
