using Pixagen.Ecs.DI;

namespace Pixagen.Game.Features.SharedFeature.Helper;

public sealed class EntityStateHelper
{
    private readonly WorldInject _world = default;
    private readonly ComponentInject<Transform> _transforms = default;
    private readonly ComponentInject<LocalTransform> _localTransforms = default;
    private readonly ComponentInject<Children> _children = default;
    private readonly ComponentInject<Parent> _parents = default;
    private readonly ComponentInject<Info> _infos = default;
    private readonly ComponentInject<IsEnable> _enableStates = default;
    private readonly ComponentInject<DestroyOneTick> _destroyTicks = default;
    private readonly ComponentInject<SpawnOneTick> _spawnTicks = default;
    private readonly ComponentInject<EnableNextTick> _enableNextTicks = default;
    private readonly ComponentInject<DisableNextTick> _disableNextTicks = default;
    private readonly ComponentInject<EnableOneTick> _enableTicks = default;
    private readonly ComponentInject<DisableOneTick> _disableTicks = default;

    public Entity CreateObject()
    {
        Entity entity = _world.Create<Transform>();
        ref Transform transform = ref _transforms.Get(entity);
        transform = new Transform(Vector3.Zero);
        _infos.Add(entity, Info.Create());
        _localTransforms.Add(entity, LocalTransform.FromTransform(transform));
        _children.Set(entity);
        _spawnTicks.Add(entity, new SpawnOneTick());
        return entity;
    }

    public bool IsAlive(in Entity entity)
    {
        return _world.IsAlive(entity);
    }

    public bool IsEnabled(in Entity entity)
    {
        Entity current = entity;
        uint depth = 0;
        uint maxDepth = _world.Value.Entities.Count + 1;

        while (current != Entity.Empty && _world.IsAlive(current))
        {
            if (++depth > maxDepth)
            {
                return false;
            }

            if (_enableStates.Has(current) && !_enableStates.Get(current).Value)
            {
                return false;
            }

            if (!_parents.Has(current))
            {
                return true;
            }

            ref Parent parent = ref _parents.Get(current);
            if (parent.Entity == Entity.Empty)
            {
                return true;
            }

            current = parent.Entity;
        }

        return false;
    }

    public bool AddChild(in Entity parent, in Entity child)
    {
        return MoveToParent(child, parent);
    }

    public bool Enable(in Entity entity)
    {
        return SetEnabled(entity, true);
    }

    public bool Disable(in Entity entity)
    {
        return SetEnabled(entity, false);
    }

    public bool SetEnabled(in Entity entity, bool value)
    {
        if (!IsAlive(entity))
        {
            return false;
        }

        if (value)
        {
            if (_enableNextTicks.Has(entity))
            {
                return false;
            }

            if (_disableNextTicks.Has(entity))
            {
                _disableNextTicks.Remove(entity);
                return true;
            }

            if (IsEnabled(entity))
            {
                return false;
            }

            _enableNextTicks.Add(entity, new EnableNextTick());
            return true;
        }

        if (_disableNextTicks.Has(entity))
        {
            return false;
        }

        if (_enableNextTicks.Has(entity))
        {
            _enableNextTicks.Remove(entity);
            return true;
        }

        if (!IsEnabled(entity))
        {
            return false;
        }

        _disableNextTicks.Add(entity, new DisableNextTick());
        return true;
    }

    public bool AddDestroyOneTick(in Entity entity)
    {
        var visited = new HashSet<Entity>();
        AddDestroyOneTick(entity, visited);
        return visited.Count > 0;
    }

    public Entity CopyObject(in Entity entity)
    {
        if (!IsAlive(entity))
        {
            return Entity.Empty;
        }

        Entity parent = Entity.Empty;
        if (_parents.Has(entity))
        {
            parent = _parents.Get(entity).Entity;
        }

        return CopyObject(entity, parent);
    }

    public Entity CopyObject(in Entity entity, in Entity parent)
    {
        if (!IsAlive(entity))
        {
            return Entity.Empty;
        }

        if (parent != Entity.Empty && !IsAlive(parent))
        {
            return Entity.Empty;
        }

        return CopyObjectHierarchy(entity, parent);
    }

    public bool MoveToParent(in Entity child, in Entity newParent)
    {
        if (!IsAlive(child) || !IsAlive(newParent) || child == newParent || WouldCreateCycle(child, newParent))
        {
            return false;
        }

        RemoveFromParent(child);
        EnsureLocalTransform(child);
        SetParent(child, newParent);
        EnsureChildren(newParent);

        ref Children children = ref _children.Get(newParent);
        if (!children.Entities.Contains(child))
        {
            children.Entities.Add(child);
        }
        return true;
    }

    public bool RemoveFromParent(in Entity child)
    {
        if (!IsAlive(child) || !_parents.Has(child))
        {
            return false;
        }

        ref Parent parent = ref _parents.Get(child);
        Entity oldParent = parent.Entity;
        parent.Entity = Entity.Empty;

        if (!IsAlive(oldParent) || !_children.Has(oldParent))
        {
            return false;
        }

        ref Children children = ref _children.Get(oldParent);
        children.Entities.Remove(child);
        return true;
    }

    public bool RemoveChild(in Entity parent, in Entity child)
    {
        bool removed = false;
        if (IsAlive(parent) && _children.Has(parent))
        {
            ref Children children = ref _children.Get(parent);
            children.Entities.Remove(child);
            removed = true;
        }

        if (IsAlive(child) && _parents.Has(child))
        {
            ref Parent parentComponent = ref _parents.Get(child);
            if (parentComponent.Entity == parent)
            {
                parentComponent.Entity = Entity.Empty;
                removed = true;
            }
        }

        return removed;
    }

    private void SetParent(in Entity child, in Entity parent)
    {
        if (!_parents.Has(child))
        {
            _parents.Add(child, new Parent(parent));
            return;
        }

        ref Parent parentComponent = ref _parents.Get(child);
        parentComponent.Entity = parent;
    }

    private void EnsureChildren(in Entity parent)
    {
        if (!_children.Has(parent))
        {
            _children.Set(parent);
        }
    }

    private void EnsureLocalTransform(in Entity child)
    {
        if (!_localTransforms.Has(child) && _transforms.Has(child))
        {
            _localTransforms.Add(child, LocalTransform.FromTransform(_transforms.Get(child)));
        }
    }

    private bool WouldCreateCycle(in Entity child, in Entity newParent)
    {
        var visited = new HashSet<Entity>();
        Entity current = newParent;

        while (current != Entity.Empty && _world.IsAlive(current))
        {
            if (current == child || !visited.Add(current))
            {
                return true;
            }

            if (!_parents.Has(current))
            {
                return false;
            }

            ref Parent parent = ref _parents.Get(current);
            current = parent.Entity;
        }

        return false;
    }

    private void AddDestroyOneTick(in Entity entity, HashSet<Entity> visited)
    {
        if (!IsAlive(entity) || !visited.Add(entity))
        {
            return;
        }

        if (!_destroyTicks.Has(entity))
        {
            _destroyTicks.Add(entity, new DestroyOneTick());
        }

        if (!_children.Has(entity))
        {
            return;
        }

        ref Children children = ref _children.Get(entity);
        foreach (Entity child in children.Entities)
        {
            AddDestroyOneTick(child, visited);
        }
    }

    private Entity CopyObjectHierarchy(in Entity source, in Entity parent)
    {
        Entity copy = _world.Copy(source);
        PrepareObjectCopy(copy, parent);

        if (!_children.Has(source))
        {
            return copy;
        }

        ref Children children = ref _children.Get(source);
        var childItems = children.Entities;
        for (int i = 0; i < childItems.Count; i++)
        {
            Entity child = childItems[i];
            if (IsAlive(child))
            {
                CopyObjectHierarchy(child, copy);
            }
        }

        return copy;
    }

    private void PrepareObjectCopy(in Entity copy, in Entity parent)
    {
        ResetInfo(copy);
        ResetChildren(copy);
        RemoveParent(copy);
        RemoveCopiedOneTicks(copy);

        if (parent != Entity.Empty)
        {
            MoveToParent(copy, parent);
        }

        if (!_spawnTicks.Has(copy))
        {
            _spawnTicks.Add(copy, new SpawnOneTick());
        }
    }

    private void ResetInfo(in Entity entity)
    {
        if (!_infos.Has(entity))
        {
            _infos.Add(entity, Info.Create());
            return;
        }

        ref Info info = ref _infos.Get(entity);
        info = Info.Create(info.Name);
    }

    private void ResetChildren(in Entity entity)
    {
        if (!_children.Has(entity))
        {
            _children.Set(entity);
            return;
        }

        ref Children children = ref _children.Get(entity);
        children.Entities.Clear();
    }

    private void RemoveParent(in Entity entity)
    {
        if (_parents.Has(entity))
        {
            _parents.Remove(entity);
        }
    }

    private void RemoveCopiedOneTicks(in Entity entity)
    {
        if (_destroyTicks.Has(entity))
        {
            _destroyTicks.Remove(entity);
        }

        if (_enableTicks.Has(entity))
        {
            _enableTicks.Remove(entity);
        }

        if (_disableTicks.Has(entity))
        {
            _disableTicks.Remove(entity);
        }

        if (_enableNextTicks.Has(entity))
        {
            _enableNextTicks.Remove(entity);
        }

        if (_disableNextTicks.Has(entity))
        {
            _disableNextTicks.Remove(entity);
        }

        if (_spawnTicks.Has(entity))
        {
            _spawnTicks.Remove(entity);
        }
    }

}
