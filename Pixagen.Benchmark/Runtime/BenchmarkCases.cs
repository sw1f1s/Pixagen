using Pixagen.Ecs.Runtime;

namespace Pixagen.Benchmark;

public sealed class SystemsBenchmarkCase : IBenchmarkCase
{
    private readonly EngineBenchmarkContext _context;
    private readonly Systems _systems;
    private readonly Action<EngineBenchmarkContext>? _beforeUpdate;
    private readonly Func<EngineBenchmarkContext, IReadOnlyDictionary<string, double>> _counters;
    private bool _disposed;

    public SystemsBenchmarkCase(
        string name,
        int entityCount,
        EngineBenchmarkContext context,
        Systems systems,
        Action<EngineBenchmarkContext>? beforeUpdate = null,
        Func<EngineBenchmarkContext, IReadOnlyDictionary<string, double>>? counters = null)
    {
        Name = name;
        EntityCount = entityCount;
        _context = context;
        _systems = systems;
        _beforeUpdate = beforeUpdate;
        _counters = counters ?? (ctx => ctx.CommonCounters());
    }

    public string Name { get; }
    public int EntityCount { get; }

    public void Step()
    {
        _context.AdvanceFrame();
        _beforeUpdate?.Invoke(_context);
        _systems.Update();
    }

    public IReadOnlyDictionary<string, double> GetCounters()
    {
        return _counters(_context);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _systems.Dispose();
        _context.Dispose();
    }
}

public sealed class DelegateBenchmarkCase : IBenchmarkCase
{
    private readonly Action _step;
    private readonly Func<IReadOnlyDictionary<string, double>> _counters;
    private readonly Action _dispose;
    private bool _disposed;

    public DelegateBenchmarkCase(
        string name,
        int entityCount,
        Action step,
        Func<IReadOnlyDictionary<string, double>> counters,
        Action dispose)
    {
        Name = name;
        EntityCount = entityCount;
        _step = step;
        _counters = counters;
        _dispose = dispose;
    }

    public string Name { get; }
    public int EntityCount { get; }

    public void Step()
    {
        _step();
    }

    public IReadOnlyDictionary<string, double> GetCounters()
    {
        return _counters();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _dispose();
    }
}

public sealed class SetupOnlyBenchmarkCase : IBenchmarkCase
{
    private readonly EngineBenchmarkContext _context;
    private readonly Func<EngineBenchmarkContext, IReadOnlyDictionary<string, double>> _counters;
    private bool _disposed;

    public SetupOnlyBenchmarkCase(
        string name,
        int entityCount,
        EngineBenchmarkContext context,
        Func<EngineBenchmarkContext, IReadOnlyDictionary<string, double>> counters)
    {
        Name = name;
        EntityCount = entityCount;
        _context = context;
        _counters = counters;
    }

    public string Name { get; }
    public int EntityCount { get; }

    public void Step()
    {
    }

    public IReadOnlyDictionary<string, double> GetCounters()
    {
        return _counters(_context);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _context.Dispose();
    }
}
