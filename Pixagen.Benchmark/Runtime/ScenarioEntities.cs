using Pixagen.Ecs.Runtime;
using Pixagen.Ecs.DI;
using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.SharedFeature.Components;

namespace Pixagen.Benchmark;

public static class ScenarioEntities
{
    public static Entity CreateCamera(EngineBenchmarkContext context)
    {
        var world = new WorldInject(context.World);
        var transforms = new ComponentInject<Transform>(context.World);
        var cameras = new ComponentInject<Camera>(context.World);

        Entity entity = world.Create<Transform>();
        transforms.Get(entity) = new Transform(new Vector3(Fix.Zero, new Fix(8), new Fix(-48)));
        cameras.Add(entity, new Camera(
            Fix.One,
            Fix.One,
            new Fix(9) / new Fix(16),
            Fix.FromDouble(256)));
        return entity;
    }

    public static Entity CreateLight(EngineBenchmarkContext context)
    {
        var world = new WorldInject(context.World);
        var transforms = new ComponentInject<Transform>(context.World);
        var lights = new ComponentInject<LightDirection>(context.World);

        Entity entity = world.Create<Transform>();
        transforms.Get(entity) = new Transform(
            Vector3.Zero,
            Quaternion.FromAxisAngle(Vector3.Right, Fix.Pi / new Fix(5)),
            Vector3.One);
        lights.Add(entity, new LightDirection(
            Fix.One,
            Fix.One / new Fix(5),
            new Fix(3) / new Fix(5),
            Fix.One / new Fix(20),
            Fix.FromDouble(100)));
        return entity;
    }
}
