namespace Pixagen.Core.Performance;

public readonly record struct RenderPerformanceReport(
    int Triangles,
    int ShadowTriangles,
    int TextureCount,
    long TextureBytes);

public readonly record struct BackendPerformanceReport(
    int DrawCalls,
    long VramBytes);

public readonly record struct MemoryPerformanceReport(
    long ManagedBytes,
    long WorkingSetBytes,
    long PrivateBytes,
    long GcHeapBytes);
