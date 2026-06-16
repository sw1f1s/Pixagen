using Pixagen.Ecs.Runtime;
using Pixagen.Tests.TestSupport;

namespace Pixagen.Tests.Features.SharedFeature;

public sealed class SystemPhaseTests
{
    [Fact]
    public void SystemsUpdate_RunsFixedByAccumulatorBeforeUpdateAndLateAfterAllUpdates()
    {
        using var context = new EcsTestContext();
        var events = new List<string>();
        context.SetDeltaTime(Fix.One / new Fix(30));

        var systems = context.BuildSystems(
            new PhaseSystem("phase", events),
            new UpdateOnlySystem("update-only", events));

        systems.Update();

        Assert.Equal(
            [
                "phase:pre",
                "phase:fixed",
                "phase:fixed",
                "phase:update",
                "update-only:update",
                "phase:late",
            ],
            events);
        Assert.Equal(2UL, context.Time.FixedFrameIndex);
    }

    private sealed class PhaseSystem(string name, List<string> events) : IPreUpdateSystem, IFixedUpdateSystem, IUpdateSystem, ILateUpdateSystem
    {
        public void PreUpdate()
        {
            events.Add($"{name}:pre");
        }

        public void FixedUpdate()
        {
            events.Add($"{name}:fixed");
        }

        public void Update()
        {
            events.Add($"{name}:update");
        }

        public void LateUpdate()
        {
            events.Add($"{name}:late");
        }
    }

    private sealed class UpdateOnlySystem(string name, List<string> events) : IUpdateSystem
    {
        public void Update()
        {
            events.Add($"{name}:update");
        }
    }
}
