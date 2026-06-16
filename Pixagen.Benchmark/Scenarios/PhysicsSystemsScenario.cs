using Pixagen.Ecs.Runtime;
using Pixagen.Ecs.DI;
using Pixagen.Game.Features.PhysicsFeature;
using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.SharedFeature.Components;

namespace Pixagen.Benchmark;

public sealed class PhysicsSystemsScenario : IBenchmarkScenario
{
    public string Name => "physics.fixed";
    public string Description => "Bepu body creation, activation, kinematic sync, fixed timestep, and transform sync.";
    public BenchmarkMeasurementMode Mode => BenchmarkMeasurementMode.SteadyState;

    public IBenchmarkCase Create(BenchmarkContext context)
    {
        var engine = new EngineBenchmarkContext(context.Config);
        var world = new WorldInject(engine.World);
        var transforms = new ComponentInject<Transform>(engine.World);
        var rigidBodies = new ComponentInject<RigidBody>(engine.World);
        var colliders = new ComponentInject<Collider>(engine.World);

        int dynamicBodies = 0;
        int kinematicBodies = 0;
        int staticBodies = 0;
        for (int i = 0; i < context.EntityCount; i++)
        {
            Entity entity = world.Create<Transform>();
            transforms.Get(entity) = new Transform(BenchmarkMath.GridPosition(i, context.EntityCount, new Fix(3), new Fix(4), Fix.Zero));
            RigidBody rigidBody = CreateRigidBody(i);
            rigidBodies.Add(entity, rigidBody);
            colliders.Add(entity, CreateCollider(i));

            switch (rigidBody.Kind)
            {
                case PhysicsBodyKind.Static:
                    staticBodies++;
                    break;
                case PhysicsBodyKind.Kinematic:
                    kinematicBodies++;
                    break;
                default:
                    dynamicBodies++;
                    break;
            }
        }

        Systems systems = engine.BuildSystems(new PhysicsFeatureSystemsGroup());

        return new SystemsBenchmarkCase(
            Name,
            context.EntityCount,
            engine,
            systems,
            counters: ctx =>
            {
                Dictionary<string, double> counters = ctx.CommonCounters();
                counters["dynamicBodies"] = dynamicBodies;
                counters["kinematicBodies"] = kinematicBodies;
                counters["staticBodies"] = staticBodies;
                return counters;
            });
    }

    private static RigidBody CreateRigidBody(int index)
    {
        if (index % 7 == 0)
        {
            return RigidBody.Static();
        }

        if (index % 5 == 0)
        {
            return RigidBody.Kinematic(lockRotation: (index & 1) == 0);
        }

        return RigidBody.Dynamic(Fix.One, lockRotation: index % 11 == 0);
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
}
