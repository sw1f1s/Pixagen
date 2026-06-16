namespace Pixagen.Core.Performance;

public readonly struct PerformanceFrameScope : IDisposable
{
    private readonly PerformanceStats? _stats;

    internal PerformanceFrameScope(PerformanceStats stats, long startTicks)
    {
        _stats = stats;
        StartTicks = startTicks;
    }

    public long StartTicks { get; }

    public void Dispose()
    {
        _stats?.EndFrame(StartTicks);
    }
}
