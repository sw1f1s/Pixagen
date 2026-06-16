using System;
using Pixagen.Ecs.DI;
using Pixagen.Ecs.Runtime;
using Pixagen.Game.Features.SharedFeature.Components;
using Pixagen.Game.Features.SharedFeature.Helper;

namespace Pixagen.Game.Features.SharedFeature.Systems;

public sealed class HierarchyTransformSystem : IUpdateSystem
{
    private readonly HashSet<Entity> _visited = new();

    private readonly FilterInject<Include<Transform, Children>> _roots = default;
    private readonly CustomInject<EntityStateHelper> _entityState = default;
    private readonly ComponentInject<Transform> _transforms = default;
    private readonly ComponentInject<Children> _children = default;
    private readonly ComponentInject<Parent> _parents = default;
    private readonly ComponentInject<LocalTransform> _localTransforms = default;

    public void Update()
    {
        _visited.Clear();
        _roots.Value.ForEachChunkSequential(new ChunkJob(
            _visited,
            _entityState,
            _transforms,
            _children,
            _parents,
            _localTransforms));
    }

    private readonly struct ChunkJob : IFilterChunkProcessor
    {
        private readonly HashSet<Entity> _visited;
        private readonly CustomInject<EntityStateHelper> _entityState;
        private readonly ComponentInject<Transform> _transforms;
        private readonly ComponentInject<Children> _children;
        private readonly ComponentInject<Parent> _parents;
        private readonly ComponentInject<LocalTransform> _localTransforms;

        public ChunkJob(
            HashSet<Entity> visited,
            CustomInject<EntityStateHelper> entityState,
            ComponentInject<Transform> transforms,
            ComponentInject<Children> children,
            ComponentInject<Parent> parents,
            ComponentInject<LocalTransform> localTransforms)
        {
            _visited = visited;
            _entityState = entityState;
            _transforms = transforms;
            _children = children;
            _parents = parents;
            _localTransforms = localTransforms;
        }

        public void Execute(FilterChunk chunk)
        {
            foreach (Entity root in chunk.Entities)
            {
                if (HasAliveParent(root) || !_entityState.Value.IsEnabled(root))
                {
                    continue;
                }

                UpdateChildren(root);
            }
        }

        private void UpdateChildren(Entity parentEntity)
        {
            if (parentEntity == Entity.Empty ||
                !_entityState.Value.IsAlive(parentEntity) ||
                !_visited.Add(parentEntity) ||
                !_transforms.Has(parentEntity) ||
                !_children.Has(parentEntity))
            {
                return;
            }

            ref Transform parentTransform = ref _transforms.Get(parentEntity);
            ref Children children = ref _children.Get(parentEntity);

            foreach (Entity child in children.Entities)
            {
                if (!_entityState.Value.IsAlive(child) || !_entityState.Value.IsEnabled(child))
                {
                    continue;
                }

                if (_transforms.Has(child) && _parents.Has(child) && _localTransforms.Has(child))
                {
                    ref Parent parent = ref _parents.Get(child);
                    if (parent.Entity == parentEntity)
                    {
                        ApplyLocalTransform(parentTransform, child);
                    }
                }

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

        private bool HasAliveParent(Entity entity)
        {
            if (!_parents.Has(entity))
            {
                return false;
            }

            ref Parent parent = ref _parents.Get(entity);
            return _entityState.Value.IsAlive(parent.Entity);
        }
    }
}
