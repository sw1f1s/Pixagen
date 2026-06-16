using Pixagen.Core.Debugging;

namespace Pixagen.Tests.Core.Debugging;

public sealed class DebugTests
{
    [Fact]
    public void WritesMessagesToLogFile()
    {
        string logFilePath = Path.Combine(Path.GetTempPath(), $"pixagen-debug-{Guid.NewGuid():N}.log");

        try
        {
            using (var debug = new Debug(logFilePath))
            {
                debug.Log("hello");
                debug.Warning("careful");
                debug.Error("broken");
                debug.Exception(new InvalidOperationException("boom"), "context");
            }

            string log = File.ReadAllText(logFilePath);
            Assert.Contains("[Log] hello", log);
            Assert.Contains("[Warning] careful", log);
            Assert.Contains("[Error] broken", log);
            Assert.Contains("[Exception] context", log);
            Assert.Contains("System.InvalidOperationException: boom", log);
        }
        finally
        {
            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }
        }
    }
}
