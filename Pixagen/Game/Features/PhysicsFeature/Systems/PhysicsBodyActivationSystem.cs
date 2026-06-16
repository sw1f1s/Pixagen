using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.PhysicsFeature.Runtime;
using Pixagen.Game.Features.SharedFeature.Components;
using Pixagen.Game.Features.SharedFeature.Helper;
using Pixagen.Ecs.Runtime;
using Pixagen.Ecs.DI;

namespace Pixagen.Game.Features.PhysicsFeature.Systems;

public sealed class PhysicsBodyActivationSystem : IFixedUpdateSystem
{
    private readonly CustomInject<PhysicsWorld> _physicsWorld = default;
    private readonly FilterInject<Include<Transform, RigidBody, Collider, PhysicsBodyReference>> _bodies = default;
    private readonly CustomInject<EntityStateHelper> _entityState = default;
    private readonly ComponentInject<Transform> _transforms = default;
    private readonly ComponentInject<RigidBody> _rigidBodies = default;
    private readonly ComponentInject<Collider> _colliders = default;
    private readonly ComponentInject<PhysicsBodyReference> _references = default;

    public void FixedUpdate()
    {
        PhysicsWorld physicsWorld = _physicsWorld.Value;

        foreach (Entity entity in _bodies.Value)
        {
            ref RigidBody rigidBody = ref _rigidBodies.Get(entity);
            if (rigidBody.Kind == PhysicsBodyKind.Static)
            {
                continue;
            }

            bool enabled = _entityState.Value.IsEnabled(entity);
            ref PhysicsBodyReference reference = ref _references.Get(entity);

            if (!enabled && reference.Active)
            {
                physicsWorld.RemoveBody(reference);
                reference.Active = false;
                continue;
            }

            if (enabled && !reference.Active)
            {
                ref Transform transform = ref _transforms.Get(entity);
                ref Collider collider = ref _colliders.Get(entity);
                reference = physicsWorld.AddBody(transform, rigidBody, collider);
            }
        }
    }
}
