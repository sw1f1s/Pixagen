namespace Pixagen.Core.Performance;

public readonly record struct PerformanceSnapshot(
    double CpuFrameMilliseconds,
    double GpuFrameMilliseconds,
    double FramesPerSecond,
    double InstantFramesPerSecond,
    ulong FrameCount,
    int DrawCalls,
    int Triangles,
    int ShadowTriangles,
    int TextureCount,
    long TextureBytes,
    long VramBytes,
    long ManagedMemoryBytes,
    long WorkingSetBytes,
    long PrivateMemoryBytes,
    long GcHeapBytes)
{
    public static PerformanceSnapshot Empty => new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}
