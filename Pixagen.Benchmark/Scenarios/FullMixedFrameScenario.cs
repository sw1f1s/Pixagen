using Pixagen.Ecs.Runtime;
using Pixagen.Ecs.DI;
using Pixagen.Game.Features.CharacterFeature;
using Pixagen.Game.Features.CharacterFeature.Components;
using Pixagen.Game.Features.FreeCameraFeature;
using Pixagen.Game.Features.PhysicsFeature;
using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.RenderFeature;
using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.SharedFeature;
using Pixagen.Game.Features.SharedFeature.Components;
using Pixagen.Game.Features.UIFeature;
using Pixagen.Game.Features.UIFeature.Components;
using Pixagen.Rendering;
using FreeCameraComponent = Pixagen.Game.Features.FreeCameraFeature.Components.FreeCamera;

namespace Pixagen.Benchmark;

public sealed class FullMixedFrameScenario : IBenchmarkScenario
{
    public string Name => "full.mixed";
    public string Description => "Headless engine frame with controller input, physics subset, shared transforms, render, and UI.";
    public BenchmarkMeasurementMode Mode => BenchmarkMeasurementMode.SteadyState;

    public IBenchmarkCase Create(BenchmarkContext context)
    {
        var engine = new EngineBenchmarkContext(context.Config);
        engine.Resources.LoadMesh("plane");
        engine.Resources.LoadMesh("cube");
        engine.Resources.LoadTexture("checker");
        ScenarioEntities.CreateCamera(engine);
        ScenarioEntities.CreateLight(engine);
        engine.Input.SetKey(InputKey.W, true);
        engine.Input.SetKey(InputKey.D, true);

        var world = new WorldInject(engine.World);
        var transforms = new ComponentInject<Transform>(engine.World);
        var localTransforms = new ComponentInject<LocalTransform>(engine.World);
        var velocities = new ComponentInject<Velocity>(engine.World);
        var lerps = new ComponentInject<LerpMovement>(engine.World);
        var rotations = new ComponentInject<RotationMotion>(engine.World);
        var meshes = new ComponentInject<Mesh>(engine.World);
        var materials = new ComponentInject<Material>(engine.World);
        var shadows = new ComponentInject<ShadowCaster>(engine.World);
        var staticMarkers = new ComponentInject<IsStaticRender>(engine.World);
        var rigidBodies = new ComponentInject<RigidBody>(engine.World);
        var colliders = new ComponentInject<Collider>(engine.World);
        var freeCameras = new ComponentInject<FreeCameraComponent>(engine.World);
        var characterComponents = new ComponentInject<FpsCharacter>(engine.World);
        var cameraComponents = new ComponentInject<FpsCameraCharacter>(engine.World);
        var cameras = new ComponentInject<Camera>(engine.World);
        var uiTransforms = new ComponentInject<TransformUI>(engine.World);
        var texts = new ComponentInject<TextUI>(engine.World);
        var fpsCounters = new ComponentInject<FPSCounterUI>(engine.World);
        var profilers = new ComponentInject<ProfilerUI>(engine.World);

        var toggleEntities = new List<Entity>(Math.Max(1, context.EntityCount / 8));
        int renderables = 0;
        int physicsBodies = 0;
        int uiEntities = 0;
        int controllerEntities = 0;
        int characterEntities = 0;

        for (int i = 0; i < context.EntityCount; i++)
        {
            Entity entity = world.Create<Transform>();
            transforms.Get(entity) = new Transform(
                BenchmarkMath.GridPosition(i, context.EntityCount, Fix.One, Fix.Zero, new Fix(8)),
                Quaternion.Identity,
                Vector3.One);
            velocities.Add(entity, new Velocity());

            if (i % 2 == 0)
            {
                meshes.Add(entity, new Mesh(i % 14 == 0 ? "cube" : "plane"));
                materials.Add(entity, CreateMaterial(i));
                renderables++;

                if (i % 6 == 0)
                {
                    shadows.Add(entity, new ShadowCaster());
                }

                if (i % 20 == 0)
                {
                    staticMarkers.Add(entity, new IsStaticRender());
                }
            }

            if (i % 3 == 0)
            {
                lerps.Add(entity, new LerpMovement(
                    BenchmarkMath.GridPosition(i, context.EntityCount, Fix.One, Fix.Zero, new Fix(8)),
                    BenchmarkMath.GridPosition(i, context.EntityCount, Fix.One, new Fix(2), new Fix(10)),
                    new Fix(3)));
            }

            if (i % 4 == 0)
            {
                rotations.Add(entity, new RotationMotion(Vector3.Up, Fix.One / new Fix(3)));
            }

            if (i % 10 == 0)
            {
                rigidBodies.Add(entity, i % 30 == 0 ? RigidBody.Kinematic() : RigidBody.Dynamic(Fix.One));
                colliders.Add(entity, CreateCollider(i));
                physicsBodies++;
            }

            if (i % 64 == 0)
            {
                freeCameras.Add(entity, new FreeCameraComponent(new Fix(6), Fix.One));
                controllerEntities++;
            }

            if (i % 128 == 0)
            {
                EnsurePhysicsBody(entity, i, rigidBodies, colliders, ref physicsBodies);
                characterComponents.Add(entity, new FpsCharacter(new Fix(5), Fix.One));
                CreateFpsCameraCharacterChild(engine, entity, world, transforms, localTransforms, cameras, cameraComponents);
                characterEntities++;
                controllerEntities++;
            }

            if (i % 5 == 0)
            {
                AddOrReplaceUi(entity, i, uiTransforms, texts);
                uiEntities++;
            }

            if (i % 128 == 0)
            {
                AddOrReplaceUi(entity, i, uiTransforms, texts);
                fpsCounters.Add(entity, new FPSCounterUI());
            }

            if (i % 256 == 0)
            {
                AddOrReplaceUi(entity, i, uiTransforms, texts);
                profilers.Add(entity, new ProfilerUI(Fix.One / new Fix(2)));
            }

            if (i % 8 == 0)
            {
                toggleEntities.Add(entity);
            }
        }

        Systems systems = engine.BuildSystems(
            new VelocityWorkloadSystem(),
            new EntityToggleWorkloadSystem(toggleEntities.ToArray()),
            new FreeCameraFeatureSystemsGroup(),
            new PhysicsFeatureSystemsGroup(),
            new CharacterFeatureSystemsGroup(),
            new SharedFeatureSystemsGroup(),
            new RenderFeatureSystemsGroup(),
            new UIFeatureSystemsGroup());

        return new SystemsBenchmarkCase(
            Name,
            context.EntityCount,
            engine,
            systems,
            counters: ctx =>
            {
                Dictionary<string, double> counters = ctx.CommonCounters();
                counters["renderables"] = renderables;
                counters["physicsBodies"] = physicsBodies;
                counters["uiEntities"] = uiEntities;
                counters["controllers"] = controllerEntities;
                counters["characterComponents"] = characterEntities;
                return counters;
            });
    }

    private static Material CreateMaterial(int index)
    {
        PixelColor color = PixelColor.FromRgb(
            BenchmarkMath.Channel(index, 19),
            BenchmarkMath.Channel(index, 20),
            BenchmarkMath.Channel(index, 21));

        return index % 4 == 0
            ? new Material(color, new MaterialTexture("checker"))
            : new Material(color);
    }

    private static Collider CreateCollider(int index)
    {
        return (index % 3) switch
        {
            0 => Collider.Box(Vector3.One),
            1 => Collider.Sphere(Fix.One / new Fix(2)),
            _ => Collider.Capsule(Fix.One / new Fix(3), Fix.One)
        };
    }

    private static void EnsurePhysicsBody(
        Entity entity,
        int index,
        ComponentInject<RigidBody> rigidBodies,
        ComponentInject<Collider> colliders,
        ref int physicsBodies)
    {
        if (!rigidBodies.Has(entity))
        {
            rigidBodies.Add(entity, RigidBody.Dynamic(Fix.One, lockRotation: true));
            physicsBodies++;
        }

        if (!colliders.Has(entity))
        {
            colliders.Add(entity, CreateCollider(index));
        }
    }

    private static void CreateFpsCameraCharacterChild(
        EngineBenchmarkContext engine,
        Entity parent,
        WorldInject world,
        ComponentInject<Transform> transforms,
        ComponentInject<LocalTransform> localTransforms,
        ComponentInject<Camera> cameras,
        ComponentInject<FpsCameraCharacter> cameraComponents)
    {
        Entity camera = world.Create<Transform>();
        var localTransform = new LocalTransform(new Vector3(Fix.Zero, Fix.One, Fix.Zero));
        transforms.Get(camera) = new Transform(new Vector3(Fix.Zero, Fix.One, Fix.Zero));
        localTransforms.Add(camera, localTransform);
        engine.State.AddChild(parent, camera);
        cameras.Add(camera, new Camera(Fix.One, Fix.One, new Fix(9) / new Fix(16), new Fix(64)));
        cameraComponents.Add(camera, new FpsCameraCharacter(Fix.Zero));
    }

    private static void AddOrReplaceUi(
        Entity entity,
        int index,
        ComponentInject<TransformUI> uiTransforms,
        ComponentInject<TextUI> texts)
    {
        var transform = new TransformUI(index % 80, index / 80, index % 32);
        var text = new TextUI(
            $"mix {index:000000}",
            PixelColor.FromRgb(
                BenchmarkMath.Channel(index, 22),
                BenchmarkMath.Channel(index, 23),
                BenchmarkMath.Channel(index, 24)));

        if (uiTransforms.Has(entity))
        {
            uiTransforms.Replace(entity, transform);
        }
        else
        {
            uiTransforms.Add(entity, transform);
        }

        if (texts.Has(entity))
        {
            texts.Replace(entity, text);
        }
        else
        {
            texts.Add(entity, text);
        }
    }
}
