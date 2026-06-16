using Pixagen.Ecs.Runtime;
using Pixagen.Ecs.DI;

namespace Pixagen.Tests.TestSupport;

internal static class EcsTestAccess
{
    public static TestEntityAccess Access(Entity entity)
    {
        IWorld? world = entity == Entity.Empty || !WorldBuilder.AliveWorld(entity.WorldId)
            ? null
            : WorldBuilder.GetWorld(entity.WorldId);

        return new TestEntityAccess(world, entity);
    }

    public static TestEntityAccess Access(IWorld world, Entity entity)
    {
        return new TestEntityAccess(world, entity);
    }

    public static bool WorldAlive(IWorld world)
    {
        return WorldBuilder.AliveWorld(world.Id) &&
               ReferenceEquals(WorldBuilder.GetWorld(world.Id), world);
    }
}

internal readonly struct TestEntityAccess
{
    private readonly IWorld? _world;
    private readonly Entity _entity;

    public TestEntityAccess(IWorld? world, Entity entity)
    {
        _world = world;
        _entity = entity;
    }

    public bool IsAlive()
    {
        return _world is not null &&
               _entity != Entity.Empty &&
               WorldBuilder.AliveWorld(_entity.WorldId) &&
               new WorldInject(_world).IsAlive(_entity);
    }

    public bool Has<T>() where T : struct, IComponent
    {
        return new ComponentInject<T>(RequireWorld()).Has(_entity);
    }

    public T Get<T>() where T : struct, IComponent
    {
        var components = new ComponentInject<T>(RequireWorld());
        return components.Get(_entity);
    }

    public T Set<T>() where T : struct, IComponent
    {
        var components = new ComponentInject<T>(RequireWorld());
        return components.Set(_entity);
    }

    public T GetOrSet<T>() where T : struct, IComponent
    {
        var components = new ComponentInject<T>(RequireWorld());
        return components.GetOrSet(_entity);
    }

    public Entity Add<T>(in T component) where T : struct, IComponent
    {
        var components = new ComponentInject<T>(RequireWorld());
        return components.Add(_entity, component);
    }

    public Entity Replace<T>(in T component) where T : struct, IComponent
    {
        var components = new ComponentInject<T>(RequireWorld());
        return components.Replace(_entity, component);
    }

    public void Remove<T>() where T : struct, IComponent
    {
        var components = new ComponentInject<T>(RequireWorld());
        components.Remove(_entity);
    }

    public Entity Copy()
    {
        return new WorldInject(RequireWorld()).Copy(_entity);
    }

    public void Destroy()
    {
        new WorldInject(RequireWorld()).Destroy(_entity);
    }

    private IWorld RequireWorld()
    {
        return _world ?? throw new Exception($"{_entity} has no alive world.");
    }
}
