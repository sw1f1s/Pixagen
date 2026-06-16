using System.Diagnostics;

namespace Pixagen.Benchmark;

public sealed class BenchmarkRunner
{
    private readonly BenchmarkConfig _config;
    private readonly IReadOnlyList<IBenchmarkScenario> _scenarios;

    public BenchmarkRunner(BenchmarkConfig config, IReadOnlyList<IBenchmarkScenario> scenarios)
    {
        _config = config;
        _scenarios = scenarios;
    }

    public IReadOnlyList<BenchmarkResult> Run()
    {
        IReadOnlyList<IBenchmarkScenario> selected = SelectScenarios();
        var results = new List<BenchmarkResult>(selected.Count * _config.EntityCounts.Length);

        Console.WriteLine($"Pixagen.Benchmark | frames={_config.MeasureFrames}, warmup={_config.WarmupFrames}, render={_config.RenderWidth}x{_config.RenderHeight}");
        Console.WriteLine($".NET {Environment.Version} | {Environment.ProcessorCount} logical CPUs | {GCSettingsLabel()} GC");
        Console.WriteLine();

        foreach (IBenchmarkScenario scenario in selected)
        {
            foreach (int entityCount in _config.EntityCounts)
            {
                Console.Write($"{scenario.Name} / {entityCount:n0} entities ... ");
                BenchmarkResult result = RunOne(scenario, entityCount);
                results.Add(result);
                Console.WriteLine(result.Error is null ? "ok" : "failed");
            }
        }

        Console.WriteLine();
        return results;
    }

    private BenchmarkResult RunOne(IBenchmarkScenario scenario, int entityCount)
    {
        var context = new BenchmarkContext(_config, entityCount);
        try
        {
            if (_config.ForceFullCollection)
            {
                ForceFullCollection();
            }

            using var process = Process.GetCurrentProcess();
            ResourceSnapshot beforeSetup = ResourceSnapshot.Take(process);
            long setupAllocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            long setupTimestamp = Stopwatch.GetTimestamp();

            using IBenchmarkCase benchmarkCase = scenario.Create(context);

            double setupMilliseconds = ElapsedMilliseconds(setupTimestamp);
            long setupAllocatedBytes = GC.GetTotalAllocatedBytes(precise: true) - setupAllocatedBefore;
            ResourceSnapshot afterSetup = ResourceSnapshot.Take(process);

            if (scenario.Mode == BenchmarkMeasurementMode.SetupOnly)
            {
                return BenchmarkResult.SetupOnly(
                    scenario.Name,
                    scenario.Description,
                    entityCount,
                    setupMilliseconds,
                    setupAllocatedBytes,
                    beforeSetup,
                    afterSetup,
                    benchmarkCase.GetCounters());
            }

            for (int i = 0; i < _config.WarmupFrames; i++)
            {
                benchmarkCase.Step();
            }

            if (_config.ForceFullCollection)
            {
                ForceFullCollection();
            }

            process.Refresh();
            TimeSpan cpuBefore = process.TotalProcessorTime;
            ResourceSnapshot beforeMeasure = ResourceSnapshot.Take(process);
            long allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            int gen0Before = GC.CollectionCount(0);
            int gen1Before = GC.CollectionCount(1);
            int gen2Before = GC.CollectionCount(2);
            var samples = new double[_config.MeasureFrames];
            long measuredStart = Stopwatch.GetTimestamp();

            for (int i = 0; i < samples.Length; i++)
            {
                long frameStart = Stopwatch.GetTimestamp();
                benchmarkCase.Step();
                samples[i] = ElapsedMilliseconds(frameStart);
            }

            double measuredMilliseconds = ElapsedMilliseconds(measuredStart);
            process.Refresh();
            TimeSpan cpuAfter = process.TotalProcessorTime;
            ResourceSnapshot afterMeasure = ResourceSnapshot.Take(process);

            return BenchmarkResult.Measured(
                scenario.Name,
                scenario.Description,
                entityCount,
                setupMilliseconds,
                setupAllocatedBytes,
                samples,
                measuredMilliseconds,
                (cpuAfter - cpuBefore).TotalMilliseconds,
                GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore,
                GC.CollectionCount(0) - gen0Before,
                GC.CollectionCount(1) - gen1Before,
                GC.CollectionCount(2) - gen2Before,
                beforeSetup,
                afterSetup,
                beforeMeasure,
                afterMeasure,
                benchmarkCase.GetCounters());
        }
        catch (Exception error)
        {
            return BenchmarkResult.Failed(
                scenario.Name,
                scenario.Description,
                entityCount,
                error);
        }
    }

    private IReadOnlyList<IBenchmarkScenario> SelectScenarios()
    {
        if (_config.ScenarioNames.Length == 0)
        {
            return _scenarios;
        }

        var selected = new List<IBenchmarkScenario>();
        foreach (string requested in _config.ScenarioNames)
        {
            IBenchmarkScenario? scenario = _scenarios.FirstOrDefault(item =>
                string.Equals(item.Name, requested, StringComparison.OrdinalIgnoreCase));
            if (scenario is null)
            {
                throw new InvalidOperationException($"Benchmark scenario '{requested}' was not found. Use --list to inspect available scenarios.");
            }

            selected.Add(scenario);
        }

        return selected;
    }

    private static void ForceFullCollection()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    private static double ElapsedMilliseconds(long startTimestamp)
    {
        return (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
    }

    private static string GCSettingsLabel()
    {
        return System.Runtime.GCSettings.IsServerGC ? "server" : "workstation";
    }
}
