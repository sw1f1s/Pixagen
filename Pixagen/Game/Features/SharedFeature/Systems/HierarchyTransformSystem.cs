using Pixagen.Ecs.DI;
using Pixagen.Ecs.Collections;

namespace Pixagen.Game.Features.SharedFeature.Systems;

public sealed class HierarchyTransformSystem : IInitSystem, IUpdateSystem
{
    private readonly FilterInject<Include<Transform, HierarchyDirty>, Exclude<DisabledInHierarchy>> _hierarchyDirtyNodes = default;
    private readonly FilterInject<Include<Transform, TransformDirty>, Exclude<DisabledInHierarchy>> _transformDirtyNodes = default;
    private readonly FilterInject<Include<HierarchyDirtyQueue>> _dirtyQueues = default;
    private readonly WorldInject _world = default;
    private readonly ComponentInject<Transform> _transforms = default;
    private readonly ComponentInject<Children> _children = default;
    private readonly ComponentInject<HasChildren> _hasChildren = default;
    private readonly ComponentInject<Parent> _parents = default;
    private readonly ComponentInject<LocalTransform> _localTransforms = default;
    private readonly ComponentInject<HierarchyDirty> _hierarchyDirty = default;
    private readonly ComponentInject<TransformDirty> _transformDirty = default;
    private readonly ComponentInject<DisabledInHierarchy> _disabledInHierarchy = default;
    private readonly ComponentInject<HierarchyDirtyQueue> _dirtyQueueComponents = default;
    private Entity _dirtyQueueEntity;

    public void Init()
    {
        _ = GetDirtyQueue();
    }

    public void Update()
    {
        ref HierarchyDirtyQueue queue = ref GetDirtyQueue();
        PooledList<Entity> transformDirtyEntities = queue.TransformDirtyEntities;
        for (int i = 0; i < transformDirtyEntities.Count; i++)
        {
            Entity entity = transformDirtyEntities[i];
            if (!HasDirtyAncestor(entity))
            {
                ProcessTransformDirty(entity);
            }
        }

        transformDirtyEntities.Clear();

        foreach (Entity entity in _hierarchyDirtyNodes.Value)
        {
            if (!HasDirtyAncestor(entity))
            {
                ProcessHierarchyDirty(entity);
            }
        }

        foreach (Entity entity in _transformDirtyNodes.Value)
        {
            if (!HasDirtyAncestor(entity))
            {
                ProcessTransformDirty(entity);
            }
        }
    }

    private ref HierarchyDirtyQueue GetDirtyQueue()
    {
        if (_dirtyQueueEntity != Entity.Empty &&
            _world.IsAlive(_dirtyQueueEntity) &&
            _dirtyQueueComponents.Has(_dirtyQueueEntity))
        {
            return ref _dirtyQueueComponents.Get(_dirtyQueueEntity);
        }

        foreach (Entity entity in _dirtyQueues.Value)
        {
            _dirtyQueueEntity = entity;
            return ref _dirtyQueueComponents.Get(entity);
        }

        throw new InvalidOperationException($"{nameof(HierarchyDirtyQueue)} was not created. Add HierarchyDirtyQueueInitSystem before hierarchy systems.");
    }

    private void ProcessHierarchyDirty(Entity entity)
    {
        if (entity == Entity.Empty || !_world.IsAlive(entity))
        {
            return;
        }

        if (_disabledInHierarchy.Has(entity))
        {
            return;
        }

        if (!_transforms.Has(entity))
        {
            ClearDirty(entity);
            return;
        }

        if (_parents.Has(entity) && _localTransforms.Has(entity))
        {
            ref Parent parent = ref _parents.Get(entity);
            Entity parentEntity = parent.Entity;
            if (parentEntity != Entity.Empty)
            {
                if (!_world.IsAlive(parentEntity) || !_transforms.Has(parentEntity))
                {
                    ClearDirty(entity);
                    return;
                }

                if (_disabledInHierarchy.Has(parentEntity))
                {
                    return;
                }

                ApplyLocalTransform(_transforms.Get(parentEntity), entity);
            }
        }

        ClearDirty(entity);
        UpdateChildren(entity);
    }

    private void ProcessTransformDirty(Entity entity)
    {
        if (!CanProcess(entity))
        {
            return;
        }

        ClearDirty(entity);
        if (_hasChildren.Has(entity))
        {
            UpdateChildren(entity);
        }
    }

    private bool HasDirtyAncestor(Entity entity)
    {
        while (_parents.Has(entity))
        {
            ref Parent parent = ref _parents.Get(entity);
            entity = parent.Entity;
            if (entity == Entity.Empty || !_world.IsAlive(entity))
            {
                return false;
            }

            if (_disabledInHierarchy.Has(entity))
            {
                return true;
            }

            if (_hierarchyDirty.Has(entity) || _transformDirty.Has(entity))
            {
                return true;
            }
        }

        return false;
    }

    private bool CanProcess(Entity entity)
    {
        return entity != Entity.Empty &&
               _world.IsAlive(entity) &&
               !_disabledInHierarchy.Has(entity) &&
               _transforms.Has(entity);
    }

    private void UpdateChildren(Entity parentEntity)
    {
        if (!_children.Has(parentEntity))
        {
            return;
        }

        ref Transform parentTransform = ref _transforms.Get(parentEntity);
        ref Children children = ref _children.Get(parentEntity);

        foreach (Entity child in children.Entities)
        {
            if (child == Entity.Empty || !_world.IsAlive(child))
            {
                continue;
            }

            if (_disabledInHierarchy.Has(child))
            {
                MarkHierarchyDirty(child);
                continue;
            }

            if (!CanProcess(child))
            {
                continue;
            }

            bool keepWorldTransform = _transformDirty.Has(child) && !_hierarchyDirty.Has(child);
            if (!keepWorldTransform && _parents.Has(child) && _localTransforms.Has(child))
            {
                ref Parent parent = ref _parents.Get(child);
                if (parent.Entity == parentEntity)
                {
                    ApplyLocalTransform(parentTransform, child);
                }
            }

            ClearDirty(child);
            UpdateChildren(child);
        }
    }

    private void ApplyLocalTransform(in Transform parentTransform, Entity child)
    {
        ref LocalTransform localTransform = ref _localTransforms.Get(child);
        ref Transform transform = ref _transforms.Get(child);
        Quaternion parentRotation = parentTransform.Rotation.MagnitudeSquared <= Fix.Epsilon
            ? Quaternion.Identity
            : parentTransform.Rotation.Normalized;
        Quaternion localRotation = localTransform.Rotation.MagnitudeSquared <= Fix.Epsilon
            ? Quaternion.Identity
            : localTransform.Rotation.Normalized;

        transform.Position = parentTransform.Position + parentRotation.Rotate(localTransform.Position);
        transform.Rotation = (parentRotation * localRotation).Normalized;
        transform.Scale = new Vector3(
            parentTransform.Scale.X * localTransform.Scale.X,
            parentTransform.Scale.Y * localTransform.Scale.Y,
            parentTransform.Scale.Z * localTransform.Scale.Z);
    }

    private void ClearDirty(Entity entity)
    {
        if (_hierarchyDirty.Has(entity))
        {
            _hierarchyDirty.Remove(entity);
        }

        if (_transformDirty.Has(entity))
        {
            _transformDirty.Remove(entity);
        }
    }

    private void MarkHierarchyDirty(Entity entity)
    {
        if (!_hierarchyDirty.Has(entity))
        {
            _hierarchyDirty.Add(entity, new HierarchyDirty());
        }
    }
}
