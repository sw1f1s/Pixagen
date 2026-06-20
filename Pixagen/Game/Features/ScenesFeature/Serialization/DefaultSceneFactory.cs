using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.UIFeature.Components;
using Pixagen.Rendering;
using FpsCameraCharacterComponent = Pixagen.Game.Features.CharacterFeature.Components.FpsCameraCharacter;
using FpsCharacterComponent = Pixagen.Game.Features.CharacterFeature.Components.FpsCharacter;

namespace Pixagen.Game.Features.ScenesFeature.Serialization;

public static class DefaultSceneFactory
{
    private const string ControlsText = "WASD Space Mouse | ramps / tower / moving platforms | Esc/Ctrl+C exits";

    public static SceneDefinition Create()
    {
        var objects = new List<SceneObjectDefinition>
        {
            CreateCharacter(),
            CreateLight(),
            CreateSkybox(),
            CreateGround(),
        };

        objects.AddRange(CreateArenaWalls());
        objects.AddRange(CreateVerticalRoute());
        objects.AddRange(CreateMovingPlatforms());
        objects.AddRange(CreateSpiralTower());
        objects.AddRange(CreatePhysicsProps());
        objects.Add(CreateFpsCounter());
        objects.Add(CreateProfiler());
        objects.Add(CreateControlsText());

        return new SceneDefinition
        {
            Id = "default",
            Name = "Vertical Character Prototype",
            Objects = objects
        };
    }

    private static SceneObjectDefinition CreateCharacter()
    {
        return new SceneObjectDefinition
        {
            Components =
            [
                new Info("character", "Character"),
                new Transform(V(0, 0, -16)),
                new Velocity(),
                new FpsCharacterComponent(F(5), Fix.Two, F(5), F(1.05), F(0.6), F(1.75)),
                RigidBody.Dynamic(F(1), lockRotation: true),
                Collider.Capsule(F(0.3), F(1.15))
            ],
            Children =
            [
                CreateFpsCameraCharacter()
            ]
        };
    }

    private static SceneObjectDefinition CreateFpsCameraCharacter()
    {
        return new SceneObjectDefinition
        {
            Components =
            [
                new Info("character-camera", "Character Camera"),
                new Transform(V(0, 0.875, -16)),
                new LocalTransform(V(0, 0.875, 0)),
                new Camera(Fix.One, Fix.One, F(9) / F(16), F(96)),
                new FpsCameraCharacterComponent(Fix.Zero)
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
                    V(-18, 16, -12),
                    Quaternion.FromDirection(V(-1, 2.2, -0.6).Normalized),
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

    private static SceneObjectDefinition CreateSkybox()
    {
        return new SceneObjectDefinition
        {
            Components =
            [
                new Info("skybox", "Skybox"),
                new SkyboxColor(PixelColor.FromRgb(109, 154, 184)),
                new SkyboxTexture("sky_clouds.ppm")
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
                new Transform(V(0, -1, 12), Quaternion.Identity, V(64, 1, 84)),
                new Mesh("plane.obj"),
                new Material(PixelColor.FromRgb(108, 158, 113), new MaterialTexture("checker.ppm", F(12), F(18))),
                RigidBody.Static(),
                Collider.Box(V(64, 0.05, 84)),
                new IsStaticRender()
            ]
        };
    }

    private static IEnumerable<SceneObjectDefinition> CreateArenaWalls()
    {
        yield return CreateStaticBox("wall-left", "Left Boundary Wall", V(-32, 1.5, 12), V(1, 5, 84), PixelColor.FromRgb(74, 85, 96));
        yield return CreateStaticBox("wall-right", "Right Boundary Wall", V(32, 1.5, 12), V(1, 5, 84), PixelColor.FromRgb(74, 85, 96));
        yield return CreateStaticBox("wall-front", "Spawn Boundary Wall", V(0, 1.5, -30), V(64, 5, 1), PixelColor.FromRgb(74, 85, 96));
        yield return CreateStaticBox("wall-back", "Tower Boundary Wall", V(0, 1.5, 54), V(64, 5, 1), PixelColor.FromRgb(74, 85, 96));

        yield return CreateStaticBox("left-training-rail", "Left Training Rail", V(-2.85, 0.65, -5.5), V(0.32, 1.3, 13.5), PixelColor.FromRgb(91, 103, 114));
        yield return CreateStaticBox("right-training-rail", "Right Training Rail", V(2.85, 0.65, -5.5), V(0.32, 1.3, 13.5), PixelColor.FromRgb(91, 103, 114));
        yield return CreateStaticBox("left-ramp-rail", "Left Ramp Rail", V(0.45, 1.35, 8.9), V(0.32, 1.25, 6.8), PixelColor.FromRgb(91, 103, 114));
        yield return CreateStaticBox("right-ramp-rail", "Right Ramp Rail", V(5.95, 1.35, 8.9), V(0.32, 1.25, 6.8), PixelColor.FromRgb(91, 103, 114));
        yield return CreateStaticBox("tower-guide-wall-left", "Tower Guide Wall Left", V(10.2, 2.05, 22.1), V(0.35, 1.5, 7.4), PixelColor.FromRgb(91, 103, 114));
        yield return CreateStaticBox("tower-guide-wall-right", "Tower Guide Wall Right", V(16.8, 2.05, 22.1), V(0.35, 1.5, 7.4), PixelColor.FromRgb(91, 103, 114));
    }

    private static IEnumerable<SceneObjectDefinition> CreateVerticalRoute()
    {
        yield return CreateStaticBox("spawn-step-01", "Spawn Step 01", V(0, -0.1, -12), V(5, 0.3, 2.4), PixelColor.FromRgb(133, 142, 151));
        yield return CreateStaticBox("spawn-step-02", "Spawn Step 02", V(0, 0.17, -9.2), V(5, 0.3, 2.4), PixelColor.FromRgb(142, 151, 158));
        yield return CreateStaticBox("spawn-step-03", "Spawn Step 03", V(0, 0.44, -6.4), V(5, 0.3, 2.4), PixelColor.FromRgb(151, 159, 164));
        yield return CreateStaticBox("spawn-step-04", "Spawn Step 04", V(0, 0.71, -3.6), V(5, 0.3, 2.4), PixelColor.FromRgb(160, 166, 169));
        yield return CreateStaticBox("spawn-step-05", "Spawn Step 05", V(0, 0.98, -0.8), V(5, 0.3, 2.4), PixelColor.FromRgb(169, 173, 174));
        yield return CreateStaticBox("first-deck", "First Deck", V(0, 1.18, 2.9), V(7, 0.36, 4.2), PixelColor.FromRgb(144, 159, 174));

        yield return CreateRamp("first-ramp", "First Angled Ramp", V(3.2, 1.52, 8.9), V(5.2, 0.28, 6.8), PixelColor.FromRgb(154, 162, 158), -0.06);
        yield return CreateStaticBox("bridge-start", "Bridge Start", V(3.5, 1.64, 14.2), V(4.5, 0.32, 3.2), PixelColor.FromRgb(120, 148, 166));
        yield return CreateStaticBox("bridge-end", "Bridge End", V(14.5, 1.64, 14.2), V(4.5, 0.32, 3.2), PixelColor.FromRgb(120, 148, 166));

        yield return CreateRamp("tower-approach-ramp", "Tower Approach Ramp", V(13.5, 2.02, 18.9), V(6.2, 0.28, 6.0), PixelColor.FromRgb(149, 159, 152), -0.06);
        yield return CreateStaticBox("tower-entry-deck", "Tower Entry Deck", V(13.5, 2.32, 24.0), V(8.2, 0.34, 4.0), PixelColor.FromRgb(132, 158, 177));

        yield return CreateStaticBox("side-physics-pad", "Side Physics Pad", V(-10, 0.1, -7), V(7, 0.35, 6), PixelColor.FromRgb(154, 131, 108));
        yield return CreateRamp("side-return-ramp", "Side Return Ramp", V(-6, 0.55, -1.5), V(5, 0.28, 7), PixelColor.FromRgb(165, 140, 110), -0.14);
    }

    private static IEnumerable<SceneObjectDefinition> CreateMovingPlatforms()
    {
        yield return CreateMovingPlatform(
            "moving-bridge-platform",
            "Moving Bridge Platform",
            V(7.8, 1.64, 14.2),
            V(10.2, 1.64, 14.2),
            V(4, 0.32, 3.2),
            PixelColor.FromRgb(84, 166, 220),
            F(3.2));

        yield return CreateMovingPlatform(
            "tower-lift-platform",
            "Tower Lift Platform",
            V(22.5, 0.25, 28),
            V(22.5, 5.4, 28),
            V(4.2, 0.34, 4.2),
            PixelColor.FromRgb(97, 207, 148),
            F(4.5));

        yield return CreateMovingPlatform(
            "practice-platform",
            "Practice Platform",
            V(-13, 0.65, 1),
            V(-8, 1.25, 4),
            V(4.2, 0.34, 3.4),
            PixelColor.FromRgb(203, 119, 212),
            F(4));
    }

    private static IEnumerable<SceneObjectDefinition> CreateSpiralTower()
    {
        const double centerX = 13.5;
        const double centerZ = 32;
        const double radius = 5.8;
        const double startAngle = -Math.PI / 2;
        const int stepCount = 32;
        const double stepHeight = 0.22;
        const double startY = 2.35;

        yield return CreateStaticBox("tower-core", "Tower Core", V(centerX, 5.4, centerZ), V(2.6, 10.8, 2.6), PixelColor.FromRgb(98, 100, 112));

        for (int i = 0; i < stepCount; i++)
        {
            double angle = startAngle + i * 0.24;
            double x = centerX + Math.Cos(angle) * radius;
            double z = centerZ + Math.Sin(angle) * radius;
            double y = startY + i * stepHeight;
            PixelColor color = i % 2 == 0
                ? PixelColor.FromRgb(156, 166, 180)
                : PixelColor.FromRgb(134, 148, 166);

            yield return CreateStaticBox(
                $"tower-spiral-step-{i:00}",
                $"Tower Spiral Step {i:00}",
                V(x, y, z),
                V(5.2, 0.3, 2.8),
                color,
                Quaternion.FromAxisAngle(Vector3.Up, Angle(angle + Math.PI / 2)));

            if (i > 0 && i % 8 == 0)
            {
                yield return CreateStaticBox(
                    $"tower-rest-landing-{i:00}",
                    $"Tower Rest Landing {i:00}",
                    V(x, y + 0.03, z),
                    V(6.6, 0.3, 3.4),
                    PixelColor.FromRgb(121, 148, 170),
                    Quaternion.FromAxisAngle(Vector3.Up, Angle(angle + Math.PI / 2)));
            }
        }

        double finalAngle = startAngle + (stepCount - 1) * 0.24;
        double finalX = centerX + Math.Cos(finalAngle) * radius;
        double finalZ = centerZ + Math.Sin(finalAngle) * radius;
        double finalStepY = startY + (stepCount - 1) * stepHeight;
        double topY = finalStepY + 0.22;

        yield return CreateStaticBox(
            "tower-top-entry-landing",
            "Tower Top Entry Landing",
            V((finalX + centerX + 1.2) / 2, topY - 0.04, (finalZ + centerZ - 0.8) / 2),
            V(4.2, 0.28, 2.6),
            PixelColor.FromRgb(137, 160, 184));

        yield return CreateStaticBox("tower-top-platform", "Tower Top Platform", V(centerX - 0.2, topY, centerZ + 0.4), V(5.6, 0.32, 5.6), PixelColor.FromRgb(128, 156, 181));
        yield return CreateStaticBox("tower-top-rail-north", "Tower Top North Rail", V(centerX - 0.2, topY + 0.52, centerZ + 3.2), V(5.6, 0.75, 0.35), PixelColor.FromRgb(84, 95, 107));
        yield return CreateStaticBox("tower-top-rail-west", "Tower Top West Rail", V(centerX - 3, topY + 0.52, centerZ + 0.4), V(0.35, 0.75, 5.6), PixelColor.FromRgb(84, 95, 107));
    }

    private static IEnumerable<SceneObjectDefinition> CreatePhysicsProps()
    {
        yield return CreateDynamicCube("physics-blue-cube", "Physics Blue Cube", V(-11, 1, -8), V(1.1, 1.1, 1.1), PixelColor.FromRgb(85, 129, 236), F(1.2));
        yield return CreateDynamicCube("physics-yellow-cube", "Physics Yellow Cube", V(-8.5, 1, -5.5), V(1.3, 1.3, 1.3), PixelColor.FromRgb(219, 190, 72), F(1.5));
        yield return CreateDynamicCube("physics-orange-crate", "Physics Orange Crate", V(-9.8, 1.1, 1.5), V(1, 1, 1), PixelColor.FromRgb(221, 123, 72), F(1));
        yield return CreateDynamicSphere("physics-red-ball", "Physics Red Ball", V(-6.5, 1.1, -7.5), F(0.75), PixelColor.FromRgb(221, 80, 72), F(0.8));
        yield return CreateDynamicSphere("physics-cyan-ball", "Physics Cyan Ball", V(18, 3.1, 25.5), F(0.65), PixelColor.FromRgb(95, 217, 219), F(0.65));
        yield return CreateDynamicSphere("physics-purple-ball", "Physics Purple Ball", V(17.5, 9.1, 32), F(0.75), PixelColor.FromRgb(185, 97, 211), F(0.8));
    }

    private static SceneObjectDefinition CreateStaticBox(
        string id,
        string name,
        Vector3 position,
        Vector3 scale,
        PixelColor color)
    {
        return CreateStaticBox(id, name, position, scale, color, Quaternion.Identity);
    }

    private static SceneObjectDefinition CreateStaticBox(
        string id,
        string name,
        Vector3 position,
        Vector3 scale,
        PixelColor color,
        Quaternion rotation)
    {
        return new SceneObjectDefinition
        {
            Components =
            [
                new Info(id, name),
                new Transform(position, rotation, scale),
                new Mesh("cube.obj"),
                new Material(color),
                RigidBody.Static(),
                Collider.Box(scale),
                new IsStaticRender(),
                new ShadowCaster()
            ]
        };
    }

    private static SceneObjectDefinition CreateRamp(
        string id,
        string name,
        Vector3 position,
        Vector3 scale,
        PixelColor color,
        double pitchRadians)
    {
        return CreateStaticBox(
            id,
            name,
            position,
            scale,
            color,
            Quaternion.FromAxisAngle(Vector3.Right, Angle(pitchRadians)));
    }

    private static SceneObjectDefinition CreateMovingPlatform(
        string id,
        string name,
        Vector3 from,
        Vector3 to,
        Vector3 scale,
        PixelColor color,
        Fix duration)
    {
        return new SceneObjectDefinition
        {
            Components =
            [
                new Info(id, name),
                new Transform(from, Quaternion.Identity, scale),
                new Velocity(),
                new Mesh("cube.obj"),
                new Material(color),
                RigidBody.Kinematic(lockRotation: true),
                Collider.Box(scale),
                new ShadowCaster(),
                new LerpMovement(from, to, duration, MovementLoopMode.PingPong)
            ]
        };
    }

    private static SceneObjectDefinition CreateDynamicCube(
        string id,
        string name,
        Vector3 position,
        Vector3 scale,
        PixelColor color,
        Fix mass)
    {
        return new SceneObjectDefinition
        {
            Components =
            [
                new Info(id, name),
                new Transform(position, Quaternion.Identity, scale),
                new Velocity(),
                new Mesh("cube.obj"),
                new Material(color),
                RigidBody.Dynamic(mass),
                Collider.Box(scale),
                new ShadowCaster()
            ]
        };
    }

    private static SceneObjectDefinition CreateDynamicSphere(
        string id,
        string name,
        Vector3 position,
        Fix radius,
        PixelColor color,
        Fix mass)
    {
        return new SceneObjectDefinition
        {
            Components =
            [
                new Info(id, name),
                new Transform(position, Quaternion.Identity, new Vector3(radius, radius, radius)),
                new Velocity(),
                new Mesh("sphere.obj"),
                new Material(color),
                RigidBody.Dynamic(mass),
                Collider.Sphere(radius),
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

    private static Fix Angle(double radians)
    {
        while (radians <= -Math.PI)
        {
            radians += Math.Tau;
        }

        while (radians > Math.PI)
        {
            radians -= Math.Tau;
        }

        return F(radians);
    }
}
