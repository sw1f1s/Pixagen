using Pixagen.Game.Features.PhysicsFeature.Runtime;
using Pixagen.Game.Features.ResourceFeature.Runtime;
using Pixagen.Game.Features.RenderFeature.Raycasting;
using Pixagen.Game.Features.SharedFeature.Helper;
using Pixagen.Ecs.DI;

namespace Pixagen.Tests.TestSupport;

public sealed class EcsTestContext : IDisposable
{
    public EcsTestContext(params object[] services)
    {
        World = WorldBuilder.Build();
        Time = new Time();
        Input = new InputState();
        Options = services.OfType<EngineOptions>().FirstOrDefault() ?? new EngineOptions();
        Resources = services.OfType<ResourceManager>().FirstOrDefault() ?? new ResourceManager();
        PhysicsWorld = new PhysicsWorld();
        RenderSceneCache = new RenderSceneCache();
        State = new EntityStateHelper();
        using (var injector = new Systems(World))
        {
            injector.InjectObject(State);
        }
        Services = services;
    }

    public IWorld World { get; }
    public Time Time { get; }
    public InputState Input { get; }
    public EngineOptions Options { get; }
    public ResourceManager Resources { get; }
    public PhysicsWorld PhysicsWorld { get; }
    public RenderSceneCache RenderSceneCache { get; }
    public EntityStateHelper State { get; }
    public object[] Services { get; }

    public ComponentInject<T> Component<T>() where T : struct, IComponent
    {
        return new ComponentInject<T>(World);
    }

    public T Inject<T>(T target, params object[] services) where T : class
    {
        using var container = new Systems(World);
        return container.InjectObject(target, services.Concat(Services).Concat(DefaultServices()).ToArray());
    }

    public Systems BuildSystems(params ISystem[] systems)
    {
        var container = new Systems(World);
        foreach (ISystem system in systems)
        {
            container.Add(system);
        }

        container.Inject(Services.Concat(DefaultServices()).ToArray());
        container.Init();
        return container;
    }

    public Systems BuildSystems(params IGroupSystem[] groups)
    {
        var container = new Systems(World);
        foreach (IGroupSystem group in groups)
        {
            container.Add(group);
        }

        container.Inject(Services.Concat(DefaultServices()).ToArray());
        container.Init();
        return container;
    }

    public void SetDeltaTime(Fix deltaTime)
    {
        Time.Advance(deltaTime);
    }

    public void Dispose()
    {
        PhysicsWorld.Dispose();
        Resources.Dispose();
    }

    private object[] DefaultServices()
    {
        return [Time, Input, Options, Resources, PhysicsWorld, RenderSceneCache, State];
    }

}
