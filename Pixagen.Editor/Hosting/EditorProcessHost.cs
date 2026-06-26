using System.Diagnostics;

namespace Pixagen.Editor.Hosting;

public abstract class EditorProcessHost : IDisposable
{
    private readonly List<string> _tempFiles = new();
    private Process? _process;
    private bool _disposed;

    public bool IsRunning => _process is { HasExited: false };
    public string LastError { get; private set; } = string.Empty;

    public void Stop()
    {
        if (_process is null)
        {
            return;
        }

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(2000);
            }
        }
        catch
        {
            // External preview/game windows are best-effort child processes.
        }
        finally
        {
            _process.Dispose();
            _process = null;
            DeleteTempFiles();
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
        GC.SuppressFinalize(this);
    }

    protected bool Restart(ProcessStartInfo startInfo, params string[] tempFiles)
    {
        Stop();
        LastError = string.Empty;
        foreach (string tempFile in tempFiles)
        {
            RegisterTempFile(tempFile);
        }

        startInfo.UseShellExecute = false;
        startInfo.WorkingDirectory = RepositoryRootResolver.Resolve();
        try
        {
            _process = Process.Start(startInfo);
            if (_process is null)
            {
                LastError = "Process.Start returned null.";
                DeleteTempFiles();
                return false;
            }

            Thread.Sleep(250);
            if (_process.HasExited)
            {
                LastError = $"Process exited immediately with code {_process.ExitCode}.";
                _process.Dispose();
                _process = null;
                DeleteTempFiles();
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            _process?.Dispose();
            _process = null;
            DeleteTempFiles();
            return false;
        }
    }

    private void RegisterTempFile(string path)
    {
        if (!string.IsNullOrWhiteSpace(path) && !_tempFiles.Contains(path, StringComparer.OrdinalIgnoreCase))
        {
            _tempFiles.Add(path);
        }
    }

    protected static string Quote(string value)
    {
        return "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }

    private void DeleteTempFiles()
    {
        foreach (string path in _tempFiles.ToArray())
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Temp cleanup is best effort; stale files should not crash editor shutdown.
            }
        }

        _tempFiles.Clear();
    }
}
