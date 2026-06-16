using Pixagen.Core.Debugging;
using Pixagen.Ecs.Runtime;
using Pixagen.Tests.TestSupport;

namespace Pixagen.Tests.Ecs;

public sealed class SystemRuntimeExceptionTests
{
    [Fact]
    public void UpdateSystemException_IsLoggedAndNextSystemStillRuns()
    {
        string logFilePath = Path.Combine(Path.GetTempPath(), $"pixagen-runtime-{Guid.NewGuid():N}.log");
        var nextSystem = new CountingUpdateSystem();

        try
        {
            using var debug = new Debug(logFilePath);
            using var context = new EcsTestContext();
            using Systems systems = context.BuildSystems(
                new ThrowingUpdateSystem(),
                nextSystem);
            systems.SystemException += systemException => debug.Exception(
                systemException.Exception,
                $"System runtime exception in {systemException.System.GetType().FullName}.{systemException.Stage}. Execution will continue.");

            systems.Update();

            Assert.Equal(1, nextSystem.UpdateCount);

            string log = File.ReadAllText(logFilePath);
            Assert.Contains("System runtime exception", log);
            Assert.Contains(nameof(ThrowingUpdateSystem), log);
            Assert.Contains("Execution will continue", log);
            Assert.Contains("runtime boom", log);
        }
        finally
        {
            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }
        }
    }

    private sealed class ThrowingUpdateSystem : IUpdateSystem
    {
        public void Update()
        {
            throw new InvalidOperationException("runtime boom");
        }
    }

    private sealed class CountingUpdateSystem : IUpdateSystem
    {
        public int UpdateCount { get; private set; }

        public void Update()
        {
            UpdateCount++;
        }
    }
}
