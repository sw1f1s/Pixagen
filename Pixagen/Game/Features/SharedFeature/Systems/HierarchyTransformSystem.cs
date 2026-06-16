using Pixagen.Game.Features.SharedFeature.Components;
using Pixagen.Game.Features.SharedFeature.Helper;
using Pixagen.Ecs.Runtime;
using Pixagen.Ecs.DI;

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

        foreach (Entity root in _roots.Value)
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
