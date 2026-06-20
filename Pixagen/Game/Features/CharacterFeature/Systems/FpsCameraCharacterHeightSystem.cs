using Pixagen.Ecs.DI;
using Pixagen.Game.Features.CharacterFeature.Components;
using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.SharedFeature.Helper;

namespace Pixagen.Game.Features.CharacterFeature.Systems;

public sealed class FpsCameraCharacterHeightSystem : IUpdateSystem
{
    private readonly FilterInject<Include<FpsCameraCharacter, LocalTransform, Parent>> _cameras = default;
    private readonly CustomInject<EntityStateHelper> _entityState = default;
    private readonly ComponentInject<FpsCharacter> _characterComponents = default;
    private readonly ComponentInject<Collider> _colliders = default;
    private readonly ComponentInject<LocalTransform> _localTransforms = default;
    private readonly ComponentInject<Parent> _parents = default;

    public void Update()
    {
        foreach (Entity entity in _cameras.Value)
        {
            if (!_entityState.Value.IsEnabled(entity))
            {
                continue;
            }

            ref Parent parent = ref _parents.Get(entity);
            if (parent.Entity == Entity.Empty ||
                !_entityState.Value.IsAlive(parent.Entity) ||
                !_characterComponents.Has(parent.Entity) ||
                !_colliders.Has(parent.Entity))
            {
                continue;
            }

            ref FpsCharacter character = ref _characterComponents.Get(parent.Entity);
            ref Collider collider = ref _colliders.Get(parent.Entity);
            Fix colliderHeight = ResolveColliderHeight(collider, character.Height);
            Fix targetY = ResolveCameraLocalHeight(colliderHeight, character.CameraHeightFactor);

            LocalTransform localTransform = _localTransforms.Get(entity);
            if (Abs(localTransform.Position.Y - targetY) <= Fix.Epsilon)
            {
                continue;
            }

            localTransform.Position = new Vector3(localTransform.Position.X, targetY, localTransform.Position.Z);
            _entityState.Value.SetLocalTransform(entity, localTransform);
        }
    }

    private static Fix ResolveCameraLocalHeight(Fix colliderHeight, Fix factor)
    {
        return colliderHeight * Clamp01(factor) - colliderHeight / new Fix(2);
    }

    private static Fix ResolveColliderHeight(in Collider collider, Fix fallbackHeight)
    {
        Fix height = collider.Shape switch
        {
            ColliderShape.Box => Abs(collider.Size.Y),
            ColliderShape.Sphere => Abs(collider.Radius) * new Fix(2),
            ColliderShape.Capsule => Abs(collider.Length) + Abs(collider.Radius) * new Fix(2),
            _ => fallbackHeight
        };

        if (height > Fix.Epsilon)
        {
            return height;
        }

        return fallbackHeight > Fix.Epsilon ? fallbackHeight : Fix.One;
    }

    private static Fix Clamp01(Fix value)
    {
        if (value <= Fix.Zero)
        {
            return Fix.Zero;
        }

        return value >= Fix.One ? Fix.One : value;
    }

    private static Fix Abs(Fix value)
    {
        return value < Fix.Zero ? Fix.Zero - value : value;
    }
}
