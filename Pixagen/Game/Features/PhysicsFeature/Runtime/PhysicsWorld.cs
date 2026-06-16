using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;
using BepuUtilities.Memory;
using Pixagen.Ecs.DI;
using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.SharedFeature.Components;
using NumericQuaternion = System.Numerics.Quaternion;
using NumericVector3 = System.Numerics.Vector3;

namespace Pixagen.Game.Features.PhysicsFeature.Runtime;

public sealed class PhysicsWorld : IDisposable, IDisposeInject
{
    private const float MinimumMass = 0.0001f;
    private const float MinimumShapeSize = 0.0001f;

    private readonly PhysicsMaterialStore _materials = new();
    private bool _disposed;
    
    public BufferPool Pool { get; }

    public Simulation Simulation { get; }

    public PhysicsWorld()
    {
        Pool = new BufferPool();
        Simulation = Simulation.Create(
            Pool,
            new PhysicsNarrowPhaseCallbacks(_materials),
            new PhysicsPoseIntegratorCallbacks(new NumericVector3(0, -9.81f, 0)),
            new SolveDescription(8, 1, SolveDescription.DefaultFallbackBatchThreshold),
            new DefaultTimestepper());
    }

    public PhysicsBodyReference AddBody(Transform transform, RigidBody rigidBody, Collider collider)
    {
        RigidPose pose = ToPose(transform);
        TypedIndex shapeIndex = AddShape(collider);

        if (rigidBody.Kind == PhysicsBodyKind.Static)
        {
            StaticHandle staticHandle = Simulation.Statics.Add(new StaticDescription(pose, shapeIndex));
            _materials.Set(new CollidableReference(staticHandle), rigidBody);
            return new PhysicsBodyReference(staticHandle, shapeIndex);
        }

        CollidableDescription collidable = new(shapeIndex, 0.03f, float.MaxValue);
        BodyActivityDescription activity = new(0.01f, 32);

        if (rigidBody.Kind == PhysicsBodyKind.Kinematic)
        {
            BodyHandle kinematicHandle = Simulation.Bodies.Add(
                BodyDescription.CreateKinematic(pose, collidable, activity));
            _materials.Set(new CollidableReference(CollidableMobility.Kinematic, kinematicHandle), rigidBody);
            return new PhysicsBodyReference(kinematicHandle, PhysicsBodyKind.Kinematic, shapeIndex);
        }

        BodyInertia inertia = ComputeInertia(collider, MathF.Max(MinimumMass, PhysicsConvert.ToFloat(rigidBody.Mass)));
        if (rigidBody.LockRotation)
        {
            inertia.InverseInertiaTensor = default;
        }

        BodyHandle dynamicHandle = Simulation.Bodies.Add(
            BodyDescription.CreateDynamic(pose, inertia, collidable, activity));
        _materials.Set(new CollidableReference(CollidableMobility.Dynamic, dynamicHandle), rigidBody);
        return new PhysicsBodyReference(dynamicHandle, PhysicsBodyKind.Dynamic, shapeIndex);
    }

    public void Step(float deltaTime)
    {
        if (deltaTime <= 0)
        {
            return;
        }

        Simulation.Timestep(deltaTime);
    }

    public bool TryGetBody(PhysicsBodyReference reference, out BodyReference body)
    {
        if (reference.Kind == PhysicsBodyKind.Static ||
            !reference.Active ||
            reference.BodyHandle.Value < 0 ||
            !Simulation.Bodies.BodyExists(reference.BodyHandle))
        {
            body = default;
            return false;
        }

        body = Simulation.Bodies[reference.BodyHandle];
        return true;
    }

    public void SetKinematicPose(PhysicsBodyReference reference, Transform transform, float deltaTime)
    {
        if (reference.Kind != PhysicsBodyKind.Kinematic ||
            !TryGetBody(reference, out BodyReference body))
        {
            return;
        }

        NumericVector3 nextPosition = PhysicsConvert.ToFloat(transform.Position);
        NumericQuaternion nextRotation = PhysicsConvert.ToFloat(transform.Rotation);
        NumericVector3 previousPosition = body.Pose.Position;
        body.Pose.Position = nextPosition;
        body.Pose.Orientation = nextRotation;
        body.Velocity.Linear = (nextPosition - previousPosition) / MathF.Max(0.0001f, deltaTime);
        body.Velocity.Angular = NumericVector3.Zero;
        body.Awake = true;
    }

    public void RemoveBody(PhysicsBodyReference reference)
    {
        if (!reference.Active)
        {
            return;
        }

        if (reference.Kind == PhysicsBodyKind.Static)
        {
            _materials.Remove(new CollidableReference(reference.StaticHandle));
            if (Simulation.Statics.StaticExists(reference.StaticHandle))
            {
                Simulation.Statics.Remove(reference.StaticHandle);
            }

            Simulation.Shapes.Remove(reference.ShapeIndex);
            return;
        }

        CollidableMobility mobility = reference.Kind == PhysicsBodyKind.Kinematic
            ? CollidableMobility.Kinematic
            : CollidableMobility.Dynamic;
        _materials.Remove(new CollidableReference(mobility, reference.BodyHandle));
        if (Simulation.Bodies.BodyExists(reference.BodyHandle))
        {
            Simulation.Bodies.Remove(reference.BodyHandle);
        }

        Simulation.Shapes.Remove(reference.ShapeIndex);
    }

    public bool IsGrounded(
        PhysicsBodyReference reference,
        NumericVector3 origin,
        float distance,
        float minimumNormalY)
    {
        if (distance <= 0)
        {
            return false;
        }

        NumericVector3 direction = new(0, -1, 0);
        var handler = new GroundRayHitHandler(reference.BodyHandle, minimumNormalY);
        Simulation.RayCast(in origin, in direction, distance, ref handler);
        return handler.Hit;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Simulation.Dispose();
        Pool.Clear();
    }

    public void DisposeInject()
    {
        Dispose();
    }

    private TypedIndex AddShape(Collider collider)
    {
        return collider.Shape switch
        {
            ColliderShape.Box => Simulation.Shapes.Add(CreateBox(collider)),
            ColliderShape.Sphere => Simulation.Shapes.Add(CreateSphere(collider)),
            ColliderShape.Capsule => Simulation.Shapes.Add(CreateCapsule(collider)),
            _ => Simulation.Shapes.Add(CreateBox(collider))
        };
    }

    private static BodyInertia ComputeInertia(Collider collider, float mass)
    {
        return collider.Shape switch
        {
            ColliderShape.Box => CreateBox(collider).ComputeInertia(mass),
            ColliderShape.Sphere => CreateSphere(collider).ComputeInertia(mass),
            ColliderShape.Capsule => CreateCapsule(collider).ComputeInertia(mass),
            _ => CreateBox(collider).ComputeInertia(mass)
        };
    }

    private static Box CreateBox(Collider collider)
    {
        NumericVector3 size = PhysicsConvert.ToFloat(collider.Size);
        return new Box(
            MathF.Max(MinimumShapeSize, MathF.Abs(size.X)),
            MathF.Max(MinimumShapeSize, MathF.Abs(size.Y)),
            MathF.Max(MinimumShapeSize, MathF.Abs(size.Z)));
    }

    private static Sphere CreateSphere(Collider collider)
    {
        return new Sphere(MathF.Max(MinimumShapeSize, MathF.Abs(PhysicsConvert.ToFloat(collider.Radius))));
    }

    private static Capsule CreateCapsule(Collider collider)
    {
        return new Capsule(
            MathF.Max(MinimumShapeSize, MathF.Abs(PhysicsConvert.ToFloat(collider.Radius))),
            MathF.Max(MinimumShapeSize, MathF.Abs(PhysicsConvert.ToFloat(collider.Length))));
    }

    private static RigidPose ToPose(Transform transform)
    {
        NumericVector3 position = PhysicsConvert.ToFloat(transform.Position);
        NumericQuaternion rotation = PhysicsConvert.ToFloat(transform.Rotation);
        return new RigidPose(position, rotation);
    }

    private struct GroundRayHitHandler : IRayHitHandler
    {
        private readonly BodyHandle _ignoredBody;
        private readonly float _minimumNormalY;

        public GroundRayHitHandler(BodyHandle ignoredBody, float minimumNormalY)
        {
            _ignoredBody = ignoredBody;
            _minimumNormalY = minimumNormalY;
            Hit = false;
        }

        public bool Hit { get; private set; }

        public bool AllowTest(CollidableReference collidable)
        {
            return collidable.Mobility != CollidableMobility.Dynamic ||
                collidable.BodyHandle.Value != _ignoredBody.Value;
        }

        public bool AllowTest(CollidableReference collidable, int childIndex)
        {
            return AllowTest(collidable);
        }

        public void OnRayHit(
            in RayData ray,
            ref float maximumT,
            float t,
            in NumericVector3 normal,
            CollidableReference collidable,
            int childIndex)
        {
            if (normal.Y < _minimumNormalY)
            {
                return;
            }

            Hit = true;
            maximumT = t;
        }
    }
}
