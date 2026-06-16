using Pixagen.Ecs.DI;

namespace Pixagen.Game.Features.SharedFeature.Systems;

public sealed class HierarchyTransformSystem : IUpdateSystem
{
    private readonly FilterInject<Include<Transform, Children>, Exclude<DisabledInHierarchy>> _roots = default;
    private readonly WorldInject _world = default;
    private readonly ComponentInject<Transform> _transforms = default;
    private readonly ComponentInject<Children> _children = default;
    private readonly ComponentInject<Parent> _parents = default;
    private readonly ComponentInject<LocalTransform> _localTransforms = default;
    private readonly ComponentInject<DisabledInHierarchy> _disabledInHierarchy = default;

    public void Update()
    {
        _roots.Value.ForEachChunkSequential(new ChunkJob(
            _world,
            _transforms,
            _children,
            _parents,
            _localTransforms,
            _disabledInHierarchy));
    }

    private readonly struct ChunkJob : IFilterChunkProcessor
    {
        private readonly WorldInject _world;
        private readonly ComponentInject<Transform> _transforms;
        private readonly ComponentInject<Children> _children;
        private readonly ComponentInject<Parent> _parents;
        private readonly ComponentInject<LocalTransform> _localTransforms;
        private readonly ComponentInject<DisabledInHierarchy> _disabledInHierarchy;

        public ChunkJob(
            WorldInject world,
            ComponentInject<Transform> transforms,
            ComponentInject<Children> children,
            ComponentInject<Parent> parents,
            ComponentInject<LocalTransform> localTransforms,
            ComponentInject<DisabledInHierarchy> disabledInHierarchy)
        {
            _world = world;
            _transforms = transforms;
            _children = children;
            _parents = parents;
            _localTransforms = localTransforms;
            _disabledInHierarchy = disabledInHierarchy;
        }

        public void Execute(FilterChunk chunk)
        {
            foreach (Entity root in chunk.Entities)
            {
                if (HasAliveParent(root))
                {
                    continue;
                }

                UpdateChildren(root);
            }
        }

        private void UpdateChildren(Entity parentEntity)
        {
            if (parentEntity == Entity.Empty ||
                !_world.IsAlive(parentEntity) ||
                _disabledInHierarchy.Has(parentEntity) ||
                !_transforms.Has(parentEntity) ||
                !_children.Has(parentEntity))
            {
                return;
            }

            ref Transform parentTransform = ref _transforms.Get(parentEntity);
            ref Children children = ref _children.Get(parentEntity);

            foreach (Entity child in children.Entities)
            {
                if (!_world.IsAlive(child) || _disabledInHierarchy.Has(child))
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
            return _world.IsAlive(parent.Entity);
        }
    }
}
