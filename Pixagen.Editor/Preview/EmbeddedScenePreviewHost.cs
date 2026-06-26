using Pixagen.Editor.Workspace;
using System.Diagnostics;

namespace Pixagen.Editor.Preview;

public sealed class EmbeddedScenePreviewHost : IDisposable
{
    private const int TargetFps = 60;
    private readonly string _overlayPath = Path.Combine(
        Path.GetTempPath(),
        $"pixagen-editor-overlay-{Guid.NewGuid():N}.json");
    private readonly object _gate = new();

    private Thread? _thread;
    private CancellationTokenSource? _stopSource;
    private EditorPreviewApp? _app;
    private string? _scenePath;
    private bool _disposed;

    public bool IsRunning
    {
        get
        {
            lock (_gate)
            {
                return _thread is { IsAlive: true };
            }
        }
    }

    public string LastError { get; private set; } = string.Empty;
    public event Action<string>? StatusChanged;

    public bool StartOrRestart(
        EditorWorkspace workspace,
        IntPtr nativeWindowHandle,
        string? handleDescriptor,
        int width,
        int height)
    {
        Stop();
        LastError = string.Empty;

        try
        {
            _scenePath = workspace.SavePreviewSceneSnapshot(updateStatus: false);
            UpdateOverlay(workspace);

            var stopSource = new CancellationTokenSource();
            string scenePath = _scenePath;
            var thread = new Thread(() => RunPreview(
                scenePath,
                nativeWindowHandle,
                handleDescriptor,
                width,
                height,
                stopSource.Token))
            {
                IsBackground = true,
                Name = "Pixagen.Editor.SceneViewport"
            };

            lock (_gate)
            {
                if (_disposed)
                {
                    stopSource.Dispose();
                    DeleteFile(_scenePath);
                    _scenePath = null;
                    LastError = "Scene preview host is disposed.";
                    return false;
                }

                _stopSource = stopSource;
                _thread = thread;
            }

            thread.Start();
            NotifyStatus("Scene viewport: starting embedded Vulkan");
            return true;
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            Stop();
            return false;
        }
    }

    public void UpdateOverlay(EditorWorkspace workspace)
    {
        try
        {
            workspace.CreateOverlayState().Save(_overlayPath);
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
        }
    }

    public bool Tick()
    {
        return IsRunning;
    }

    public void ResizeViewport(int width, int height)
    {
        EditorPreviewApp? app = GetApp();
        app?.ResizeViewport(width, height);
    }

    public void SetKey(InputKey key, bool isDown)
    {
        EditorPreviewApp? app = GetApp();
        app?.SetKey(key, isDown);
    }

    public void SetMousePosition(float x, float y)
    {
        EditorPreviewApp? app = GetApp();
        app?.SetMousePosition(x, y);
    }

    public void SetMouseButton(InputMouseButton button, bool isDown)
    {
        EditorPreviewApp? app = GetApp();
        app?.SetMouseButton(button, isDown);
    }

    public void AddMouseDelta(float deltaX, float deltaY)
    {
        EditorPreviewApp? app = GetApp();
        app?.AddMouseDelta(deltaX, deltaY);
    }

    public void AddMouseWheelDelta(float delta)
    {
        EditorPreviewApp? app = GetApp();
        app?.AddMouseWheelDelta(delta);
    }

    public void Stop()
    {
        Thread? thread;
        CancellationTokenSource? stopSource;
        lock (_gate)
        {
            thread = _thread;
            stopSource = _stopSource;
        }

        stopSource?.Cancel();
        if (thread is not null && thread != Thread.CurrentThread && !thread.Join(TimeSpan.FromSeconds(2)))
        {
            LastError = "Scene render thread did not stop in time.";
            return;
        }

        lock (_gate)
        {
            if (thread is null || ReferenceEquals(_thread, thread))
            {
                _thread = null;
                _stopSource?.Dispose();
                _stopSource = null;
                DeleteFile(_scenePath);
                _scenePath = null;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
        DeleteFile(_overlayPath);
        GC.SuppressFinalize(this);
    }

    private static void DeleteFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Temp cleanup should never interrupt editor shutdown.
        }
    }

    private void RunPreview(
        string scenePath,
        IntPtr nativeWindowHandle,
        string? handleDescriptor,
        int width,
        int height,
        CancellationToken stopToken)
    {
        EditorPreviewApp? app = null;
        try
        {
            app = EditorPreviewApp.CreateEmbedded(scenePath, _overlayPath, width, height);
            lock (_gate)
            {
                _app = app;
            }

            app.InitializeFromNativeWindow(nativeWindowHandle, handleDescriptor);
            NotifyStatus("Scene viewport: running embedded Vulkan");

            while (!stopToken.IsCancellationRequested)
            {
                long frameStart = Stopwatch.GetTimestamp();
                if (!app.Tick())
                {
                    LastError = app.LastError;
                    NotifyStatus(string.IsNullOrWhiteSpace(LastError)
                        ? "Scene viewport: stopped"
                        : $"Scene viewport: failed ({LastError})");
                    break;
                }

                SleepToTargetFrameTime(frameStart, stopToken);
            }
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            NotifyStatus($"Scene viewport: failed ({exception.Message})");
        }
        finally
        {
            try
            {
                app?.Dispose();
            }
            catch (Exception exception)
            {
                LastError = exception.Message;
            }

            lock (_gate)
            {
                if (Thread.CurrentThread == _thread)
                {
                    _app = null;
                    _thread = null;
                    _stopSource?.Dispose();
                    _stopSource = null;
                    DeleteFile(_scenePath);
                    _scenePath = null;
                }
            }
        }
    }

    private static void SleepToTargetFrameTime(long frameStart, CancellationToken stopToken)
    {
        long targetTicks = Stopwatch.Frequency / TargetFps;
        long targetTimestamp = frameStart + targetTicks;
        long remainingTicks = targetTimestamp - Stopwatch.GetTimestamp();
        if (remainingTicks <= 0)
        {
            return;
        }

        int sleepMs = (int)Math.Max(0, remainingTicks * 1000 / Stopwatch.Frequency);
        if (sleepMs > 0)
        {
            stopToken.WaitHandle.WaitOne(sleepMs);
        }
    }

    private void NotifyStatus(string value)
    {
        StatusChanged?.Invoke(value);
    }

    private EditorPreviewApp? GetApp()
    {
        lock (_gate)
        {
            return _app;
        }
    }
}
