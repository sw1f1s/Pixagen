using Pixagen.Core.App;
using Pixagen.Core.Input;
using Pixagen.Core.Performance;
using Pixagen.Core.Runtime;
using Pixagen.Core.Timing;
using Pixagen.Game.Features.PhysicsFeature.Runtime;
using Pixagen.Ecs.Runtime;
using Pixagen.Ecs.DI;

namespace Pixagen.Game.Features.PhysicsFeature.Systems;

public sealed class PhysicsStepSystem : IFixedUpdateSystem
{
    private readonly CustomInject<Time> _time = default;
    private readonly CustomInject<PhysicsWorld> _physicsWorld = default;

    public void FixedUpdate()
    {
        float deltaTime = PhysicsConvert.ToFloat(_time.Value.FixedDeltaTime);
        _physicsWorld.Value.Step(deltaTime);
    }
}
