namespace Pixagen.Benchmark;

public enum BenchmarkMeasurementMode
{
    SteadyState,
    SetupOnly
}

public interface IBenchmarkScenario
{
    string Name { get; }
    string Description { get; }
    BenchmarkMeasurementMode Mode { get; }
    IBenchmarkCase Create(BenchmarkContext context);
}

public interface IBenchmarkCase : IDisposable
{
    string Name { get; }
    int EntityCount { get; }
    void Step();
    IReadOnlyDictionary<string, double> GetCounters();
}

public readonly record struct BenchmarkContext(BenchmarkConfig Config, int EntityCount);
