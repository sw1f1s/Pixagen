using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using Pixagen.Ecs.DI;
using Pixagen.Game.Features.RenderFeature;
using Pixagen.Game.Features.ResourceFeature.Runtime;
using Pixagen.Rendering;

namespace Pixagen.Game.Features.DebugFeature.Systems;

public sealed class StartupLogSystem : IInitSystem
{
    private readonly CustomInject<Debug> _debug = default;
    private readonly CustomInject<EngineOptions> _options = default;
    private readonly CustomInject<RenderBackendOptions> _backendOptions = default;
    private readonly CustomInject<RenderSettings> _renderSettings = default;
    private readonly CustomInject<IRenderBackend> _renderBackend = default;
    private readonly CustomInject<ResourceManager> _resources = default;

    public void Init()
    {
        Debug debug = _debug.Value;
        EngineOptions options = _options.Value;
        RenderBackendOptions backendOptions = _backendOptions.Value;
        RenderSettings renderSettings = _renderSettings.Value;

        debug.Log("Made with Pixagen");
        debug.Log($"Pixagen version: {ResolvePixagenVersion()} ({BuildConfiguration})");
        debug.Log($"Log file: {debug.LogFilePath}");
        debug.Log($"Device: {Environment.MachineName}");
        debug.Log($"OS: {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
        debug.Log($"Runtime: {RuntimeInformation.FrameworkDescription}; CLR {Environment.Version}");
        debug.Log($"Process: {RuntimeInformation.ProcessArchitecture}; 64-bit process: {Environment.Is64BitProcess}");
        debug.Log($"CPU: {Environment.ProcessorCount} logical processors");
        debug.Log($"GC: {(GCSettings.IsServerGC ? "Server" : "Workstation")}; latency {GCSettings.LatencyMode}");
        debug.Log($"Memory: working set {FormatBytes(Environment.WorkingSet)}; GC available {FormatBytes(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes)}");
        debug.Log($"Render backend: {_renderBackend.Value.GetType().FullName}");
        debug.Log($"Window: {backendOptions.WindowWidth}x{backendOptions.WindowHeight}; fullscreen {backendOptions.Fullscreen}; cell {backendOptions.CellPixelSize}px");
        debug.Log($"Input: capture mouse {backendOptions.CaptureMouse}; show cursor {backendOptions.ShowCursor}");
        debug.Log($"App: target FPS {options.TargetFps}; auto resize {options.AutoResize}; run single frame {options.RunSingleFrame}");
        debug.Log($"Scene: {ResolveSceneLogValue(options)}");
        debug.Log($"Render: max internal {renderSettings.MaxInternalResolution.Width}x{renderSettings.MaxInternalResolution.Height}; scale {renderSettings.RenderScaleMode}; shadows {renderSettings.ShadowQuality}; softness {renderSettings.ShadowSoftness}");
        debug.Log($"Distances: draw {renderSettings.DrawDistance}; shadows {renderSettings.ShadowRenderDistance}");
    }

    private static string ResolvePixagenVersion()
    {
        Assembly assembly = typeof(EngineOptions).Assembly;
        string? informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return informationalVersion;
        }

        return assembly.GetName().Version?.ToString() ?? "unknown";
    }

    private string ResolveSceneLogValue(EngineOptions options)
    {
        string? path = _resources.Value.ResolveStartupScenePath(options.ScenePath);
        return path ?? "<generated default>";
    }

    private static string FormatBytes(long bytes)
    {
        const double kib = 1024.0;
        const double mib = kib * 1024.0;
        const double gib = mib * 1024.0;

        return Math.Abs(bytes) switch
        {
            >= (long)gib => $"{bytes / gib:0.##} GiB",
            >= (long)mib => $"{bytes / mib:0.##} MiB",
            >= (long)kib => $"{bytes / kib:0.##} KiB",
            _ => $"{bytes} B"
        };
    }

#if DEBUG
    private const string BuildConfiguration = "Debug";
#else
    private const string BuildConfiguration = "Release";
#endif
}
