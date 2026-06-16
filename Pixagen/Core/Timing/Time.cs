using System.Diagnostics;

namespace Pixagen.Core.Timing;

public sealed class Time
{
    private static readonly Fix MaxDeltaTime = Fix.One / new Fix(15);
    private static readonly Fix DefaultFixedDeltaTime = Fix.One / new Fix(60);

    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private TimeSpan _lastTick;
    private Fix _fixedAccumulator;

    public Fix DeltaTime { get; private set; }
    public Fix FixedDeltaTime { get; private set; } = DefaultFixedDeltaTime;
    public Fix ElapsedTime { get; private set; }
    public Fix FixedTime { get; private set; }
    public ulong FrameIndex { get; private set; }
    public ulong FixedFrameIndex { get; private set; }
    public int MaxFixedStepsPerFrame { get; private set; } = 4;

    public void Tick()
    {
        TimeSpan now = _stopwatch.Elapsed;
        long deltaTicks = Math.Max((now - _lastTick).Ticks, 1L);
        Fix deltaTime = (Fix)deltaTicks / (Fix)TimeSpan.TicksPerSecond;
        Advance(deltaTime, (Fix)now.Ticks / (Fix)TimeSpan.TicksPerSecond);
        _lastTick = now;
    }

    public void Advance(Fix deltaTime)
    {
        Advance(deltaTime, ElapsedTime + ClampDeltaTime(deltaTime));
    }

    internal int ConsumeFixedSteps()
    {
        if (FixedDeltaTime <= Fix.Zero || MaxFixedStepsPerFrame <= 0)
        {
            return 0;
        }

        int steps = 0;
        while (_fixedAccumulator >= FixedDeltaTime && steps < MaxFixedStepsPerFrame)
        {
            _fixedAccumulator -= FixedDeltaTime;
            FixedTime += FixedDeltaTime;
            FixedFrameIndex++;
            steps++;
        }

        return steps;
    }

    private void Advance(Fix deltaTime, Fix elapsedTime)
    {
        DeltaTime = ClampDeltaTime(deltaTime);
        ElapsedTime = elapsedTime;
        _fixedAccumulator += DeltaTime;
        FrameIndex++;
    }

    private static Fix ClampDeltaTime(Fix deltaTime)
    {
        if (deltaTime <= Fix.Zero)
        {
            return Fix.Zero;
        }

        return deltaTime > MaxDeltaTime ? MaxDeltaTime : deltaTime;
    }
}
