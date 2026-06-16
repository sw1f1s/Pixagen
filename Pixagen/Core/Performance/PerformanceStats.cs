using System.Diagnostics;

namespace Pixagen.Core.Performance;

public sealed class PerformanceStats
{
    private readonly object _sync = new();
    private readonly long _memoryRefreshIntervalTicks;
    private readonly long _fpsRefreshIntervalTicks;
    private PerformanceSnapshot _snapshot = PerformanceSnapshot.Empty;
    private long _frameStartTicks;
    private long _fpsWindowStartTicks;
    private long _lastMemoryRefreshTicks;
    private int _fpsWindowFrames;
    private ulong _frameCount;
    private bool _isFrameActive;

    public PerformanceStats()
        : this(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(250))
    {
    }

    public PerformanceStats(TimeSpan memoryRefreshInterval)
        : this(memoryRefreshInterval, TimeSpan.FromMilliseconds(250))
    {
    }

    public PerformanceStats(TimeSpan memoryRefreshInterval, TimeSpan fpsRefreshInterval)
    {
        _memoryRefreshIntervalTicks = memoryRefreshInterval <= TimeSpan.Zero
            ? 0
            : Math.Max(1, (long)(Stopwatch.Frequency * memoryRefreshInterval.TotalSeconds));
        _fpsRefreshIntervalTicks = fpsRefreshInterval <= TimeSpan.Zero
            ? 0
            : Math.Max(1, (long)(Stopwatch.Frequency * fpsRefreshInterval.TotalSeconds));
    }

    public PerformanceSnapshot Snapshot
    {
        get
        {
            lock (_sync)
            {
                return _snapshot;
            }
        }
    }

    public static long Timestamp => Stopwatch.GetTimestamp();

    public static double TicksToMilliseconds(long ticks)
    {
        return Math.Max(0, ticks) * 1000.0 / Stopwatch.Frequency;
    }

    public PerformanceFrameScope BeginFrame()
    {
        long startTicks = Timestamp;
        lock (_sync)
        {
            _frameStartTicks = startTicks;
            _isFrameActive = true;
        }

        return new PerformanceFrameScope(this, startTicks);
    }

    public void EndFrame()
    {
        long startTicks;
        lock (_sync)
        {
            if (!_isFrameActive)
            {
                return;
            }

            startTicks = _frameStartTicks;
            _isFrameActive = false;
        }

        RecordFrame(startTicks, Timestamp);
        RefreshMemory();
    }

    public void EndFrame(long startTicks)
    {
        long endTicks = Timestamp;
        lock (_sync)
        {
            if (_isFrameActive && _frameStartTicks == startTicks)
            {
                _isFrameActive = false;
            }
        }

        RecordFrame(startTicks, endTicks);
        RefreshMemory();
    }

    public double ElapsedMillisecondsSince(long startTicks)
    {
        return TicksToMilliseconds(Timestamp - startTicks);
    }

    public void RecordCpuFrame(double frameMilliseconds)
    {
        lock (_sync)
        {
            _snapshot = _snapshot with
            {
                CpuFrameMilliseconds = Math.Max(0, frameMilliseconds)
            };
        }
    }

    public void RecordFrame(double frameMilliseconds)
    {
        double safeFrameMilliseconds = Math.Max(0, frameMilliseconds);
        double instantFramesPerSecond = safeFrameMilliseconds > 0
            ? 1000.0 / safeFrameMilliseconds
            : 0;

        lock (_sync)
        {
            RecordFrameUnsafe(safeFrameMilliseconds, instantFramesPerSecond, 0, Timestamp, forceFpsRefresh: true);
        }
    }

    public void RecordGpuFrame(double frameMilliseconds)
    {
        lock (_sync)
        {
            _snapshot = _snapshot with
            {
                GpuFrameMilliseconds = Math.Max(0, frameMilliseconds)
            };
        }
    }

    public void RecordGpuFrameSince(long startTicks)
    {
        RecordGpuFrame(ElapsedMillisecondsSince(startTicks));
    }

    public void RecordRenderScene(
        int triangles,
        int shadowTriangles,
        int textureCount,
        long textureBytes)
    {
        RecordRenderScene(new RenderPerformanceReport(
            triangles,
            shadowTriangles,
            textureCount,
            textureBytes));
    }

    public void RecordRenderScene(RenderPerformanceReport report)
    {
        lock (_sync)
        {
            _snapshot = _snapshot with
            {
                Triangles = Math.Max(0, report.Triangles),
                ShadowTriangles = Math.Max(0, report.ShadowTriangles),
                TextureCount = Math.Max(0, report.TextureCount),
                TextureBytes = Math.Max(0, report.TextureBytes)
            };
        }
    }

    public void RecordBackendFrame(int drawCalls, int passes, long vramBytes)
    {
        RecordBackendFrame(new BackendPerformanceReport(drawCalls, passes, vramBytes));
    }

    public void RecordBackendFrame(BackendPerformanceReport report)
    {
        lock (_sync)
        {
            _snapshot = _snapshot with
            {
                DrawCalls = Math.Max(0, report.DrawCalls),
                Passes = Math.Max(0, report.Passes),
                VramBytes = Math.Max(0, report.VramBytes)
            };
        }
    }

    public void RecordMemory(long managedBytes, long workingSetBytes, long privateBytes, long gcHeapBytes)
    {
        RecordMemory(new MemoryPerformanceReport(managedBytes, workingSetBytes, privateBytes, gcHeapBytes));
    }

    public void RecordMemory(MemoryPerformanceReport report)
    {
        lock (_sync)
        {
            _snapshot = _snapshot with
            {
                ManagedMemoryBytes = Math.Max(0, report.ManagedBytes),
                WorkingSetBytes = Math.Max(0, report.WorkingSetBytes),
                PrivateMemoryBytes = Math.Max(0, report.PrivateBytes),
                GcHeapBytes = Math.Max(0, report.GcHeapBytes)
            };
        }
    }

    public void RefreshMemory(bool force = false)
    {
        long now = Timestamp;
        lock (_sync)
        {
            if (!force &&
                _memoryRefreshIntervalTicks > 0 &&
                now - _lastMemoryRefreshTicks < _memoryRefreshIntervalTicks)
            {
                return;
            }

            _lastMemoryRefreshTicks = now;
        }

        RecordMemory(CaptureMemory());
    }

    private static MemoryPerformanceReport CaptureMemory()
    {
        using Process process = Process.GetCurrentProcess();
        GCMemoryInfo gcMemory = GC.GetGCMemoryInfo();
        return new MemoryPerformanceReport(
            GC.GetTotalMemory(forceFullCollection: false),
            process.WorkingSet64,
            process.PrivateMemorySize64,
            gcMemory.HeapSizeBytes);
    }

    private void RecordFrame(long startTicks, long endTicks)
    {
        double frameMilliseconds = TicksToMilliseconds(endTicks - startTicks);
        double instantFramesPerSecond = frameMilliseconds > 0
            ? 1000.0 / frameMilliseconds
            : 0;

        lock (_sync)
        {
            if (_fpsWindowStartTicks == 0)
            {
                _fpsWindowStartTicks = startTicks;
            }

            RecordFrameUnsafe(
                frameMilliseconds,
                instantFramesPerSecond,
                Math.Max(0, endTicks - _fpsWindowStartTicks),
                endTicks,
                forceFpsRefresh: false);
        }
    }

    private void RecordFrameUnsafe(
        double frameMilliseconds,
        double instantFramesPerSecond,
        long fpsWindowTicks,
        long fpsWindowEndTicks,
        bool forceFpsRefresh)
    {
        _frameCount++;
        _fpsWindowFrames++;

        double framesPerSecond = _snapshot.FramesPerSecond;
        if (forceFpsRefresh || _fpsRefreshIntervalTicks <= 0 || fpsWindowTicks >= _fpsRefreshIntervalTicks)
        {
            framesPerSecond = fpsWindowTicks > 0
                ? _fpsWindowFrames * (double)Stopwatch.Frequency / fpsWindowTicks
                : instantFramesPerSecond;
            _fpsWindowStartTicks = fpsWindowEndTicks;
            _fpsWindowFrames = 0;
        }
        else if (framesPerSecond <= 0)
        {
            framesPerSecond = instantFramesPerSecond;
        }

        _snapshot = _snapshot with
        {
            CpuFrameMilliseconds = frameMilliseconds,
            FramesPerSecond = Math.Max(0, framesPerSecond),
            InstantFramesPerSecond = Math.Max(0, instantFramesPerSecond),
            FrameCount = _frameCount
        };
    }
}
