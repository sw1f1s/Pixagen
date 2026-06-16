using Pixagen.Benchmark;

BenchmarkConfig config;
try
{
    config = BenchmarkConfig.Parse(args);
}
catch (Exception error)
{
    Console.Error.WriteLine(error.Message);
    Console.Error.WriteLine();
    BenchmarkConfig.PrintHelp();
    return 2;
}

if (config.ShowHelp)
{
    BenchmarkConfig.PrintHelp();
    return 0;
}

IReadOnlyList<IBenchmarkScenario> scenarios = BenchmarkCatalog.Create();
if (config.ListScenarios)
{
    foreach (IBenchmarkScenario scenario in scenarios)
    {
        Console.WriteLine($"{scenario.Name,-24} {scenario.Description}");
    }

    return 0;
}

var runner = new BenchmarkRunner(config, scenarios);
IReadOnlyList<BenchmarkResult> results = runner.Run();
BenchmarkReport.Print(results, config);
BenchmarkReport.WriteFiles(results, config);

return results.Any(result => result.Error is not null) ? 1 : 0;
