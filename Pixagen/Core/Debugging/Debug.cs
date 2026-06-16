using System.Text;

namespace Pixagen.Core.Debugging;

public sealed class Debug : IDisposable
{
    private readonly object _sync = new();
    private readonly string _logFilePath;
    private StreamWriter? _fileWriter;
    private UnhandledExceptionEventHandler? _unhandledExceptionHandler;
    private EventHandler<UnobservedTaskExceptionEventArgs>? _unobservedTaskExceptionHandler;
    private bool _globalHandlersInstalled;
    private bool _disposed;

    public Debug(string? logFilePath = null)
    {
        _logFilePath = string.IsNullOrWhiteSpace(logFilePath)
            ? ResolveDefaultLogFilePath()
            : Path.GetFullPath(logFilePath);

        OpenLogFile();
    }

    public string LogFilePath => _logFilePath;

    public static Debug CreateDefault()
    {
        return new Debug();
    }

    public void InstallGlobalExceptionHandlers()
    {
        lock (_sync)
        {
            if (_globalHandlersInstalled)
            {
                return;
            }

            _unhandledExceptionHandler = (_, args) =>
            {
                if (args.ExceptionObject is Exception exception)
                {
                    Exception(exception, "Unhandled exception");
                    return;
                }

                Error($"Unhandled non-exception object: {args.ExceptionObject}");
            };

            _unobservedTaskExceptionHandler = (_, args) =>
            {
                Exception(args.Exception, "Unobserved task exception");
                args.SetObserved();
            };

            AppDomain.CurrentDomain.UnhandledException += _unhandledExceptionHandler;
            TaskScheduler.UnobservedTaskException += _unobservedTaskExceptionHandler;
            _globalHandlersInstalled = true;
        }
    }

    public void Log(object? data)
    {
        Write(DebugLogLevel.Log, data);
    }

    public void Warning(object? data)
    {
        Write(DebugLogLevel.Warning, data);
    }

    public void Error(object? data)
    {
        Write(DebugLogLevel.Error, data);
    }

    public void Exception(Exception exception)
    {
        Exception(exception, null);
    }

    public void Exception(Exception exception, object? data)
    {
        if (exception is null)
        {
            Error(data ?? "Exception was null.");
            return;
        }

        string prefix = data is null ? string.Empty : $"{data}{Environment.NewLine}";
        Write(DebugLogLevel.Exception, $"{prefix}{exception}");
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            UninstallGlobalExceptionHandlers();
            _fileWriter?.Dispose();
            _fileWriter = null;
        }
    }

    private static string ResolveDefaultLogFilePath()
    {
        return Path.Combine(AppContext.BaseDirectory, "Logs", "pixagen.log");
    }

    private void OpenLogFile()
    {
        try
        {
            string? directory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _fileWriter = new StreamWriter(
                new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
            {
                AutoFlush = true
            };
        }
        catch (Exception exception)
        {
            _fileWriter = null;
            Console.Error.WriteLine($"[Pixagen][Debug][Error] Could not open log file '{_logFilePath}': {exception}");
        }
    }

    private void Write(DebugLogLevel level, object? data)
    {
        string message = data?.ToString() ?? "<null>";
        string formattedMessage = Format(level, message);

        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            WriteConsole(level, formattedMessage);
            WriteFile(formattedMessage);
        }
    }

    private static string Format(DebugLogLevel level, string message)
    {
        return $"[{DateTimeOffset.Now:O}][{level}] {message}";
    }

    private static void WriteConsole(DebugLogLevel level, string message)
    {
        TextWriter output = level is DebugLogLevel.Warning or DebugLogLevel.Error or DebugLogLevel.Exception
            ? Console.Error
            : Console.Out;

        output.WriteLine(message);
    }

    private void WriteFile(string message)
    {
        if (_fileWriter is null)
        {
            return;
        }

        try
        {
            _fileWriter.WriteLine(message);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"[Pixagen][Debug][Error] Could not write log file '{_logFilePath}': {exception}");
            _fileWriter.Dispose();
            _fileWriter = null;
        }
    }

    private void UninstallGlobalExceptionHandlers()
    {
        if (!_globalHandlersInstalled)
        {
            return;
        }

        if (_unhandledExceptionHandler is not null)
        {
            AppDomain.CurrentDomain.UnhandledException -= _unhandledExceptionHandler;
            _unhandledExceptionHandler = null;
        }

        if (_unobservedTaskExceptionHandler is not null)
        {
            TaskScheduler.UnobservedTaskException -= _unobservedTaskExceptionHandler;
            _unobservedTaskExceptionHandler = null;
        }

        _globalHandlersInstalled = false;
    }
}
