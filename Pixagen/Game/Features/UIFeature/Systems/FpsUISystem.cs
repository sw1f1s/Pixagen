using Pixagen.Core.App;
using Pixagen.Core.Input;
using Pixagen.Core.Performance;
using Pixagen.Core.Runtime;
using Pixagen.Core.Timing;
using Pixagen.Game.Features.SharedFeature.Helper;
using Pixagen.Game.Features.UIFeature.Components;
using Pixagen.Ecs.Runtime;
using Pixagen.Ecs.DI;

namespace Pixagen.Game.Features.UIFeature.Systems;

public sealed class FpsUISystem : IUpdateSystem
{
    private readonly CustomInject<PerformanceStats> _performanceStats = default;
    private readonly FilterInject<Include<FPSCounterUI, TextUI>> _counters = default;
    private readonly CustomInject<EntityStateHelper> _entityState = default;
    private readonly ComponentInject<TextUI> _texts = default;

    public void Update()
    {
        PerformanceSnapshot snapshot = _performanceStats.Value.Snapshot;
        int displayFps = Math.Max(0, (int)Math.Round(snapshot.FramesPerSecond));

        foreach (Entity entity in _counters.Value)
        {
            if (!_entityState.Value.IsEnabled(entity))
            {
                continue;
            }

            ref TextUI text = ref _texts.Get(entity);
            text.Value = $"FPS {displayFps,4}";
        }
    }
}
