using System.Globalization;

namespace Pixagen.Benchmark;

public sealed class BenchmarkConfig
{
    public static readonly int[] DefaultEntityCounts = [10, 100, 1_000, 10_000, 100_000];

    public int[] EntityCounts { get; init; } = DefaultEntityCounts;
    public int WarmupFrames { get; init; } = 4;
    public int MeasureFrames { get; init; } = 16;
    public int RenderWidth { get; init; } = 160;
    public int RenderHeight { get; init; } = 90;
    public string[] ScenarioNames { get; init; } = [];
    public string? CsvPath { get; init; }
    public string? MarkdownPath { get; init; }
    public bool ListScenarios { get; init; }
    public bool ShowHelp { get; init; }
    public bool ForceFullCollection { get; init; } = true;

    public static BenchmarkConfig Parse(string[] args)
    {
        var config = new MutableConfig();
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            string name = arg;
            string? value = null;
            int equalsIndex = arg.IndexOf('=');
            if (equalsIndex >= 0)
            {
                name = arg[..equalsIndex];
                value = arg[(equalsIndex + 1)..];
            }

            switch (NormalizeName(name))
            {
                case "h":
                case "help":
                    config.ShowHelp = true;
                    break;

                case "list":
                case "listscenarios":
                    config.ListScenarios = true;
                    break;

                case "quick":
                    config.EntityCounts = [10, 100, 1_000];
                    config.WarmupFrames = 2;
                    config.MeasureFrames = 6;
                    break;

                case "entities":
                case "counts":
                    config.EntityCounts = ParseIntList(ReadValue(args, ref i, value, name), name);
                    break;

                case "scenario":
                case "scenarios":
                    config.ScenarioNames = ParseStringList(ReadValue(args, ref i, value, name));
                    break;

                case "warmup":
                case "warmupframes":
                    config.WarmupFrames = ParsePositiveInt(ReadValue(args, ref i, value, name), name, allowZero: true);
                    break;

                case "frames":
                case "measureframes":
                case "iterations":
                    config.MeasureFrames = ParsePositiveInt(ReadValue(args, ref i, value, name), name, allowZero: false);
                    break;

                case "rendersize":
                case "render":
                case "resolution":
                    (config.RenderWidth, config.RenderHeight) = ParseResolution(ReadValue(args, ref i, value, name), name);
                    break;

                case "csv":
                    config.CsvPath = ReadValue(args, ref i, value, name);
                    break;

                case "markdown":
                case "md":
                    config.MarkdownPath = ReadValue(args, ref i, value, name);
                    break;

                case "nogc":
                    config.ForceFullCollection = false;
                    break;

                default:
                    throw new ArgumentException($"Unknown benchmark option '{name}'.");
            }
        }

        return config.ToImmutable();
    }

    public static void PrintHelp()
    {
        Console.WriteLine("""
Pixagen.Benchmark

Usage:
  dotnet run -c Release --project Pixagen.Benchmark -- [options]

Options:
  --entities 10,100,1000,10000,100000   Entity counts to run.
  --scenarios ecs.storage,render.dynamic Comma-separated scenario names.
  --frames 16                           Measured frames per scenario.
  --warmup 4                            Warmup frames before measurement.
  --render-size 160x90                  Headless render resolution.
  --quick                               Run 10/100/1000 with fewer frames.
  --list                                Print available scenarios.
  --csv results.csv                     Write CSV report.
  --markdown results.md                 Write Markdown report.
  --no-gc                               Do not force full GC around phases.
""");
    }

    private static string NormalizeName(string name)
    {
        return name.TrimStart('-').Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
    }

    private static string ReadValue(string[] args, ref int index, string? inlineValue, string name)
    {
        if (!string.IsNullOrWhiteSpace(inlineValue))
        {
            return inlineValue;
        }

        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Option '{name}' requires a value.");
        }

        return args[++index];
    }

    private static int ParsePositiveInt(string value, string name, bool allowZero)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ||
            result < 0 ||
            (!allowZero && result == 0))
        {
            throw new ArgumentException($"Option '{name}' expects a positive integer.");
        }

        return result;
    }

    private static int[] ParseIntList(string value, string name)
    {
        int[] counts = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => ParsePositiveInt(item, name, allowZero: false))
            .Distinct()
            .Order()
            .ToArray();

        if (counts.Length == 0)
        {
            throw new ArgumentException($"Option '{name}' expects at least one entity count.");
        }

        return counts;
    }

    private static string[] ParseStringList(string value)
    {
        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static (int Width, int Height) ParseResolution(string value, string name)
    {
        string[] parts = value.Split('x', 'X');
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int width) ||
            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int height) ||
            width <= 0 ||
            height <= 0)
        {
            throw new ArgumentException($"Option '{name}' expects WIDTHxHEIGHT.");
        }

        return (width, height);
    }

    private sealed class MutableConfig
    {
        public int[] EntityCounts { get; set; } = DefaultEntityCounts;
        public int WarmupFrames { get; set; } = 4;
        public int MeasureFrames { get; set; } = 16;
        public int RenderWidth { get; set; } = 160;
        public int RenderHeight { get; set; } = 90;
        public string[] ScenarioNames { get; set; } = [];
        public string? CsvPath { get; set; }
        public string? MarkdownPath { get; set; }
        public bool ListScenarios { get; set; }
        public bool ShowHelp { get; set; }
        public bool ForceFullCollection { get; set; } = true;

        public BenchmarkConfig ToImmutable()
        {
            return new BenchmarkConfig
            {
                EntityCounts = EntityCounts,
                WarmupFrames = WarmupFrames,
                MeasureFrames = MeasureFrames,
                RenderWidth = RenderWidth,
                RenderHeight = RenderHeight,
                ScenarioNames = ScenarioNames,
                CsvPath = CsvPath,
                MarkdownPath = MarkdownPath,
                ListScenarios = ListScenarios,
                ShowHelp = ShowHelp,
                ForceFullCollection = ForceFullCollection
            };
        }
    }
}
