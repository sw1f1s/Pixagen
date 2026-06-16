using System.Diagnostics;

namespace Pixagen.Benchmark;

public sealed class BenchmarkResult
{
    private BenchmarkResult(
        string scenario,
        string description,
        int entityCount,
        double setupMilliseconds,
        long setupAllocatedBytes,
        double[] frameMilliseconds,
        double measuredMilliseconds,
        double cpuMilliseconds,
        long allocatedBytes,
        int gen0Collections,
        int gen1Collections,
        int gen2Collections,
        ResourceSnapshot setupBefore,
        ResourceSnapshot setupAfter,
        ResourceSnapshot measureBefore,
        ResourceSnapshot measureAfter,
        IReadOnlyDictionary<string, double> counters,
        Exception? error)
    {
        Scenario = scenario;
        Description = description;
        EntityCount = entityCount;
        SetupMilliseconds = setupMilliseconds;
        SetupAllocatedBytes = setupAllocatedBytes;
        FrameMilliseconds = frameMilliseconds;
        MeasuredMilliseconds = measuredMilliseconds;
        CpuMilliseconds = cpuMilliseconds;
        AllocatedBytes = allocatedBytes;
        Gen0Collections = gen0Collections;
        Gen1Collections = gen1Collections;
        Gen2Collections = gen2Collections;
        SetupBefore = setupBefore;
        SetupAfter = setupAfter;
        MeasureBefore = measureBefore;
        MeasureAfter = measureAfter;
        Counters = counters;
        Error = error;
    }

    public string Scenario { get; }
    public string Description { get; }
    public int EntityCount { get; }
    public double SetupMilliseconds { get; }
    public long SetupAllocatedBytes { get; }
    public double[] FrameMilliseconds { get; }
    public int Frames => FrameMilliseconds.Length;
    public double MeasuredMilliseconds { get; }
    public double CpuMilliseconds { get; }
    public long AllocatedBytes { get; }
    public int Gen0Collections { get; }
    public int Gen1Collections { get; }
    public int Gen2Collections { get; }
    public ResourceSnapshot SetupBefore { get; }
    public ResourceSnapshot SetupAfter { get; }
    public ResourceSnapshot MeasureBefore { get; }
    public ResourceSnapshot MeasureAfter { get; }
    public IReadOnlyDictionary<string, double> Counters { get; }
    public Exception? Error { get; }

    public double MeanMilliseconds => Frames == 0 ? 0 : FrameMilliseconds.Average();
    public double P95Milliseconds => Percentile(0.95);
    public double WorstMilliseconds => Frames == 0 ? 0 : FrameMilliseconds.Max();
    public double FramesPerSecond => MeanMilliseconds <= 0 ? 0 : 1000.0 / MeanMilliseconds;
    public double AllocatedBytesPerFrame => Frames == 0 ? 0 : AllocatedBytes / (double)Frames;
    public double CpuPercentAllCores => MeasuredMilliseconds <= 0 ? 0 : CpuMilliseconds / MeasuredMilliseconds / Environment.ProcessorCount * 100.0;
    public double CpuPercentSingleCore => MeasuredMilliseconds <= 0 ? 0 : CpuMilliseconds / MeasuredMilliseconds * 100.0;

    public static BenchmarkResult Measured(
        string scenario,
        string description,
        int entityCount,
        double setupMilliseconds,
        long setupAllocatedBytes,
        double[] frameMilliseconds,
        double measuredMilliseconds,
        double cpuMilliseconds,
        long allocatedBytes,
        int gen0Collections,
        int gen1Collections,
        int gen2Collections,
        ResourceSnapshot setupBefore,
        ResourceSnapshot setupAfter,
        ResourceSnapshot measureBefore,
        ResourceSnapshot measureAfter,
        IReadOnlyDictionary<string, double> counters)
    {
        return new BenchmarkResult(
            scenario,
            description,
            entityCount,
            setupMilliseconds,
            setupAllocatedBytes,
            frameMilliseconds,
            measuredMilliseconds,
            cpuMilliseconds,
            allocatedBytes,
            gen0Collections,
            gen1Collections,
            gen2Collections,
            setupBefore,
            setupAfter,
            measureBefore,
            measureAfter,
            counters,
            null);
    }

    public static BenchmarkResult SetupOnly(
        string scenario,
        string description,
        int entityCount,
        double setupMilliseconds,
        long setupAllocatedBytes,
        ResourceSnapshot setupBefore,
        ResourceSnapshot setupAfter,
        IReadOnlyDictionary<string, double> counters)
    {
        return new BenchmarkResult(
            scenario,
            description,
            entityCount,
            setupMilliseconds,
            setupAllocatedBytes,
            [],
            0,
            0,
            0,
            0,
            0,
            0,
            setupBefore,
            setupAfter,
            setupAfter,
            setupAfter,
            counters,
            null);
    }

    public static BenchmarkResult Failed(string scenario, string description, int entityCount, Exception error)
    {
        ResourceSnapshot empty = ResourceSnapshot.Empty;
        return new BenchmarkResult(
            scenario,
            description,
            entityCount,
            0,
            0,
            [],
            0,
            0,
            0,
            0,
            0,
            0,
            empty,
            empty,
            empty,
            empty,
            new Dictionary<string, double>(),
            error);
    }

    private double Percentile(double percentile)
    {
        if (Frames == 0)
        {
            return 0;
        }

        double[] sorted = new double[FrameMilliseconds.Length];
        Array.Copy(FrameMilliseconds, sorted, FrameMilliseconds.Length);
        Array.Sort(sorted);
        int index = Math.Clamp((int)Math.Ceiling(percentile * sorted.Length) - 1, 0, sorted.Length - 1);
        return sorted[index];
    }
}

public readonly record struct ResourceSnapshot(
    long ManagedBytes,
    long HeapBytes,
    long WorkingSetBytes,
    long PrivateBytes,
    long PeakWorkingSetBytes)
{
    public static ResourceSnapshot Empty => new(0, 0, 0, 0, 0);

    public static ResourceSnapshot Take(Process process)
    {
        process.Refresh();
        GCMemoryInfo gcInfo = GC.GetGCMemoryInfo();
        return new ResourceSnapshot(
            GC.GetTotalMemory(forceFullCollection: false),
            gcInfo.HeapSizeBytes,
            process.WorkingSet64,
            process.PrivateMemorySize64,
            process.PeakWorkingSet64);
    }
}
