using System.Globalization;
using System.Text;

namespace Pixagen.Benchmark;

public static class BenchmarkReport
{
    public static void Print(IReadOnlyList<BenchmarkResult> results, BenchmarkConfig config)
    {
        string markdown = BuildMarkdown(results, config);
        Console.WriteLine(markdown);
    }

    public static void WriteFiles(IReadOnlyList<BenchmarkResult> results, BenchmarkConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.MarkdownPath))
        {
            File.WriteAllText(config.MarkdownPath, BuildMarkdown(results, config));
        }

        if (!string.IsNullOrWhiteSpace(config.CsvPath))
        {
            File.WriteAllText(config.CsvPath, BuildCsv(results));
        }
    }

    public static string BuildMarkdown(IReadOnlyList<BenchmarkResult> results, BenchmarkConfig config)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Pixagen Benchmark");
        builder.AppendLine();
        builder.AppendLine(CultureInfo.InvariantCulture, $"- Frames: {config.MeasureFrames}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"- Warmup: {config.WarmupFrames}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"- Render: {config.RenderWidth}x{config.RenderHeight}");
        builder.AppendLine();
        AppendLegend(builder);

        foreach (IGrouping<string, BenchmarkResult> group in results.GroupBy(result => result.Scenario))
        {
            AppendScenario(builder, group.Key, group);
        }

        AppendFailures(builder, results);

        return builder.ToString();
    }

    private static void AppendLegend(StringBuilder builder)
    {
        builder.AppendLine("## Legend");
        builder.AppendLine();
        builder.AppendLine("- `setup`: scenario creation, resource loading, ECS entity/component creation, and system init.");
        builder.AppendLine("- `mean/p95/worst`: measured frame time after warmup.");
        builder.AppendLine("- `alloc/f`: managed bytes allocated per measured frame.");
        builder.AppendLine("- `managed`: managed heap observed after measurement.");
        builder.AppendLine("- `working`: process working set observed after measurement.");
        builder.AppendLine("- `cpu`: process CPU time divided by wall time and logical CPU count.");
        builder.AppendLine("- `gc`: Gen0/Gen1/Gen2 collections during measured frames.");
        builder.AppendLine("- `counters`: scenario-specific work counters, printed below each timing table.");
        builder.AppendLine();
    }

    private static void AppendScenario(
        StringBuilder builder,
        string scenario,
        IEnumerable<BenchmarkResult> results)
    {
        BenchmarkResult[] ordered = results.OrderBy(result => result.EntityCount).ToArray();
        builder.AppendLine(CultureInfo.InvariantCulture, $"## {scenario}");
        if (!string.IsNullOrWhiteSpace(ordered[0].Description))
        {
            builder.AppendLine();
            builder.AppendLine(ordered[0].Description);
        }

        builder.AppendLine();
        builder.AppendLine("| entities | setup | mean | p95 | worst | fps | alloc/f | managed | working | cpu | gc |");
        builder.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");

        foreach (BenchmarkResult result in ordered)
        {
            if (result.Error is not null)
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"| {result.EntityCount:N0} | error | error | error | error | error | error | error | error | error | error |");
                continue;
            }

            builder.AppendLine(CultureInfo.InvariantCulture,
                $"| {result.EntityCount:N0} | {FormatMilliseconds(result.SetupMilliseconds)} / {FormatBytes(result.SetupAllocatedBytes)} | {FormatMilliseconds(result.MeanMilliseconds)} | {FormatMilliseconds(result.P95Milliseconds)} | {FormatMilliseconds(result.WorstMilliseconds)} | {result.FramesPerSecond:0.#} | {FormatBytes(result.AllocatedBytesPerFrame)} | {ToMiB(result.MeasureAfter.ManagedBytes):0.#} MB | {ToMiB(result.MeasureAfter.WorkingSetBytes):0.#} MB | {result.CpuPercentAllCores:0.#}% | {result.Gen0Collections}/{result.Gen1Collections}/{result.Gen2Collections} |");
        }

        AppendCounters(builder, ordered);
    }

    private static void AppendCounters(StringBuilder builder, IReadOnlyList<BenchmarkResult> results)
    {
        BenchmarkResult[] rows = results
            .Where(result => result.Error is null && result.Counters.Count > 0)
            .ToArray();
        if (rows.Length == 0)
        {
            builder.AppendLine();
            return;
        }

        builder.AppendLine();
        builder.AppendLine("Counters:");
        foreach (BenchmarkResult result in rows)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"- `{result.EntityCount:N0}`: {FormatCounters(result.Counters)}");
        }

        builder.AppendLine();
    }

    private static void AppendFailures(StringBuilder builder, IReadOnlyList<BenchmarkResult> results)
    {
        IReadOnlyList<BenchmarkResult> failures = results.Where(result => result.Error is not null).ToArray();
        if (failures.Count == 0)
        {
            return;
        }

        builder.AppendLine("## Failures");
        builder.AppendLine();
        foreach (BenchmarkResult failure in failures)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"### {failure.Scenario} / {failure.EntityCount:N0}");
            builder.AppendLine();
            if (failure.Error is not null)
            {
                builder.AppendLine("```text");
                builder.AppendLine(failure.Error.ToString());
                builder.AppendLine("```");
                builder.AppendLine();
            }
        }
    }

    private static string BuildCsv(IReadOnlyList<BenchmarkResult> results)
    {
        var builder = new StringBuilder();
        builder.AppendLine("scenario,entities,frames,setup_ms,setup_alloc_bytes,mean_ms,p95_ms,worst_ms,fps,alloc_bytes_per_frame,managed_mb,heap_mb,working_set_mb,private_mb,cpu_all_cores_percent,cpu_single_core_percent,gen0,gen1,gen2,counters,error");
        foreach (BenchmarkResult result in results)
        {
            builder.Append(Csv(result.Scenario)).Append(',');
            builder.Append(result.EntityCount).Append(',');
            builder.Append(result.Frames).Append(',');
            builder.Append(Invariant(result.SetupMilliseconds)).Append(',');
            builder.Append(result.SetupAllocatedBytes).Append(',');
            builder.Append(Invariant(result.MeanMilliseconds)).Append(',');
            builder.Append(Invariant(result.P95Milliseconds)).Append(',');
            builder.Append(Invariant(result.WorstMilliseconds)).Append(',');
            builder.Append(Invariant(result.FramesPerSecond)).Append(',');
            builder.Append(Invariant(result.AllocatedBytesPerFrame)).Append(',');
            builder.Append(Invariant(ToMiB(result.MeasureAfter.ManagedBytes))).Append(',');
            builder.Append(Invariant(ToMiB(result.MeasureAfter.HeapBytes))).Append(',');
            builder.Append(Invariant(ToMiB(result.MeasureAfter.WorkingSetBytes))).Append(',');
            builder.Append(Invariant(ToMiB(result.MeasureAfter.PrivateBytes))).Append(',');
            builder.Append(Invariant(result.CpuPercentAllCores)).Append(',');
            builder.Append(Invariant(result.CpuPercentSingleCore)).Append(',');
            builder.Append(result.Gen0Collections).Append(',');
            builder.Append(result.Gen1Collections).Append(',');
            builder.Append(result.Gen2Collections).Append(',');
            builder.Append(Csv(FormatCounters(result.Counters))).Append(',');
            builder.AppendLine(Csv(result.Error?.ToString() ?? string.Empty));
        }

        return builder.ToString();
    }

    private static string FormatCounters(IReadOnlyDictionary<string, double> counters)
    {
        KeyValuePair<string, double>[] visibleCounters = counters
            .Where(pair => Math.Abs(pair.Value) > double.Epsilon)
            .ToArray();
        if (visibleCounters.Length == 0)
        {
            return string.Empty;
        }

        return string.Join("; ", visibleCounters.Select(pair => string.Create(CultureInfo.InvariantCulture, $"{pair.Key}={pair.Value:0.###}")));
    }

    private static string FormatBytes(double bytes)
    {
        const double kib = 1024.0;
        const double mib = kib * 1024.0;

        if (bytes >= mib)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{bytes / mib:0.##} MB");
        }

        if (bytes >= kib)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{bytes / kib:0.##} KB");
        }

        return string.Create(CultureInfo.InvariantCulture, $"{bytes:0.#} B");
    }

    private static string FormatMilliseconds(double milliseconds)
    {
        return milliseconds == 0
            ? "0 ms"
            : string.Create(CultureInfo.InvariantCulture, $"{milliseconds:0.###} ms");
    }

    private static double ToMiB(long bytes)
    {
        return bytes / 1024.0 / 1024.0;
    }

    private static string Invariant(double value)
    {
        return value.ToString("0.########", CultureInfo.InvariantCulture);
    }

    private static string Csv(string value)
    {
        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static string Escape(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }
}
