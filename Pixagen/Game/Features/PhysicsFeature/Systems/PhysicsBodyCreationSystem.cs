using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.PhysicsFeature.Runtime;
using Pixagen.Game.Features.SharedFeature.Helper;
using Pixagen.Ecs.DI;

namespace Pixagen.Game.Features.PhysicsFeature.Systems;

public sealed class PhysicsBodyCreationSystem : IInitSystem, IFixedUpdateSystem
{
    private readonly CustomInject<PhysicsWorld> _physicsWorld = default;
    private readonly FilterInject<Include<Transform, RigidBody, Collider>, Exclude<PhysicsBodyReference>> _bodies = default;
    private readonly CustomInject<EntityStateHelper> _entityState = default;
    private readonly ComponentInject<Transform> _transforms = default;
    private readonly ComponentInject<RigidBody> _rigidBodies = default;
    private readonly ComponentInject<Collider> _colliders = default;
    private readonly ComponentInject<PhysicsBodyReference> _references = default;

    public void Init()
    {
        CreateMissingBodies();
    }

    public void FixedUpdate()
    {
        CreateMissingBodies();
    }

    private void CreateMissingBodies()
    {
        PhysicsWorld physicsWorld = _physicsWorld.Value;
        foreach (Entity entity in _bodies.Value)
        {
            ref Transform transform = ref _transforms.Get(entity);
            ref RigidBody rigidBody = ref _rigidBodies.Get(entity);
            ref Collider collider = ref _colliders.Get(entity);
            if (rigidBody.Kind != PhysicsBodyKind.Static && !_entityState.Value.IsEnabled(entity))
            {
                continue;
            }

            _references.Add(entity, physicsWorld.AddBody(transform, rigidBody, collider));
        }
    }
}
