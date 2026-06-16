using System.Globalization;
using Pixagen.Game.Features.SharedFeature.Helper;
using Pixagen.Game.Features.UIFeature.Components;
using Pixagen.Ecs.DI;

namespace Pixagen.Game.Features.UIFeature.Systems;

public sealed class ProfilerUISystem : IUpdateSystem
{
    private readonly CustomInject<Time> _time = default;
    private readonly CustomInject<PerformanceStats> _performanceStats = default;
    private readonly FilterInject<Include<ProfilerUI, TextUI>> _profilers = default;
    private readonly CustomInject<EntityStateHelper> _entityState = default;
    private readonly ComponentInject<ProfilerUI> _profilerComponents = default;
    private readonly ComponentInject<TextUI> _texts = default;

    public void Update()
    {
        Fix dt = _time.Value.DeltaTime;

        foreach (Entity entity in _profilers.Value)
        {
            if (!_entityState.Value.IsEnabled(entity))
            {
                continue;
            }

            ref ProfilerUI profiler = ref _profilerComponents.Get(entity);
            ref TextUI text = ref _texts.Get(entity);

            profiler.Elapsed += dt;
            if (profiler.UpdateInterval > Fix.Zero &&
                profiler.Elapsed < profiler.UpdateInterval &&
                !string.IsNullOrEmpty(text.Value))
            {
                continue;
            }

            profiler.Elapsed = Fix.Zero;
            text.Value = Format(_performanceStats.Value.Snapshot);
        }
    }

    private static string Format(PerformanceSnapshot stats)
    {
        return string.Create(CultureInfo.InvariantCulture, $"""
CPU   {stats.CpuFrameMilliseconds,6:0.00} MS
GPU   {stats.GpuFrameMilliseconds,6:0.00} MS
FPS   {stats.FramesPerSecond,6:0.0}
CALLS {stats.DrawCalls,6}
TRIS  {stats.Triangles,6} / SH {stats.ShadowTriangles}
VRAM  {FormatBytes(stats.VramBytes),9}
TEX   {stats.TextureCount,6} / {FormatBytes(stats.TextureBytes)}
MEM   {FormatBytes(stats.ManagedMemoryBytes),9}
PROC  {FormatBytes(stats.WorkingSetBytes),9}
HEAP  {FormatBytes(stats.GcHeapBytes),9}
""");
    }

    private static string FormatBytes(long bytes)
    {
        const double kib = 1024.0;
        const double mib = kib * 1024.0;

        if (bytes >= mib)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{bytes / mib:0.0} MB");
        }

        if (bytes >= kib)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{bytes / kib:0.0} KB");
        }

        return string.Create(CultureInfo.InvariantCulture, $"{bytes} B");
    }
}
