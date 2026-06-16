namespace Pixagen.Tests.Features.PerformanceFeature;

public sealed class PerformanceStatsTests
{
    [Fact]
    public void BeginFrame_Dispose_RecordsCpuFrameAndMemory()
    {
        var stats = new PerformanceStats(TimeSpan.Zero);

        using (stats.BeginFrame())
        {
            Thread.SpinWait(10_000);
        }

        PerformanceSnapshot snapshot = stats.Snapshot;
        Assert.True(snapshot.CpuFrameMilliseconds >= 0);
        Assert.Equal(1UL, snapshot.FrameCount);
        Assert.True(snapshot.FramesPerSecond >= 0);
        Assert.True(snapshot.InstantFramesPerSecond >= 0);
        Assert.True(snapshot.WorkingSetBytes > 0);
        Assert.True(snapshot.PrivateMemoryBytes >= 0);
        Assert.True(snapshot.ManagedMemoryBytes >= 0);
        Assert.True(snapshot.GcHeapBytes >= 0);
    }

    [Fact]
    public void Reports_NormalizeNegativeValues()
    {
        var stats = new PerformanceStats(TimeSpan.Zero);

        stats.RecordCpuFrame(-1);
        stats.RecordGpuFrame(-1);
        stats.RecordRenderScene(new RenderPerformanceReport(-1, -2, -3, -4));
        stats.RecordBackendFrame(new BackendPerformanceReport(-5, -6));
        stats.RecordMemory(new MemoryPerformanceReport(-7, -8, -9, -10));

        PerformanceSnapshot snapshot = stats.Snapshot;
        Assert.Equal(0, snapshot.CpuFrameMilliseconds);
        Assert.Equal(0, snapshot.GpuFrameMilliseconds);
        Assert.Equal(0, snapshot.FramesPerSecond);
        Assert.Equal(0, snapshot.InstantFramesPerSecond);
        Assert.Equal(0UL, snapshot.FrameCount);
        Assert.Equal(0, snapshot.Triangles);
        Assert.Equal(0, snapshot.ShadowTriangles);
        Assert.Equal(0, snapshot.TextureCount);
        Assert.Equal(0, snapshot.TextureBytes);
        Assert.Equal(0, snapshot.DrawCalls);
        Assert.Equal(0, snapshot.VramBytes);
        Assert.Equal(0, snapshot.ManagedMemoryBytes);
        Assert.Equal(0, snapshot.WorkingSetBytes);
        Assert.Equal(0, snapshot.PrivateMemoryBytes);
        Assert.Equal(0, snapshot.GcHeapBytes);
    }

    [Fact]
    public void Reports_UpdateIndependentSnapshotSections()
    {
        var stats = new PerformanceStats(TimeSpan.Zero);

        stats.RecordFrame(1.25);
        stats.RecordGpuFrame(2.5);
        stats.RecordRenderScene(new RenderPerformanceReport(10, 20, 3, 400));
        stats.RecordBackendFrame(new BackendPerformanceReport(4, 512));
        stats.RecordMemory(new MemoryPerformanceReport(1024, 2048, 4096, 8192));

        PerformanceSnapshot snapshot = stats.Snapshot;
        Assert.Equal(1.25, snapshot.CpuFrameMilliseconds);
        Assert.Equal(2.5, snapshot.GpuFrameMilliseconds);
        Assert.Equal(800, snapshot.FramesPerSecond, precision: 3);
        Assert.Equal(800, snapshot.InstantFramesPerSecond, precision: 3);
        Assert.Equal(1UL, snapshot.FrameCount);
        Assert.Equal(10, snapshot.Triangles);
        Assert.Equal(20, snapshot.ShadowTriangles);
        Assert.Equal(3, snapshot.TextureCount);
        Assert.Equal(400, snapshot.TextureBytes);
        Assert.Equal(4, snapshot.DrawCalls);
        Assert.Equal(512, snapshot.VramBytes);
        Assert.Equal(1024, snapshot.ManagedMemoryBytes);
        Assert.Equal(2048, snapshot.WorkingSetBytes);
        Assert.Equal(4096, snapshot.PrivateMemoryBytes);
        Assert.Equal(8192, snapshot.GcHeapBytes);
    }

    [Fact]
    public void EndFrame_CalculatesFramesPerSecond()
    {
        var stats = new PerformanceStats(TimeSpan.Zero, TimeSpan.Zero);

        stats.EndFrame(PerformanceStats.Timestamp - System.Diagnostics.Stopwatch.Frequency);

        PerformanceSnapshot snapshot = stats.Snapshot;
        Assert.Equal(1UL, snapshot.FrameCount);
        Assert.InRange(snapshot.CpuFrameMilliseconds, 900, 1100);
        Assert.InRange(snapshot.FramesPerSecond, 0.9, 1.1);
        Assert.InRange(snapshot.InstantFramesPerSecond, 0.9, 1.1);
    }
}
